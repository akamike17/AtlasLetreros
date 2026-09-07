using AtlasLetrero.App.Services;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/devices")]
public sealed class DevicesController(DeviceConnectionService device) : ControllerBase
{
    [HttpGet("serial")]public object Serial()=>new { ports=device.Ports() };
    [HttpGet("status")]public object Status()=>device.Status;
    [HttpPost("virtual/connect")]public object ConnectVirtual()=>device.ConnectVirtual();
    [HttpPost("upload"), RequestSizeLimit(8 * 1024 * 1024)] public object Upload(JsonObject request)
    {
        var checksum=request["checksum"]?.GetValue<string>() ?? throw new InvalidDataException("Falta checksum.");
        var packet=request["packet"] as JsonObject ?? throw new InvalidDataException("Falta paquete.");
        var actual=device.Upload(packet,checksum); return new { checksum=actual, activated=true };
    }
    public sealed record ConnectRequest(string Port);
    [HttpPost("connect")]public IActionResult Connect(ConnectRequest request)
    {
        try{return Ok(device.Connect(request.Port));}
        catch(Exception ex)when(ex is IOException or TimeoutException or InvalidDataException or UnauthorizedAccessException or ArgumentException){return BadRequest(new {code="DEVICE_HANDSHAKE_FAILED",message="No se recibió un handshake AtlasLED válido. Comprueba el puerto y el firmware.",details=(string?)null});}
    }
    [HttpPost("disconnect")]public object Disconnect(){device.Disconnect();return device.Status;}
}
