FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BetStrike.Streaming.Worker/BetStrike.Streaming.Worker.csproj", "src/BetStrike.Streaming.Worker/"]
COPY ["src/BetStrike.Middleware.Contracts/BetStrike.Middleware.Contracts.csproj", "src/BetStrike.Middleware.Contracts/"]
RUN dotnet restore "src/BetStrike.Streaming.Worker/BetStrike.Streaming.Worker.csproj"

COPY . .
RUN dotnet publish "src/BetStrike.Streaming.Worker/BetStrike.Streaming.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BetStrike.Streaming.Worker.dll"]
