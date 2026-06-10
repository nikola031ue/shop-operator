FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/ShopOperator/ShopOperator.csproj ShopOperator/
RUN dotnet restore ShopOperator/ShopOperator.csproj

COPY src/ShopOperator/ ShopOperator/
RUN dotnet publish ShopOperator/ShopOperator.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "ShopOperator.dll"]
