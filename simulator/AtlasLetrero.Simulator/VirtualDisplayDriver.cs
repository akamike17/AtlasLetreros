using AtlasLetrero.Application;
using AtlasLetrero.Domain;

namespace AtlasLetrero.Simulator;

public sealed class VirtualDisplayDriver : IDisplayDriver
{
    private readonly object _gate = new();
    private DeviceConfiguration? _configuration;
    public string Id => "simulator.virtual";
    public DriverCapabilities Capabilities { get; } = new("virtual", [ColorModel.Rgb, ColorModel.Rgbw], 1_048_576, 64);
    public FrameBuffer? LastFrame { get; private set; }
    public byte[]? LastPhysicalFrame { get; private set; }
    public byte Brightness { get; private set; } = 255;
    public long RenderedFrameCount { get; private set; }
    public event Action<VirtualDisplaySnapshot>? FrameRendered;

    public ValueTask InitializeAsync(DeviceConfiguration configuration, CancellationToken cancellationToken = default)
    { lock (_gate) { _configuration = configuration; Brightness = configuration.Brightness; } return ValueTask.CompletedTask; }
    public ValueTask SetBrightnessAsync(byte brightness, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Brightness = brightness;
            if (LastFrame is not null && _configuration is not null)
            {
                LastFrame.SetBrightness(brightness);
                LastPhysicalFrame = new MatrixMapper(_configuration.Topology).Encode(LastFrame);
                FrameRendered?.Invoke(CreateSnapshot());
            }
        }
        return ValueTask.CompletedTask;
    }
    public ValueTask RenderAsync(FrameBuffer frame, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var config = _configuration ?? throw new InvalidOperationException("Driver is not initialized.");
            LastFrame = frame.Clone();
            LastPhysicalFrame = new MatrixMapper(config.Topology).Encode(frame);
            RenderedFrameCount++;
            FrameRendered?.Invoke(CreateSnapshot());
        }
        return ValueTask.CompletedTask;
    }
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_configuration is { } config) LastFrame = new(config.Topology.Width, config.Topology.Height);
            LastPhysicalFrame = LastFrame is null || _configuration is null ? null : new MatrixMapper(_configuration.Topology).Encode(LastFrame);
            if (LastFrame is not null) FrameRendered?.Invoke(CreateSnapshot());
        }
        return ValueTask.CompletedTask;
    }
    public ValueTask TestAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var config = _configuration ?? throw new InvalidOperationException("Driver is not initialized.");
            var frame = new FrameBuffer(config.Topology.Width, config.Topology.Height);
            frame[0, 0] = new(255, 0, 0);
            return RenderAsync(frame, cancellationToken);
        }
    }

    public VirtualDisplaySnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            var configuration = _configuration ?? throw new InvalidOperationException("Driver is not initialized.");
            var frame = LastFrame ?? new FrameBuffer(configuration.Topology.Width, configuration.Topology.Height);
            var mapper = new MatrixMapper(configuration.Topology);
            var pixels = new VirtualPixel[frame.Width * frame.Height];
            for (var y = 0; y < frame.Height; y++) for (var x = 0; x < frame.Width; x++)
            {
                var address = mapper.Map(x, y);
                pixels[y * frame.Width + x] = new(x, y, frame.GetOutputPixel(x, y), address.TileIndex,
                    address.PhysicalIndex, address.AbsoluteIndex);
            }
            return new(frame.Width, frame.Height, frame.ColorModel, frame.Brightness, pixels,
                LastPhysicalFrame?.ToArray() ?? mapper.Encode(frame), configuration.Topology.Tiles.ToArray(), RenderedFrameCount);
        }
    }
}

public sealed record VirtualPixel(int X, int Y, Pixel Color, int TileIndex, int PhysicalIndex, int AbsoluteIndex)
{
    public bool IsOn => Color != Pixel.Off;
}

public sealed record VirtualDisplaySnapshot(int Width, int Height, ColorModel ColorModel, byte Brightness,
    IReadOnlyList<VirtualPixel> Pixels, byte[] PhysicalChannels, IReadOnlyList<MatrixTile> Tiles, long FrameNumber)
{
    public VirtualPixel this[int x, int y] => Pixels[y * Width + x];
}

public sealed class MemorySceneStore : ISceneStore
{
    private readonly Dictionary<Guid, Scene> _scenes = [];
    private Guid? _activeId;
    public ValueTask SaveAsync(Scene scene, CancellationToken cancellationToken = default) { _scenes[scene.Id] = scene; return ValueTask.CompletedTask; }
    public ValueTask<Scene?> LoadAsync(Guid id, CancellationToken cancellationToken = default) => ValueTask.FromResult(_scenes.GetValueOrDefault(id));
    public ValueTask SaveActiveSceneIdAsync(Guid? id, CancellationToken cancellationToken = default) { _activeId = id; return ValueTask.CompletedTask; }
    public ValueTask<Guid?> LoadActiveSceneIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_activeId);
}
