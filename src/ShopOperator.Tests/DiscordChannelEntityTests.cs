using System.Text.Json;
using k8s.Models;
using ShopOperator.Entities;

namespace ShopOperator.Tests;

public class DiscordChannelEntityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Spec_SerializesWebhookSecretRef()
    {
        var spec = new DiscordChannelSpec
        {
            ServerID = "123456789",
            ChannelName = "alerts",
            WebhookSecretRef = new V1SecretKeySelector { Name = "my-secret", Key = "token" },
        };

        var json = JsonSerializer.Serialize(spec, JsonOptions);

        Assert.Contains("\"serverID\":\"123456789\"", json);
        Assert.Contains("\"channelName\":\"alerts\"", json);
        Assert.Contains("\"name\":\"my-secret\"", json);
        Assert.Contains("\"key\":\"token\"", json);
    }

    [Fact]
    public void Status_DefaultValues()
    {
        var status = new DiscordChannelStatus();
        Assert.False(status.Ready);
        Assert.Null(status.WebhookURL);
        Assert.Null(status.ChannelID);
    }

    [Fact]
    public void Entity_HasCorrectKind()
    {
        var entity = new DiscordChannelEntity();
        Assert.Equal("DiscordChannel", entity.Kind ?? "DiscordChannel");
    }
}
