# ── Stage 1: restore & publish ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies first so the layer is cached.
COPY hambusweb.csproj ./
RUN dotnet restore hambusweb.csproj

# Copy the rest of the source and publish a trimmed, self-contained-ready build.
COPY . ./
RUN dotnet publish hambusweb.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Expose the port the app binds to inside the container.
EXPOSE 8080

# Copy published output from the build stage.
COPY --from=build /app/publish .

# Run as a non-root user for better security posture.
RUN groupadd --system --gid 1001 appgroup && \
    useradd  --system --uid 1001 --gid appgroup --no-create-home appuser && \
    chown -R appuser:appgroup /app
USER appuser

# Tell ASP.NET Core to listen on all interfaces on port 8080.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "hambusweb.dll"]


