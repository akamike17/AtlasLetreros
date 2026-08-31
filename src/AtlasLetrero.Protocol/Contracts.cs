using System.Buffers.Binary;
using System.Text.Json;
using AtlasLetrero.Domain;

namespace AtlasLetrero.Protocol;

public static class ProtocolVersions { public const int Current = 1; }

public sealed record StatusResponse(int ProtocolVersion, string DeviceId, bool IsPlaying,
    Guid? ActiveSceneId, byte Brightness, long UptimeSeconds, string FirmwareVersion, string? LastError);

public sealed record CapabilitiesResponse(int ProtocolVersion, string Device, int Width, int Height,
    ColorModel ColorModel, string[] Features, string[] Drivers, long SceneStorageBytes);

public sealed record PlayRequest(int ProtocolVersion, Guid SceneId);
public sealed record BrightnessRequest(int ProtocolVersion, byte Brightness);
public sealed record StopRequest(int ProtocolVersion);
public sealed record FrameRequest(int ProtocolVersion, byte[] Payload);
public sealed record SceneUploadRequest(int ProtocolVersion, SceneDocument Scene);
public sealed record CommandResponse(int ProtocolVersion, bool Accepted, string? ErrorCode = null, string? Message = null);

public static class ProtocolJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        WriteIndented = false
    };

    public static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    public static T Deserialize<T>(ReadOnlySpan<byte> payload, int maximumBytes = 1_048_576)
    {
        if (payload.Length == 0 || payload.Length > maximumBytes) throw new ProtocolException("Payload size is invalid.");
        try { return JsonSerializer.Deserialize<T>(payload, Options) ?? throw new ProtocolException("Payload is empty."); }
        catch (JsonException exception) { throw new ProtocolException("Payload JSON is invalid.", exception); }
    }

    public static void ValidateVersion(int version)
    {
        if (version != ProtocolVersions.Current) throw new ProtocolException($"Protocol version {version} is unsupported.");
    }
}

public static class BinaryFrameCodec
{
    private static readonly byte[] Magic = "ATLF"u8.ToArray();
    public static byte[] Encode(FrameBuffer frame)
    {
        var channels = frame.ColorModel == ColorModel.Rgbw ? 4 : 3;
        if (frame.Width > ushort.MaxValue || frame.Height > ushort.MaxValue) throw new ProtocolException("Frame dimensions exceed protocol limits.");
        var result = new byte[14 + frame.Pixels.Length * channels];
        Magic.CopyTo(result, 0);
        result[4] = ProtocolVersions.Current;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(5), checked((ushort)frame.Width));
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(7), checked((ushort)frame.Height));
        result[9] = (byte)frame.ColorModel;
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(10), frame.Pixels.Length * channels);
        var offset = 14;
        foreach (var pixel in frame.Pixels)
        {
            result[offset++] = pixel.R; result[offset++] = pixel.G; result[offset++] = pixel.B;
            if (channels == 4) result[offset++] = pixel.W;
        }
        return result;
    }

    public static FrameBuffer Decode(ReadOnlySpan<byte> payload, int maximumPixels = 1_048_576)
    {
        if (payload.Length < 14 || !payload[..4].SequenceEqual(Magic)) throw new ProtocolException("Frame header is invalid.");
        ProtocolJson.ValidateVersion(payload[4]);
        var width = BinaryPrimitives.ReadUInt16LittleEndian(payload[5..]);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(payload[7..]);
        var model = (ColorModel)payload[9];
        if (!Enum.IsDefined(model)) throw new ProtocolException("Color model is invalid.");
        var channels = model == ColorModel.Rgbw ? 4 : 3;
        var pixelCount = (long)width * height;
        if (pixelCount <= 0 || pixelCount > maximumPixels) throw new ProtocolException("Frame dimensions exceed protocol limits.");
        var pixels = (int)pixelCount;
        var dataLength = BinaryPrimitives.ReadInt32LittleEndian(payload[10..]);
        if (pixels <= 0 || pixels > maximumPixels || dataLength != pixels * channels || payload.Length != 14 + dataLength)
            throw new ProtocolException("Frame dimensions or length are invalid.");
        var frame = new FrameBuffer(width, height, model);
        var offset = 14;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var r = payload[offset++]; var g = payload[offset++]; var b = payload[offset++];
            var w = channels == 4 ? payload[offset++] : (byte)0;
            frame[x, y] = new(r, g, b, w);
        }
        return frame;
    }
}

