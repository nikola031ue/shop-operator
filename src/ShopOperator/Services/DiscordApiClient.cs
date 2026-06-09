using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShopOperator.Services;

public class DiscordApiClient : IDiscordApiClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://discord.com/api/v10";

    public DiscordApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> CreateChannelAsync(string guildId, string channelName, string botToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/guilds/{guildId}/channels");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
        request.Content = JsonContent.Create(new { name = channelName, type = 0 });

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Discord API returned channel without id");
    }

    public async Task<string> CreateWebhookAsync(string channelId, string webhookName, string botToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/channels/{channelId}/webhooks");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
        request.Content = JsonContent.Create(new { name = webhookName });

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Discord API returned webhook without url");
    }

    public async Task DeleteChannelAsync(string channelId, string botToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/channels/{channelId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
