namespace ShopOperator.Services;

public record WalletCredentials(string Address, string PrivateKey);

public interface IWalletGenerator
{
    WalletCredentials Generate();
}
