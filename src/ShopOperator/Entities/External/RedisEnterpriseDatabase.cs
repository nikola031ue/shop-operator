using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace ShopOperator.Entities.External;

[KubernetesEntity(Group = "app.redislabs.com", ApiVersion = "v1alpha1", Kind = "RedisEnterpriseDatabase", PluralName = "redisenterprisedatabases")]
public class RedisEnterpriseDatabase : CustomKubernetesEntity<RedisEnterpriseDatabaseSpec>
{
}

public class RedisEnterpriseDatabaseSpec
{
    public string MemorySize { get; set; } = "100MB";
}
