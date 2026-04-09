# ProductStore

Aplicação fullstack de gerenciamento de produtos com integração à API [Bluesoft Cosmos](https://cosmos.bluesoft.com.br/) para enriquecimento de dados por GTIN/código de barras.

# **Em local**, o fluxo de SKU por **código de barras / GTIN (Cosmos)** só funciona se definir `Cosmos__Token` no `.env` da raiz (chave da API Bluesoft). Sem essa chave, use SKU **interno** ou experimente a aplicação já configurada em **[https://product-store-teste.vercel.app/](https://product-store-teste.vercel.app/)**. Em produção, login e registo usam **Cloudflare Turnstile**; em desenvolvimento local a API aceita registo e conclusão de login sem o widget.

## Stack

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | ASP.NET Core 10 · EF Core 10 · SQLite · ASP.NET Core Identity · JWT Bearer · FluentValidation |
| **Frontend** | React 19 · TypeScript · Vite 8 · React Router 7 · Cloudflare Turnstile |
| **Testes** | xUnit · `WebApplicationFactory` (integração) |
| **Orquestração** | Node.js (`concurrently`) |

## Funcionalidades

- **CRUD completo de produtos** com listagem paginada, busca por nome/SKU/descrição e filtros (categoria, faixa de preço, situação de estoque)
- **Integração Bluesoft Cosmos**: pré-visualização de produto por GTIN antes de salvar, com importação de nome, marca, imagem, dimensões e metadados EAN/NCM/GPC
- **Categorias**: listagem e criação; regra de negócio — categoria equivalente a *eletrônico* exige preço mínimo de **R$ 50**; nomes normalizados (acentos/caso) com índice único na base do tenant
- **Validação de dados** no backend (FluentValidation) e no frontend, com mensagens de erro detalhadas via Problem Details (RFC 7807)
- **Tratamento global de erros**: mapeamento de exceções de domínio → códigos HTTP corretos (400/404/409/429/502/503)
- **Tema claro/escuro** persistido via `localStorage`
- **Log de requisições HTTP** embutido no frontend (painel com scroll para debug)
- **Autenticação**: registo e login com nome de utilizador e palavra-passe; **Cloudflare Turnstile** no registo e após o login (antes do JWT de sessão). JWT armazenado no cliente. Cada utilizador tem um SQLite próprio em `data/users/{id}.db`; as migrações do tenant aplicam-se automaticamente no primeiro acesso após deploy

## Pré-requisitos

| Ferramenta | Versão mínima |
|------------|--------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |
| [Node.js](https://nodejs.org/) | 20 LTS |
| [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) | instalado via `dotnet-tools.json` |

## Configuração e execução

### Arranque com um único comando

Na raiz do repositório (com [.NET SDK 10](https://dotnet.microsoft.com/download) e [Node.js 20+](https://nodejs.org/) instalados):

```bash
npm start
```

Isto executa, em sequência: `dotnet tool restore`, `npm install` na raiz e em `frontend/`, cria `.env` e `frontend/.env` a partir dos ficheiros `.env.example` **só se ainda não existirem**, e de seguida inicia a API e o Vite em paralelo (o mesmo que `npm run dev`).

- **Frontend** → [http://localhost:5173](http://localhost:5173)
- **API** → [http://localhost:5127](http://localhost:5127)
- **OpenAPI** → [http://localhost:5127/openapi/v1.json](http://localhost:5127/openapi/v1.json)

Se já tiver dependências instaladas e só quiser subir os serviços:

```bash
npm run dev
```

### Variáveis de ambiente (opcional)

Edite `.env` na raiz (ou `backend/ProductStore.Api/.env` — o último carregado prevalece por chave) e `frontend/.env` conforme necessário:

- **Cosmos** (`Cosmos__Token`): obrigatório para pré-visualização e gravação com SKU tipo GTIN/Cosmos; sem token, use SKU **interno** ou o ambiente em [product-store-teste.vercel.app](https://product-store-teste.vercel.app/).
- **Turnstile** (`Turnstile__SecretKey` na raiz + `VITE_TURNSTILE_SITE_KEY` no frontend): necessários em builds de produção; em **Development** a API dispensa o widget.
- **Frontend local**: deixe `VITE_API_BASE_URL` vazio — o proxy do Vite encaminha `/api` para a porta 5127.

Em **produção** (Render): `Jwt__Key` (≥32 caracteres), `CORS_ORIGINS` com a origem Vercel, e opcionalmente `AllowedHosts`.

Na primeira execução são criados `data/identity.db` (contas) e, por cada registo, `data/users/{id}.db` (produtos e categorias desse utilizador). Em desenvolvimento, os ficheiros ficam em `data/` na raiz do repositório.

## Deploy (Vercel + Render)

| Serviço | O quê |
|---------|--------|
| **Vercel** | Frontend React (Vite). **Root Directory:** `frontend`. Configuração em `frontend/vercel.json` (build, `dist`, rewrites SPA). Variáveis: `VITE_API_BASE_URL` (URL pública da API, sem barra final), `VITE_TURNSTILE_SITE_KEY`. |
| **Render** | API via **Docker** (`Dockerfile` na raiz): imagem multi-stage, utilizador não-root, `PORT` e `ASPNETCORE_URLS` conforme documentação Render. Variáveis: `Jwt__Key`, `Cosmos__Token`, `Turnstile__SecretKey`, `CORS_ORIGINS`, opcionalmente `AllowedHosts`. |

O backend confia no último salto de `X-Forwarded-For` (`ForwardLimit = 1`), adequado a um proxy único como o da Render.

## Executar testes

```bash
npm test
```

Ou em modo watch:

```bash
npm run test:watch
```

Os testes de integração usam SQLite em memória e um stub da API Cosmos, sem dependências externas.

### Build de produção (verificação local)

```bash
npm run build
```

Compila o frontend (`tsc` + `vite build`). A API publica-se com `dotnet publish` ou com o `Dockerfile` usado na Render.

## Estrutura do projeto

```
product_manager/
├── backend/
│   ├── ProductStore.Api/
│   │   ├── Controllers/               # Auth, Products, Categories, Cosmos
│   │   ├── Services/                  # ProductService, CategoryService, Tenant*, Turnstile, JWT
│   │   ├── Domain/                    # CategoryRules, SkuSource
│   │   ├── DTOs/
│   │   ├── Models/
│   │   ├── Data/                      # AppDbContext, Migrations, DesignTimeAppDbContextFactory (EF design-time)
│   │   ├── Middleware/                # GlobalExceptionHandler
│   │   ├── Validation/
│   │   └── Exceptions/
│   └── ProductStore.Api.Tests/
├── frontend/
│   ├── vercel.json                    # Deploy Vercel (Root Directory = frontend)
│   └── src/
│       ├── pages/
│       ├── components/                # HttpLogViewer, ErrorBoundary, ProtectedRoute, …
│       ├── contexts/
│       ├── api/
│       ├── lib/
│       ├── hooks/
│       └── types/
├── data/                              # identity.db e users/*.db (runtime; não versionar segredos)
├── Dockerfile                         # Build da API para Render
├── .env.example
└── package.json
```

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/auth/register` | Corpo: `userName`, `password`, `website` (honeypot), `turnstileToken`. Valida Turnstile (exceto Development), cria utilizador e tenant, devolve JWT |
| `POST` | `/api/auth/login` | Credenciais válidas → `pendingToken` + prazo (ainda não autoriza `/api/products`) |
| `POST` | `/api/auth/complete-turnstile` | `pendingToken` + token Turnstile → JWT de sessão (rate limit como login) |
| `GET` | `/api/products` | Lista paginada com filtros |
| `POST` | `/api/products` | Cria produto |
| `GET` | `/api/products/{id}` | Busca produto por ID |
| `PUT` | `/api/products/{id}` | Atualiza produto |
| `DELETE` | `/api/products/{id}` | Remove produto |
| `GET` | `/api/categories` | Lista categorias |
| `POST` | `/api/categories` | Cria categoria |
| `GET` | `/api/cosmos/gtins/{gtin}` | Pré-visualização por GTIN (token Cosmos; JWT obrigatório) |

Rotas `/api/products`, `/api/categories` e `/api/cosmos/*` exigem `Authorization: Bearer <jwt>`.

### Parâmetros de listagem (`GET /api/products`)

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `search` | string | Busca em nome, SKU e descrição (máx. 256 caracteres) |
| `sku` / `name` | string | Filtros (máx. 64 / 256 caracteres) |
| `categoryId` | GUID | Filtra por categoria |
| `minPrice` / `maxPrice` | decimal | Faixa de preço |
| `stockFilter` | `available` \| `low` | Situação de estoque |
| `page` | int | Página (padrão: 1) |
| `pageSize` | int | Itens por página (padrão: 10, máx: 100) |

## Decisões de arquitetura

- **Turnstile no login e no registo**: o login continua em dois passos (`pendingToken` → `complete-turnstile`). O registo envia `turnstileToken` no mesmo pedido que cria a conta; em Development a verificação é ignorada na API.
- **Multi-tenant por ficheiro**: cada utilizador (claim `NameIdentifier`) usa `data/users/{userId}.db`; no registo o ficheiro é criado e migrado. Novas migrações EF aplicam-se ao tenant na primeira abertura do contexto após reinício do processo.
- **Categorias**: `NormalizedName` único por tenant alinha duplicados lógicos (acentos/caso) com a regra `CategoryRules`.
- **SQLite em desenvolvimento**: dados em `data/` fora do projeto da API para o `dotnet watch` não reagir a ficheiros WAL.
- **Proxy Vite**: `/api/*` → API local sem CORS extra no browser.
- **DotEnvBootstrap**: carrega `.env` da raiz e do projeto API; variáveis de ambiente do sistema (Render) sobrepõem-se a `appsettings`.
- **GlobalExceptionHandler**: erros de domínio com HTTP semântico; detalhes sensíveis apenas em Development.
- **Integração**: `WebApplicationFactory` cobre a pipeline HTTP com SQLite em memória.
