namespace Shared.Contracts.Messages;

public sealed record AssetReading(
    string AssetId,
    DateTimeOffset Timestamp,
    double PrimaryValue,
    double? SecondaryValue);
