using CoordinatorService.Messaging;
using CoordinatorService.Registry;
using Quartz;
using Shared.Contracts.Messages;

namespace CoordinatorService.Jobs;

[DisallowConcurrentExecution]
public sealed class CalibrateAllAssetsJob(AssetRegistry registry, CommandPublisherAccessor accessor) : IJob
{
    private readonly AssetRegistry _registry = registry;
    private readonly CommandPublisherAccessor _accessor = accessor;

    public async Task Execute(IJobExecutionContext context)
    {
        var publisher = _accessor.Publisher
            ?? throw new InvalidOperationException("Publisher not ready.");

        var assetIds = await _registry.GetAllAssetIdsAsync(context.CancellationToken);
        if (assetIds.Count == 0) return;

        await publisher.PublishToAllAsync(
            assetIds,
            CommandType.Calibrate,
            ct: context.CancellationToken);
    }
}