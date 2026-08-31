using System.Diagnostics;
using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;

namespace AtlasLetrero.Simulator;

public sealed class SimulatorDevice
{
    private readonly ISceneStore _store;
    private ControllerRuntime _runtime;
    private VirtualDisplayDriver _driver;
    private string? _playbackError;
    private CancellationTokenSource? _playbackCancellation;
    private Task? _playbackTask;
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    public SimulatorDevice(DeviceConfiguration configuration, ISceneStore? store = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _store = store ?? new MemorySceneStore();
        _driver = new VirtualDisplayDriver();
        _runtime = new(_driver, _store);
    }

    public DeviceConfiguration Configuration { get; }
    public VirtualDisplayDriver Driver => Volatile.Read(ref _driver);
    public RuntimeStatus Status => Volatile.Read(ref _runtime).Status with { LastError = Volatile.Read(ref _playbackError) };
    public VirtualDisplaySnapshot Snapshot => Volatile.Read(ref _driver).CreateSnapshot();

    public async ValueTask BootAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            await _runtime.BootAsync(Configuration, cancellationToken);
            if (_runtime.Status.IsPlaying) StartPlaybackLoop();
        }
        finally { _operationGate.Release(); }
    }

    public CapabilitiesResponse GetCapabilities() => new(ProtocolVersions.Current, Configuration.DeviceId,
        Configuration.Topology.Width, Configuration.Topology.Height,
        Configuration.Topology.ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb ? ColorModel.Rgbw : ColorModel.Rgb,
        ["frames", "scenes", "text", "playlist", "storage", "simulator"], [Driver.Id], 16 * 1024 * 1024);

    public async ValueTask UploadSceneAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var scene = SceneProtocolCodec.Decode(payload.Span);
        ValidateDimensions(scene.Width, scene.Height, scene.ColorModel);
        await _operationGate.WaitAsync(cancellationToken);
        try { await _runtime.UploadAsync(scene, cancellationToken); }
        finally { _operationGate.Release(); }
    }

    public async ValueTask PlayAsync(PlayRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            await _runtime.PlayAsync(request.SceneId, cancellationToken);
            StartPlaybackLoop();
        }
        finally { _operationGate.Release(); }
    }

    public async ValueTask StopAsync(StopRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            await _runtime.StopAsync(cancellationToken);
        }
        finally { _operationGate.Release(); }
    }

    public async ValueTask SetBrightnessAsync(BrightnessRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _operationGate.WaitAsync(cancellationToken);
        try { await _runtime.SetBrightnessAsync(request.Brightness, cancellationToken); }
        finally { _operationGate.Release(); }
    }

    public async ValueTask RenderAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try { await _runtime.RenderAsync(position, cancellationToken); }
        finally { _operationGate.Release(); }
    }

    public async ValueTask ReceiveFrameAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var frame = BinaryFrameCodec.Decode(payload.Span, Configuration.Topology.Width * Configuration.Topology.Height);
        ValidateDimensions(frame.Width, frame.Height, frame.ColorModel);
        await _operationGate.WaitAsync(cancellationToken);
        try { await _driver.RenderAsync(frame, cancellationToken); }
        finally { _operationGate.Release(); }
    }

    public async ValueTask RestartAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            await StopPlaybackLoopAsync();
            var driver = new VirtualDisplayDriver();
            var runtime = new ControllerRuntime(driver, _store);
            await runtime.BootAsync(Configuration, cancellationToken);
            Volatile.Write(ref _driver, driver);
            Volatile.Write(ref _runtime, runtime);
            if (runtime.Status.IsPlaying) StartPlaybackLoop();
        }
        finally { _operationGate.Release(); }
    }

    private void StartPlaybackLoop()
    {
        Volatile.Write(ref _playbackError, null);
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
            catch (Exception exception)
            {
                Volatile.Write(ref _playbackError, exception.Message);
                Trace.TraceError("Simulator playback failed: {0}", exception);
                try { await _runtime.StopAsync(CancellationToken.None); }
                catch (Exception stopException) { Trace.TraceError("Simulator playback recovery failed: {0}", stopException); }
            }
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
