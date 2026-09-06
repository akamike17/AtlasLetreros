using Microsoft.AspNetCore.Mvc;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/health")] public class HealthController : ControllerBase { [HttpGet] public object Get() => new { status = "ok" }; }
