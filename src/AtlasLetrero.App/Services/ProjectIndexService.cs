using System.Text.Json.Nodes;
namespace AtlasLetrero.App.Services;
public sealed class ProjectIndexService(AppPaths paths, ProjectPackageService packages) {
    readonly object gate = new(); readonly Dictionary<string, (DateTime Stamp, JsonObject Info)> cache = new();
    public JsonArray List() { lock (gate) {
        var result = new JsonArray(); foreach (var path in Directory.EnumerateFiles(paths.Projects, "*.atlasled").OrderByDescending(File.GetLastWriteTimeUtc)) {
            try { var stamp = File.GetLastWriteTimeUtc(path); if (!cache.TryGetValue(path, out var item) || item.Stamp != stamp) {
                var p = packages.Read(path); item = (stamp, new JsonObject { ["id"] = p["id"]!.DeepClone(), ["name"] = p["name"]!.DeepClone(), ["matrixConfiguration"] = p["matrixConfiguration"]!.DeepClone(), ["modifiedUtc"] = p["modifiedUtc"]!.DeepClone() }); cache[path] = item;
            } result.Add(item.Info.DeepClone()); } catch (InvalidDataException) { /* corrupt packages remain untouched */ }
        } return result;
    } }
}
