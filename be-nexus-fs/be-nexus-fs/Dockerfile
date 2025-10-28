# =============================================================================
# NexusFS - Multi-stage Docker Build
# =============================================================================

# Stage 1: Build
# Use the official .NET SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

# Copy all .csproj files first for layer caching optimization
# This allows Docker to cache NuGet restore if project files haven't changed
COPY ["be-nexus-fs/be-nexus-fs.csproj", "be-nexus-fs/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore NuGet packages
RUN dotnet restore "be-nexus-fs/be-nexus-fs.csproj"

# Copy the entire solution
COPY . .

# Set working directory to the Web API project
WORKDIR "/src/be-nexus-fs"

# Build the application in Release mode
RUN dotnet build "be-nexus-fs.csproj" -c Release -o /app/build

# Stage 2: Publish
# Publish the application to /app/publish
FROM build AS publish
RUN dotnet publish "be-nexus-fs.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
# Use the smaller ASP.NET runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Set working directory
WORKDIR /app

# Expose ports (Railway will override these)
EXPOSE 8080
EXPOSE 8081

# Create a non-root user for security
RUN addgroup --system --gid 1001 nexusfs && \
    adduser --system --uid 1001 --ingroup nexusfs nexusfs

# Copy published files from the publish stage
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R nexusfs:nexusfs /app

# Switch to non-root user
USER nexusfs

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

# Entry point
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]