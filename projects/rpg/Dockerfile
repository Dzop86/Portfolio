# The accounts and characters API. Build context: projects/rpg (the API needs the rules library and its data).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props .editorconfig ./
COPY src/Rpg.Core/Rpg.Core.csproj src/Rpg.Core/
COPY src/Rpg.Api/Rpg.Api.csproj src/Rpg.Api/
RUN dotnet restore src/Rpg.Api/Rpg.Api.csproj
COPY data/ data/
COPY src/Rpg.Core/ src/Rpg.Core/
COPY src/Rpg.Api/ src/Rpg.Api/
RUN dotnet publish src/Rpg.Api/Rpg.Api.csproj -c Release -o /app --no-restore

# Chiseled: no shell, no package manager, runs as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rpg.Api.dll"]
