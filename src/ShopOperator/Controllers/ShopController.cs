using k8s.Models;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using ShopOperator.Entities;
using ShopOperator.Entities.External;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(ShopEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Service), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Ingress), Verbs = RbacVerb.All)]
[EntityRbac(typeof(CnpgCluster), Verbs = RbacVerb.All)]
[EntityRbac(typeof(RedisEnterpriseDatabase), Verbs = RbacVerb.All)]
public class ShopController : IEntityController<ShopEntity>
{
    private readonly IKubernetesClient _client;
    private readonly ILogger<ShopController> _logger;

    public ShopController(IKubernetesClient client, ILogger<ShopController> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task ReconcileAsync(ShopEntity entity, CancellationToken cancellationToken)
    {
        var name = entity.Metadata.Name;
        var ns = entity.Metadata.NamespaceProperty;
        _logger.LogInformation("Reconciling Shop {Name}", name);

        var replicas = entity.Spec.Availability == Availability.standard ? 2 : 3;

        await SaveDeploymentAsync(entity, name, ns, replicas, cancellationToken);
        await SaveServiceAsync(entity, name, ns, cancellationToken);
        await SaveIngressAsync(entity, name, ns, cancellationToken);
        await SaveDatabaseAsync(entity, name, ns, cancellationToken);

        entity.Status.Replicas = replicas;
        entity.Status.Url = $"https://{entity.Spec.IngressHost}";
        entity.Status.Ready = true;
        await _client.UpdateStatusAsync(entity, cancellationToken);
    }

    public Task DeletedAsync(ShopEntity entity, CancellationToken cancellationToken)
    {
        // Owner references on all child resources ensure K8s garbage collects them automatically.
        _logger.LogInformation("Deleted Shop {Name}", entity.Metadata.Name);
        return Task.CompletedTask;
    }

    private async Task SaveDeploymentAsync(
        ShopEntity owner, string name, string ns, int replicas, CancellationToken ct)
    {
        var labels = new Dictionary<string, string> { ["app"] = name };

        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, Labels = labels },
            Spec = new V1DeploymentSpec
            {
                Replicas = replicas,
                Selector = new V1LabelSelector { MatchLabels = labels },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = new V1PodSpec
                    {
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "shop",
                                Image = owner.Spec.Image,
                                Ports = [new V1ContainerPort { ContainerPort = 8080, Name = "http" }],
                            },
                        ],
                    },
                },
            },
        };
        deployment.WithOwnerReference(owner);
        await _client.SaveAsync(deployment, ct);
    }

    private async Task SaveServiceAsync(ShopEntity owner, string name, string ns, CancellationToken ct)
    {
        var service = new V1Service
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string> { ["app"] = name },
                Ports =
                [
                    new V1ServicePort { Name = "http", Port = 80, TargetPort = (IntOrString)8080, Protocol = "TCP" },
                ],
            },
        };
        service.WithOwnerReference(owner);
        await _client.SaveAsync(service, ct);
    }

    private async Task SaveIngressAsync(ShopEntity owner, string name, string ns, CancellationToken ct)
    {
        var ingress = new V1Ingress
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1IngressSpec
            {
                Rules =
                [
                    new V1IngressRule
                    {
                        Host = owner.Spec.IngressHost,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths =
                            [
                                new V1HTTPIngressPath
                                {
                                    Path = "/",
                                    PathType = "Prefix",
                                    Backend = new V1IngressBackend
                                    {
                                        Service = new V1IngressServiceBackend
                                        {
                                            Name = name,
                                            Port = new V1ServiceBackendPort { Number = 80 },
                                        },
                                    },
                                },
                            ],
                        },
                    },
                ],
            },
        };
        ingress.WithOwnerReference(owner);
        await _client.SaveAsync(ingress, ct);
    }

    private async Task SaveDatabaseAsync(ShopEntity owner, string name, string ns, CancellationToken ct)
    {
        var dbName = $"{name}-db";

        if (owner.Spec.DatabaseType == DatabaseType.postgresql)
        {
            var cluster = new CnpgCluster
            {
                Metadata = new V1ObjectMeta { Name = dbName, NamespaceProperty = ns },
                Spec = new CnpgClusterSpec
                {
                    Instances = 1,
                    Storage = new CnpgStorageConfig { Size = "1Gi" },
                },
            };
            cluster.WithOwnerReference(owner);
            await _client.SaveAsync(cluster, ct);
        }
        else
        {
            var redb = new RedisEnterpriseDatabase
            {
                Metadata = new V1ObjectMeta { Name = dbName, NamespaceProperty = ns },
                Spec = new RedisEnterpriseDatabaseSpec { MemorySize = "100MB" },
            };
            redb.WithOwnerReference(owner);
            await _client.SaveAsync(redb, ct);
        }
    }
}
