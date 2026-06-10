using System.Text;
using k8s.Models;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using ShopOperator.Entities;
using ShopOperator.Services;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(WalletEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get | RbacVerb.Create | RbacVerb.Update)]
public class WalletController : IEntityController<WalletEntity>
{
    private readonly IKubernetesClient _client;
    private readonly IWalletGenerator _walletGenerator;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IKubernetesClient client,
        IWalletGenerator walletGenerator,
        ILogger<WalletController> logger)
    {
        _client = client;
        _walletGenerator = walletGenerator;
        _logger = logger;
    }

    public async Task ReconcileAsync(WalletEntity entity, CancellationToken cancellationToken)
    {
        var name = entity.Metadata.Name;
        _logger.LogInformation("Reconciling Wallet {Name}", name);

        // Idempotency — skip if wallet was already created in a previous reconcile.
        if (!string.IsNullOrEmpty(entity.Status.Address))
        {
            _logger.LogInformation("Wallet {Name} already provisioned (address={Address}), skipping", name, entity.Status.Address);
            return;
        }

        var credentials = _walletGenerator.Generate();

        await SaveWalletSecretAsync(entity, credentials, cancellationToken);

        entity.Status.Address = credentials.Address;
        entity.Status.Ready = true;
        await _client.UpdateStatusAsync(entity, cancellationToken);

        _logger.LogInformation(
            "Wallet {Name} provisioned on {Blockchain}/{Network} — address={Address}",
            name,
            entity.Spec.Blockchain,
            entity.Spec.Network,
            credentials.Address);
    }

    public Task DeletedAsync(WalletEntity entity, CancellationToken cancellationToken)
    {
        // The wallet Secret is cascade-deleted via owner reference.
        return Task.CompletedTask;
    }

    private async Task SaveWalletSecretAsync(WalletEntity owner, WalletCredentials credentials, CancellationToken ct)
    {
        var secretRef = owner.Spec.SecretRef;
        var ns = string.IsNullOrEmpty(secretRef.NamespaceProperty)
            ? owner.Metadata.NamespaceProperty
            : secretRef.NamespaceProperty;

        var secret = new V1Secret
        {
            Metadata = new V1ObjectMeta
            {
                Name = secretRef.Name,
                NamespaceProperty = ns,
            },
            Type = "Opaque",
            Data = new Dictionary<string, byte[]>
            {
                ["address"] = Encoding.UTF8.GetBytes(credentials.Address),
                ["privateKey"] = Encoding.UTF8.GetBytes(credentials.PrivateKey),
            },
        };
        secret.WithOwnerReference(owner);
        await _client.SaveAsync(secret, ct);
    }
}
