using System.ComponentModel.DataAnnotations;
using k8s.Models;

namespace ShopOperator.Entities;

public class DiscordChannelSpec
{
    [Required]
    public string ServerID { get; set; } = string.Empty;

    [Required]
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>Reference to the Kubernetes Secret containing the Discord bot token.</summary>
    [Required]
    public V1SecretKeySelector WebhookSecretRef { get; set; } = new();
}
