namespace CoordinatorService.Messaging;

/// <summary>
/// Holds the async-initialized CommandPublisher so Quartz jobs can resolve it via DI.
/// </summary>
public sealed class CommandPublisherAccessor
{
    public CommandPublisher? Publisher { get; set; }
}
