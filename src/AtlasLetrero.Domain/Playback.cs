namespace AtlasLetrero.Domain;

public sealed record PlaylistItem
{
    public PlaylistItem(Guid sceneId, TimeSpan duration, int repeat = 1)
    {
        if (sceneId == Guid.Empty) throw new ArgumentException("Scene identity is required.", nameof(sceneId));
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (repeat is <= 0 or > 10_000) throw new ArgumentOutOfRangeException(nameof(repeat));
        SceneId = sceneId; Duration = duration; Repeat = repeat;
    }
    public Guid SceneId { get; }
    public TimeSpan Duration { get; }
    public int Repeat { get; }
}

public sealed class Playlist
{
    private readonly List<PlaylistItem> _items = [];
    public Playlist(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Playlist identity is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Playlist name is required.", nameof(name));
        Id = id; Name = name.Trim();
    }
    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyList<PlaylistItem> Items => _items;
    public TimeSpan TotalDuration => TimeSpan.FromTicks(_items.Sum(item => checked(item.Duration.Ticks * item.Repeat)));
    public void Add(PlaylistItem item) { ArgumentNullException.ThrowIfNull(item); _items.Add(item); }
    public void RemoveAt(int index) => _items.RemoveAt(index);
    public PlaylistItem Resolve(TimeSpan elapsed)
    {
        if (_items.Count == 0) throw new InvalidOperationException("Playlist is empty.");
        var total = TotalDuration;
        var position = elapsed.Ticks % total.Ticks;
        if (position < 0) position += total.Ticks;
        foreach (var item in _items)
        {
            var span = checked(item.Duration.Ticks * item.Repeat);
            if (position < span) return item;
            position -= span;
        }
        return _items[^1];
    }
}

public sealed record Schedule
{
    public Schedule(Guid id, Guid playlistId, DayOfWeek[] days, TimeOnly startsAt, TimeOnly endsAt, bool enabled = true)
    {
        if (id == Guid.Empty || playlistId == Guid.Empty) throw new ArgumentException("Schedule and playlist identities are required.");
        if (days is null || days.Length == 0) throw new ArgumentException("At least one day is required.", nameof(days));
        Id = id; PlaylistId = playlistId; Days = days.Distinct().Order().ToArray(); StartsAt = startsAt; EndsAt = endsAt; Enabled = enabled;
    }
    public Guid Id { get; }
    public Guid PlaylistId { get; }
    public DayOfWeek[] Days { get; }
    public TimeOnly StartsAt { get; }
    public TimeOnly EndsAt { get; }
    public bool Enabled { get; }
    public bool IsActive(DateTimeOffset instant, TimeZoneInfo zone)
    {
        if (!Enabled) return false;
        var local = TimeZoneInfo.ConvertTime(instant, zone);
        var time = TimeOnly.FromDateTime(local.DateTime);
        if (StartsAt <= EndsAt) return Days.Contains(local.DayOfWeek) && time >= StartsAt && time < EndsAt;
        var effectiveDay = time < EndsAt ? local.AddDays(-1).DayOfWeek : local.DayOfWeek;
        return Days.Contains(effectiveDay) && (time >= StartsAt || time < EndsAt);
    }
}

public sealed record FontProfile
{
    public FontProfile(string id, int glyphWidth, int glyphHeight, int spacing, IReadOnlyDictionary<char, ulong> glyphs)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Font id is required.", nameof(id));
        if (glyphWidth <= 0 || glyphHeight <= 0 || glyphWidth * glyphHeight > 64) throw new ArgumentOutOfRangeException(nameof(glyphWidth));
        if (spacing < 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        if (glyphs is not { Count: > 0 }) throw new ArgumentException("At least one glyph is required.", nameof(glyphs));
        Id = id.Trim(); GlyphWidth = glyphWidth; GlyphHeight = glyphHeight; Spacing = spacing; Glyphs = glyphs;
    }
    public string Id { get; }
    public int GlyphWidth { get; }
    public int GlyphHeight { get; }
    public int Spacing { get; }
    public IReadOnlyDictionary<char, ulong> Glyphs { get; }
    public bool IsPixelSet(char character, int x, int y)
    {
        if ((uint)x >= GlyphWidth || (uint)y >= GlyphHeight) return false;
        if (!Glyphs.TryGetValue(character, out var bits) && !Glyphs.TryGetValue('?', out bits)) return false;
        return (bits & (1UL << (y * GlyphWidth + x))) != 0;
    }
}

public sealed record TextElement(string Text, int X, int Y, Pixel Color, FontProfile Font, int LetterSpacing = 0) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position)
    {
        if (string.IsNullOrEmpty(Text)) return;
        var cursor = X;
        foreach (var character in Text)
        {
            for (var y = 0; y < Font.GlyphHeight; y++) for (var x = 0; x < Font.GlyphWidth; x++)
                if (Font.IsPixelSet(character, x, y)) target.TrySetPixel(cursor + x, Y + y, Color);
            cursor += Font.GlyphWidth + Font.Spacing + LetterSpacing;
        }
    }
}
