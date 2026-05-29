using CoordinatorService.Messaging;
using CoordinatorService.Registry;
using Quartz;
using Shared.Contracts.Messages;

namespace CoordinatorService.Jobs;

[DisallowConcurrentExecution]
public sealed class DiagnosticCheckJob(AssetRegistry registry, CommandPublisherAccessor accessor) : IJob
{
    private readonly AssetRegistry _registry = registry;
    private readonly CommandPublisherAccessor _accessor = accessor;

    public async Task Execute(IJobExecutionContext context)
    {
        var staleIds = await _registry.GetStaleAssetIdsAsync(context.CancellationToken);

        if (staleIds.Count == 0)
        {
            return;
        }

        var publisher = _accessor.Publisher
            ?? throw new InvalidOperationException("Publisher not ready.");

        await publisher.PublishToAllAsync(
            staleIds,
            CommandType.RunDiagnostic,
            ct: context.CancellationToken);
    }
}