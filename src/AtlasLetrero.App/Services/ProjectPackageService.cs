using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace AtlasLetrero.App.Services;
public sealed class ProjectPackageService(AtomicFileWriter writer) {
    public static void Validate(JsonObject p) {
        if (p["formatVersion"]?.GetValue<int>() != 1) throw new InvalidDataException("Versión de proyecto no compatible.");
        if (!Guid.TryParse(p["id"]?.GetValue<string>(), out _)) throw new InvalidDataException("Identificador inválido.");
        if (string.IsNullOrWhiteSpace(p["name"]?.GetValue<string>())) throw new InvalidDataException("Escribe un nombre de proyecto.");
        var m = p["matrixConfiguration"]!;
        int w = m["width"]!.GetValue<int>(), h = m["height"]!.GetValue<int>();
        if (w < 1 || h < 1 || w > 256 || h > 256 || w * h > 16384) throw new InvalidDataException("La matriz debe tener entre 1 y 16384 LED, con lados de hasta 256.");
        int fps = m["preferredFps"]!.GetValue<int>();
        if (fps < 1 || fps > 60) throw new InvalidDataException("Los FPS deben estar entre 1 y 60.");
        if (p["scenes"] is not JsonArray scenes || scenes.Count == 0) throw new InvalidDataException("El proyecto necesita una escena.");
        foreach (var scene in scenes) if (scene?["frames"] is not JsonArray frames || frames.Count == 0 || frames.Any(f => f?["durationMs"]?.GetValue<double>() is not >= 16 or > 60000)) throw new InvalidDataException("Cada escena necesita frames con duración válida.");
    }
    public byte[] Pack(JsonObject p) {
        Validate(p); using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true)) {
            using var manifest = zip.CreateEntry("manifest.json").Open(); JsonSerializer.Serialize(manifest, p);
            // Embedded imported images are kept inside the manifest for a self-contained roundtrip.
        }
        return output.ToArray();
    }
    public JsonObject Read(string path) { if (!File.Exists(path)) throw new FileNotFoundException("No se encontró el proyecto."); return Read(File.ReadAllBytes(path)); }
    public JsonObject Read(byte[] bytes) {
        try { using var input = new MemoryStream(bytes); using var zip = new ZipArchive(input, ZipArchiveMode.Read);
            var entry = zip.GetEntry("manifest.json") ?? throw new InvalidDataException("Falta manifest.json.");
            if (entry.Length > 32 * 1024 * 1024) throw new InvalidDataException("Proyecto demasiado grande.");
            using var stream = entry.Open(); var p = JsonNode.Parse(stream)?.AsObject() ?? throw new InvalidDataException("Proyecto vacío."); Validate(p); return p;
        } catch (JsonException) { throw new InvalidDataException("El proyecto está dañado."); }
    }
    public void Save(string path, JsonObject p) => writer.Write(path, Pack(p), t => Read(t));
}
