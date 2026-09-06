using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/assets")]
public sealed class AssetsController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonArray> Catalogs = new();
    [HttpGet("{type}")]
    public object Get(string type, string? q = null)
    {
        if (type is not ("icons" or "emojis" or "fonts")) return NotFound();
        var catalog = Catalogs.GetOrAdd(type, key => JsonNode.Parse(System.IO.File.ReadAllText(Path.Combine(environment.WebRootPath, "assets", "catalogs", key + ".json")))!.AsArray());
        return catalog.Where(item => q is null || item!.ToJsonString().Contains(q, StringComparison.OrdinalIgnoreCase)).Select(item => item!.DeepClone()).ToArray();
    }
}
