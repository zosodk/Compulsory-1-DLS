
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["IndexerService/IndexerService.csproj", "IndexerService/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
COPY *.sln ./

RUN dotnet restore "IndexerService/IndexerService.csproj"

COPY . ./
WORKDIR "/src/IndexerService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    OTEL_EXPORTER_OTLP_ENDPOINT=http://zipkin:9411/api/v2/spans \
    TZ=Etc/UTC \
    ASPNETCORE_URLS=http://+:8080 \
    SERILOG__WRITETO__SEQ__SERVERURL=http://seq:5341 \
    RABBITMQ_HOST=rabbitmq

ENTRYPOINT ["dotnet", "IndexerService.dll"]


