using System.Text;
using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShopOperator.Controllers;
using ShopOperator.Entities;
using ShopOperator.Services;

namespace ShopOperator.Tests;

public class DiscordChannelControllerTests
{
    private readonly IKubernetesClient _client = Substitute.For<IKubernetesClient>();
    private readonly IDiscordApiClient _discord = Substitute.For<IDiscordApiClient>();
    private readonly DiscordChannelController _controller;

    private const string BotToken = "test-bot-token";
    private const string ChannelId = "111222333444";
    private const string WebhookUrl = "https://discord.com/api/webhooks/123/abc";

    public DiscordChannelControllerTests()
    {
        _controller = new DiscordChannelController(
            _client,
            _discord,
            NullLogger<DiscordChannelController>.Instance);
    }

    private static DiscordChannelEntity BuildEntity(string? existingChannelId = null) => new()
    {
        Metadata = new V1ObjectMeta { Name = "test-channel", NamespaceProperty = "default" },
        Spec = new DiscordChannelSpec
        {
            ServerID = "999888777666",
            ChannelName = "alerts",
            WebhookSecretRef = new V1SecretKeySelector { Name = "discord-secret", Key = "token" },
        },
        Status = new DiscordChannelStatus { ChannelID = existingChannelId },
    };

    private void SetupBotTokenSecret()
    {
        _client.GetAsync<V1Secret>("discord-secret", "default", Arg.Any<CancellationToken>())
            .Returns(new V1Secret
            {
                Data = new Dictionary<string, byte[]>
                {
                    ["token"] = Encoding.UTF8.GetBytes(BotToken),
                },
            });
    }

    [Fact]
    public async Task Reconcile_AlreadyProvisioned_SkipsApiCalls()
    {
        var entity = BuildEntity(existingChannelId: ChannelId);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _discord.DidNotReceive().CreateChannelAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_SecretNotFound_Throws()
    {
        var entity = BuildEntity();
        _client.GetAsync<V1Secret>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((V1Secret?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.ReconcileAsync(entity, CancellationToken.None));
    }

    [Fact]
    public async Task Reconcile_MissingTokenKey_Throws()
    {
        var entity = BuildEntity();
        _client.GetAsync<V1Secret>("discord-secret", "default", Arg.Any<CancellationToken>())
            .Returns(new V1Secret { Data = new Dictionary<string, byte[]>() });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.ReconcileAsync(entity, CancellationToken.None));
    }

    [Fact]
    public async Task Reconcile_HappyPath_CallsDiscordApiWithCorrectArgs()
    {
        var entity = BuildEntity();
        SetupBotTokenSecret();
        _discord.CreateChannelAsync("999888777666", "alerts", BotToken, Arg.Any<CancellationToken>())
            .Returns(ChannelId);
        _discord.CreateWebhookAsync(ChannelId, "test-channel", BotToken, Arg.Any<CancellationToken>())
            .Returns(WebhookUrl);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _discord.Received(1).CreateChannelAsync("999888777666", "alerts", BotToken, Arg.Any<CancellationToken>());
        await _discord.Received(1).CreateWebhookAsync(ChannelId, "test-channel", BotToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_HappyPath_SavesWebhookSecret()
    {
        var entity = BuildEntity();
        SetupBotTokenSecret();
        _discord.CreateChannelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ChannelId);
        _discord.CreateWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(WebhookUrl);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        await _client.Received(1).SaveAsync(
            Arg.Is<V1Secret>(s =>
                s.Metadata.Name == "test-channel-webhook" &&
                s.Data["webhookUrl"].SequenceEqual(Encoding.UTF8.GetBytes(WebhookUrl))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reconcile_HappyPath_UpdatesStatus()
    {
        var entity = BuildEntity();
        SetupBotTokenSecret();
        _discord.CreateChannelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ChannelId);
        _discord.CreateWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(WebhookUrl);

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.Equal(ChannelId, entity.Status.ChannelID);
        Assert.Equal(WebhookUrl, entity.Status.WebhookURL);
        Assert.True(entity.Status.Ready);
        await _client.Received(1).UpdateStatusAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_WithChannelID_CallsDiscordDelete()
    {
        var entity = BuildEntity(existingChannelId: ChannelId);
        SetupBotTokenSecret();

        await _controller.DeletedAsync(entity, CancellationToken.None);

        await _discord.Received(1).DeleteChannelAsync(ChannelId, BotToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_WithoutChannelID_SkipsDiscordDelete()
    {
        var entity = BuildEntity(existingChannelId: null);

        await _controller.DeletedAsync(entity, CancellationToken.None);

        await _discord.DidNotReceive().DeleteChannelAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_DiscordApiFails_DoesNotThrow()
    {
        var entity = BuildEntity(existingChannelId: ChannelId);
        SetupBotTokenSecret();
        _discord.DeleteChannelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Discord unavailable"));

        // Should not throw — cleanup continues regardless.
        await _controller.DeletedAsync(entity, CancellationToken.None);
    }
}
