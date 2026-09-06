using System.IO.Ports;
using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Services;
public sealed class SerialDiscoveryService(DeviceProtocolService protocol,DeviceConnectionService connection) {
    public object[] Scan(CancellationToken token) => SerialPort.GetPortNames().Select(name => {
        if(connection.Status is {Connected:true} status && status.Port==name) return (object)new {port=name,verified=true,message="AtlasLED conectado"};
        try { using var p=new SerialPort(name,115200){ReadTimeout=250,WriteTimeout=250}; p.Open(); p.DiscardInBuffer(); var hello=protocol.Exchange(p,new(AtlasLedCommand.Hello,1,[]),token); var caps=protocol.Exchange(p,new(AtlasLedCommand.GetCapabilities,2,[]),token); bool valid=hello.Command==AtlasLedCommand.HelloAck && caps.Command==AtlasLedCommand.Capabilities; return new {port=name,verified=valid,message=valid?"AtlasLED detectado":"Puerto sin verificar"}; }
        catch(Exception e) when(e is not OperationCanceledException) { return new {port=name,verified=false,message="No respondió como AtlasLED o está ocupado"}; }
    }).ToArray();
}
