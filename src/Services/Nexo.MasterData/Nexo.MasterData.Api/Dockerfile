# Nexo.MasterData.Api — multi-stage build.
# IMPORTANT: build context MUST be the repository root, e.g.:
#   docker build -f src/Services/Nexo.MasterData/Nexo.MasterData.Api/Dockerfile -t nexo-masterdata-api .
# (docker-compose sets context: . and dockerfile: this path.)

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Central build/package configuration (Central Package Management).
COPY Directory.Build.props Directory.Packages.props global.json ./

# Project files first, for a cached restore layer.
COPY src/BuildingBlocks/Nexo.BuildingBlocks.Domain/Nexo.BuildingBlocks.Domain.csproj src/BuildingBlocks/Nexo.BuildingBlocks.Domain/
COPY src/BuildingBlocks/Nexo.BuildingBlocks.Application/Nexo.BuildingBlocks.Application.csproj src/BuildingBlocks/Nexo.BuildingBlocks.Application/
COPY src/BuildingBlocks/Nexo.BuildingBlocks.MultiTenancy/Nexo.BuildingBlocks.MultiTenancy.csproj src/BuildingBlocks/Nexo.BuildingBlocks.MultiTenancy/
COPY src/BuildingBlocks/Nexo.BuildingBlocks.Messaging/Nexo.BuildingBlocks.Messaging.csproj src/BuildingBlocks/Nexo.BuildingBlocks.Messaging/
COPY src/BuildingBlocks/Nexo.BuildingBlocks.Observability/Nexo.BuildingBlocks.Observability.csproj src/BuildingBlocks/Nexo.BuildingBlocks.Observability/
COPY src/BuildingBlocks/Nexo.BuildingBlocks.Web/Nexo.BuildingBlocks.Web.csproj src/BuildingBlocks/Nexo.BuildingBlocks.Web/
COPY src/Services/Nexo.MasterData/Nexo.MasterData.Domain/Nexo.MasterData.Domain.csproj src/Services/Nexo.MasterData/Nexo.MasterData.Domain/
COPY src/Services/Nexo.MasterData/Nexo.MasterData.Application/Nexo.MasterData.Application.csproj src/Services/Nexo.MasterData/Nexo.MasterData.Application/
COPY src/Services/Nexo.MasterData/Nexo.MasterData.Infrastructure/Nexo.MasterData.Infrastructure.csproj src/Services/Nexo.MasterData/Nexo.MasterData.Infrastructure/
COPY src/Services/Nexo.MasterData/Nexo.MasterData.Api/Nexo.MasterData.Api.csproj src/Services/Nexo.MasterData/Nexo.MasterData.Api/

RUN dotnet restore src/Services/Nexo.MasterData/Nexo.MasterData.Api/Nexo.MasterData.Api.csproj

# Copy the remaining sources and publish.
COPY src/ src/
RUN dotnet publish src/Services/Nexo.MasterData/Nexo.MasterData.Api/Nexo.MasterData.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Nexo.MasterData.Api.dll"]