public enum SceneElementKind { Pixel, Rectangle, Line, Circle, Text, Image }

public sealed record FontDocument(string Id, int Width, int Height, int Spacing,
    IReadOnlyDictionary<char, ushort[]>? Rows = null,
    IReadOnlyDictionary<char, ulong>? Glyphs = null)
{
    public FontProfile ToDomain()
    {
        if (Rows is { Count: > 0 }) return new(Id, Width, Height, Spacing, Rows);
        if (Glyphs is { Count: > 0 }) return new(Id, Width, Height, Spacing, Glyphs);
        throw new ProtocolException("Font rows are required.");
    }
    public static FontDocument FromDomain(FontProfile font) =>
        new(font.Id, font.GlyphWidth, font.GlyphHeight, font.Spacing, Rows: font.Rows);
}

public sealed record ImageDocument(int Width, int Height, ColorModel ColorModel, IReadOnlyList<Pixel> Pixels)
{
    public FrameBuffer ToDomain()
    {
        if (Width <= 0 || Height <= 0 || Pixels.Count != checked(Width * Height))
            throw new ProtocolException("Image dimensions or pixels are invalid.");
        var frame = new FrameBuffer(Width, Height, ColorModel);
        for (var index = 0; index < Pixels.Count; index++) frame[index % Width, index / Width] = Pixels[index];
        return frame;
    }
    public static ImageDocument FromDomain(FrameBuffer image) =>
        new(image.Width, image.Height, image.ColorModel, image.Pixels.ToArray());
}

public sealed record SceneElementDocument(SceneElementKind Kind, int X, int Y, int X2, int Y2,
    int Width, int Height, int Radius, Pixel Color, bool Fill, string? Text = null,
    int LetterSpacing = 0, FontDocument? Font = null, int? OutputGlyphWidth = null,
    int? OutputGlyphHeight = null, bool Scroll = false, long ScrollPeriodMilliseconds = 0,
    bool TransparentOff = true, ImageDocument? Image = null, int LineSpacing = 1)
{
    public ISceneElement ToDomain() => Kind switch
    {
        SceneElementKind.Pixel => new PixelElement(X, Y, Color),
        SceneElementKind.Rectangle => new RectangleElement(X, Y, Width, Height, Color, Fill),
        SceneElementKind.Line => new LineElement(X, Y, X2, Y2, Color),
        SceneElementKind.Circle => new CircleElement(X, Y, Radius, Color),
        SceneElementKind.Text when Font is not null => new TextElement(Text ?? string.Empty, X, Y, Color, Font.ToDomain(), LetterSpacing,
            OutputGlyphWidth, OutputGlyphHeight, Scroll, ScrollPeriodMilliseconds > 0 ? TimeSpan.FromMilliseconds(ScrollPeriodMilliseconds) : null,
            LineSpacing),
        SceneElementKind.Text => throw new ProtocolException("Text element requires a font."),
        SceneElementKind.Image when Image is not null => new ImageElement(X, Y, Image.ToDomain(), TransparentOff),
        SceneElementKind.Image => throw new ProtocolException("Image element requires image data."),
        _ => throw new ProtocolException("Scene element kind is unsupported.")
    };

    public static SceneElementDocument FromDomain(ISceneElement element) => element switch
    {
        PixelElement value => new(SceneElementKind.Pixel, value.X, value.Y, 0, 0, 0, 0, 0, value.Color, false),
        RectangleElement value => new(SceneElementKind.Rectangle, value.X, value.Y, 0, 0, value.Width, value.Height, 0, value.Color, value.Fill),
        LineElement value => new(SceneElementKind.Line, value.X0, value.Y0, value.X1, value.Y1, 0, 0, 0, value.Color, false),
        CircleElement value => new(SceneElementKind.Circle, value.CenterX, value.CenterY, 0, 0, 0, 0, value.Radius, value.Color, false),
        TextElement value => new(SceneElementKind.Text, value.X, value.Y, 0, 0, 0, 0, 0, value.Color, false,
            value.Text, value.LetterSpacing, FontDocument.FromDomain(value.Font), value.OutputGlyphWidth, value.OutputGlyphHeight,
            value.Scroll, checked((long)(value.ScrollPeriod?.TotalMilliseconds ?? 0)), LineSpacing: value.LineSpacing),
        ImageElement value => new(SceneElementKind.Image, value.X, value.Y, 0, 0, value.Image.Width, value.Image.Height, 0,
            Pixel.Off, false, TransparentOff: value.TransparentOff, Image: ImageDocument.FromDomain(value.Image)),
        _ => throw new ProtocolException($"Element '{element.GetType().Name}' is not supported by autonomous scene protocol V1.")
    };
}

