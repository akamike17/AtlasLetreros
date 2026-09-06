namespace AtlasLetrero.App.Protocol;
public enum AtlasLedCommand : byte { Hello=1, HelloAck, GetCapabilities, Capabilities, BeginUpload, FrameData, EndUpload, Verify, Activate, Play, Stop, SetBrightness, Ping, Ack, Nack, Error }
