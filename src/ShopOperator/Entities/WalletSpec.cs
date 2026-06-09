using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using k8s.Models;

namespace ShopOperator.Entities;

public class WalletSpec
{
    [Required]
    public Blockchain Blockchain { get; set; }

    [Required]
    public Network Network { get; set; }

    /// <summary>Reference to the Secret where the generated private key will be stored.</summary>
    [Required]
    public V1SecretReference SecretRef { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Blockchain
{
    ethereum,
    polygon,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Network
{
    mainnet,
    testnet,
}
