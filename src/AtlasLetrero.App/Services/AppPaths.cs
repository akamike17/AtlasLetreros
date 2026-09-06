namespace AtlasLetrero.App.Services;

public sealed class AppPaths
{
    public string Projects { get; }
    public string Recovery { get; }
    public AppPaths(IConfiguration configuration)
    {
        var testRoot = configuration["Atlas:DataRoot"];
        Projects = testRoot is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtlasLetrero", "Projects") : Path.Combine(testRoot, "Projects");
        Recovery = testRoot is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtlasLetrero", "Recovery") : Path.Combine(testRoot, "Recovery");
        Directory.CreateDirectory(Projects);
        Directory.CreateDirectory(Recovery);
    }
    public string Project(Guid id) => Path.Combine(Projects, id + ".atlasled");
    public string Autosave(Guid id) => Path.Combine(Recovery, id + ".atlasled");
}
