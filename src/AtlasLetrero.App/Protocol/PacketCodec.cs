using System.Buffers.Binary;
namespace AtlasLetrero.App.Protocol;
public static class PacketCodec
{
    public const int HeaderLength=12;
    public static byte[] Encode(AtlasLedPacket packet)
    {
        var bytes = new byte[HeaderLength+packet.Payload.Length+4];
        "ATLS"u8.CopyTo(bytes); bytes[4]=1; bytes[5]=(byte)packet.Command;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6),packet.Sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)packet.Payload.Length);
        packet.Payload.CopyTo(bytes,HeaderLength);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(bytes.Length-4),Crc32.Compute(bytes.AsSpan(0,bytes.Length-4)));
        return bytes;
    }
    public static AtlasLedPacket Decode(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length<16 || !bytes[..4].SequenceEqual("ATLS"u8) || bytes[4]!=1) throw new InvalidDataException("Cabecera AtlasLED inválida.");
        var length=BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]);
        if(length>4*1024*1024 || bytes.Length!=HeaderLength+(long)length+4 || BinaryPrimitives.ReadUInt32LittleEndian(bytes[^4..])!=Crc32.Compute(bytes[..^4])) throw new InvalidDataException("Longitud o CRC incorrectos.");
        return new((AtlasLedCommand)bytes[5],BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]),bytes.Slice(HeaderLength,(int)length).ToArray());
    }
}
