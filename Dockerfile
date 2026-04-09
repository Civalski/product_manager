# Build e run da API no Render (ambiente Docker = SDK + runtime .NET disponíveis).
# No painel: tipo de serviço "Docker", sem Build/Start em shell (usa este arquivo).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ProductStore.Api/ProductStore.Api.csproj backend/ProductStore.Api/
RUN dotnet restore backend/ProductStore.Api/ProductStore.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/ProductStore.Api/ProductStore.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render injeta PORT em runtime; Kestrel precisa escutar nessa porta.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} && exec dotnet ProductStore.Api.dll"]
