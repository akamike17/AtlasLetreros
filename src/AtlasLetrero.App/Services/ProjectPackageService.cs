using System.IO.Compression;
using System.Text.Json.Nodes;

namespace AtlasLetrero.App.Services;

public sealed class ProjectPackageService(AppPaths paths, AtomicFileWriter writer)
{
    private readonly object gate = new();
    public static void Validate(JsonObject document)
    {
        if (!Guid.TryParse(document["id"]?.GetValue<string>(), out _) || string.IsNullOrWhiteSpace(document["name"]?.GetValue<string>())) throw new InvalidDataException();
        if (document["formatVersion"]?.GetValue<int>() != 1) throw new InvalidDataException();
        var matrix = document["matrix"] ?? throw new InvalidDataException();
        var w = matrix["width"]!.GetValue<int>(); var h = matrix["height"]!.GetValue<int>();
        if (w < 1 || h < 1 || w > 256 || h > 256 || matrix["fps"]!.GetValue<int>() is < 1 or > 60) throw new InvalidDataException();
        if (document["scenes"] is not JsonArray { Count: > 0 }) throw new InvalidDataException();
    }
    public JsonObject Read(string file)
    {
        using var archive = ZipFile.OpenRead(file);
        var entry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException();
        if (entry.Length > 64 * 1024 * 1024) throw new InvalidDataException();
        using var stream = entry.Open();
        var document = JsonNode.Parse(stream)?.AsObject() ?? throw new InvalidDataException();
        Validate(document);
        return document;
    }
    public JsonObject Get(Guid id) { lock (gate) return Read(paths.Project(id)); }
    public object[] List()
    {
        lock (gate) return Directory.EnumerateFiles(paths.Projects, "*.atlasled").Select(file =>
        {
            try { var p = Read(file); return new { id = p["id"]!.GetValue<string>(), name = p["name"]!.GetValue<string>(), matrix = p["matrix"]!.DeepClone(), modifiedUtc = p["modifiedUtc"]!.GetValue<string>() }; }
            catch (InvalidDataException) { return null; }
        }).Where(p => p is not null).OrderByDescending(p => p!.modifiedUtc).Cast<object>().ToArray();
    }
    public JsonObject Save(Guid id, JsonObject document, bool autosave = false)
    {
        lock (gate)
        {
            Validate(document);
            if (document["id"]!.GetValue<string>() != id.ToString()) throw new InvalidDataException();
            document["modifiedUtc"] = DateTime.UtcNow.ToString("O");
            using var memory = new MemoryStream();
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
            {
                using var stream = new StreamWriter(zip.CreateEntry("manifest.json").Open());
                stream.Write(document.ToJsonString());
            }
            writer.Write(autosave ? paths.Autosave(id) : paths.Project(id), memory.ToArray(), file => Read(file));
            if (!autosave && File.Exists(paths.Autosave(id))) File.Delete(paths.Autosave(id));
            return document;
        }
    }
    public JsonObject? Recovery(Guid id)
    {
        lock (gate) return File.Exists(paths.Autosave(id)) && File.GetLastWriteTimeUtc(paths.Autosave(id)) > File.GetLastWriteTimeUtc(paths.Project(id)) ? Read(paths.Autosave(id)) : null;
    }
    public void Delete(Guid id)
    {
        lock (gate)
        {
            if (!File.Exists(paths.Project(id))) throw new FileNotFoundException();
            File.Delete(paths.Project(id));
            if (File.Exists(paths.Autosave(id))) File.Delete(paths.Autosave(id));
        }
    }
}
