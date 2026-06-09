FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /workspace
COPY . .
RUN dotnet publish src/ShopOperator/ShopOperator.csproj \
    -c Release \
    -o /app \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:9.0-jammy-chiseled
WORKDIR /app
COPY --from=builder /app .
USER app
ENTRYPOINT ["./ShopOperator"]
