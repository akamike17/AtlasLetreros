namespace AtlasLetrero.Domain;

public enum MatrixOrigin { TopLeft, TopRight, BottomLeft, BottomRight }
public enum MatrixTraversal { Row, Column }
public enum MatrixLayout { Progressive, Serpentine }
public enum MatrixRotation { None, Clockwise90, Clockwise180, Clockwise270 }
public enum MatrixMirror { None, Horizontal, Vertical, Both }
public enum ChannelOrder { Rgb, Rbg, Grb, Gbr, Brg, Bgr, Rgbw, Grbw, Brgw, Wrgb }

public sealed record MatrixTile(int LogicalX, int LogicalY, int Width, int Height,
    MatrixOrigin Origin = MatrixOrigin.TopLeft,
    MatrixTraversal Traversal = MatrixTraversal.Row,
    MatrixLayout Layout = MatrixLayout.Progressive,
    MatrixRotation Rotation = MatrixRotation.None,
    MatrixMirror Mirror = MatrixMirror.None,
    int ChainIndex = 0)
{
    public int PixelCount => checked(Width * Height);
    public int PhysicalWidth => Rotation is MatrixRotation.Clockwise90 or MatrixRotation.Clockwise270 ? Height : Width;
    public int PhysicalHeight => Rotation is MatrixRotation.Clockwise90 or MatrixRotation.Clockwise270 ? Width : Height;
}

public sealed class MatrixTopology
{
    public MatrixTopology(int width, int height, IEnumerable<MatrixTile> tiles, ChannelOrder channelOrder = ChannelOrder.Rgb)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        ChannelOrder = channelOrder;
        Tiles = tiles.OrderBy(tile => tile.ChainIndex).ToArray();
        if (Tiles.Count == 0) throw new ArgumentException("At least one tile is required.", nameof(tiles));
        Validate();
    }

    public int Width { get; }
    public int Height { get; }
    public ChannelOrder ChannelOrder { get; }
    public IReadOnlyList<MatrixTile> Tiles { get; }

    private void Validate()
    {
        var chainIndexes = new HashSet<int>();
        var occupied = new bool[Width * Height];
        foreach (var tile in Tiles)
        {
            if (tile.Width <= 0 || tile.Height <= 0 || tile.LogicalX < 0 || tile.LogicalY < 0 ||
                tile.LogicalX + tile.Width > Width || tile.LogicalY + tile.Height > Height)
                throw new ArgumentException("Tile is outside the logical canvas.");
            if (!chainIndexes.Add(tile.ChainIndex)) throw new ArgumentException("Tile chain indexes must be unique.");
            for (var y = tile.LogicalY; y < tile.LogicalY + tile.Height; y++)
                for (var x = tile.LogicalX; x < tile.LogicalX + tile.Width; x++)
                    if (occupied[y * Width + x]) throw new ArgumentException("Tiles cannot overlap.");
                    else occupied[y * Width + x] = true;
        }
        if (occupied.Any(value => !value)) throw new ArgumentException("Tiles must cover the full canvas.");
    }
}

public readonly record struct PhysicalPixelAddress(int TileIndex, int PhysicalIndex, int AbsoluteIndex);

public sealed class MatrixMapper
{
    private readonly MatrixTopology _topology;
    private readonly int[] _tileOffsets;

    public MatrixMapper(MatrixTopology topology)
    {
        _topology = topology;
        _tileOffsets = new int[topology.Tiles.Count];
        var offset = 0;
        for (var i = 0; i < topology.Tiles.Count; i++) { _tileOffsets[i] = offset; offset += topology.Tiles[i].PixelCount; }
    }

