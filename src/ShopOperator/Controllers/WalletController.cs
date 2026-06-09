using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;
using ShopOperator.Entities;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(WalletEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(global::k8s.Models.V1Secret), Verbs = RbacVerb.Get | RbacVerb.Create | RbacVerb.Update)]
public class WalletController : IEntityController<WalletEntity>
{
    private readonly ILogger<WalletController> _logger;

    public WalletController(ILogger<WalletController> logger)
    {
        _logger = logger;
    }

    public Task ReconcileAsync(WalletEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reconciling Wallet {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(WalletEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleted Wallet {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }
}
