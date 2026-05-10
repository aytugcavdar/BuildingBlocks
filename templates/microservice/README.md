# Microservice.Template

Starter ASP.NET Core microservice using BuildingBlocks packages.

## Run

```powershell
dotnet restore
dotnet run
```

Default endpoints:

```text
GET /
GET /api/status
GET /health/live
GET /health/ready
GET /health/startup
```

## Features

Feature switches live under `Features` in `appsettings.json`.

Caching is enabled with memory cache only. Messaging, JWT, distributed locking, SMS, and rate limiting are disabled by default so the service can start without Redis or RabbitMQ.

## Local Package Feed

Before the packages are published to an internal feed, add the local package output as a source:

```powershell
dotnet nuget add source C:\proje\Core\BuildingBlocks\artifacts\packages --name BuildingBlocksLocal
```
