FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neon.TwitchMessageService/Neon.TwitchMessageService.csproj", "Neon.TwitchMessageService/"]
COPY ["Neon.Core/Neon.Core.csproj", "Neon.Core/"]
COPY ["Neon.Persistence/Neon.Persistence.csproj", "Neon.Persistence/"]
COPY ["Neon.TwitchService/Neon.TwitchService.csproj", "Neon.TwitchService/"]
RUN dotnet restore "Neon.TwitchMessageService/Neon.TwitchMessageService.csproj"
COPY . .
WORKDIR "/src/Neon.TwitchMessageService"
RUN dotnet build "./Neon.TwitchMessageService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Neon.TwitchMessageService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neon.TwitchMessageService.dll"]
