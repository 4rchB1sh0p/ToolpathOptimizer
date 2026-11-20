# -----------------------------------------------------------------------------------
# Stage: base image for ASP.NET Core runtime
# This stage is used by Visual Studio in "fast mode" (default for Debug configuration)
# -----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Set useful environment variables for runtime behavior
ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:8080

# Run as a non-root user. APP_UID can be overridden at build-time if needed.
ARG APP_UID=1000
ENV APP_UID=${APP_UID}
USER ${APP_UID}

WORKDIR /app
EXPOSE 8080

# -----------------------------------------------------------------------------------
# Stage: build image with .NET SDK
# This stage compiles the service project and restores NuGet packages.
# -----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Allow build configuration to be overridden (Debug or Release)
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy the source. Remove host-generated outputs so Windows NuGet assets cannot
# override the Linux restore performed in the container.
COPY . .
RUN find /src/src -type d \( -name bin -o -name obj \) -prune -exec rm -rf '{}' + \
    && dotnet restore "src/ToolpathOptimizer.Web/ToolpathOptimizer.Web.csproj"

WORKDIR "/src/src/ToolpathOptimizer.Web"

# Build the application
RUN dotnet build "ToolpathOptimizer.Web.csproj" \
    --no-restore \
    -c $BUILD_CONFIGURATION

# -----------------------------------------------------------------------------------
# Stage: publish the compiled application
# Produces optimized output without the app host for leaner images
# -----------------------------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ToolpathOptimizer.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false

# -----------------------------------------------------------------------------------
# Stage: final runtime image
# Used when Visual Studio runs in normal mode (default when not debugging)
# -----------------------------------------------------------------------------------
FROM base AS final

# ⚠️ Temporarily switch to root for setup
USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published output from the publish stage
COPY --from=publish /app/publish .

# Create writable folder
RUN mkdir -p /app/data && chown -R nobody:nogroup /app/data

# Health check uses the application's liveness endpoint.
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl --fail --silent http://localhost:8080/health/live || exit 1

# Switch back to secure user
USER nobody

# Default entry point for the container
ENTRYPOINT ["dotnet", "ToolpathOptimizer.Web.dll"]