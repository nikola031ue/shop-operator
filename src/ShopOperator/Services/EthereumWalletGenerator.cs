using Nethereum.Signer;

namespace ShopOperator.Services;

public class EthereumWalletGenerator : IWalletGenerator
{
    public WalletCredentials Generate()
    {
        var key = EthECKey.GenerateKey();
        return new WalletCredentials(
            Address: key.GetPublicAddress(),
            PrivateKey: key.GetPrivateKey());
    }
}
