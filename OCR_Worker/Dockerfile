# syntax=docker/dockerfile:1

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# copy repo and restore/publish the worker
COPY . .
WORKDIR /src/OCR_Worker
RUN dotnet restore ./DocumentManagementSystem.OCR_Worker.csproj
RUN dotnet publish ./DocumentManagementSystem.OCR_Worker.csproj -c Release -o /app/publish \
    -p:UseAppHost=false -p:SelfContained=false -p:GenerateRuntimeConfigurationFiles=true

# ---------- runtime stage (with OCR tools) ----------
FROM mcr.microsoft.com/dotnet/runtime:8.0

RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-deu \
    ghostscript \
 && rm -rf /var/lib/apt/lists/*

ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/4.00/tessdata \
    RABBIT_HOST=rabbitmq \
    RABBIT_QUEUE=ocr-queue

WORKDIR /app
COPY --from=builder /app/publish .
ENTRYPOINT ["dotnet","DocumentManagementSystem.OCR_Worker.dll"]
