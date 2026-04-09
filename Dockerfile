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

RUN if command -v addgroup >/dev/null 2>&1 && command -v adduser >/dev/null 2>&1; then \
        addgroup --system --gid 1001 appgroup \
        && adduser --system --uid 1001 --ingroup appgroup --no-create-home appuser; \
    elif command -v groupadd >/dev/null 2>&1 && command -v useradd >/dev/null 2>&1; then \
        groupadd --system --gid 1001 appgroup \
        && useradd --system --uid 1001 --gid appgroup --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin appuser; \
    else \
        echo "Nenhum utilitario suportado para criar usuario/grupo foi encontrado na imagem base." >&2; \
        exit 1; \
    fi \
    && mkdir -p /app/Data/users \
    && chown -R appuser:appgroup /app

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render injeta PORT em runtime; Kestrel precisa escutar nessa porta.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} && exec dotnet ProductStore.Api.dll"]
