FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything from repo root
COPY . .

# The .sln is in be-nexus-fs/
# The main .csproj is in be-nexus-fs/be-nexus-fs/

# Restore only the main project (not the solution, not tests)
RUN dotnet restore "be-nexus-fs/be-nexus-fs/be-nexus-fs.csproj"

# Publish only the main project
RUN dotnet publish "be-nexus-fs/be-nexus-fs/be-nexus-fs.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]