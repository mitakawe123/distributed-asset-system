namespace Shared.Contracts.Messages;

public sealed record AssetCommand(
    string AssetId,
    CommandType Type,
    DateTimeOffset IssuedAt,
    IReadOnlyDictionary<string, string>? Parameters = null);

public enum CommandType
{
    RunDiagnostic,
    Calibrate,
    ChangeReportingFrequency
}
