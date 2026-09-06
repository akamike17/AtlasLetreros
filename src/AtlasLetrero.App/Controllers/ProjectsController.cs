using System.Text.Json.Nodes;
using AtlasLetrero.App.Services;
using Microsoft.AspNetCore.Mvc;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/projects")]
public sealed class ProjectsController(ProjectPackageService projects) : ControllerBase
{
    [HttpGet] public object List() => projects.List();
    [HttpGet("{id:guid}")] public object Get(Guid id) => projects.Get(id);
    [HttpPost] public object Create(JsonObject document) => projects.Save(Guid.Parse(document["id"]!.GetValue<string>()), document);
    [HttpPut("{id:guid}")] public object Save(Guid id, JsonObject document) => projects.Save(id, document);
    [HttpPut("{id:guid}/autosave")] public object Autosave(Guid id, JsonObject document) => projects.Save(id, document, true);
    [HttpGet("{id:guid}/recovery")] public IActionResult Recovery(Guid id) => Ok(new { project = projects.Recovery(id) });
    [HttpDelete("{id:guid}")] public object Delete(Guid id) { projects.Delete(id); return new { deleted = true }; }
    [HttpPost("{id:guid}/duplicate")] public object Duplicate(Guid id)
    {
        var document = projects.Get(id); var newId = Guid.NewGuid();
        document["id"] = newId.ToString(); document["name"] = document["name"]!.GetValue<string>() + " — copia";
        document["createdUtc"] = DateTime.UtcNow.ToString("O");
        return projects.Save(newId, document);
    }
}
