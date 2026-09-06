using System.IO.Ports;
using System.Buffers.Binary;
using System.Text.Json.Nodes;
using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Services;
public sealed class DeviceConnectionService : IDisposable
{
    private SerialPort? port;
    private ushort sequence;
    public JsonObject? Capabilities {get;private set;}
    public object Status => new { connected=port?.IsOpen==true && Capabilities is not null, port=port?.PortName, capabilities=Capabilities };
    public string[] Ports() => SerialPort.GetPortNames().Order().ToArray();
    private readonly object gate=new();
    public object Connect(string name)
    {
        lock(gate)
        {
            Disconnect();
            if(!Ports().Contains(name))throw new ArgumentException("Puerto no disponible.");
            try
            {
                port=new SerialPort(name,115200){ReadTimeout=1800,WriteTimeout=1800,DtrEnable=false,RtsEnable=false};port.Open();
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
    public AtlasLedPacket Exchange(AtlasLedCommand command,byte[] payload)
    {
        lock(gate)
        {
            if(port?.IsOpen!=true)throw new InvalidOperationException("No hay dispositivo conectado.");
            var seq=++sequence;var bytes=PacketCodec.Encode(new(command,seq,payload));
            port.Write(bytes,0,bytes.Length);
            var header=new byte[12];ReadExact(header);var size=BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8));
            if(size>4*1024*1024)throw new InvalidDataException();
            var response=new byte[16+(int)size];header.CopyTo(response,0);var rest=new byte[size+4];ReadExact(rest);rest.CopyTo(response,12);
            var packet=PacketCodec.Decode(response);if(packet.Sequence!=seq)throw new InvalidDataException("Secuencia incorrecta.");return packet;
        }
    }
    private void ReadExact(byte[] buffer){var offset=0;while(offset<buffer.Length){var n=port!.Read(buffer,offset,buffer.Length-offset);if(n<=0)throw new TimeoutException();offset+=n;}}
    public void Disconnect(){lock(gate){port?.Dispose();port=null;Capabilities=null;}}
    public void Dispose()=>Disconnect();
}
