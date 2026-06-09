using System.Text.Json;
using ShopOperator.Entities;

namespace ShopOperator.Tests;

public class WalletEntityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Theory]
    [InlineData(Blockchain.ethereum, "ethereum")]
    [InlineData(Blockchain.polygon, "polygon")]
    public void Blockchain_SerializesToLowercase(Blockchain blockchain, string expected)
    {
        var spec = new WalletSpec { Blockchain = blockchain };
        var json = JsonSerializer.Serialize(spec, JsonOptions);
        Assert.Contains($"\"blockchain\":\"{expected}\"", json);
    }

    [Theory]
    [InlineData(Network.mainnet, "mainnet")]
    [InlineData(Network.testnet, "testnet")]
    public void Network_SerializesToLowercase(Network network, string expected)
    {
        var spec = new WalletSpec { Network = network };
        var json = JsonSerializer.Serialize(spec, JsonOptions);
        Assert.Contains($"\"network\":\"{expected}\"", json);
    }

    [Fact]
    public void Status_DefaultValues()
    {
        var status = new WalletStatus();
        Assert.False(status.Ready);
        Assert.Null(status.Address);
    }
}
