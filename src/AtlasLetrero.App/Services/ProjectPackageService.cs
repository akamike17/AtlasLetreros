using System.IO.Compression;
using System.Text.Json.Nodes;

namespace AtlasLetrero.App.Services;

public sealed class ProjectPackageService(AppPaths paths, AtomicFileWriter writer)
{
    public const int MaxScenes = 64, MaxFramesPerScene = 256, MaxLayersPerFrame = 64, MaxObjectsPerLayer = 512;
    private readonly object gate = new();
    public static void Validate(JsonObject document)
    {
        try
        {
            var name = document["name"]?.GetValue<string>();
            if (!Guid.TryParse(document["id"]?.GetValue<string>(), out _) || string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException();
            if (document["formatVersion"]?.GetValue<int>() != 1) throw new InvalidDataException();
            var matrix = document["matrix"] ?? throw new InvalidDataException();
            var w = matrix["width"]!.GetValue<int>(); var h = matrix["height"]!.GetValue<int>();
            if (w < 1 || h < 1 || w > 256 || h > 256 || matrix["fps"]!.GetValue<int>() is < 1 or > 60) throw new InvalidDataException();
            if (document["scenes"] is not JsonArray { Count: > 0 } scenes || scenes.Count > MaxScenes) throw new InvalidDataException();
            foreach (var scene in scenes)
            {
                if (scene?["frames"] is not JsonArray { Count: > 0 } frames || frames.Count > MaxFramesPerScene) throw new InvalidDataException();
                foreach (var frame in frames)
                {
                    if (frame?["durationMs"]?.GetValue<int>() is not (>= 20 and <= 600000)) throw new InvalidDataException();
                    if (frame?["layers"] is not JsonArray { Count: > 0 } layers || layers.Count > MaxLayersPerFrame || layers.Any(layer => layer?["objects"] is not JsonArray objects || objects.Count > MaxObjectsPerLayer)) throw new InvalidDataException();
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or NullReferenceException)
        {
            throw new InvalidDataException("El proyecto contiene campos incompletos o de tipo incorrecto.", ex);
        }
    }
    public JsonObject Read(string file)
    {
        using var archive = ZipFile.OpenRead(file);
        var entry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException();
        if (entry.Length > 8 * 1024 * 1024) throw new InvalidDataException("El manifiesto supera el límite de 8 MB.");
        using var stream = entry.Open();
        JsonObject document;
        try { document = JsonNode.Parse(stream) as JsonObject ?? throw new InvalidDataException(); }
        catch (System.Text.Json.JsonException ex) { throw new InvalidDataException("El manifiesto no es JSON válido.", ex); }
        Validate(document);
        return document;
    }
    public JsonObject Get(Guid id) { lock (gate) return Read(paths.Project(id)); }
    public object[] List()
    {
        lock (gate) return Directory.EnumerateFiles(paths.Projects, "*.atlasled").Select(file =>
        {
            try
            {
                var p = Read(file);
                var modified = p["modifiedUtc"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(modified) || !DateTimeOffset.TryParse(modified, out _)) throw new InvalidDataException("Fecha de modificación inválida.");
                return new { id = p["id"]!.GetValue<string>(), name = p["name"]!.GetValue<string>(), matrix = p["matrix"]!.DeepClone(), modifiedUtc = modified };
            }
            catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or FormatException or NullReferenceException or System.Text.Json.JsonException or IOException)
            {
                return null;
            }
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
        lock (gate)
        {
            var autosave = paths.Autosave(id);
            if (!File.Exists(autosave)) return null;
            var project = paths.Project(id);
            var autosaveTime = File.GetLastWriteTimeUtc(autosave);
            var projectTime = File.Exists(project) ? File.GetLastWriteTimeUtc(project) : DateTime.MinValue;
            return autosaveTime > projectTime ? Read(autosave) : null;
        }
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
