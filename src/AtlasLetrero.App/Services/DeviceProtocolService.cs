using System.IO.Ports;
using System.Buffers.Binary;
using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Services;
public sealed class DeviceProtocolService {
    public AtlasLedPacket Exchange(SerialPort port, AtlasLedPacket request, CancellationToken token) {
        var bytes=PacketCodec.Encode(request);
        for(int attempt=0;attempt<3;attempt++) {
            token.ThrowIfCancellationRequested();
            try { port.Write(bytes,0,bytes.Length); var header=Read(port,14,token); int size=BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(10)); if(size<0||size>65536) throw new InvalidDataException("Respuesta demasiado grande.");
                var response=PacketCodec.Decode(header.Concat(Read(port,size+4,token)).ToArray());
                if(response.Sequence!=request.Sequence) throw new InvalidDataException("Secuencia incorrecta.");
                if(response.Command is AtlasLedCommand.Nack or AtlasLedCommand.Error) throw new InvalidDataException("El dispositivo rechazó el paquete."); return response;
            } catch(TimeoutException) when(attempt<2) { port.DiscardInBuffer(); }
        } throw new TimeoutException();
    }
    static byte[] Read(SerialPort port,int length,CancellationToken token) { var b=new byte[length]; int offset=0; while(offset<length) { token.ThrowIfCancellationRequested(); offset+=port.Read(b,offset,length-offset); } return b; }
}
