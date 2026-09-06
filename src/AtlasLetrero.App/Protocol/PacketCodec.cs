using System.Buffers.Binary;
namespace AtlasLetrero.App.Protocol;
public static class PacketCodec {
    public const int HeaderSize=14;
    public static byte[] Encode(AtlasLedPacket packet) {
        var b=new byte[HeaderSize+packet.Payload.Length+4]; "ATLS"u8.CopyTo(b); b[4]=1; b[5]=(byte)packet.Command;
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(6),packet.Sequence); BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(10),packet.Payload.Length);
        packet.Payload.CopyTo(b,14); BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(b.Length-4),Crc32.Compute(b.AsSpan(0,b.Length-4))); return b;
    }
    public static AtlasLedPacket Decode(byte[] b) {
        if(b.Length<18 || !b.AsSpan(0,4).SequenceEqual("ATLS"u8) || b[4]!=1) throw new InvalidDataException("Cabecera de protocolo inválida.");
        int size=BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(10));
        if(size<0 || size>16*1024*1024 || b.Length!=size+18 || Crc32.Compute(b.AsSpan(0,b.Length-4))!=BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(b.Length-4))) throw new InvalidDataException("Longitud o CRC incorrecto.");
        return new((AtlasLedCommand)b[5],BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(6)),b.AsSpan(14,size).ToArray());
    }
}
