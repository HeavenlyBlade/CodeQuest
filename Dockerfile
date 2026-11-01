# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first (layer caching)
COPY CodeQuest.csproj .
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Render injects PORT — ASP.NET Core reads ASPNETCORE_URLS or we set it in Program.cs
# Expose a default port for documentation (actual port comes from PORT env var)
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "CodeQuest.dll"]
