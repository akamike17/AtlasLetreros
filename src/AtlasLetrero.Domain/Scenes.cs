namespace AtlasLetrero.Domain;

public enum AnimationKind { Blink, Fade, Scroll, Slide, Zoom, Pulse, Wipe, Marquee, Frame }

public sealed record Animation
{
    public Animation(AnimationKind kind, TimeSpan duration, double speed = 1, bool repeat = false)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (speed <= 0 || double.IsNaN(speed)) throw new ArgumentOutOfRangeException(nameof(speed));
        Kind = kind; Duration = duration; Speed = speed; Repeat = repeat;
    }
    public AnimationKind Kind { get; }
    public TimeSpan Duration { get; }
    public double Speed { get; }
    public bool Repeat { get; }
}

public interface ISceneElement
{
    void Render(FrameBuffer target, TimeSpan position);
}

public sealed record PixelElement(int X, int Y, Pixel Color) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position) => target.TrySetPixel(X, Y, Color);
}

public sealed record RectangleElement(int X, int Y, int Width, int Height, Pixel Color, bool Fill) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position) => Drawing.Rectangle(target, X, Y, Width, Height, Color, Fill);
}

public sealed record LineElement(int X0, int Y0, int X1, int Y1, Pixel Color) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position) => Drawing.Line(target, X0, Y0, X1, Y1, Color);
}

public sealed record CircleElement(int CenterX, int CenterY, int Radius, Pixel Color) : ISceneElement
{
    public void Render(FrameBuffer target, TimeSpan position) => Drawing.Circle(target, CenterX, CenterY, Radius, Color);
}

public sealed class ImageElement : ISceneElement
{
    private readonly FrameBuffer _image;
    public ImageElement(int x, int y, FrameBuffer image, bool transparentOff = true)
    { X = x; Y = y; _image = image?.Clone() ?? throw new ArgumentNullException(nameof(image)); TransparentOff = transparentOff; }
    public int X { get; }
    public int Y { get; }
    public bool TransparentOff { get; }
    public void Render(FrameBuffer target, TimeSpan position) =>
        target.CopyFrom(_image, 0, 0, _image.Width, _image.Height, X, Y, TransparentOff);
}

public sealed record Layer(string Name, IReadOnlyList<ISceneElement> Elements, bool Visible = true);

public sealed class Scene
{
    public Scene(Guid id, string name, int width, int height, TimeSpan duration, IEnumerable<Layer> layers,
        ColorModel colorModel = ColorModel.Rgb, IEnumerable<Animation>? animations = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Scene identity is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Scene name is required.", nameof(name));
        if (width <= 0 || height <= 0 || duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(width));
        Id = id; Name = name.Trim(); Width = width; Height = height; Duration = duration;
        Layers = layers.ToArray(); ColorModel = colorModel; Animations = animations?.ToArray() ?? [];
    }
    public Guid Id { get; }
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public TimeSpan Duration { get; }
    public ColorModel ColorModel { get; }
    public IReadOnlyList<Layer> Layers { get; }
    public IReadOnlyList<Animation> Animations { get; }
}

public static class SceneEngine
{
    public static FrameBuffer Render(Scene scene, TimeSpan position, byte brightness = 255)
    {
        var frame = new FrameBuffer(scene.Width, scene.Height, scene.ColorModel);
        var normalized = TimeSpan.FromTicks(position.Ticks % scene.Duration.Ticks);
        foreach (var layer in scene.Layers)
            if (layer.Visible)
                foreach (var element in layer.Elements) element.Render(frame, normalized);
        foreach (var animation in scene.Animations) frame = Apply(frame, animation, normalized);
        frame.SetBrightness((byte)((frame.Brightness * brightness + 127) / 255));
        return frame;
    }

