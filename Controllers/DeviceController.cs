using System.Collections.Concurrent;
using AtlasLetrero.Infrastructure;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using AtlasLetreros.Models;
using Microsoft.AspNetCore.Mvc;

namespace AtlasLetreros.Controllers;

public sealed class DeviceCatalog(IDeviceDiscovery discovery, IHttpClientFactory httpClientFactory)
{
    private readonly ConcurrentDictionary<string, DiscoveredDevice> _devices = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<DiscoveredDevice>> RefreshAsync(CancellationToken cancellationToken)
    {
        foreach (var device in await discovery.DiscoverAsync(TimeSpan.FromMilliseconds(350), cancellationToken))
            _devices[device.DeviceId] = device;
        return _devices.Values.ToArray();
    }

    public bool TryResolve(string id, out DiscoveredDevice device) => _devices.TryGetValue(id, out device!);

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
        foreach (var device in discovered)
        {
            try
            {
                var capabilities = await catalog.CreateClient(device).GetCapabilitiesAsync(cancellationToken);
                result.Add(DescribePhysical(device, true, capabilities));
            }
            catch (Exception exception) when (exception is ControllerCommunicationException or HttpRequestException or TaskCanceledException)
            { result.Add(DescribePhysical(device, false, null)); }
        }
        return Ok(result);
    }

    [HttpGet("{deviceId}/capabilities")]
    public async Task<IActionResult> Capabilities(string deviceId, CancellationToken cancellationToken)
    {
        if (IsSimulator(deviceId)) return Ok(simulator.GetCapabilities());
        if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
        return Ok(await catalog.CreateClient(device).GetCapabilitiesAsync(cancellationToken));
    }

    [HttpGet("{deviceId}/status")]
    public async Task<IActionResult> Status(string deviceId, CancellationToken cancellationToken)
    {
        if (IsSimulator(deviceId)) return Ok(new { protocolVersion = 1, device = simulator.GetCapabilities(), runtime = simulator.Status });
        if (!catalog.TryResolve(deviceId, out var device)) return UnknownDevice(deviceId);
        var client = catalog.CreateClient(device);
        var status = await client.GetStatusAsync(cancellationToken);
        var capabilities = await client.GetCapabilitiesAsync(cancellationToken);
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
        var status = await client.GetStatusAsync(cancellationToken);
        var capabilities = await client.GetCapabilitiesAsync(cancellationToken);
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
            var capabilities = await client.GetCapabilitiesAsync(cancellationToken);
            if (!Compatible(scene, capabilities)) return Mismatch(scene, capabilities);
            await client.UploadSceneAsync(scene, cancellationToken);
            await client.SetBrightnessAsync(request.Brightness, cancellationToken);
            await client.PlayAsync(scene.Id, cancellationToken);
        }
        return Ok(new { protocolVersion = 1, sceneId = scene.Id, playing = true });
    }

    [HttpPost("{deviceId}/play")]
    public async Task<IActionResult> Play(string deviceId, PlayRequest request, CancellationToken cancellationToken) =>
        await Command(deviceId, client => client.PlayAsync(request.SceneId, cancellationToken),
            () => simulator.PlayAsync(request, cancellationToken));

    [HttpPost("{deviceId}/stop")]
    public async Task<IActionResult> Stop(string deviceId, StopRequest request, CancellationToken cancellationToken) =>
        await Command(deviceId, client => client.StopAsync(cancellationToken), () => simulator.StopAsync(request, cancellationToken));

    [HttpPost("{deviceId}/brightness")]
    public async Task<IActionResult> Brightness(string deviceId, BrightnessRequest request, CancellationToken cancellationToken) =>
        await Command(deviceId, client => client.SetBrightnessAsync(request.Brightness, cancellationToken),
            () => simulator.SetBrightnessAsync(request, cancellationToken));

    private async Task<IActionResult> Command(string id, Func<AtlasControllerClient, ValueTask> physical, Func<ValueTask> local)
    {
        if (IsSimulator(id)) await local();
        else
        {
            if (!catalog.TryResolve(id, out var device)) return UnknownDevice(id);
            await physical(catalog.CreateClient(device));
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
    private IActionResult Mismatch(AtlasLetrero.Domain.Scene scene, CapabilitiesResponse capabilities) =>
        ProblemResult(400, "CanvasMismatch", $"The scene is {scene.Width}x{scene.Height}; the device is {capabilities.Width}x{capabilities.Height}.");
    private static bool Compatible(AtlasLetrero.Domain.Scene scene, CapabilitiesResponse capabilities) =>
        scene.Width == capabilities.Width && scene.Height == capabilities.Height && scene.ColorModel == capabilities.ColorModel;
    private ObjectResult ProblemResult(int status, string title, string detail) => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
