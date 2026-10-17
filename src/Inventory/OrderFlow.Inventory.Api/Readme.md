Stage 1
Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
This image contains the .NET SDK, including the tools needed to restore, compile, and publish the application.


Copy project files and restore
COPY Directory.Build.props ./
COPY src/Inventory/...csproj ...
RUN dotnet restore ...


Docker copies the project files before the source code so that dependency restoration can be cached independently of ordinary source changes.


Publish the application
RUN dotnet publish ... -c Release -o /app/publish --no-restore


The app is compiled and published. --no-restore avoids repeating the restore step that already ran.


Stage 2
Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
COPY --from=build /app/publish ./
The final image starts from the smaller ASP.NET runtime image and copies in the published output, rather than carrying the SDK and source tree into the final stage.


Start the application
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Inventory.Api.dll"]


The container starts the published application using dotnet. EXPOSE 8080 documents the intended container port; it does not publish that port to your host by itself.


Important: The project paths and target framework must match your repository. This Dockerfile assumes the project targets .NET 10 and that Directory.Build.props is the relevant shared build configuration file.