public sealed record LayerDocument(string Name, bool Visible, IReadOnlyList<SceneElementDocument> Elements);
public sealed record AnimationDocument(AnimationKind Kind, long DurationMilliseconds, double Speed, bool Repeat,
    EasingKind Easing = EasingKind.Linear)
{
    public Animation ToDomain() => new(Kind, TimeSpan.FromMilliseconds(DurationMilliseconds), Speed, Repeat, Easing);
    public static AnimationDocument FromDomain(Animation value) => new(value.Kind, checked((long)value.Duration.TotalMilliseconds), value.Speed, value.Repeat, value.Easing);
}
public sealed record TransitionDocument(TransitionKind Kind, long DurationMilliseconds, EasingKind Easing)
{
    public SceneTransition ToDomain() => new(Kind, TimeSpan.FromMilliseconds(DurationMilliseconds), Easing);
    public static TransitionDocument FromDomain(SceneTransition value) =>
        new(value.Kind, checked((long)value.Duration.TotalMilliseconds), value.Easing);
}

public sealed record SceneDocument(int ProtocolVersion, Guid Id, string Name, int Width, int Height,
    long DurationMilliseconds, ColorModel ColorModel, IReadOnlyList<LayerDocument> Layers,
    IReadOnlyList<AnimationDocument> Animations, TransitionDocument? Transition = null)
{
    public Scene ToDomain()
    {
        ProtocolJson.ValidateVersion(ProtocolVersion);
        if (Layers.Count > 128 || Layers.Sum(layer => layer.Elements.Count) > 10_000) throw new ProtocolException("Scene complexity exceeds protocol limits.");
        try
        {
            return new Scene(Id, Name, Width, Height, TimeSpan.FromMilliseconds(DurationMilliseconds),
                Layers.Select(layer => new Layer(layer.Name, layer.Elements.Select(element => element.ToDomain()).ToArray(), layer.Visible)),
                ColorModel, Animations.Select(animation => animation.ToDomain()), Transition?.ToDomain());
        }
        catch (ProtocolException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        { throw new ProtocolException("Scene document is invalid.", exception); }
    }

    public static SceneDocument FromDomain(Scene scene) => new(ProtocolVersions.Current, scene.Id, scene.Name, scene.Width,
        scene.Height, checked((long)scene.Duration.TotalMilliseconds), scene.ColorModel,
        scene.Layers.Select(layer => new LayerDocument(layer.Name, layer.Visible,
            layer.Elements.Select(SceneElementDocument.FromDomain).ToArray())).ToArray(),
        scene.Animations.Select(AnimationDocument.FromDomain).ToArray(),
        scene.Transition is null ? null : TransitionDocument.FromDomain(scene.Transition));
}

public static class SceneProtocolCodec
{
    public const int MaximumSceneBytes = 2 * 1024 * 1024;
    public static byte[] Encode(Scene scene)
    {
        var payload = ProtocolJson.Serialize(SceneDocument.FromDomain(scene));
        if (payload.Length > MaximumSceneBytes) throw new ProtocolException("Scene payload exceeds protocol limits.");
        return payload;
    }

    public static Scene Decode(ReadOnlySpan<byte> payload) =>
        ProtocolJson.Deserialize<SceneDocument>(payload, MaximumSceneBytes).ToDomain();
}

public sealed class ProtocolException : Exception
{
    public ProtocolException(string message, Exception? innerException = null) : base(message, innerException) { }
}
