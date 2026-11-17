FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY WarframeBuilds/WarframeBuilds.csproj WarframeBuilds/
RUN dotnet restore WarframeBuilds/WarframeBuilds.csproj
COPY WarframeBuilds/ WarframeBuilds/
RUN dotnet publish WarframeBuilds/WarframeBuilds.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/App_Data/Keys && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WarframeBuilds.dll"]