    public PhysicalPixelAddress Map(int logicalX, int logicalY)
    {
        if ((uint)logicalX >= _topology.Width || (uint)logicalY >= _topology.Height)
            throw new ArgumentOutOfRangeException(nameof(logicalX));
        for (var tileIndex = 0; tileIndex < _topology.Tiles.Count; tileIndex++)
        {
            var tile = _topology.Tiles[tileIndex];
            if (logicalX < tile.LogicalX || logicalY < tile.LogicalY ||
                logicalX >= tile.LogicalX + tile.Width || logicalY >= tile.LogicalY + tile.Height) continue;
            var x = logicalX - tile.LogicalX;
            var y = logicalY - tile.LogicalY;
            (x, y) = Transform(x, y, tile);
            var major = tile.Traversal == MatrixTraversal.Row ? y : x;
            var minor = tile.Traversal == MatrixTraversal.Row ? x : y;
            var minorSize = tile.Traversal == MatrixTraversal.Row ? tile.PhysicalWidth : tile.PhysicalHeight;
            if (tile.Layout == MatrixLayout.Serpentine && (major & 1) == 1) minor = minorSize - 1 - minor;
            var index = major * minorSize + minor;
            return new(tileIndex, index, _tileOffsets[tileIndex] + index);
        }
        throw new InvalidOperationException("Topology does not cover this pixel.");
    }

    public byte[] Encode(FrameBuffer frame, byte brightness = 255)
    {
        if (frame.Width != _topology.Width || frame.Height != _topology.Height)
            throw new ArgumentException("Frame dimensions do not match the topology.", nameof(frame));
        var channels = _topology.ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb ? 4 : 3;
        if (channels == 4 && frame.ColorModel != ColorModel.Rgbw) throw new ArgumentException("RGBW output requires an RGBW frame.");
        var result = new byte[frame.Width * frame.Height * channels];
        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
        {
            var destination = Map(x, y).AbsoluteIndex * channels;
            var effectiveBrightness = (byte)((brightness * frame.Brightness + 127) / 255);
            var pixel = frame[x, y].Scale(effectiveBrightness);
            var values = _topology.ChannelOrder switch
            {
                ChannelOrder.Rgb => new[] { pixel.R, pixel.G, pixel.B },
                ChannelOrder.Rbg => new[] { pixel.R, pixel.B, pixel.G },
                ChannelOrder.Grb => new[] { pixel.G, pixel.R, pixel.B },
                ChannelOrder.Gbr => new[] { pixel.G, pixel.B, pixel.R },
                ChannelOrder.Brg => new[] { pixel.B, pixel.R, pixel.G },
                ChannelOrder.Bgr => new[] { pixel.B, pixel.G, pixel.R },
                ChannelOrder.Rgbw => new[] { pixel.R, pixel.G, pixel.B, pixel.W },
                ChannelOrder.Grbw => new[] { pixel.G, pixel.R, pixel.B, pixel.W },
                ChannelOrder.Brgw => new[] { pixel.B, pixel.R, pixel.G, pixel.W },
                ChannelOrder.Wrgb => new[] { pixel.W, pixel.R, pixel.G, pixel.B },
                _ => throw new InvalidOperationException()
            };
            values.CopyTo(result, destination);
        }
        return result;
    }

    private static (int X, int Y) Transform(int x, int y, MatrixTile tile)
    {
        if (tile.Origin is MatrixOrigin.TopRight or MatrixOrigin.BottomRight) x = tile.Width - 1 - x;
        if (tile.Origin is MatrixOrigin.BottomLeft or MatrixOrigin.BottomRight) y = tile.Height - 1 - y;
        if (tile.Mirror is MatrixMirror.Horizontal or MatrixMirror.Both) x = tile.Width - 1 - x;
        if (tile.Mirror is MatrixMirror.Vertical or MatrixMirror.Both) y = tile.Height - 1 - y;
        return tile.Rotation switch
        {
            MatrixRotation.None => (x, y),
            MatrixRotation.Clockwise90 => (tile.Height - 1 - y, x),
            MatrixRotation.Clockwise180 => (tile.Width - 1 - x, tile.Height - 1 - y),
            MatrixRotation.Clockwise270 => (y, tile.Width - 1 - x),
            _ => throw new InvalidOperationException("Rotation is invalid.")
        };
    }
}
