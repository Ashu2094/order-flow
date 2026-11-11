  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /src
  COPY Directory.Build.props ./
  COPY src/Gateway/OrderFlow.Gateway/OrderFlow.Gateway.csproj src/Gateway/OrderFlow.Gateway/
  RUN dotnet restore src/Gateway/OrderFlow.Gateway/OrderFlow.Gateway.csproj
  COPY src/Gateway/ src/Gateway/
  RUN dotnet publish src/Gateway/OrderFlow.Gateway/OrderFlow.Gateway.csproj -c Release -o /app/publish --no-restore

  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
  WORKDIR /app
  COPY --from=build /app/publish ./
  EXPOSE 8080
  ENTRYPOINT ["dotnet", "OrderFlow.Gateway.dll"]