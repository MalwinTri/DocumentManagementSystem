# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# kopiert das ganze repo (weil compose context: ..)
COPY . .

RUN dotnet restore ./DocumentManagementSystem.AccessBatch/DocumentManagementSystem.AccessBatch.csproj
RUN dotnet publish ./DocumentManagementSystem.AccessBatch/DocumentManagementSystem.AccessBatch.csproj \
    -c Release -o /app/publish --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.AccessBatch.dll"]
