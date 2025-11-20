FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Neon.StreamElementsService/Neon.StreamElementsService.csproj", "Neon.StreamElementsService/"]
COPY ["Neon.Core/Neon.Core.csproj", "Neon.Core/"]
COPY ["Neon.Persistence/Neon.Persistence.csproj", "Neon.Persistence/"]
RUN dotnet restore "Neon.StreamElementsService/Neon.StreamElementsService.csproj"
COPY . .
WORKDIR "/src/Neon.StreamElementsService"
RUN dotnet build "./Neon.StreamElementsService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Neon.StreamElementsService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Neon.StreamElementsService.dll"]