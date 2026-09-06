using System.IO.Ports;
using System.Text.Json;
using AtlasLetrero.App.Models;
using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Services;
public sealed class DeviceConnectionService(DeviceProtocolService protocol) : IDisposable {
    public readonly SemaphoreSlim Gate=new(1,1);
    SerialPort? port; DeviceCapabilities? capabilities; uint sequence;
    public DeviceStatus Status => new(port?.IsOpen==true && capabilities!=null, port?.PortName, capabilities);
    public AtlasLedPacket Request(AtlasLedCommand command, byte[] payload, CancellationToken token) {
        if(port?.IsOpen!=true) throw new InvalidDataException("No hay un dispositivo conectado.");
        try { return protocol.Exchange(port,new(command,++sequence,payload),token); } catch { Disconnect(); throw; }
    }
    public void Connect(string name,CancellationToken token) {
        if(!SerialPort.GetPortNames().Contains(name,StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("El puerto seleccionado no existe.");
        Disconnect(); port=new SerialPort(name,115200){ReadTimeout=750,WriteTimeout=750,DtrEnable=false,RtsEnable=false};
        try { port.Open(); port.DiscardInBuffer(); var hello=Request(AtlasLedCommand.Hello,[],token); if(hello.Command!=AtlasLedCommand.HelloAck) throw new InvalidDataException("El puerto no responde como AtlasLED.");
            var caps=Request(AtlasLedCommand.GetCapabilities,[],token); if(caps.Command!=AtlasLedCommand.Capabilities) throw new InvalidDataException("Capacidades no válidas.");
            capabilities=JsonSerializer.Deserialize<DeviceCapabilities>(caps.Payload,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
            if(capabilities is null || capabilities.Protocol!=1 || capabilities.Width<1 || capabilities.Height<1) throw new InvalidDataException("Protocolo incompatible.");
        } catch { Disconnect(); throw; }
    }
    public void Disconnect() { capabilities=null; port?.Dispose(); port=null; }
    public void Dispose()=>Disconnect();
}
