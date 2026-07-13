# Nexo.Production.Api — multi-stage build.
# IMPORTANT: build context MUST be the repository root, e.g.:
#   docker build -f src/Services/Nexo.Production/Nexo.Production.Api/Dockerfile -t nexo-production-api .
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
COPY src/Services/Nexo.Production/Nexo.Production.Domain/Nexo.Production.Domain.csproj src/Services/Nexo.Production/Nexo.Production.Domain/
COPY src/Services/Nexo.Production/Nexo.Production.Application/Nexo.Production.Application.csproj src/Services/Nexo.Production/Nexo.Production.Application/
COPY src/Services/Nexo.Production/Nexo.Production.Infrastructure/Nexo.Production.Infrastructure.csproj src/Services/Nexo.Production/Nexo.Production.Infrastructure/
COPY src/Services/Nexo.Production/Nexo.Production.Api/Nexo.Production.Api.csproj src/Services/Nexo.Production/Nexo.Production.Api/

RUN dotnet restore src/Services/Nexo.Production/Nexo.Production.Api/Nexo.Production.Api.csproj

# Copy the remaining sources and publish.
COPY src/ src/
RUN dotnet publish src/Services/Nexo.Production/Nexo.Production.Api/Nexo.Production.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Nexo.Production.Api.dll"]
