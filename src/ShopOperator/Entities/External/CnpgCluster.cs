using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace ShopOperator.Entities.External;

[KubernetesEntity(Group = "postgresql.cnpg.io", ApiVersion = "v1", Kind = "Cluster", PluralName = "clusters")]
public class CnpgCluster : CustomKubernetesEntity<CnpgClusterSpec>
{
}

public class CnpgClusterSpec
{
    public int Instances { get; set; } = 1;
    public CnpgStorageConfig Storage { get; set; } = new();
}

public class CnpgStorageConfig
{
    public string Size { get; set; } = "1Gi";
}
