using System.Text;
using k8s.Models;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using ShopOperator.Entities;
using ShopOperator.Services;

namespace ShopOperator.Controllers;

[EntityRbac(typeof(DiscordChannelEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get | RbacVerb.Create | RbacVerb.Update)]
public class DiscordChannelController : IEntityController<DiscordChannelEntity>
{
    private readonly IKubernetesClient _client;
    private readonly IDiscordApiClient _discord;
    private readonly ILogger<DiscordChannelController> _logger;

    public DiscordChannelController(
        IKubernetesClient client,
        IDiscordApiClient discord,
        ILogger<DiscordChannelController> logger)
    {
        _client = client;
        _discord = discord;
        _logger = logger;
    }

    public async Task ReconcileAsync(DiscordChannelEntity entity, CancellationToken cancellationToken)
    {
        var name = entity.Metadata.Name;
        _logger.LogInformation("Reconciling DiscordChannel {Name}", name);

        // Idempotency — skip if channel was already created in a previous reconcile.
        if (!string.IsNullOrEmpty(entity.Status.ChannelID))
        {
            _logger.LogInformation("DiscordChannel {Name} already provisioned (channelID={ID}), skipping", name, entity.Status.ChannelID);
            return;
        }

        var botToken = await ReadBotTokenAsync(entity, cancellationToken);

        var channelId = await _discord.CreateChannelAsync(
            entity.Spec.ServerID,
            entity.Spec.ChannelName,
            botToken,
            cancellationToken);

        var webhookUrl = await _discord.CreateWebhookAsync(
            channelId,
            name,
            botToken,
            cancellationToken);

        await SaveWebhookSecretAsync(entity, webhookUrl, cancellationToken);

        entity.Status.ChannelID = channelId;
        entity.Status.WebhookURL = webhookUrl;
        entity.Status.Ready = true;
        await _client.UpdateStatusAsync(entity, cancellationToken);

        _logger.LogInformation("DiscordChannel {Name} provisioned (channelID={ID})", name, channelId);
    }

    public async Task DeletedAsync(DiscordChannelEntity entity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.Status.ChannelID))
            return;

        _logger.LogInformation("Deleting Discord channel {ChannelID}", entity.Status.ChannelID);

        try
        {
            var botToken = await ReadBotTokenAsync(entity, cancellationToken);
            await _discord.DeleteChannelAsync(entity.Status.ChannelID, botToken, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log and continue — the K8s Secret with webhook URL will be garbage-collected
            // via owner reference regardless of whether the Discord API call succeeds.
            _logger.LogWarning(ex, "Failed to delete Discord channel {ChannelID}, continuing cleanup", entity.Status.ChannelID);
        }
    }

    private async Task<string> ReadBotTokenAsync(DiscordChannelEntity entity, CancellationToken ct)
    {
        var secretRef = entity.Spec.WebhookSecretRef;
        var secret = await _client.GetAsync<V1Secret>(
            secretRef.Name,
            entity.Metadata.NamespaceProperty,
            ct);

        if (secret is null)
            throw new InvalidOperationException($"Secret '{secretRef.Name}' not found in namespace '{entity.Metadata.NamespaceProperty}'");

        if (secret.Data is null || !secret.Data.TryGetValue(secretRef.Key, out var tokenBytes))
            throw new InvalidOperationException($"Key '{secretRef.Key}' not found in Secret '{secretRef.Name}'");

        return Encoding.UTF8.GetString(tokenBytes);
    }

    private async Task SaveWebhookSecretAsync(DiscordChannelEntity owner, string webhookUrl, CancellationToken ct)
    {
        var secret = new V1Secret
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"{owner.Metadata.Name}-webhook",
                NamespaceProperty = owner.Metadata.NamespaceProperty,
            },
            Data = new Dictionary<string, byte[]>
            {
                ["webhookUrl"] = Encoding.UTF8.GetBytes(webhookUrl),
            },
        };
        secret.WithOwnerReference(owner);
        await _client.SaveAsync(secret, ct);
    }
}
