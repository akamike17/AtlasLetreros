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
    public FontProfile(string id, int glyphWidth, int glyphHeight, int spacing, IReadOnlyDictionary<char, ushort[]> rows)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Font id is required.", nameof(id));
        if (glyphWidth <= 0 || glyphWidth > 16) throw new ArgumentOutOfRangeException(nameof(glyphWidth));
        if (glyphHeight <= 0) throw new ArgumentOutOfRangeException(nameof(glyphHeight));
        if (spacing < 0) throw new ArgumentOutOfRangeException(nameof(spacing));
        if (rows is not { Count: > 0 }) throw new ArgumentException("At least one glyph is required.", nameof(rows));
        var allowedBits = glyphWidth == 16 ? ushort.MaxValue : (ushort)((1u << glyphWidth) - 1);
        foreach (var glyph in rows.Values)
        {
            if (glyph is null || glyph.Length != glyphHeight)
                throw new ArgumentException("Each glyph must contain exactly glyphHeight rows.", nameof(rows));
            if (glyph.Any(row => (row & ~allowedBits) != 0))
                throw new ArgumentException("A glyph row uses bits outside glyphWidth.", nameof(rows));
        }
        Id = id.Trim(); GlyphWidth = glyphWidth; GlyphHeight = glyphHeight; Spacing = spacing; Rows = rows;
    }

    public FontProfile(string id, int glyphWidth, int glyphHeight, int spacing, IReadOnlyDictionary<char, ulong> glyphs)
        : this(id, glyphWidth, glyphHeight, spacing, FromLegacy(glyphWidth, glyphHeight, glyphs)) { }
    public string Id { get; }
    public int GlyphWidth { get; }
    public int GlyphHeight { get; }
    public int Spacing { get; }
    public IReadOnlyDictionary<char, ushort[]> Rows { get; }
    public bool IsPixelSet(char character, int x, int y)
    {
        if ((uint)x >= GlyphWidth || (uint)y >= GlyphHeight) return false;
        if (!Rows.TryGetValue(character, out var rows) && !Rows.TryGetValue('?', out rows)) return false;
        return (rows[y] & (1 << x)) != 0;
    }

    private static IReadOnlyDictionary<char, ushort[]> FromLegacy(
        int width, int height, IReadOnlyDictionary<char, ulong> glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        if (width <= 0 || width > 16 || height <= 0 || (long)width * height > 64)
            throw new ArgumentOutOfRangeException(nameof(width), "Legacy glyphs cannot exceed 64 bits.");
        return glyphs.ToDictionary(pair => pair.Key, pair => Enumerable.Range(0, height)
            .Select(y => (ushort)((pair.Value >> (y * width)) & ((1UL << width) - 1))).ToArray());
    }
}

public sealed record TextElement(string Text, int X, int Y, Pixel Color, FontProfile Font, int LetterSpacing = 0,
    int? OutputGlyphWidth = null, int? OutputGlyphHeight = null, bool Scroll = false, TimeSpan? ScrollPeriod = null,
    int LineSpacing = 1) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position)
    {
        if (string.IsNullOrEmpty(Text)) return;
        var width = OutputGlyphWidth ?? Font.GlyphWidth;
        var height = OutputGlyphHeight ?? Font.GlyphHeight;
        var origin = X;
        if (Scroll)
        {
            var period = ScrollPeriod.GetValueOrDefault(TimeSpan.FromSeconds(1));
            var advance = width + Font.Spacing + LetterSpacing;
            var contentWidth = Text.Replace("\r", "").Split('\n')
                .Max(line => Math.Max(0, line.Length * advance - Font.Spacing - LetterSpacing));
            origin = MarqueeOrigin(target.Width, contentWidth, position, period);
        }
        var lines = Text.Replace("\r", "").Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var cursor = origin;
            foreach (var character in lines[lineIndex])
            {
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                    if (Font.IsPixelSet(character, x * Font.GlyphWidth / width, y * Font.GlyphHeight / height))
                        target.TrySetPixel(cursor + x, Y + lineIndex * (height + Math.Max(0, LineSpacing)) + y, Color);
                cursor += width + Font.Spacing + LetterSpacing;
            }
        }
    }

    public static int MarqueeOrigin(int canvasWidth, int contentWidth, TimeSpan position, TimeSpan period)
    {
        if (canvasWidth <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (contentWidth < 0) throw new ArgumentOutOfRangeException(nameof(contentWidth));
        if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period));
        var elapsed = position.TotalMilliseconds % period.TotalMilliseconds;
        if (elapsed < 0) elapsed += period.TotalMilliseconds;
        var progress = elapsed / period.TotalMilliseconds;
        var distance = canvasWidth + contentWidth;
        return canvasWidth - (int)Math.Floor(progress * distance);
    }
}
