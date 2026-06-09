namespace ShopOperator.Entities;

public class DiscordChannelStatus
{
    public string? WebhookURL { get; set; }
    public string? ChannelID { get; set; }
    public bool Ready { get; set; }
}
