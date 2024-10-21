FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY ./CardGame.csproj .
RUN dotnet restore
COPY ./ .
RUN dotnet publish ./CardGame.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ./appsettings.json .
ENTRYPOINT dotnet CardGame.dll
