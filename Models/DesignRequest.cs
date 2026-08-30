namespace AtlasLetreros.Models;

public sealed record DesignRequest(
    int ProtocolVersion,
    Guid SceneId,
    string? Name,
    int Width,
    int Height,
    uint[]? Pixels,
    byte Brightness,
    string? Animation,
    double DurationSeconds,
    double Speed,
    bool Repeat);
