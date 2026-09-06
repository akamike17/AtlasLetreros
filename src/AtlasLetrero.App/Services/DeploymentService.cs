using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Services;
public sealed class DeploymentService(DeviceConnectionService device) {
    public object Upload(byte[] bytes,CancellationToken token) {
        if(bytes.Length<16 || bytes.Length>device.Status.Capabilities?.MaxBytes) throw new InvalidDataException("El paquete supera la capacidad del dispositivo.");
        var crc=Crc32.Compute(bytes);
        void Send(AtlasLedCommand c,byte[] p) { if(device.Request(c,p,token).Command!=AtlasLedCommand.Ack) throw new InvalidDataException("Confirmación no válida."); }
        Send(AtlasLedCommand.BeginUpload,BitConverter.GetBytes(bytes.Length));
        for(int offset=0;offset<bytes.Length;offset+=1024) Send(AtlasLedCommand.FrameData,BitConverter.GetBytes(offset).Concat(bytes.Skip(offset).Take(1024)).ToArray());
        Send(AtlasLedCommand.EndUpload,[]); Send(AtlasLedCommand.Verify,BitConverter.GetBytes(crc)); Send(AtlasLedCommand.Activate,[]);
        return new {status="Correcto",checksum=crc,bytes=bytes.Length};
    }
}