    public static FrameBuffer Transition(FrameBuffer from, FrameBuffer to, TransitionKind kind, double progress)
    {
        ArgumentNullException.ThrowIfNull(from); ArgumentNullException.ThrowIfNull(to);
        if (from.Width != to.Width || from.Height != to.Height || from.ColorModel != to.ColorModel)
            throw new ArgumentException("Transition frames must have equal dimensions and color models.");
        progress = Math.Clamp(progress, 0, 1);
        var output = new FrameBuffer(from.Width, from.Height, from.ColorModel);
        for (var y = 0; y < output.Height; y++) for (var x = 0; x < output.Width; x++)
        {
            output[x, y] = kind switch
            {
                TransitionKind.Fade => Lerp(from[x, y], to[x, y], progress),
                TransitionKind.Wipe => x < Math.Ceiling(output.Width * progress) ? to[x, y] : from[x, y],
                TransitionKind.Slide => SlidePixel(from, to, x, y, progress),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
        return output;
    }

    private static FrameBuffer Apply(FrameBuffer source, Animation animation, TimeSpan position)
    {
        var progress = Progress(animation, position);
        return animation.Kind switch
        {
            AnimationKind.Blink => progress < .5 ? source : EmptyLike(source),
            AnimationKind.Fade => WithBrightness(source, progress),
            AnimationKind.Pulse => WithBrightness(source, .5 + .5 * Math.Sin(progress * Math.PI * 2)),
            AnimationKind.Scroll or AnimationKind.Marquee => Offset(source, -(int)Math.Floor(progress * source.Width), 0, wrap: true),
            AnimationKind.Slide => Offset(source, (int)Math.Ceiling((1 - progress) * source.Width), 0),
            AnimationKind.Wipe => Wipe(source, progress),
            AnimationKind.Zoom => Zoom(source, Math.Max(.01, progress)),
            AnimationKind.Frame => source,
            _ => source
        };
    }

    private static double Progress(Animation animation, TimeSpan position)
    {
        var scaled = position.TotalMilliseconds * animation.Speed;
        var duration = animation.Duration.TotalMilliseconds;
        return animation.Repeat ? (scaled % duration) / duration : Math.Clamp(scaled / duration, 0, 1);
    }

    private static FrameBuffer WithBrightness(FrameBuffer source, double factor)
    {
        var output = source.Clone();
        output.SetBrightness((byte)Math.Clamp(Math.Round(source.Brightness * factor), 0, 255));
        return output;
    }

    private static FrameBuffer EmptyLike(FrameBuffer source) => new(source.Width, source.Height, source.ColorModel, source.Brightness);

    private static FrameBuffer Offset(FrameBuffer source, int offsetX, int offsetY, bool wrap = false)
    {
        var output = EmptyLike(source);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var targetX = x + offsetX; var targetY = y + offsetY;
            if (wrap) { targetX = Mod(targetX, source.Width); targetY = Mod(targetY, source.Height); }
            output.TrySetPixel(targetX, targetY, source[x, y]);
        }
        return output;
    }

    private static FrameBuffer Wipe(FrameBuffer source, double progress)
    {
        var output = EmptyLike(source);
        var visibleWidth = (int)Math.Ceiling(source.Width * progress);
        output.CopyFrom(source, 0, 0, visibleWidth, source.Height, 0, 0);
        return output;
    }

    private static FrameBuffer Zoom(FrameBuffer source, double scale)
    {
        var output = EmptyLike(source);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var left = (source.Width - width) / 2; var top = (source.Height - height) / 2;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var readX = Math.Min(source.Width - 1, x * source.Width / width);
            var readY = Math.Min(source.Height - 1, y * source.Height / height);
            output.TrySetPixel(left + x, top + y, source[readX, readY]);
        }
        return output;
    }

    private static Pixel SlidePixel(FrameBuffer from, FrameBuffer to, int x, int y, double progress)
    {
        var shift = (int)Math.Round(from.Width * progress);
        var fromX = x + shift;
        return fromX < from.Width ? from[fromX, y] : to[fromX - from.Width, y];
    }

    private static Pixel Lerp(Pixel from, Pixel to, double progress) => new(
        Blend(from.R, to.R, progress), Blend(from.G, to.G, progress), Blend(from.B, to.B, progress), Blend(from.W, to.W, progress));
    private static byte Blend(byte from, byte to, double progress) => (byte)Math.Clamp(Math.Round(from + (to - from) * progress), 0, 255);
    private static int Mod(int value, int divisor) => (value % divisor + divisor) % divisor;
}

public enum TransitionKind { Fade, Wipe, Slide }
