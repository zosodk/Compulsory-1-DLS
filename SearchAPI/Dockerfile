
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SearchAPI/SearchAPI.csproj", "SearchAPI/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
COPY *.sln ./

RUN dotnet restore "SearchAPI/SearchAPI.csproj"

COPY . ./ 
WORKDIR "/src/SearchAPI"
RUN dotnet build "SearchAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SearchAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    OTEL_EXPORTER_OTLP_ENDPOINT=http://zipkin:9411/api/v2/spans \
    TZ=Etc/UTC \
    ASPNETCORE_URLS=http://+:8080 \
    SERILOG__WRITETO__SEQ__SERVERURL=http://seq:5341

ENTRYPOINT ["dotnet", "SearchAPI.dll"]
#ENTRYPOINT ["dotnet", "SearchAPI.dll", "--urls", "http://0.0.0.0:8082"]



