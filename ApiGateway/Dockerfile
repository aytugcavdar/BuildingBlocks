# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
COPY ["BuildingBlocks.Core/BuildingBlocks.Core.csproj", "BuildingBlocks.Core/"]
COPY ["BuildingBlocks.CrossCutting/BuildingBlocks.CrossCutting.csproj", "BuildingBlocks.CrossCutting/"]
COPY ["BuildingBlocks.HealthChecks/BuildingBlocks.HealthChecks.csproj", "BuildingBlocks.HealthChecks/"]
COPY ["BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj", "BuildingBlocks.Infrastructure/"]
COPY ["BuildingBlocks.Logging/BuildingBlocks.Logging.csproj", "BuildingBlocks.Logging/"]
COPY ["BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks.Messaging/"]
COPY ["BuildingBlocks.Security/BuildingBlocks.Security.csproj", "BuildingBlocks.Security/"]

# Restore dependencies
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r apigateway && useradd -r -g apigateway apigateway

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R apigateway:apigateway /app

# Switch to non-root user
USER apigateway

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Start application
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
