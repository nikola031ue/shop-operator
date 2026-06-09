using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace ShopOperator.Entities;

[KubernetesEntity(Group = "shophub.io", ApiVersion = "v1alpha1", Kind = "DiscordChannel", PluralName = "discordchannels")]
public class DiscordChannelEntity : CustomKubernetesEntity<DiscordChannelSpec, DiscordChannelStatus>
{
}
