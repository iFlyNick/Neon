FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neon.Emotes.Api/Neon.Emotes.Api.csproj", "Neon.Emotes.Api/"]
COPY ["Neon.Core/Neon.Core.csproj", "Neon.Core/"]
COPY ["Neon.Persistence/Neon.Persistence.csproj", "Neon.Persistence/"]
RUN dotnet restore "Neon.Emotes.Api/Neon.Emotes.Api.csproj"
COPY . .
WORKDIR "/src/Neon.Emotes.Api"
RUN dotnet build "./Neon.Emotes.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Neon.Emotes.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neon.Emotes.Api.dll"]
