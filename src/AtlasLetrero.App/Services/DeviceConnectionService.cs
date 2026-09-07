using System.IO.Ports;
using System.Buffers.Binary;
using System.Text.Json.Nodes;
using AtlasLetrero.App.Protocol;
using System.Security.Cryptography;
using System.Text;
namespace AtlasLetrero.App.Services;
public interface ISerialTransport : IDisposable
{
    bool IsOpen { get; }
    string Name { get; }
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}

public sealed class SerialPortTransport : ISerialTransport
{
    private readonly SerialPort port;
    public SerialPortTransport(SerialPort port) => this.port = port;
    public bool IsOpen => port.IsOpen;
    public string Name => port.PortName;
    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => port.BaseStream.WriteAsync(buffer, cancellationToken).AsTask();
    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => port.BaseStream.ReadAsync(buffer, cancellationToken).AsTask();
    public void Dispose() => port.Dispose();
}

public sealed class DeviceConnectionService : IDisposable
{
    private ISerialTransport? port;
    private readonly Func<string, ISerialTransport>? transportFactory;
    private bool virtualConnected;
    private ushort sequence;
    public JsonObject? Capabilities {get;private set;}
    public object Status => new { connected=virtualConnected || port?.IsOpen==true && Capabilities is not null, @virtual=virtualConnected, physical=!virtualConnected && port?.IsOpen==true && Capabilities is not null, port=virtualConnected ? "AtlasLED Virtual" : port?.Name, capabilities=Capabilities };
    public DeviceConnectionService() { }
    public DeviceConnectionService(Func<string, ISerialTransport> transportFactory) => this.transportFactory = transportFactory;
    public string[] Ports() => SerialPort.GetPortNames().Order().ToArray();
    public object ConnectVirtual()
    {
        lock(gate) { Disconnect(); virtualConnected=true; Capabilities=(JsonObject)JsonNode.Parse("{\"firmware\":\"AtlasLED Virtual 0.1\",\"protocol\":1,\"width\":256,\"height\":256}")!; return Status; }
    }
    private readonly object gate=new();
    public object Connect(string name)
    {
        lock(gate)
        {
            Disconnect();
            if (transportFactory is null && !Ports().Contains(name)) throw new ArgumentException("Puerto no disponible.");
            try
            {
                if (transportFactory is not null) port=transportFactory(name);
                else { var serial=new SerialPort(name,115200){ReadTimeout=1800,WriteTimeout=1800,DtrEnable=false,RtsEnable=false}; serial.Open(); port=new SerialPortTransport(serial); }
                var hello=Exchange(AtlasLedCommand.Hello,[]);
                if(hello.Command!=AtlasLedCommand.HelloAck)throw new InvalidDataException("El puerto no respondió como AtlasLED.");
                var caps=Exchange(AtlasLedCommand.GetCapabilities,[]);
                if(caps.Command!=AtlasLedCommand.Capabilities)throw new InvalidDataException("No se recibieron capacidades.");
                Capabilities=JsonNode.Parse(caps.Payload)?.AsObject()??throw new InvalidDataException();
                if(Capabilities["protocol"]?.GetValue<int>()!=1 || Capabilities["width"]?.GetValue<int>() is not >0 || Capabilities["height"]?.GetValue<int>() is not >0)throw new InvalidDataException();
                return Status;
            }
            catch {Disconnect();throw;}
        }
    }
    public AtlasLedPacket Exchange(AtlasLedCommand command,byte[] payload, CancellationToken cancellationToken = default)
    {
        lock(gate)
        {
            if(port?.IsOpen!=true)throw new InvalidOperationException("No hay dispositivo conectado.");
            var seq=++sequence;var bytes=PacketCodec.Encode(new(command,seq,payload));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(1800));
            timeout.Token.ThrowIfCancellationRequested(); port.WriteAsync(bytes, timeout.Token).GetAwaiter().GetResult();
            var header=new byte[12];ReadExact(header,timeout.Token);var size=BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8));
            if(size>4*1024*1024)throw new InvalidDataException();
            var response=new byte[16+(int)size];header.CopyTo(response,0);var rest=new byte[size+4];ReadExact(rest,timeout.Token);rest.CopyTo(response,12);
            var packet=PacketCodec.Decode(response);if(packet.Sequence!=seq)throw new InvalidDataException("Secuencia incorrecta.");return packet;
        }
    }
    public const int PhysicalPayloadLimit = 8 * 1024 * 1024;
    public const int PhysicalRequestLimit = PhysicalPayloadLimit + 16 * 1024;
    private sealed record Candidate(string Id, string Checksum);
    private Candidate? verifiedCandidate;
    public string? CandidateId => verifiedCandidate?.Id;
    public string Upload(JsonObject packet, string expectedChecksum, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            verifiedCandidate = null;
            try {
                if (virtualConnected || port?.IsOpen != true) throw new InvalidOperationException("No hay un ESP32 AtlasLED conectado.");
                var bytes = Encoding.UTF8.GetBytes(packet.ToJsonString());
                if (bytes.Length > PhysicalPayloadLimit) throw new InvalidDataException("El paquete supera el límite físico de 8 MB.");
                ValidateCapabilities(packet);
                var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                if (!String.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("El checksum del paquete físico no coincide.");
                SendAck(AtlasLedCommand.BeginUpload, Encoding.UTF8.GetBytes($"{{\"length\":{bytes.Length},\"sha256\":\"{checksum}\"}}"), cancellationToken);
                const int chunk = 60000;
                for (var offset = 0; offset < bytes.Length; offset += chunk) { cancellationToken.ThrowIfCancellationRequested(); SendAck(AtlasLedCommand.FrameData, bytes.AsSpan(offset, Math.Min(chunk, bytes.Length - offset)).ToArray(), cancellationToken); }
                SendAck(AtlasLedCommand.Verify, Encoding.UTF8.GetBytes(checksum), cancellationToken);
                verifiedCandidate = new(Guid.NewGuid().ToString("N"), checksum);
                return checksum;
            } catch { verifiedCandidate = null; throw; }
        }
    }
    public string Activate(string candidate, string requestedChecksum, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (verifiedCandidate is null || !String.Equals(verifiedCandidate.Id, candidate, StringComparison.OrdinalIgnoreCase) || !String.Equals(verifiedCandidate.Checksum, requestedChecksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("No hay candidato verificado.");
            try { SendAck(AtlasLedCommand.Activate, Encoding.UTF8.GetBytes(verifiedCandidate.Checksum), cancellationToken); var checksum=verifiedCandidate.Checksum; verifiedCandidate = null; return checksum; }
            catch { verifiedCandidate = null; throw; }
        }
    }
    private void ValidateCapabilities(JsonObject packet)
    {
        try {
            if (Capabilities is null || packet["version"]?.GetValue<int>() != Capabilities["protocol"]?.GetValue<int>() || packet["width"]?.GetValue<int>() is not int w || packet["height"]?.GetValue<int>() is not int h || w < 1 || h < 1 || w > Capabilities["width"]!.GetValue<int>() || h > Capabilities["height"]!.GetValue<int>()) throw new InvalidDataException();
            var fps=packet["fps"]?.GetValue<int>() ?? 0; var duration=packet["durationMs"]?.GetValue<int>() ?? 0; var expectedFrames=duration>0&&fps>0?((long)duration*fps+999)/1000:0;
            if ((long)w*h*4 > PhysicalPayloadLimit || fps is < 1 or > 60 || duration <= 0 || expectedFrames > 256 || packet["frames"] is not JsonArray frames || frames.Count != expectedFrames || frames.Any(f => f is not JsonArray a || a.Count != (long)w*h*4 || a.Any(v => v?.GetValue<int>() is < 0 or > 255))) throw new InvalidDataException();
        } catch (Exception ex) when (ex is InvalidOperationException or FormatException or NullReferenceException) { throw new InvalidDataException("El paquete físico contiene campos inválidos.", ex); }
        catch (InvalidDataException) { throw new InvalidDataException("El paquete excede el protocolo o las capacidades del AtlasLED."); }
    }
    private void SendAck(AtlasLedCommand command, byte[] payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = Exchange(command, payload, cancellationToken);
        if (response.Command is AtlasLedCommand.Nack or AtlasLedCommand.Error || response.Command != AtlasLedCommand.Ack) throw new InvalidDataException($"El dispositivo rechazó {command}.");
    }
    private void ReadExact(byte[] buffer, CancellationToken cancellationToken){var offset=0;while(offset<buffer.Length){cancellationToken.ThrowIfCancellationRequested();var n=port!.ReadAsync(buffer.AsMemory(offset), cancellationToken).GetAwaiter().GetResult();if(n<=0)throw new TimeoutException();offset+=n;}}
    public void Disconnect(){lock(gate){verifiedCandidate=null;port?.Dispose();port=null;virtualConnected=false;Capabilities=null;}}
    public void Dispose()=>Disconnect();
}
