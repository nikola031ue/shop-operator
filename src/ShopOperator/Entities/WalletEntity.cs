using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace ShopOperator.Entities;

[KubernetesEntity(Group = "shophub.io", ApiVersion = "v1alpha1", Kind = "Wallet", PluralName = "wallets")]
public class WalletEntity : CustomKubernetesEntity<WalletSpec, WalletStatus>
{
}
