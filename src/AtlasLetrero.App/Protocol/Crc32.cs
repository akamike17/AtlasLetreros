namespace AtlasLetrero.App.Protocol;
public static class Crc32 {
    public static uint Compute(ReadOnlySpan<byte> bytes) { uint crc=0xffffffff; foreach(var b in bytes) { crc^=b; for(int i=0;i<8;i++) crc=(crc>>1)^((crc&1)!=0 ? 0xedb88320u:0u); } return ~crc; }
}
