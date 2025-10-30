# -------- build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Worker + seine Referenzen (mit Ordnerpräfix!)
COPY DocumentManagementSystem.OCR_Worker/DocumentManagementSystem.OCR_Worker.csproj DocumentManagementSystem.OCR_Worker/
COPY DocumentManagementSystem.Models/DocumentManagementSystem.Models.csproj DocumentManagementSystem.Models/
COPY DocumentManagementSystem.Shared.Exceptions/DocumentManagementSystem.Shared.Exceptions.csproj DocumentManagementSystem.Shared.Exceptions/
# ggf. weitere Referenzen ergänzen:
# COPY DocumentManagementSystem.Infrastructure/DocumentManagementSystem.Infrastructure.csproj DocumentManagementSystem.Infrastructure/
# COPY DocumentManagementSystem.DAL/DocumentManagementSystem.DAL.csproj DocumentManagementSystem.DAL/
# ...

RUN dotnet restore DocumentManagementSystem.OCR_Worker/DocumentManagementSystem.OCR_Worker.csproj

# jetzt den Rest des Repos
COPY . .

# Publish
WORKDIR /src/DocumentManagementSystem.OCR_Worker
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -------- runtime stage --------
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV RABBIT_HOST=rabbitmq \
    RABBIT_QUEUE=ocr-queue
ENTRYPOINT ["dotnet","DocumentManagementSystem.OCR_Worker.dll"]
