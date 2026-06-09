namespace ShopOperator.Entities;

public class ShopStatus
{
    public bool Ready { get; set; }
    public string? Url { get; set; }
    public int Replicas { get; set; }
}
