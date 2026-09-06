using System.Security.Cryptography;
using System.Text.Json;
using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;

namespace AtlasLetrero.Infrastructure;

public sealed class AtomicFileSceneStore : ISceneStore
{
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AtomicFileSceneStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Storage path is required.", nameof(root));
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async ValueTask SaveAsync(Scene scene, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var payload = SceneProtocolCodec.Encode(scene);
        var envelope = new StoredEnvelope(Convert.ToHexString(SHA256.HashData(payload)), payload);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, ProtocolJson.Options);
        await _gate.WaitAsync(cancellationToken);
        try { await WriteAtomicAsync(ScenePath(scene.Id), bytes, cancellationToken); }
        finally { _gate.Release(); }
    }

    public async ValueTask<Scene?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Scene identity is required.", nameof(id));
        var path = ScenePath(id);
        if (!File.Exists(path)) return null;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var envelope = JsonSerializer.Deserialize<StoredEnvelope>(await File.ReadAllBytesAsync(path, cancellationToken), ProtocolJson.Options)
                ?? throw new InvalidDataException("Stored scene envelope is empty.");
            var checksum = Convert.ToHexString(SHA256.HashData(envelope.Payload));
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(checksum), Convert.FromHexString(envelope.Sha256)))
                throw new InvalidDataException("Stored scene checksum is invalid.");
            return SceneProtocolCodec.Decode(envelope.Payload);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ProtocolException)
        { throw new InvalidDataException("Stored scene is corrupt or incompatible.", exception); }
        finally { _gate.Release(); }
    }

    public async ValueTask SaveActiveSceneIdAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new ActiveState(1, id), ProtocolJson.Options);
        await _gate.WaitAsync(cancellationToken);
        try { await WriteAtomicAsync(StatePath, bytes, cancellationToken); }
        finally { _gate.Release(); }
    }

    public async ValueTask<Guid?> LoadActiveSceneIdAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StatePath)) return null;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = JsonSerializer.Deserialize<ActiveState>(await File.ReadAllBytesAsync(StatePath, cancellationToken), ProtocolJson.Options)
                ?? throw new InvalidDataException("Stored state is empty.");
            ProtocolJson.ValidateVersion(state.ProtocolVersion);
            return state.ActiveSceneId;
        }
        catch (Exception exception) when (exception is JsonException or ProtocolException)
        { throw new InvalidDataException("Stored state is corrupt or incompatible.", exception); }
        finally { _gate.Release(); }
    }

    private string StatePath => Path.Combine(_root, "active-state.json");
    private string ScenePath(Guid id) => Path.Combine(_root, $"scene-{id:N}.json");

    private static async Task WriteAtomicAsync(string destination, byte[] bytes, CancellationToken cancellationToken)
    {
        var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            { await stream.WriteAsync(bytes, cancellationToken); await stream.FlushAsync(cancellationToken); }
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed record StoredEnvelope(string Sha256, byte[] Payload);
    private sealed record ActiveState(int ProtocolVersion, Guid? ActiveSceneId);
}
