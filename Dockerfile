# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (paths are now from repo root)
COPY ["be-nexus-fs/be-nexus-fs.csproj", "be-nexus-fs/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "be-nexus-fs/be-nexus-fs.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/be-nexus-fs"
RUN dotnet publish "be-nexus-fs.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]
