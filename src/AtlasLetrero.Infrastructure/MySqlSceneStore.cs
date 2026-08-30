using System.Security.Cryptography;
using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;
using MySqlConnector;

namespace AtlasLetrero.Infrastructure;

public sealed class MySqlSceneStore : ISceneStore
{
    private readonly string _connectionString;
    public MySqlSceneStore(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required.", nameof(connectionString));
        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database)) throw new ArgumentException("A database must be specified.", nameof(connectionString));
        _connectionString = builder.ConnectionString;
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS atlas_scenes (
              id CHAR(36) NOT NULL PRIMARY KEY,
              payload LONGBLOB NOT NULL,
              sha256 BINARY(32) NOT NULL,
              updated_utc TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
            );
            CREATE TABLE IF NOT EXISTS atlas_runtime_state (
              singleton_id TINYINT NOT NULL PRIMARY KEY,
              protocol_version INT NOT NULL,
              active_scene_id CHAR(36) NULL,
              CONSTRAINT chk_atlas_singleton CHECK (singleton_id = 1)
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask SaveAsync(Scene scene, CancellationToken cancellationToken = default)
    {
        var payload = SceneProtocolCodec.Encode(scene);
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO atlas_scenes (id, payload, sha256) VALUES (@id, @payload, @sha256)
            ON DUPLICATE KEY UPDATE payload = VALUES(payload), sha256 = VALUES(sha256), updated_utc = CURRENT_TIMESTAMP(6);
            """;
        command.Parameters.AddWithValue("@id", scene.Id.ToString());
        command.Parameters.AddWithValue("@payload", payload);
        command.Parameters.AddWithValue("@sha256", SHA256.HashData(payload));
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask<Scene?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload, sha256 FROM atlas_scenes WHERE id = @id";
        command.Parameters.AddWithValue("@id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var payload = (byte[])reader[0]; var expected = (byte[])reader[1]; var actual = SHA256.HashData(payload);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected)) throw new InvalidDataException("Stored scene checksum is invalid.");
        return SceneProtocolCodec.Decode(payload);
    }

    public async ValueTask SaveActiveSceneIdAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO atlas_runtime_state (singleton_id, protocol_version, active_scene_id) VALUES (1, @version, @id)
            ON DUPLICATE KEY UPDATE protocol_version = VALUES(protocol_version), active_scene_id = VALUES(active_scene_id);
            """;
        command.Parameters.AddWithValue("@version", ProtocolVersions.Current);
        command.Parameters.AddWithValue("@id", id?.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask<Guid?> LoadActiveSceneIdAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT protocol_version, active_scene_id FROM atlas_runtime_state WHERE singleton_id = 1";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        ProtocolJson.ValidateVersion(reader.GetInt32(0));
        if (reader.IsDBNull(1)) return null;
        return reader.GetValue(1) switch
        {
            Guid value => value,
            string value => Guid.Parse(value),
            var value => Guid.Parse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                ?? throw new InvalidDataException("Stored active scene identity is invalid."))
        };
    }
}
