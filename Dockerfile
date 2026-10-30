# Transport Pipes: the ASP.NET Core server (game world + SignalR hub) that also serves the React/PixiJS frontend.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# The frontend is platform independent, so build it once on the build machine for every target platform.
FROM --platform=$BUILDPLATFORM node:25-alpine AS frontend
WORKDIR /src/TransportPipes.Frontend
COPY ["TransportPipes.Frontend/package.json", "TransportPipes.Frontend/package-lock.json", "./"]
RUN npm ci
COPY TransportPipes.Frontend/ ./
RUN npm run build

# The app is platform independent, so build once on the build machine for every target platform.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["TransportPipes.Game/TransportPipes.Game.csproj", "TransportPipes.Game/"]
COPY ["TransportPipes.Database/TransportPipes.Database.csproj", "TransportPipes.Database/"]
COPY ["TransportPipes.Server/TransportPipes.Server.csproj", "TransportPipes.Server/"]
RUN dotnet restore "TransportPipes.Server/TransportPipes.Server.csproj"
COPY . .
ARG BUILD_VERSION
ARG VER=${BUILD_VERSION:-1.0.0}
# The git commit, shown next to the version in the game (.git is not part of the build context)
ARG BUILD_COMMIT
RUN dotnet publish "TransportPipes.Server/TransportPipes.Server.csproj" -c Release --no-restore -o /app/publish /p:Version=$VER /p:SourceRevisionId=$BUILD_COMMIT /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend /src/TransportPipes.Frontend/dist ./wwwroot
USER app
ENTRYPOINT ["dotnet", "TransportPipes.Server.dll"]
