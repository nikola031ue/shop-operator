using KubeOps.Operator;
using ShopOperator.Controllers;
using ShopOperator.Entities;
using ShopOperator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IDiscordApiClient, DiscordApiClient>();

builder.Services
    .AddKubernetesOperator(settings =>
    {
        settings.Name = "shop-operator";
    })
    .AddController<ShopController, ShopEntity>()
    .AddController<DiscordChannelController, DiscordChannelEntity>()
    .AddController<WalletController, WalletEntity>();

var app = builder.Build();

await app.RunAsync();
