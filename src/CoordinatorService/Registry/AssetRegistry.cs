using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoordinatorService.Registry;

public sealed class AssetRegistry(
    IOptions<CoordinatorOptions> options,
    ILogger<AssetRegistry> logger)
{
    private readonly CoordinatorOptions _options = options.Value;
    private readonly ILogger<AssetRegistry> _logger = logger;

    /// <summary>
    /// Returns all asset IDs that have sent at least one reading ever.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllAssetIdsAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_options.DatabaseConnectionString);

        var ids = await conn.QueryAsync<string>(
            new CommandDefinition(
                commandText: """
                SELECT DISTINCT asset_id
                FROM asset_readings
                ORDER BY asset_id
                """,
                cancellationToken: ct));

        var result = ids.AsList();

        _logger.LogInformation("Asset registry found {Count} asset(s).", result.Count);

        return result;
    }

    /// <summary>
    /// Returns asset IDs that have NOT reported within the stale threshold.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetStaleAssetIdsAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_options.DatabaseConnectionString);

        var cutoff = DateTimeOffset.UtcNow
            .AddMinutes(-_options.StaleReadingThresholdMins);

        var ids = await conn.QueryAsync<string>(
            new CommandDefinition(
                commandText: """
                SELECT DISTINCT asset_id
                FROM asset_readings
                GROUP BY asset_id
                HAVING MAX(timestamp) < @Cutoff
                ORDER BY asset_id
                """,
                parameters: new { Cutoff = cutoff },
                cancellationToken: ct));

        var result = ids.AsList();

        _logger.LogInformation(
            "Found {Count} stale asset(s) (no reading since {Cutoff}).",
            result.Count, cutoff);

        return result;
    }
}
