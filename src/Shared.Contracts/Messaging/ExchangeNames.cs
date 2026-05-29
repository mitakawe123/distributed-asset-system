namespace Shared.Contracts.Messaging;

public static class ExchangeNames
{
    public const string Readings = "asset.readings";   // fanout
    public const string Commands = "asset.commands";   // direct, routing key = assetId
}
