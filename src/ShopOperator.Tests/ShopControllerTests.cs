using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ShopOperator.Controllers;
using ShopOperator.Entities;
using ShopOperator.Entities.External;

namespace ShopOperator.Tests;

public class ShopControllerTests
{
    private readonly IKubernetesClient _client = Substitute.For<IKubernetesClient>();
    private readonly ShopController _controller;

    public ShopControllerTests()
    {
        _controller = new ShopController(_client, NullLogger<ShopController>.Instance);
    }

    private static ShopEntity BuildEntity(
        string name = "test-shop",
        Availability availability = Availability.standard,
        DatabaseType dbType = DatabaseType.postgresql) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = "default" },
        Spec = new ShopSpec
        {
            Name = name,
            Availability = availability,
            WalletAddress = "0xabc",
            DatabaseType = dbType,
            Image = "shop:latest",
            IngressHost = $"{name}.local",
        },
        Status = new ShopStatus(),
    };

    [Fact]
    public async Task Reconcile_StandardAvailability_Creates2ReplicaDeployment()
    {
        var entity = BuildEntity(availability: Availability.standard);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Deployment>(d => d.Spec.Replicas == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_HighAvailability_Creates3ReplicaDeployment()
    {
        var entity = BuildEntity(availability: Availability.high);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Deployment>(d => d.Spec.Replicas == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_CreatesServiceWithCorrectSelector()
    {
        var entity = BuildEntity(name: "my-shop");

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Service>(s =>
                s.Spec.Selector["app"] == "my-shop" &&
                s.Spec.Ports[0].Port == 80),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_CreatesIngressWithCorrectHost()
    {
        var entity = BuildEntity(name: "my-shop");

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Ingress>(i => i.Spec.Rules[0].Host == "my-shop.local"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_PostgresqlDatabase_CreatesCnpgCluster()
    {
        var entity = BuildEntity(dbType: DatabaseType.postgresql);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Any<CnpgCluster>(),
            Arg.Any<CancellationToken>());
        await _client.DidNotReceive().SaveAsync(
            Arg.Any<RedisEnterpriseDatabase>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_RedisDatabase_CreatesRedisEnterpriseDatabase()
    {
        var entity = BuildEntity(dbType: DatabaseType.redis);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Any<RedisEnterpriseDatabase>(),
            Arg.Any<CancellationToken>());
        await _client.DidNotReceive().SaveAsync(
            Arg.Any<CnpgCluster>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(Availability.standard, 2)]
    [InlineData(Availability.high, 3)]
    public async Task Reconcile_SetsCorrectStatusReplicas(Availability availability, int expectedReplicas)
    {
        var entity = BuildEntity(availability: availability);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.Equal(expectedReplicas, entity.Status.Replicas);
    }

    [Fact]
    public async Task Reconcile_SetsStatusUrlFromIngressHost()
    {
        var entity = BuildEntity(name: "my-shop");

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.Equal("https://my-shop.local", entity.Status.Url);
    }

    [Fact]
    public async Task Reconcile_SetsStatusReady()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.True(entity.Status.Ready);
        await _client.Received(1).UpdateStatusAsync(
            Arg.Is<ShopEntity>(e => e.Status.Ready),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_DoesNotCallClientDelete()
    {
        var entity = BuildEntity();

        await _controller.DeletedAsync(entity, CancellationToken.None);

        await _client.DidNotReceive().DeleteAsync(
            Arg.Any<ShopEntity>(),
            Arg.Any<CancellationToken>());
    }
}
