using Microsoft.AspNetCore.Mvc;
using AtlasLetrero.App.Services;
using AtlasLetrero.App.Protocol;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/devices")] public class DevicesController(SerialDiscoveryService discovery,DeviceConnectionService device,DeploymentService deploy) : ControllerBase {
    [HttpGet("serial")] public async Task<object> Serial(CancellationToken token) { await device.Gate.WaitAsync(token); try { return await Task.Run(()=>discovery.Scan(token),token); } finally { device.Gate.Release(); } }
    public record ConnectRequest(string Port);
    [HttpPost("connect")] public async Task<object> Connect(ConnectRequest request,CancellationToken token) { await device.Gate.WaitAsync(token); try { await Task.Run(()=>device.Connect(request.Port,token),token); return device.Status; } finally { device.Gate.Release(); } }
    [HttpPost("disconnect")] public async Task<object> Disconnect(CancellationToken token) { await device.Gate.WaitAsync(token); try { device.Disconnect(); return device.Status; } finally { device.Gate.Release(); } }
    [HttpGet("status")] public object Status()=>device.Status;
    [HttpPost("test-matrix")] public async Task<object> Test(CancellationToken token) { await device.Gate.WaitAsync(token); try { return device.Request(AtlasLedCommand.Ping,[],token).Command==AtlasLedCommand.Ack ? new {status="Comunicación verificada"} : throw new InvalidDataException("No se recibió confirmación."); } finally { device.Gate.Release(); } }
    [HttpPost("upload"), RequestSizeLimit(16777216)] public async Task<object> Upload(CancellationToken token) { using var stream=new MemoryStream(); await Request.Body.CopyToAsync(stream,token); await device.Gate.WaitAsync(token); try { return await Task.Run(()=>deploy.Upload(stream.ToArray(),token),token); } finally { device.Gate.Release(); } }
}
