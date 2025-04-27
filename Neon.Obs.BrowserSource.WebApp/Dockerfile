FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neon.Obs.BrowserSource.WebApp/Neon.Obs.BrowserSource.WebApp.csproj", "Neon.Obs.BrowserSource.WebApp/"]
COPY ["Neon.Core/Neon.Core.csproj", "Neon.Core/"]
COPY ["Neon.Persistence/Neon.Persistence.csproj", "Neon.Persistence/"]
RUN dotnet restore "Neon.Obs.BrowserSource.WebApp/Neon.Obs.BrowserSource.WebApp.csproj"
COPY . .
WORKDIR "/src/Neon.Obs.BrowserSource.WebApp"
RUN dotnet build "./Neon.Obs.BrowserSource.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Neon.Obs.BrowserSource.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neon.Obs.BrowserSource.WebApp.dll"]

EXPOSE 8080
