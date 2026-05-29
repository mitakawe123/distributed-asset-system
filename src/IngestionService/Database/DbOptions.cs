namespace IngestionService.Database;

public sealed class DbOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = default!;
}
