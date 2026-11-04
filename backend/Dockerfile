# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UrlShortener.sln .
COPY UrlShortener.Api/UrlShortener.Api.csproj UrlShortener.Api/
COPY UrlShortener.Tests/UrlShortener.Tests.csproj UrlShortener.Tests/

RUN dotnet restore

COPY . .

RUN dotnet build -c Release --no-restore
RUN dotnet test -c Release --no-build --verbosity normal
RUN dotnet publish UrlShortener.Api/UrlShortener.Api.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

# Volume for the SQLite database
RUN mkdir -p /data && chown appuser:appgroup /data
VOLUME ["/data"]

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DatabasePath=/data/urlshortener.db

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]
