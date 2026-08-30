using AtlasLetrero.Domain;
using AtlasLetreros.Models;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using Microsoft.AspNetCore.Mvc;

namespace AtlasLetreros.Controllers;

[ApiController]
[Route("api")]
public sealed class SimulatorController(SimulatorDevice device) : ControllerBase
{
    [HttpGet("status")]
    public object Status() => new { protocolVersion = 1, device = device.GetCapabilities(), runtime = device.Status };

    [HttpGet("capabilities")]
    public CapabilitiesResponse Capabilities() => device.GetCapabilities();

    [HttpGet("snapshot")]
    public VirtualDisplaySnapshot Snapshot() => device.Snapshot;

    [HttpPost("frame")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Frame(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(); await Request.Body.CopyToAsync(buffer, cancellationToken);
        await device.ReceiveFrameAsync(buffer.ToArray(), cancellationToken); return NoContent();
    }

    [HttpPost("design")]
    [RequestSizeLimit(512 * 1024)]
    public async Task<IActionResult> Design(DesignRequest request, CancellationToken cancellationToken)
    {
        if (request.Width != 32 || request.Height != 16 || request.Pixels.Length != request.Width * request.Height)
            return BadRequest(new { code = "InvalidCanvas", message = "El diseño debe medir 32 × 16 píxeles." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.DurationSeconds is < 0.1 or > 3600 || request.Speed is < 0.1 or > 10)
            return BadRequest(new { code = "InvalidDesign", message = "Nombre o animación fuera de rango." });
        var elements = new List<ISceneElement>();
        for (var index = 0; index < request.Pixels.Length; index++)
        {
            var color = request.Pixels[index]; if ((color & 0xFFFFFF) == 0) continue;
            elements.Add(new PixelElement(index % request.Width, index / request.Width,
                new((byte)(color >> 16), (byte)(color >> 8), (byte)color)));
        }
        var animations = Animation(request.Animation, request.DurationSeconds, request.Speed);
        var scene = new Scene(Guid.NewGuid(), request.Name.Trim(), request.Width, request.Height,
            TimeSpan.FromSeconds(request.DurationSeconds), [new Layer("Contenido", elements)], animations: animations);
        await device.UploadSceneAsync(SceneProtocolCodec.Encode(scene), cancellationToken);
        await device.SetBrightnessAsync(new(1, request.Brightness), cancellationToken);
        await device.PlayAsync(new(1, scene.Id), cancellationToken);
        return Ok(new { sceneId = scene.Id, pixels = elements.Count, playing = true });
    }

    [HttpPost("play")]
    public async Task<IActionResult> Play(PlayRequest request, CancellationToken cancellationToken)
    { await device.PlayAsync(request, cancellationToken); return NoContent(); }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(StopRequest request, CancellationToken cancellationToken)
    { await device.StopAsync(request, cancellationToken); return NoContent(); }

    [HttpPost("brightness")]
    public async Task<IActionResult> Brightness(BrightnessRequest request, CancellationToken cancellationToken)
    { await device.SetBrightnessAsync(request, cancellationToken); return NoContent(); }

    private static Animation[] Animation(string value, double duration, double speed) => value.ToLowerInvariant() switch
    {
        "none" => [], "blink" => [new(AnimationKind.Blink, TimeSpan.FromSeconds(duration), speed, true)],
        "scroll" => [new(AnimationKind.Scroll, TimeSpan.FromSeconds(duration), speed, true)],
        "pulse" => [new(AnimationKind.Pulse, TimeSpan.FromSeconds(duration), speed, true)],
        "wipe" => [new(AnimationKind.Wipe, TimeSpan.FromSeconds(duration), speed, true)],
        _ => throw new ArgumentException("Animación no soportada.")
    };
}
