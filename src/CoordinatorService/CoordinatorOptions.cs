namespace CoordinatorService;

public sealed class CoordinatorOptions
{
    public const string SectionName = "Coordinator";

    public string CalibrateCron { get; init; } = "0 0 * * * ?";   // every hour
    public string DiagnosticCron { get; init; } = "0 */15 * * * ?"; // every 15 min
    public int StaleReadingThresholdMins { get; init; } = 30;
    public string DatabaseConnectionString { get; init; } = default!;
}
