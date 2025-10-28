# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ONLY production projects (not tests)
COPY ["be-nexus-fs/be-nexus-fs/be-nexus-fs.csproj", "be-nexus-fs/"]
COPY ["be-nexus-fs/Application/Application.csproj", "Application/"]
COPY ["be-nexus-fs/Domain/Domain.csproj", "Domain/"]
COPY ["be-nexus-fs/Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies (only production projects)
RUN dotnet restore "be-nexus-fs/be-nexus-fs.csproj"

# Copy source code (includes tests, but we won't build them)
COPY . .

# Build and publish (only the main project, not tests)
WORKDIR "/src/be-nexus-fs"
RUN dotnet publish "be-nexus-fs.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]
