using System.Net.Sockets;
using AtlasLetrero.Infrastructure;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using AtlasLetreros.Models;
using Microsoft.AspNetCore.Mvc;

namespace AtlasLetreros.Controllers;

public sealed class DeviceCatalog(IDeviceDiscovery discovery, IHttpClientFactory httpClientFactory)
{
    private IReadOnlyDictionary<string, DiscoveredDevice> _devices = new Dictionary<string, DiscoveredDevice>(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<DiscoveredDevice>> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var devices = await discovery.DiscoverAsync(TimeSpan.FromMilliseconds(350), cancellationToken);
            var current = devices.ToDictionary(device => device.DeviceId, StringComparer.OrdinalIgnoreCase);
            Volatile.Write(ref _devices, current);
            return current.Values.ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is SocketException or IOException)
        { return Volatile.Read(ref _devices).Values.ToArray(); }
    }

    public bool TryResolve(string id, out DiscoveredDevice device) => Volatile.Read(ref _devices).TryGetValue(id, out device!);

    public AtlasControllerClient CreateClient(DiscoveredDevice device)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = device.BaseAddress;
        client.Timeout = TimeSpan.FromSeconds(2);
        return new AtlasControllerClient(client);
    }
}

[ApiController]
[Route("api/devices")]
public sealed class DeviceController(SimulatorDevice simulator, DeviceCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Devices(CancellationToken cancellationToken)
    {
        var result = new List<object> { DescribeSimulator() };
        var discovered = await catalog.RefreshAsync(cancellationToken);
        var physical = await Task.WhenAll(discovered.Select(async device =>
        {
            try
            {
                var capabilities = await catalog.CreateClient(device).GetCapabilitiesAsync(cancellationToken);
                return DescribePhysical(device, true, capabilities);
            }
            catch (Exception exception) when (IsCommunicationFailure(exception))
            { return DescribePhysical(device, false, null); }
        }));
        result.AddRange(physical);
        return Ok(result);
    }

    [HttpGet("{deviceId}/capabilities")]
    public async Task<IActionResult> Capabilities(string deviceId, CancellationToken cancellationToken)
    {
        if (IsSimulator(deviceId)) return Ok(simulator.GetCapabilities());
        if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
        try { return Ok(await catalog.CreateClient(device).GetCapabilitiesAsync(cancellationToken)); }
        catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(deviceId); }
    }

    [HttpGet("{deviceId}/status")]
    public async Task<IActionResult> Status(string deviceId, CancellationToken cancellationToken)
    {
        if (IsSimulator(deviceId)) return Ok(new { protocolVersion = 1, device = simulator.GetCapabilities(), runtime = simulator.Status });
        if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
        var client = catalog.CreateClient(device);
        StatusResponse status; CapabilitiesResponse capabilities;
        try { status = await client.GetStatusAsync(cancellationToken); capabilities = await client.GetCapabilitiesAsync(cancellationToken); }
        catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(deviceId); }
        return Ok(new
        {
            protocolVersion = status.ProtocolVersion,
            device = capabilities,
            runtime = new { isPlaying = status.IsPlaying, activeSceneId = status.ActiveSceneId, activeSceneName = (string?)null,
                brightness = status.Brightness, lastError = status.LastError, positionSeconds = 0d },
            firmwareVersion = status.FirmwareVersion
        });
    }

    [HttpGet("{deviceId}/output")]
    public async Task<IActionResult> Output(string deviceId, CancellationToken cancellationToken)
    {
        if (IsSimulator(deviceId)) return Ok(new { protocolVersion = 1, device = simulator.GetCapabilities(), runtime = simulator.Status,
            snapshot = simulator.Snapshot, targetFramesPerSecond = 30, updatedAtUtc = DateTimeOffset.UtcNow });
        if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
        var client = catalog.CreateClient(device);
        StatusResponse status; CapabilitiesResponse capabilities;
        try { status = await client.GetStatusAsync(cancellationToken); capabilities = await client.GetCapabilitiesAsync(cancellationToken); }
        catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(deviceId); }
        return Ok(new { protocolVersion = 1, device = capabilities, runtime = new { isPlaying = status.IsPlaying,
            activeSceneId = status.ActiveSceneId, activeSceneName = (string?)null, brightness = status.Brightness,
            lastError = status.LastError, positionSeconds = 0d }, snapshot = (object?)null, updatedAtUtc = DateTimeOffset.UtcNow });
    }

    [HttpPost("{deviceId}/scene")]
    [RequestSizeLimit(SceneProtocolCodec.MaximumSceneBytes)]
    public async Task<IActionResult> Scene(string deviceId, SemanticDesignRequest request, CancellationToken cancellationToken)
    {
        if (request.ProtocolVersion != ProtocolVersions.Current || request.Scene is null) return BadRequest();
        AtlasLetrero.Domain.Scene scene;
        try { scene = request.Scene.ToDomain(); }
        catch (ProtocolException exception) { return ProblemResult(400, "InvalidScene", exception.Message); }
        if (IsSimulator(deviceId))
        {
            var capabilities = simulator.GetCapabilities();
            if (!Compatible(scene, capabilities)) return Mismatch(scene, capabilities);
            await simulator.UploadSceneAsync(SceneProtocolCodec.Encode(scene), cancellationToken);
            await simulator.SetBrightnessAsync(new(1, request.Brightness), cancellationToken);
            await simulator.PlayAsync(new(1, scene.Id), cancellationToken);
        }
        else
        {
            if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
            var client = catalog.CreateClient(device);
            CapabilitiesResponse capabilities;
            try { capabilities = await client.GetCapabilitiesAsync(cancellationToken); }
            catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(deviceId); }
            if (!Compatible(scene, capabilities)) return Mismatch(scene, capabilities);
            try
            {
                await client.UploadSceneAsync(scene, cancellationToken);
                await client.SetBrightnessAsync(request.Brightness, cancellationToken);
                await client.PlayAsync(scene.Id, cancellationToken);
            }
            catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(deviceId); }
        }
        return Ok(new { protocolVersion = 1, sceneId = scene.Id, playing = true });
    }

    [HttpPost("{deviceId}/play")]
    public async Task<IActionResult> Play(string deviceId, PlayRequest request, CancellationToken cancellationToken) =>
        request.ProtocolVersion != ProtocolVersions.Current ? IncompatibleProtocol() : await Command(deviceId, client => client.PlayAsync(request.SceneId, cancellationToken),
            () => simulator.PlayAsync(request, cancellationToken));

    [HttpPost("{deviceId}/stop")]
    public async Task<IActionResult> Stop(string deviceId, StopRequest request, CancellationToken cancellationToken) =>
        request.ProtocolVersion != ProtocolVersions.Current ? IncompatibleProtocol() : await Command(deviceId, client => client.StopAsync(cancellationToken), () => simulator.StopAsync(request, cancellationToken));

    [HttpPost("{deviceId}/brightness")]
    public async Task<IActionResult> Brightness(string deviceId, BrightnessRequest request, CancellationToken cancellationToken) =>
        request.ProtocolVersion != ProtocolVersions.Current ? IncompatibleProtocol() : await Command(deviceId, client => client.SetBrightnessAsync(request.Brightness, cancellationToken),
            () => simulator.SetBrightnessAsync(request, cancellationToken));

    private async Task<IActionResult> Command(string id, Func<AtlasControllerClient, ValueTask> physical, Func<ValueTask> local)
    {
        if (IsSimulator(id)) await local();
        else
        {
            if (!catalog.TryResolve(id, out var device)) return UnknownDevice(id);
            try { await physical(catalog.CreateClient(device)); }
            catch (Exception exception) when (IsCommunicationFailure(exception)) { return Unavailable(id); }
        }
        return NoContent();
    }

    private object DescribeSimulator() => new { id = "atlas-simulator", name = "Simulador local", kind = "simulator", online = true,
        baseUrl = "", route = new { scene = "/api/scene", status = "/api/status", output = "/api/output", stop = "/api/stop",
            play = "/api/play", brightness = "/api/brightness" }, address = (string?)null, port = 0, firmware = "local",
        protocolVersion = 1, capabilities = simulator.GetCapabilities() };
    private static object DescribePhysical(DiscoveredDevice device, bool online, CapabilitiesResponse? capabilities) => new
    { id = device.DeviceId, name = device.HostName, kind = "physical", online, address = device.Address.ToString(), device.Port,
        firmware = device.FirmwareVersion, device.ProtocolVersion, capabilities };
    private static bool IsSimulator(string id) => string.Equals(id, "atlas-simulator", StringComparison.OrdinalIgnoreCase);
    private IActionResult UnknownDevice(string id) => ProblemResult(404, "UnknownDevice", $"Device '{id}' is not in the discovered catalog.");
    private IActionResult Unavailable(string id) => ProblemResult(503, "DeviceUnavailable", $"Device '{id}' did not respond.");
    private IActionResult IncompatibleProtocol() => ProblemResult(400, "ProtocolMismatch", "The request protocol version is not supported.");
    private static bool IsCommunicationFailure(Exception exception) => exception is ControllerCommunicationException or HttpRequestException or TaskCanceledException or ProtocolException;
    private IActionResult Mismatch(AtlasLetrero.Domain.Scene scene, CapabilitiesResponse capabilities) =>
        ProblemResult(400, "CanvasMismatch", $"The scene is {scene.Width}x{scene.Height}; the device is {capabilities.Width}x{capabilities.Height}.");
    private static bool Compatible(AtlasLetrero.Domain.Scene scene, CapabilitiesResponse capabilities) =>
        scene.Width == capabilities.Width && scene.Height == capabilities.Height && scene.ColorModel == capabilities.ColorModel;
    private ObjectResult ProblemResult(int status, string title, string detail) => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
