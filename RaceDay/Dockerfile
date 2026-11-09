# =======================================================
# RaceDay API image (build context = repository root)
#   docker build -f RaceDay/Dockerfile -t raceday-api .
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay/RaceDay.csproj RaceDay/
RUN dotnet restore RaceDay/RaceDay.csproj
COPY RaceDay/ RaceDay/
RUN dotnet publish RaceDay/RaceDay.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.dll"]
