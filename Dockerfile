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

# Install libgssapi to silence the Npgsql Kerberos warning
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# Render injects PORT — actual port comes from PORT env var set in render.yaml
EXPOSE 8080

ENTRYPOINT ["dotnet", "CodeQuest.dll"]
