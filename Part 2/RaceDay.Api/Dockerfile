FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Part 2/RaceDay.Contracts/RaceDay.Contracts.csproj", "Part 2/RaceDay.Contracts/"]
COPY ["Part 2/RaceDay.Api/RaceDay.Api.csproj", "Part 2/RaceDay.Api/"]
RUN dotnet restore "Part 2/RaceDay.Api/RaceDay.Api.csproj"

COPY . .
WORKDIR "/src/Part 2/RaceDay.Api"
RUN dotnet publish "RaceDay.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data/media && chown -R "$APP_UID:$APP_UID" /app/App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
