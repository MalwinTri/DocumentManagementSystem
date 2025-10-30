FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# API + alle referenzierten csproj EINZELN kopieren (Caching!)
COPY DocumentManagementSystem.API/DocumentManagementSystem.API.csproj DocumentManagementSystem.API/
COPY DocumentManagementSystem.BL.Documents/DocumentManagementSystem.BL.Documents.csproj DocumentManagementSystem.BL.Documents/
COPY DocumentManagementSystem.DAL/DocumentManagementSystem.DAL.csproj DocumentManagementSystem.DAL/
COPY DocumentManagementSystem.DAL.Postgres/DocumentManagementSystem.DAL.Postgres.csproj DocumentManagementSystem.DAL.Postgres/
COPY DocumentManagementSystem.Infrastructure/DocumentManagementSystem.Infrastructure.csproj DocumentManagementSystem.Infrastructure/
COPY DocumentManagementSystem.Models/DocumentManagementSystem.Models.csproj DocumentManagementSystem.Models/
COPY DocumentManagementSystem.Shared.Exceptions/DocumentManagementSystem.Shared.Exceptions.csproj DocumentManagementSystem.Shared.Exceptions/
# ggf. weitere csproj ergänzen

RUN dotnet restore DocumentManagementSystem.API/DocumentManagementSystem.API.csproj

COPY . .
WORKDIR /src/DocumentManagementSystem.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet","DocumentManagementSystem.API.dll"]
