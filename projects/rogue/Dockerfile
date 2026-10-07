# The score API. Build context: projects/rogue (the API needs the rules library next to it).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props .editorconfig ./
COPY src/Rogue.Core/Rogue.Core.csproj src/Rogue.Core/
COPY src/Rogue.Api/Rogue.Api.csproj src/Rogue.Api/
RUN dotnet restore src/Rogue.Api/Rogue.Api.csproj
COPY src/Rogue.Core/ src/Rogue.Core/
COPY src/Rogue.Api/ src/Rogue.Api/
RUN dotnet publish src/Rogue.Api/Rogue.Api.csproj -c Release -o /app --no-restore

# Chiseled: no shell, no package manager, runs as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rogue.Api.dll"]
