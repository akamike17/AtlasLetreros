namespace AtlasLetrero.App.Protocol;
public sealed record AtlasLedPacket(AtlasLedCommand Command, ushort Sequence, byte[] Payload);
