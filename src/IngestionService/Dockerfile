FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared.Contracts/    Shared.Contracts/
COPY IngestionService/    IngestionService/

RUN dotnet publish IngestionService/IngestionService.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IngestionService.dll"]