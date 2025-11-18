# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# nur Projektdateien -> restore (besserer Cache)
COPY OCR_Worker/DocumentManagementSystem.OCR_Worker.csproj OCR_Worker/
# ggf. weitere csproj aus anderen Projekten, falls referenced
# COPY DocumentManagementSystem/DocumentManagementSystem.csproj DocumentManagementSystem/

# NuGet-Cache verwenden (Dockerfile v1.9+)
# syntax=docker/dockerfile:1.9
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore OCR_Worker/DocumentManagementSystem.OCR_Worker.csproj

# restlicher Code
COPY . .

# publish
WORKDIR /src/OCR_Worker
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish ./DocumentManagementSystem.OCR_Worker.csproj -c Release -o /app/publish \
      -p:UseAppHost=false -p:SelfContained=false -p:GenerateRuntimeConfigurationFiles=true

# ---------- runtime stage (with OCR tools) ----------
FROM mcr.microsoft.com/dotnet/runtime:8.0

# OCR-Tools & Schriften & CA-Zertifikate
RUN apt-get update && apt-get install -y --no-install-recommends \
      tesseract-ocr \
      tesseract-ocr-eng \
      tesseract-ocr-deu \
      ghostscript \
      ca-certificates \
      fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Wichtige Umgebungsvariablen
# Auf Debian/Ubuntu liegen die Sprachdaten i.d.R. hier:
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/4.00/tessdata \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    RABBIT_HOST=rabbitmq \
    RABBIT_QUEUE=ocr-queue

WORKDIR /app
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet","DocumentManagementSystem.OCR_Worker.dll"]
