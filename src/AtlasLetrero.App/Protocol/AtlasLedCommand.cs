namespace AtlasLetrero.App.Protocol;
public enum AtlasLedCommand : byte { Hello=1, HelloAck=2, GetCapabilities=3, Capabilities=4, BeginUpload=5, FrameData=6, EndUpload=7, Verify=8, Activate=9, Play=10, Stop=11, SetBrightness=12, Ping=13, Ack=14, Nack=15, Error=16 }
