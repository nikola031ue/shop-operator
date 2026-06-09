using KubeOps.Operator;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddKubernetesOperator(settings =>
    {
        settings.Name = "shop-operator";
    });

var app = builder.Build();

await app.RunAsync();
