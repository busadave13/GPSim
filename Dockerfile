# GPSim Dockerfile
# Multi-stage build for optimized container size

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (for better layer caching)
COPY GPSim.sln .
COPY nuget.config .
COPY src/GPSim.Server/GPSim.Server.csproj src/GPSim.Server/
COPY src/GPSim.Client/GPSim.Client.csproj src/GPSim.Client/
COPY src/GPSim.Shared/GPSim.Shared.csproj src/GPSim.Shared/
COPY tests/GPSim.Server.Tests/GPSim.Server.Tests.csproj tests/GPSim.Server.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY src/ src/
COPY tests/ tests/

# Build and publish the application
RUN dotnet publish src/GPSim.Server/GPSim.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published application
COPY --from=build /app/publish .

# Create data directory for route persistence
RUN mkdir -p /app/Data/Routes && chown -R appuser:appuser /app/Data

# Switch to non-root user
USER appuser

# Expose port (ASP.NET Core defaults to 8080 in containers)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/config/mapbox || exit 1

# Entry point
ENTRYPOINT ["dotnet", "GPSim.Server.dll"]
