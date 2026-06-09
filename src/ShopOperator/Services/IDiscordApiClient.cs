namespace ShopOperator.Services;

public interface IDiscordApiClient
{
    Task<string> CreateChannelAsync(string guildId, string channelName, string botToken, CancellationToken ct);
    Task<string> CreateWebhookAsync(string channelId, string webhookName, string botToken, CancellationToken ct);
    Task DeleteChannelAsync(string channelId, string botToken, CancellationToken ct);
}
