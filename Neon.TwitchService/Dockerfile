FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neon.TwitchService/Neon.TwitchService.csproj", "Neon.TwitchService/"]
COPY ["Neon.Core/Neon.Core.csproj", "Neon.Core/"]
COPY ["Neon.Persistence/Neon.Persistence.csproj", "Neon.Persistence/"]
RUN dotnet restore "Neon.TwitchService/Neon.TwitchService.csproj"
COPY . .
WORKDIR "/src/Neon.TwitchService"
RUN dotnet build "./Neon.TwitchService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Neon.TwitchService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neon.TwitchService.dll"]