using System.Text;
using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ShopOperator.Controllers;
using ShopOperator.Entities;
using ShopOperator.Services;

namespace ShopOperator.Tests;

public class WalletControllerTests
{
    private readonly IKubernetesClient _client = Substitute.For<IKubernetesClient>();
    private readonly IWalletGenerator _generator = Substitute.For<IWalletGenerator>();
    private readonly WalletController _controller;

    private const string TestAddress = "0xAb5801a7D398351b8bE11C439e05C5B3259aeC9B";
    private const string TestPrivateKey = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";

    public WalletControllerTests()
    {
        _controller = new WalletController(
            _client,
            _generator,
            NullLogger<WalletController>.Instance);

        _generator.Generate().Returns(new WalletCredentials(TestAddress, TestPrivateKey));
    }

    private static WalletEntity BuildEntity(string? existingAddress = null) => new()
    {
        Metadata = new V1ObjectMeta { Name = "test-wallet", NamespaceProperty = "default" },
        Spec = new WalletSpec
        {
            Blockchain = Blockchain.ethereum,
            Network = Network.testnet,
            SecretRef = new V1SecretReference { Name = "test-wallet-key", NamespaceProperty = "default" },
        },
        Status = new WalletStatus { Address = existingAddress },
    };

    [Fact]
    public async Task Reconcile_AlreadyProvisioned_SkipsGeneration()
    {
        var entity = BuildEntity(existingAddress: TestAddress);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        _generator.DidNotReceive().Generate();
    }

    [Fact]
    public async Task Reconcile_AlreadyProvisioned_DoesNotSaveSecret()
    {
        var entity = BuildEntity(existingAddress: TestAddress);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.DidNotReceive().SaveAsync(Arg.Any<V1Secret>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_NewWallet_CallsGenerator()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        _generator.Received(1).Generate();
    }

    [Fact]
    public async Task Reconcile_NewWallet_SavesSecretWithCorrectName()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Secret>(s => s.Metadata.Name == "test-wallet-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_NewWallet_SecretContainsAddressAndPrivateKey()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Secret>(s =>
                s.Data["address"].SequenceEqual(Encoding.UTF8.GetBytes(TestAddress)) &&
                s.Data["privateKey"].SequenceEqual(Encoding.UTF8.GetBytes(TestPrivateKey))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_NewWallet_UpdatesStatusAddress()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.Equal(TestAddress, entity.Status.Address);
    }

    [Fact]
    public async Task Reconcile_NewWallet_SetsStatusReady()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.True(entity.Status.Ready);
    }

    [Fact]
    public async Task Reconcile_NewWallet_CallsUpdateStatus()
    {
        var entity = BuildEntity();

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).UpdateStatusAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_SecretRefWithoutNamespace_UsesEntityNamespace()
    {
        var entity = BuildEntity();
        entity.Spec.SecretRef.NamespaceProperty = null;

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Secret>(s => s.Metadata.NamespaceProperty == "default"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_DoesNotCallClientDelete()
    {
        var entity = BuildEntity(existingAddress: TestAddress);

        await _controller.DeletedAsync(entity, CancellationToken.None);

        await _client.DidNotReceive().DeleteAsync<V1Secret>(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
