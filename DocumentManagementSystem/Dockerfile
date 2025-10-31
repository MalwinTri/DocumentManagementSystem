# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/DocumentManagementSystem
RUN dotnet restore ./DocumentManagementSystem.csproj
RUN dotnet publish ./DocumentManagementSystem.csproj -c Release -o /app/publish \
    -p:UseAppHost=false \
    -p:SelfContained=false \
    -p:GenerateRuntimeConfigurationFiles=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","DocumentManagementSystem.dll"]
