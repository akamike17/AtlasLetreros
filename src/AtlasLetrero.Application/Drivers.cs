using AtlasLetrero.Domain;

namespace AtlasLetrero.Application;

public sealed record DeviceConfiguration(string DeviceId, MatrixTopology Topology, byte Brightness = 255);
public sealed record DriverCapabilities(string Family, ColorModel[] ColorModels, int MaxPixels, int MaxOutputs,
    int MaxFramesPerSecond = 60, bool SupportsBrightness = true, bool SupportsTestPattern = true);

public interface IDisplayDriver
{
    string Id { get; }
    DriverCapabilities Capabilities { get; }
    ValueTask InitializeAsync(DeviceConfiguration configuration, CancellationToken cancellationToken = default);
    ValueTask SetBrightnessAsync(byte brightness, CancellationToken cancellationToken = default);
    ValueTask RenderAsync(FrameBuffer frame, CancellationToken cancellationToken = default);
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
    ValueTask TestAsync(CancellationToken cancellationToken = default);
}

public sealed class DriverRegistry
{
    private readonly Dictionary<string, Func<IDisplayDriver>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DriverProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public DriverRegistry Register(string id, Func<IDisplayDriver> factory)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Driver id is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(factory);
        if (!_factories.TryAdd(id, factory)) throw new InvalidOperationException($"Driver '{id}' is already registered.");
        return this;
    }

    public IDisplayDriver Create(string id) => _factories.TryGetValue(id, out var factory)
        ? factory() : throw new KeyNotFoundException($"Driver '{id}' is not registered.");

    public IReadOnlyCollection<string> Ids => _factories.Keys;

    public DriverRegistry RegisterProfile(DriverProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!_profiles.TryAdd(profile.Id, profile)) throw new InvalidOperationException($"Driver profile '{profile.Id}' is already registered.");
        return this;
    }

    public DriverProfile GetProfile(string id) => _profiles.TryGetValue(id, out var profile)
        ? profile : throw new KeyNotFoundException($"Driver profile '{id}' is not registered.");
    public IReadOnlyCollection<DriverProfile> Profiles => _profiles.Values;
}

public interface ISceneStore
{
    ValueTask SaveAsync(Scene scene, CancellationToken cancellationToken = default);
    ValueTask<Scene?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask SaveActiveSceneIdAsync(Guid? id, CancellationToken cancellationToken = default);
    ValueTask<Guid?> LoadActiveSceneIdAsync(CancellationToken cancellationToken = default);
}

public sealed record RuntimeStatus(bool IsPlaying, Guid? ActiveSceneId, byte Brightness, string? LastError,
    string? ActiveSceneName = null, double PositionSeconds = 0);

public sealed class ControllerRuntime
{
    private readonly IDisplayDriver _driver;
    private readonly ISceneStore _store;
    private Scene? _activeScene;
    private bool _playing;
    private byte _brightness = 255;

    public ControllerRuntime(IDisplayDriver driver, ISceneStore store) { _driver = driver; _store = store; }
    private TimeSpan _position;
    public RuntimeStatus Status => new(_playing, _activeScene?.Id, _brightness, null, _activeScene?.Name, _position.TotalSeconds);
    public TimeSpan ActiveDuration => _activeScene?.Duration ?? TimeSpan.Zero;
    public bool ActiveRepeats => _activeScene?.Animations.Any(animation => animation.Repeat) == true;

    public async ValueTask BootAsync(DeviceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var pixelCount = checked(configuration.Topology.Width * configuration.Topology.Height);
        if (pixelCount > _driver.Capabilities.MaxPixels) throw new InvalidOperationException("Display exceeds driver pixel capacity.");
        var outputColorModel = configuration.Topology.ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb
            ? ColorModel.Rgbw : ColorModel.Rgb;
        if (!_driver.Capabilities.ColorModels.Contains(outputColorModel)) throw new InvalidOperationException("Display color model is unsupported by the driver.");
        _brightness = configuration.Brightness;
        await _driver.InitializeAsync(configuration, cancellationToken);
        var activeId = await _store.LoadActiveSceneIdAsync(cancellationToken);
        if (activeId is { } id && await _store.LoadAsync(id, cancellationToken) is { } scene)
        {
            _activeScene = scene;
            _playing = true;
            _position = TimeSpan.Zero;
            await RenderAsync(TimeSpan.Zero, cancellationToken);
        }
    }

    public async ValueTask UploadAsync(Scene scene, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(scene, cancellationToken);

    public async ValueTask PlayAsync(Guid sceneId, CancellationToken cancellationToken = default)
    {
        _activeScene = await _store.LoadAsync(sceneId, cancellationToken) ?? throw new KeyNotFoundException("Scene is not stored.");
        _playing = true;
        _position = TimeSpan.Zero;
        await _store.SaveActiveSceneIdAsync(sceneId, cancellationToken);
        await RenderAsync(TimeSpan.Zero, cancellationToken);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _playing = false;
        _position = TimeSpan.Zero;
        _activeScene = null;
        await _store.SaveActiveSceneIdAsync(null, cancellationToken);
        await _driver.ClearAsync(cancellationToken);
    }

    public async ValueTask SetBrightnessAsync(byte brightness, CancellationToken cancellationToken = default)
    {
        _brightness = brightness;
        await _driver.SetBrightnessAsync(brightness, cancellationToken);
        if (_playing) await RenderAsync(TimeSpan.Zero, cancellationToken);
    }

    public async ValueTask RenderAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        if (!_playing || _activeScene is null) return;
        _position = position;
        await _driver.RenderAsync(SceneEngine.Render(_activeScene, position, _brightness), cancellationToken);
    }
}
