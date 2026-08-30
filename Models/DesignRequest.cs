namespace AtlasLetreros.Models;

public sealed record DesignRequest(string Name, int Width, int Height, uint[] Pixels, byte Brightness,
    string Animation, double DurationSeconds, double Speed);
