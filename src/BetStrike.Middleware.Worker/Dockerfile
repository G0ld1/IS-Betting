FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/BetStrike.Middleware.Worker/BetStrike.Middleware.Worker.csproj
RUN dotnet publish src/BetStrike.Middleware.Worker/BetStrike.Middleware.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BetStrike.Middleware.Worker.dll"]