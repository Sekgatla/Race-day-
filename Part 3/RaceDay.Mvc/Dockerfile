FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Part 2/RaceDay.Contracts/RaceDay.Contracts.csproj", "Part 2/RaceDay.Contracts/"]
COPY ["Part 3/RaceDay.Mvc/RaceDay.Mvc.csproj", "Part 3/RaceDay.Mvc/"]
RUN dotnet restore "Part 3/RaceDay.Mvc/RaceDay.Mvc.csproj"

COPY . .
WORKDIR "/src/Part 3/RaceDay.Mvc"
RUN dotnet build "RaceDay.Mvc.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/build \
    --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RaceDay.Mvc.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
