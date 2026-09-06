namespace AtlasLetrero.App.Services;
public sealed class AppPaths {
    public string Root { get; }
    public string Projects { get; }
    public string Autosave { get; }
    public AppPaths() {
        Root = Environment.GetEnvironmentVariable("ATLAS_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtlasLetrero");
        Projects = Environment.GetEnvironmentVariable("ATLAS_PROJECTS_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtlasLetrero", "Projects");
        Autosave = Path.Combine(Root, "Autosave");
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Projects); Directory.CreateDirectory(Autosave);
    }
    public string Project(string id, bool autosave = false) { if (!Guid.TryParse(id, out var guid)) throw new ArgumentException("Identificador de proyecto inválido."); return Path.Combine(autosave ? Autosave : Projects, guid + ".atlasled"); }
}
