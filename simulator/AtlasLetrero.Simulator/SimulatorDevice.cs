using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;

namespace AtlasLetrero.Simulator;

public sealed class SimulatorDevice
{
    private readonly MemorySceneStore _store = new();
    private ControllerRuntime _runtime;

    public SimulatorDevice(DeviceConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Driver = new VirtualDisplayDriver();
        _runtime = new(Driver, _store);
    }

    public DeviceConfiguration Configuration { get; }
    public VirtualDisplayDriver Driver { get; private set; }
    public RuntimeStatus Status => _runtime.Status;
    public VirtualDisplaySnapshot Snapshot => Driver.CreateSnapshot();

    public async ValueTask BootAsync(CancellationToken cancellationToken = default) =>
        await _runtime.BootAsync(Configuration, cancellationToken);

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
        await _runtime.PlayAsync(request.SceneId, cancellationToken);
    }

    public async ValueTask StopAsync(StopRequest request, CancellationToken cancellationToken = default)
    {
        ProtocolJson.ValidateVersion(request.ProtocolVersion);
        await _runtime.StopAsync(cancellationToken);
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
        Driver = new VirtualDisplayDriver();
        _runtime = new(Driver, _store);
        await _runtime.BootAsync(Configuration, cancellationToken);
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
