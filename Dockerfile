FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ALL projects
COPY . .

# Restore only the main project
RUN dotnet restore "be-nexus-fs/be-nexus-fs.csproj"

# Publish only the main project (this skips tests automatically)
RUN dotnet publish "be-nexus-fs/be-nexus-fs.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "be-nexus-fs.dll"]
