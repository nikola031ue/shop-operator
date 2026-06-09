using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShopOperator.Entities;

public class ShopSpec
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Availability Availability { get; set; }

    [Required]
    public string WalletAddress { get; set; } = string.Empty;

    [Required]
    public DatabaseType DatabaseType { get; set; }

    [Required]
    public string Image { get; set; } = string.Empty;

    [Required]
    public string IngressHost { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Availability
{
    standard,
    high,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseType
{
    postgresql,
    redis,
}
