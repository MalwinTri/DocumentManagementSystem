FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY DocumentManagementSystem.GenAI_Worker/*.csproj ./DocumentManagementSystem.GenAI_Worker/

RUN dotnet restore DocumentManagementSystem.GenAI_Worker/DocumentManagementSystem.GenAI_Worker.csproj

COPY . .

RUN dotnet publish DocumentManagementSystem.GenAI_Worker/DocumentManagementSystem.GenAI_Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "DocumentManagementSystem.GenAI_Worker.dll"]
