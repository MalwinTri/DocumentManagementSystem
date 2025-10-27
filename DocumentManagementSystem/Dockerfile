# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# nur die API-csproj für Caching
COPY DocumentManagementSystem.csproj ./
RUN dotnet restore DocumentManagementSystem.csproj

# restlichen API-Code
COPY . .
RUN dotnet publish DocumentManagementSystem.csproj -c Release -o /out /p:UseAppHost=false

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet","DocumentManagementSystem.dll"]
