
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Directory.Build.props ./

COPY src/Inventory/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj src/Inventory/OrderFlow.Inventory.Api/
COPY src/Inventory/OrderFlow.Inventory.Application/OrderFlow.Inventory.Application.csproj src/Inventory/OrderFlow.Inventory.Application/
COPY src/Inventory/OrderFlow.Inventory.Domain/OrderFlow.Inventory.Domain.csproj src/Inventory/OrderFlow.Inventory.Domain/
COPY src/Inventory/OrderFlow.Inventory.Infrastructure/OrderFlow.Inventory.Infrastructure.csproj src/Inventory/OrderFlow.Inventory.Infrastructure/

RUN dotnet restore src/Inventory/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj

COPY src/Inventory/ src/Inventory/

RUN dotnet publish src/Inventory/OrderFlow.Inventory.Api/OrderFlow.Inventory.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderFlow.Inventory.Api.dll"]
