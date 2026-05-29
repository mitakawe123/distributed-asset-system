using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Shared.Contracts.Messages;

namespace IngestionService.Database;

public sealed class ReadingRepository
{
    private readonly DbOptions _options;

    public ReadingRepository(IOptions<DbOptions> options)
    {
        _options = options.Value;
    }

    public async Task SaveAsync(AssetReading reading, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);

        await conn.ExecuteAsync(
            new CommandDefinition(
                commandText: """
                INSERT INTO asset_readings
                    (asset_id, timestamp, primary_value, secondary_value)
                VALUES
                    (@AssetId, @Timestamp, @PrimaryValue, @SecondaryValue)
                """,
                parameters: new
                {
                    reading.AssetId,
                    reading.Timestamp,
                    reading.PrimaryValue,
                    reading.SecondaryValue,
                },
                cancellationToken: ct));
    }
}
