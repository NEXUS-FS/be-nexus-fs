FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Find and restore the main project
# Try both possible paths
RUN dotnet restore "be-nexus-fs/be-nexus-fs.csproj" || dotnet restore "be-nexus-fs.csproj" || true

# Publish - try both paths
RUN if [ -f "be-nexus-fs/be-nexus-fs.csproj" ]; then \
      dotnet publish "be-nexus-fs/be-nexus-fs.csproj" -c Release -o /app/publish; \
    else \
      dotnet publish "be-nexus-fs.csproj" -c Release -o /app/publish; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]
