using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IngestionService.Database;


public sealed class DbInitializer
{
    private readonly DbOptions _options;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IOptions<DbOptions> options, ILogger<DbInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);

        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS asset_readings (
                id              BIGSERIAL PRIMARY KEY,
                asset_id        TEXT             NOT NULL,
                timestamp       TIMESTAMPTZ      NOT NULL,
                primary_value   DOUBLE PRECISION NOT NULL,
                secondary_value DOUBLE PRECISION NULL,
                ingested_at     TIMESTAMPTZ      NOT NULL DEFAULT now()
            );

            CREATE INDEX IF NOT EXISTS ix_asset_readings_asset_id
                ON asset_readings (asset_id);

            CREATE INDEX IF NOT EXISTS ix_asset_readings_timestamp
                ON asset_readings (timestamp DESC);
            """);

        _logger.LogInformation("Database initialized.");
    }
}
