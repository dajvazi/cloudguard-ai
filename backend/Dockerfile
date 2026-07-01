# ── Stage 1: Build ────────────────────────────────────────────────────────────
# Use the full .NET SDK image to compile the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first — Docker caches this layer so
# "dotnet restore" is skipped when only source code changes.
COPY CloudGuard.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish CloudGuard.Api.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# Use the smaller ASP.NET runtime image (no SDK = smaller final image).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Listen on all interfaces inside the container (required for Docker networking).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudGuard.Api.dll"]
