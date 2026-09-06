using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using AtlasLetrero.App.Services;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/projects")] public class ProjectsController(AppPaths paths, ProjectPackageService packages, ProjectIndexService index) : ControllerBase {
    [HttpGet] public object List() => index.List();
    [HttpPost] public object Create([FromBody] JsonObject p) { p["id"] = Guid.NewGuid().ToString(); p["createdUtc"] = DateTime.UtcNow; return Save(p["id"]!.GetValue<string>(), p); }
    [HttpGet("{id}")] public object Get(string id) => packages.Read(paths.Project(id));
    [HttpPut("{id}")] public object Save(string id, [FromBody] JsonObject p) { if (p["id"]?.GetValue<string>() != id) throw new ArgumentException("El proyecto no coincide."); p["modifiedUtc"] = DateTime.UtcNow; packages.Save(paths.Project(id), p); var auto = paths.Project(id, true); if (System.IO.File.Exists(auto)) System.IO.File.Delete(auto); return p; }
    [HttpDelete("{id}")] public object Delete(string id) { System.IO.File.Delete(paths.Project(id)); System.IO.File.Delete(paths.Project(id, true)); return new { deleted = true }; }
    [HttpPost("{id}/duplicate")] public object Duplicate(string id) { var p = packages.Read(paths.Project(id)); p["name"] = p["name"]!.GetValue<string>() + " (copia)"; return Create(p); }
    [HttpPut("{id}/autosave")] public object Autosave(string id, [FromBody] JsonObject p) { if (p["id"]?.GetValue<string>() != id) throw new ArgumentException("El proyecto no coincide."); p["modifiedUtc"] = DateTime.UtcNow; packages.Save(paths.Project(id, true), p); return new { saved = true }; }
    [HttpGet("{id}/recovery")] public object Recovery(string id) { var auto = paths.Project(id, true); return new { available = System.IO.File.Exists(auto) && System.IO.File.GetLastWriteTimeUtc(auto) > System.IO.File.GetLastWriteTimeUtc(paths.Project(id)), document = System.IO.File.Exists(auto) ? packages.Read(auto) : null }; }
    [HttpGet("{id}/export")] public IActionResult Export(string id) => File(System.IO.File.ReadAllBytes(paths.Project(id)), "application/zip", id + ".atlasled");
    [HttpPost("import"), RequestSizeLimit(33554432)] public async Task<object> Import() { using var memory = new MemoryStream(); await Request.Body.CopyToAsync(memory); return Create(packages.Read(memory.ToArray())); }
}
