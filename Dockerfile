# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files
COPY ["ProjectName.Api/ProjectName.Api.csproj", "ProjectName.Api/"]
COPY ["ProjectName.Application/ProjectName.Application.csproj", "ProjectName.Application/"]
COPY ["ProjectName.Domain/ProjectName.Domain.csproj", "ProjectName.Domain/"]
COPY ["ProjectName.Infrastructure/ProjectName.Infrastructure.csproj", "ProjectName.Infrastructure/"]
COPY ["ProjectName.Persistence/ProjectName.Persistence.csproj", "ProjectName.Persistence/"]

# Restore dependencies
RUN dotnet restore "ProjectName.Api/ProjectName.Api.csproj"

# Copy remaining files
COPY . .

# Build the application
RUN dotnet build "ProjectName.Api/ProjectName.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ProjectName.Api/ProjectName.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Expose default port
EXPOSE 5001

# Set environment variables
ENV ASPNETCORE_URLS=https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f https://localhost:5001/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "ProjectName.Api.dll"]
