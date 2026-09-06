using AtlasLetrero.Domain;
using AtlasLetrero.Infrastructure;
using MySqlConnector;
using Xunit;

namespace AtlasLetrero.Infrastructure.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "atlas-persistence-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SceneAndActiveIdentitySurviveStoreRecreation()
    {
        var scene = Scene();
        var first = new AtomicFileSceneStore(_root);
        await first.SaveAsync(scene); await first.SaveActiveSceneIdAsync(scene.Id);
        var restarted = new AtomicFileSceneStore(_root);
        var loaded = await restarted.LoadAsync(scene.Id);
        Assert.NotNull(loaded);
        Assert.Equal(scene.Name, loaded.Name);
        Assert.Equal(scene.Id, await restarted.LoadActiveSceneIdAsync());
    }

    [Fact]
    public async Task UpdatingSceneAtomicallyReplacesPriorPayload()
    {
        var original = Scene();
        var updated = new Scene(original.Id, "Actualizada", 2, 2, TimeSpan.FromSeconds(1),
            [new("content", [new PixelElement(1, 1, new(0, 255, 0))])]);
        var store = new AtomicFileSceneStore(_root);
        await store.SaveAsync(original); await store.SaveAsync(updated);
        Assert.Equal("Actualizada", (await store.LoadAsync(original.Id))!.Name);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp-*"));
    }

    [Fact]
    public async Task CorruptSceneIsDetectedInsteadOfPlayed()
    {
        var scene = Scene(); var store = new AtomicFileSceneStore(_root); await store.SaveAsync(scene);
        var path = Directory.GetFiles(_root, "scene-*.json").Single();
        await File.WriteAllTextAsync(path, "{\"sha256\":\"00\",\"payload\":\"AA==\"}");
        await Assert.ThrowsAsync<InvalidDataException>(async () => await store.LoadAsync(scene.Id));
    }

    [Fact]
    public async Task MissingStateAndSceneReturnNull()
    {
        var store = new AtomicFileSceneStore(_root);
        Assert.Null(await store.LoadAsync(Guid.NewGuid()));
        Assert.Null(await store.LoadActiveSceneIdAsync());
    }

    [Fact]
    public void MySqlStoreRequiresDatabaseAndNeverNeedsEmbeddedCredentials()
    {
        Assert.Throws<ArgumentException>(() => new MySqlSceneStore("Server=localhost;User ID=test;Password=test"));
        var store = new MySqlSceneStore("Server=localhost;Database=AtlasLetreros;User ID=test;Password=test");
        Assert.NotNull(store);
    }

    [Fact]
    public async Task MySqlRoundTripWhenLocalConnectionIsProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable("ATLAS_MYSQL_TEST");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var target = new MySqlConnectionStringBuilder(connectionString);
        var database = target.Database;
        if (string.IsNullOrWhiteSpace(database) || database.Any(character => !(char.IsLetterOrDigit(character) || character == '_')))
            throw new InvalidOperationException("Test database name is invalid.");
        var server = new MySqlConnectionStringBuilder(connectionString) { Database = string.Empty };
        await using (var connection = new MySqlConnection(server.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await command.ExecuteNonQueryAsync();
        }
        var store = new MySqlSceneStore(target.ConnectionString);
        await store.InitializeAsync();
        var scene = Scene();
        await store.SaveAsync(scene); await store.SaveActiveSceneIdAsync(scene.Id);
        Assert.Equal(scene.Name, (await store.LoadAsync(scene.Id))!.Name);
        Assert.Equal(scene.Id, await store.LoadActiveSceneIdAsync());
    }

    private static Scene Scene() => new(Guid.NewGuid(), "Persistente", 2, 2, TimeSpan.FromSeconds(1),
        [new("content", [new PixelElement(0, 0, new(255, 0, 0))])]);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }
}
