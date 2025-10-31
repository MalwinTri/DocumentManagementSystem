# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/OCR_Worker
RUN dotnet restore ./DocumentManagementSystem.OCR_Worker.csproj
RUN dotnet publish ./DocumentManagementSystem.OCR_Worker.csproj -c Release -o /app/publish \
    -p:UseAppHost=false \
    -p:SelfContained=false \
    -p:GenerateRuntimeConfigurationFiles=true

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV RABBIT_HOST=rabbitmq RABBIT_QUEUE=ocr-queue
ENTRYPOINT ["dotnet","DocumentManagementSystem.OCR_Worker.dll"]
