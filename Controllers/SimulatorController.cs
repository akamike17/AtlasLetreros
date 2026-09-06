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

    [HttpGet("output")]
    public object Output()
    {
        var capabilities = device.GetCapabilities();
        return new
        {
            protocolVersion = ProtocolVersions.Current,
            device = capabilities,
            runtime = device.Status,
            snapshot = device.Snapshot,
            targetFramesPerSecond = Math.Min(30, device.Driver.Capabilities.MaxFramesPerSecond),
            updatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    [HttpPost("frame")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Frame(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(); await Request.Body.CopyToAsync(buffer, cancellationToken);
        return await ExecuteProtocolActionAsync(
            () => device.ReceiveFrameAsync(buffer.ToArray(), cancellationToken));
    }

    [HttpPost("design")]
    [RequestSizeLimit(512 * 1024)]
    public async Task<IActionResult> Design(DesignRequest request, CancellationToken cancellationToken)
    {
        if (request.ProtocolVersion != 1)
            return DesignProblem("ProtocolVersionUnsupported", "La versión de protocolo no es compatible.");
        if (request.SceneId == Guid.Empty)
            return DesignProblem("InvalidSceneId", "La escena necesita un identificador estable.");

        var capabilities = device.GetCapabilities();
        if (request.Width != capabilities.Width || request.Height != capabilities.Height)
            return DesignProblem("CanvasMismatch", $"El dispositivo requiere {capabilities.Width} × {capabilities.Height} píxeles.");
        if (request.Pixels is null || request.Pixels.Length != checked(request.Width * request.Height))
            return DesignProblem("InvalidPixels", "La cantidad de píxeles no coincide con el canvas.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return DesignProblem("InvalidName", "El nombre debe contener entre 1 y 100 caracteres.");
        if (request.DurationSeconds is < 0.1 or > 3600 || !double.IsFinite(request.DurationSeconds))
            return DesignProblem("InvalidDuration", "La duración debe estar entre 0.1 y 3600 segundos.");
        if (request.Speed is < 0.1 or > 10 || !double.IsFinite(request.Speed))
            return DesignProblem("InvalidSpeed", "La velocidad debe estar entre 0.1 y 10.");
        if (!TryAnimation(request.Animation, request.DurationSeconds, request.Speed, request.Repeat, out var animations))
            return DesignProblem("InvalidAnimation", "La animación solicitada no está soportada.");

        var elements = new List<ISceneElement>();
        for (var index = 0; index < request.Pixels.Length; index++)
        {
            var color = request.Pixels[index]; if ((color & 0xFFFFFF) == 0) continue;
            elements.Add(new PixelElement(index % request.Width, index / request.Width,
                new((byte)(color >> 16), (byte)(color >> 8), (byte)color)));
        }
        var scene = new Scene(request.SceneId, request.Name.Trim(), request.Width, request.Height,
            TimeSpan.FromSeconds(request.DurationSeconds), [new Layer("Contenido", elements)], animations: animations);
        await device.UploadSceneAsync(SceneProtocolCodec.Encode(scene), cancellationToken);
        await device.SetBrightnessAsync(new(1, request.Brightness), cancellationToken);
        await device.PlayAsync(new(1, scene.Id), cancellationToken);
        return Ok(new { protocolVersion = 1, sceneId = scene.Id, pixels = elements.Count, playing = true });
    }

    [HttpPost("scene")]
    [RequestSizeLimit(SceneProtocolCodec.MaximumSceneBytes)]
    public async Task<IActionResult> Scene(SemanticDesignRequest request, CancellationToken cancellationToken)
    {
        if (request.ProtocolVersion != ProtocolVersions.Current)
            return DesignProblem("ProtocolVersionUnsupported", "La versión de protocolo no es compatible.");
        if (request.Scene is null) return DesignProblem("MissingScene", "El documento semántico de escena es obligatorio.");
        try
        {
            var scene = request.Scene.ToDomain();
            var capabilities = device.GetCapabilities();
            if (scene.Width != capabilities.Width || scene.Height != capabilities.Height)
                return DesignProblem("CanvasMismatch", $"El dispositivo requiere {capabilities.Width} × {capabilities.Height} píxeles.");
            await device.UploadSceneAsync(SceneProtocolCodec.Encode(scene), cancellationToken);
            await device.SetBrightnessAsync(new(ProtocolVersions.Current, request.Brightness), cancellationToken);
            await device.PlayAsync(new(ProtocolVersions.Current, scene.Id), cancellationToken);
            return Ok(new { protocolVersion = ProtocolVersions.Current, sceneId = scene.Id, layers = scene.Layers.Count, playing = true });
        }
        catch (ProtocolException exception)
        {
            return DesignProblem("InvalidScene", exception.Message);
        }
    }

    [HttpPost("play")]
    public Task<IActionResult> Play(PlayRequest request, CancellationToken cancellationToken) =>
        ExecuteProtocolActionAsync(() => device.PlayAsync(request, cancellationToken), catchArgumentException: true);

    [HttpPost("stop")]
    public Task<IActionResult> Stop(StopRequest request, CancellationToken cancellationToken) =>
        ExecuteProtocolActionAsync(() => device.StopAsync(request, cancellationToken));

    [HttpPost("brightness")]
    public Task<IActionResult> Brightness(BrightnessRequest request, CancellationToken cancellationToken) =>
        ExecuteProtocolActionAsync(() => device.SetBrightnessAsync(request, cancellationToken));

    private async Task<IActionResult> ExecuteProtocolActionAsync(
        Func<ValueTask> action,
        bool catchArgumentException = false)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (ProtocolException exception)
        {
            return ClientProblem(StatusCodes.Status400BadRequest, "InvalidProtocol", exception.Message);
        }
        catch (ArgumentException exception) when (catchArgumentException)
        {
            return ClientProblem(StatusCodes.Status400BadRequest, "InvalidArgument", exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return ClientProblem(StatusCodes.Status404NotFound, "SceneNotFound", exception.Message);
        }
    }

    private IActionResult DesignProblem(string code, string detail) => BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = code,
        Detail = detail,
        Type = $"https://atlasletrero.local/problems/{code.ToLowerInvariant()}"
    });

    private ObjectResult ClientProblem(int status, string code, string detail) => StatusCode(status, new ProblemDetails
    {
        Status = status,
        Title = code,
        Detail = detail,
        Type = $"https://atlasletrero.local/problems/{code.ToLowerInvariant()}"
    });

    private static bool TryAnimation(string? value, double duration, double speed, bool repeat, out Animation[] animations)
    {
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            animations = repeat ? [new(AnimationKind.Frame, TimeSpan.FromSeconds(duration), 1, true)] : [];
            return true;
        }

        var parsed = value?.ToLowerInvariant() switch
        {
            "blink" => AnimationKind.Blink,
            "fade" => AnimationKind.Fade,
            "scroll" => AnimationKind.Scroll,
            "slide" => AnimationKind.Slide,
            "zoom" => AnimationKind.Zoom,
            "pulse" => AnimationKind.Pulse,
            "wipe" => AnimationKind.Wipe,
            "marquee" => AnimationKind.Marquee,
            "frame" => AnimationKind.Frame,
            _ => (AnimationKind?)null
        };
        animations = parsed is null ? [] : [new(parsed.Value, TimeSpan.FromSeconds(duration), speed, repeat)];
        return parsed is not null;
    }
}
