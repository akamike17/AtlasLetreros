namespace AtlasLetrero.App.Protocol;
public record AtlasLedPacket(AtlasLedCommand Command, uint Sequence, byte[] Payload);
