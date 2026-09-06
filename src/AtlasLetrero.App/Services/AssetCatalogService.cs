using System.Text.Json.Nodes;
namespace AtlasLetrero.App.Services;
public sealed class AssetCatalogService(IWebHostEnvironment environment) {
    readonly Dictionary<string, JsonArray> catalogs = new(); readonly object gate = new();
    public JsonArray Search(string kind, string? q) { lock(gate) {
        if (!catalogs.TryGetValue(kind, out var catalog)) { var path = Path.Combine(environment.WebRootPath, "assets", "catalogs", kind + ".json"); catalog = JsonNode.Parse(File.ReadAllText(path))!.AsArray(); catalogs[kind] = catalog; }
        return new JsonArray(catalog.Where(x => string.IsNullOrWhiteSpace(q) || x!.ToJsonString().Contains(q, StringComparison.OrdinalIgnoreCase)).Select(x => x!.DeepClone()).ToArray());
    } }
}
