# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# nur csproj-Dateien (für Restore-Caching)
COPY DocumentManagementSystem/DocumentManagementSystem.csproj DocumentManagementSystem/
COPY DocumentManagementSystem.BL.Documents/DocumentManagementSystem.BL.Documents.csproj DocumentManagementSystem.BL.Documents/
COPY DocumentManagementSystem.Infrastructure/DocumentManagementSystem.Infrastructure.csproj DocumentManagementSystem.Infrastructure/
COPY DocumentManagementSystem.Models/DocumentManagementSystem.Models.csproj DocumentManagementSystem.Models/
COPY DocumentManagementSystem.DAL/DocumentManagementSystem.DAL.csproj DocumentManagementSystem.DAL/
COPY DocumentManagementSystem.DAL.Postgres/DocumentManagementSystem.DAL.Postgres.csproj DocumentManagementSystem.DAL.Postgres/

RUN dotnet restore DocumentManagementSystem/DocumentManagementSystem.csproj

# danach kompletter Code
COPY . .

WORKDIR /src/DocumentManagementSystem
RUN dotnet publish DocumentManagementSystem.csproj -c Release -o /app/publish \
    -p:UseAppHost=false -p:SelfContained=false -p:GenerateRuntimeConfigurationFiles=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","DocumentManagementSystem.dll"]
