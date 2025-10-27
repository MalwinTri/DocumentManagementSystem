FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OCR_Worker.csproj ./
RUN dotnet restore OCR_Worker.csproj

COPY . .
RUN dotnet publish OCR_Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV RABBIT_HOST=rabbitmq RABBIT_QUEUE=ocr-queue
ENTRYPOINT ["dotnet","OCR_Worker.dll"]
