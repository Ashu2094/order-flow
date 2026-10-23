FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Contracts/OrderFlow.Contracts.csproj src/Contracts/
COPY src/Payments/OrderFlow.Payments.Api/OrderFlow.Payments.Api.csproj src/Payments/OrderFlow.Payments.Api/
COPY src/Payments/OrderFlow.Payments.Application/OrderFlow.Payments.Application.csproj src/Payments/OrderFlow.Payments.Application/
COPY src/Payments/OrderFlow.Payments.Domain/OrderFlow.Payments.Domain.csproj src/Payments/OrderFlow.Payments.Domain/
COPY src/Payments/OrderFlow.Payments.Infrastructure/OrderFlow.Payments.Infrastructure.csproj src/Payments/OrderFlow.Payments.Infrastructure/
RUN dotnet restore src/Payments/OrderFlow.Payments.Api/OrderFlow.Payments.Api.csproj

COPY src/Contracts/ src/Contracts/
COPY src/Payments/ src/Payments/
RUN dotnet publish src/Payments/OrderFlow.Payments.Api/OrderFlow.Payments.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Payments.Api.dll"]