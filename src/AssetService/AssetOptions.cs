using System.ComponentModel.DataAnnotations;

namespace AssetService;

public sealed class AssetOptions
{
    public const string SectionName = "Asset";

    [Required]
    public string Id { get; init; } = "asset-local";

    public int ReportingIntervalSecs { get; init; } = 10;
}
