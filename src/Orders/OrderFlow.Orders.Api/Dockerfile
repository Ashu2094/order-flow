
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Directory.Build.props ./

COPY src/Orders/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj src/Orders/OrderFlow.Orders.Api/
COPY src/Orders/OrderFlow.Orders.Application/OrderFlow.Orders.Application.csproj src/Orders/OrderFlow.Orders.Application/
COPY src/Orders/OrderFlow.Orders.Domain/OrderFlow.Orders.Domain.csproj src/Orders/OrderFlow.Orders.Domain/
COPY src/Orders/OrderFlow.Orders.Infrastructure/OrderFlow.Orders.Infrastructure.csproj src/Orders/OrderFlow.Orders.Infrastructure/
COPY src/Contracts/OrderFlow.Contracts.csproj src/Contracts/

RUN dotnet restore src/Orders/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj

COPY src/Orders/ src/Orders/
COPY src/Contracts/ src/Contracts/

RUN dotnet publish src/Orders/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderFlow.Orders.Api.dll"]
