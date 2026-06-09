using KubeOps.Operator;
using ShopOperator.Controllers;
using ShopOperator.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddKubernetesOperator(settings =>
    {
        settings.Name = "shop-operator";
    })
    .AddController<ShopController, ShopEntity>();

var app = builder.Build();

await app.RunAsync();
