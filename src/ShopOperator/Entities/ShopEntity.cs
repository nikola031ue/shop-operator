using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace ShopOperator.Entities;

[KubernetesEntity(Group = "shophub.io", ApiVersion = "v1alpha1", Kind = "Shop", PluralName = "shops")]
public class ShopEntity : CustomKubernetesEntity<ShopSpec, ShopStatus>
{
}
