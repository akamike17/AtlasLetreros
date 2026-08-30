namespace AtlasLetrero.Domain;

public enum ColorModel { Rgb, Rgbw }

public readonly record struct Pixel(byte R, byte G, byte B, byte W = 0)
{
    public static Pixel Off => default;
    public Pixel Scale(byte brightness)
    {
        static byte ScaleChannel(byte value, byte level) => (byte)((value * level + 127) / 255);
        return new(ScaleChannel(R, brightness), ScaleChannel(G, brightness),
            ScaleChannel(B, brightness), ScaleChannel(W, brightness));
    }
}

public sealed class FrameBuffer
{
    private readonly Pixel[] _pixels;

    public FrameBuffer(int width, int height, ColorModel colorModel = ColorModel.Rgb, byte brightness = 255)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if ((long)width * height > 1_048_576) throw new ArgumentOutOfRangeException(nameof(width), "Canvas exceeds the safety limit.");
        Width = width;
        Height = height;
        ColorModel = colorModel;
        Brightness = brightness;
        _pixels = new Pixel[width * height];
    }

    public int Width { get; }
    public int Height { get; }
    public ColorModel ColorModel { get; }
    public byte Brightness { get; private set; }
    public ReadOnlySpan<Pixel> Pixels => _pixels;

    public Pixel this[int x, int y]
    {
        get => _pixels[Index(x, y)];
        set
        {
            if (ColorModel == ColorModel.Rgb && value.W != 0)
                throw new InvalidOperationException("An RGB canvas cannot store a white channel.");
            _pixels[Index(x, y)] = value;
        }
    }

    public bool TrySetPixel(int x, int y, Pixel pixel)
    {
        if ((uint)x >= Width || (uint)y >= Height) return false;
        this[x, y] = pixel;
        return true;
    }

    public void Clear(Pixel color = default) => Array.Fill(_pixels, color);

    public void SetBrightness(byte brightness) => Brightness = brightness;

    public Pixel GetOutputPixel(int x, int y) => this[x, y].Scale(Brightness);

    public void CopyFrom(FrameBuffer source, int sourceX, int sourceY, int width, int height,
        int destinationX, int destinationY, bool transparentOff = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.ColorModel != ColorModel) throw new ArgumentException("Color models must match.", nameof(source));
        if (width < 0 || height < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (ReferenceEquals(source, this)) source = source.Clone();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var readX = sourceX + x; var readY = sourceY + y;
            var writeX = destinationX + x; var writeY = destinationY + y;
            if ((uint)readX >= source.Width || (uint)readY >= source.Height ||
                (uint)writeX >= Width || (uint)writeY >= Height) continue;
            var pixel = source[readX, readY];
            if (!transparentOff || pixel != Pixel.Off) this[writeX, writeY] = pixel;
        }
    }

    public FrameBuffer Clone()
    {
        var clone = new FrameBuffer(Width, Height, ColorModel, Brightness);
        _pixels.CopyTo(clone._pixels, 0);
        return clone;
    }

    private int Index(int x, int y)
    {
        if ((uint)x >= Width || (uint)y >= Height) throw new ArgumentOutOfRangeException(nameof(x));
        return y * Width + x;
    }
}

public static class Drawing
{
    public static void Line(FrameBuffer target, int x0, int y0, int x1, int y1, Pixel color)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            target.TrySetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            var twice = 2 * error;
            if (twice >= dy) { error += dy; x0 += sx; }
            if (twice <= dx) { error += dx; y0 += sy; }
        }
    }

    public static void Rectangle(FrameBuffer target, int x, int y, int width, int height, Pixel color, bool fill = false)
    {
        if (width <= 0 || height <= 0) return;
        if (fill)
        {
            for (var row = y; row < y + height; row++)
                for (var column = x; column < x + width; column++) target.TrySetPixel(column, row, color);
            return;
        }
        Line(target, x, y, x + width - 1, y, color);
        Line(target, x, y + height - 1, x + width - 1, y + height - 1, color);
        Line(target, x, y, x, y + height - 1, color);
        Line(target, x + width - 1, y, x + width - 1, y + height - 1, color);
    }

    public static void Circle(FrameBuffer target, int centerX, int centerY, int radius, Pixel color)
    {
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        var x = radius;
        var y = 0;
        var error = 1 - radius;
        while (x >= y)
        {
            target.TrySetPixel(centerX + x, centerY + y, color); target.TrySetPixel(centerX + y, centerY + x, color);
            target.TrySetPixel(centerX - y, centerY + x, color); target.TrySetPixel(centerX - x, centerY + y, color);
            target.TrySetPixel(centerX - x, centerY - y, color); target.TrySetPixel(centerX - y, centerY - x, color);
            target.TrySetPixel(centerX + y, centerY - x, color); target.TrySetPixel(centerX + x, centerY - y, color);
            y++;
            if (error < 0) error += 2 * y + 1;
            else { x--; error += 2 * (y - x + 1); }
        }
    }

    public static int Fill(FrameBuffer target, int x, int y, Pixel replacement)
    {
        if ((uint)x >= target.Width || (uint)y >= target.Height) return 0;
        var source = target[x, y];
        if (source == replacement) return 0;
        var pending = new Queue<(int X, int Y)>();
        pending.Enqueue((x, y));
        var count = 0;
        while (pending.TryDequeue(out var point))
        {
            if ((uint)point.X >= target.Width || (uint)point.Y >= target.Height || target[point.X, point.Y] != source) continue;
            target[point.X, point.Y] = replacement;
            count++;
            pending.Enqueue((point.X - 1, point.Y)); pending.Enqueue((point.X + 1, point.Y));
            pending.Enqueue((point.X, point.Y - 1)); pending.Enqueue((point.X, point.Y + 1));
        }
        return count;
    }
}
