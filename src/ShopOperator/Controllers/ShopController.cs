using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;
using ShopOperator.Entities;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(ShopEntity), Verbs = RbacVerb.All)]
public class ShopController : IEntityController<ShopEntity>
{
    private readonly ILogger<ShopController> _logger;

    public ShopController(ILogger<ShopController> logger)
    {
        _logger = logger;
    }

    public Task ReconcileAsync(ShopEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reconciling Shop {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(ShopEntity entity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleted Shop {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }
}
