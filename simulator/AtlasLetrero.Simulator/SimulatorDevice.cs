using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;

namespace AtlasLetrero.Simulator;

public sealed class SimulatorDevice
{
    private readonly ISceneStore _store;
    private ControllerRuntime _runtime;
    private CancellationTokenSource? _playbackCancellation;
    private Task? _playbackTask;
    private readonly SemaphoreSlim _playbackGate = new(1, 1);

    public SimulatorDevice(DeviceConfiguration configuration, ISceneStore? store = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _store = store ?? new MemorySceneStore();
        Driver = new VirtualDisplayDriver();
        _runtime = new(Driver, _store);
    }

    public DeviceConfiguration Configuration { get; }
    public VirtualDisplayDriver Driver { get; private set; }
    public RuntimeStatus Status => _runtime.Status;
    public VirtualDisplaySnapshot Snapshot => Driver.CreateSnapshot();

    public async ValueTask BootAsync(CancellationToken cancellationToken = default)
    {
        await _runtime.BootAsync(Configuration, cancellationToken);
        if (_runtime.Status.IsPlaying) StartPlaybackLoop();
    }

    public CapabilitiesResponse GetCapabilities() => new(ProtocolVersions.Current, Configuration.DeviceId,
        Configuration.Topology.Width, Configuration.Topology.Height,
        Configuration.Topology.ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb ? ColorModel.Rgbw : ColorModel.Rgb,
        ["frames", "scenes", "text", "playlist", "storage", "simulator"], [Driver.Id], 16 * 1024 * 1024);

    public async ValueTask UploadSceneAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var scene = SceneProtocolCodec.Decode(payload.Span);
        ValidateDimensions(scene.Width, scene.Height, scene.ColorModel);
        await _runtime.UploadAsync(scene, cancellationToken);
    }

    public async ValueTask PlayAsync(PlayRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _playbackGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            await _runtime.PlayAsync(request.SceneId, cancellationToken);
            StartPlaybackLoop();
        }
        finally { _playbackGate.Release(); }
    }

    public async ValueTask StopAsync(StopRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _playbackGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            await _runtime.StopAsync(cancellationToken);
        }
        finally { _playbackGate.Release(); }
    }

    public async ValueTask SetBrightnessAsync(BrightnessRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _runtime.SetBrightnessAsync(request.Brightness, cancellationToken);
    }

    public async ValueTask RenderAsync(TimeSpan position, CancellationToken cancellationToken = default) =>
        await _runtime.RenderAsync(position, cancellationToken);

    public async ValueTask ReceiveFrameAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var frame = BinaryFrameCodec.Decode(payload.Span, Configuration.Topology.Width * Configuration.Topology.Height);
        ValidateDimensions(frame.Width, frame.Height, frame.ColorModel);
        await Driver.RenderAsync(frame, cancellationToken);
    }

    public async ValueTask RestartAsync(CancellationToken cancellationToken = default)
    {
        await _playbackGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            Driver = new VirtualDisplayDriver();
            _runtime = new(Driver, _store);
            await _runtime.BootAsync(Configuration, cancellationToken);
            if (_runtime.Status.IsPlaying) StartPlaybackLoop();
        }
        finally { _playbackGate.Release(); }
    }

    private void StartPlaybackLoop()
    {
        _playbackCancellation = new();
        var token = _playbackCancellation.Token;
        _playbackTask = Task.Run(async () =>
        {
            var started = TimeProvider.System.GetTimestamp();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000d / Math.Min(30, Driver.Capabilities.MaxFramesPerSecond)));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    var elapsed = TimeProvider.System.GetElapsedTime(started);
                    if (!_runtime.ActiveRepeats && elapsed >= _runtime.ActiveDuration)
                    {
                        await _runtime.StopAsync(token);
                        break;
                    }
                    await _runtime.RenderAsync(elapsed, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }, token);
    }

    private async Task StopPlaybackLoopAsync()
    {
        if (_playbackCancellation is null) return;
        await _playbackCancellation.CancelAsync();
        if (_playbackTask is not null) try { await _playbackTask; } catch (OperationCanceledException) { }
        _playbackCancellation.Dispose(); _playbackCancellation = null; _playbackTask = null;
    }

    private void ValidateDimensions(int width, int height, ColorModel colorModel)
    {
        var expectedModel = Configuration.Topology.ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb
            ? ColorModel.Rgbw : ColorModel.Rgb;
        if (width != Configuration.Topology.Width || height != Configuration.Topology.Height)
            throw new ProtocolException("Content dimensions do not match the simulated display.");
        if (colorModel != expectedModel) throw new ProtocolException("Content color model does not match the simulated display.");
    }
}
