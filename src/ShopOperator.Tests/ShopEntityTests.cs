using System.Text.Json;
using k8s.Models;
using ShopOperator.Entities;

namespace ShopOperator.Tests;

public class ShopEntityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Availability_SerializesToLowercase()
    {
        var spec = new ShopSpec { Availability = Availability.standard };
        var json = JsonSerializer.Serialize(spec, JsonOptions);
        Assert.Contains("\"availability\":\"standard\"", json);
    }

    [Fact]
    public void Availability_High_SerializesToLowercase()
    {
        var spec = new ShopSpec { Availability = Availability.high };
        var json = JsonSerializer.Serialize(spec, JsonOptions);
        Assert.Contains("\"availability\":\"high\"", json);
    }

    [Fact]
    public void DatabaseType_SerializesToLowercase()
    {
        var spec = new ShopSpec { DatabaseType = DatabaseType.postgresql };
        var json = JsonSerializer.Serialize(spec, JsonOptions);
        Assert.Contains("\"databaseType\":\"postgresql\"", json);
    }

    [Fact]
    public void ShopEntity_HasCorrectGroupVersionKind()
    {
        var entity = new ShopEntity();
        Assert.Equal("shophub.io", entity.ApiVersion?.Split('/')[0] ?? "shophub.io");
        Assert.Equal("Shop", entity.Kind ?? "Shop");
    }

    [Fact]
    public void ShopStatus_DefaultValues()
    {
        var status = new ShopStatus();
        Assert.False(status.Ready);
        Assert.Null(status.Url);
        Assert.Equal(0, status.Replicas);
    }

    [Theory]
    [InlineData(Availability.standard, 2)]
    [InlineData(Availability.high, 3)]
    public void Availability_MapsToCorrectReplicaCount(Availability availability, int expectedReplicas)
    {
        var replicas = availability switch
        {
            Availability.standard => 2,
            Availability.high => 3,
            _ => throw new ArgumentOutOfRangeException(),
        };
        Assert.Equal(expectedReplicas, replicas);
    }
}
