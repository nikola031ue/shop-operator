using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;
using ShopOperator.Entities;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(DiscordChannelEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(global::k8s.Models.V1Secret), Verbs = RbacVerb.Get)]
public class DiscordChannelController : IEntityController<DiscordChannelEntity>
{
    private readonly ILogger<DiscordChannelController> _logger;

    public DiscordChannelController(ILogger<DiscordChannelController> logger)
    {
        _logger = logger;
    }

    public Task ReconcileAsync(DiscordChannelEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reconciling DiscordChannel {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(DiscordChannelEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleted DiscordChannel {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }
}
