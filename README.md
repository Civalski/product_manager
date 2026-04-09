# ProductStore

Aplicação fullstack de gerenciamento de produtos com integração à API [Bluesoft Cosmos](https://cosmos.bluesoft.com.br/) para enriquecimento de dados por GTIN/código de barras.

> **Prova técnica / revisão:** após clonar, execute `dotnet tool restore`, `npm install`, `npm install --prefix frontend`, copie `.env.example` → `.env` e rode `npm test` e `npm run build` para validar o ambiente. O fluxo de login usa Cloudflare Turnstile (ver secção *Autenticação*).

## Stack

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | ASP.NET Core 10 · EF Core 10 · SQLite · ASP.NET Core Identity · JWT Bearer · FluentValidation |
| **Frontend** | React 19 · TypeScript · Vite 8 · React Router 7 · Cloudflare Turnstile (pós-login) |
| **Testes** | xUnit · `WebApplicationFactory` (integração) |
| **Orquestração** | Node.js (`concurrently`) |

## Funcionalidades

- **CRUD completo de produtos** com listagem paginada, busca por nome/SKU/descrição e filtros (categoria, faixa de preço, situação de estoque)
- **Integração Bluesoft Cosmos**: pré-visualização de produto por GTIN antes de salvar, com importação de nome, marca, imagem, dimensões e metadados EAN/NCM/GPC
- **Categorias**: listagem e criação; regra de negócio — eletrônicos exigem preço mínimo de R$ 100
- **Validação de dados** no backend (FluentValidation) e no frontend, com mensagens de erro detalhadas via Problem Details (RFC 7807)
- **Tratamento global de erros**: mapeamento de exceções de domínio → códigos HTTP corretos (400/404/409/429/502/503)
- **Tema claro/escuro** persistido via `localStorage`
- **Log de requisições HTTP** embutido no frontend (painel dedicado para debug)
- **Autenticação**: registo e login (nome de utilizador e palavra-passe), JWT no cliente; **após o login com palavra-passe** o utilizador conclui a verificação **Cloudflare Turnstile** antes de receber o JWT de sessão (o registo devolve JWT diretamente). Cada utilizador tem um SQLite próprio em `data/users/{id}.db` (esquema migrado, sem dados iniciais)

## Pré-requisitos

| Ferramenta | Versão mínima |
|------------|--------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |
| [Node.js](https://nodejs.org/) | 20 LTS |
| [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) | instalado via `dotnet-tools.json` |

## Configuração e execução

### 1. Restaurar ferramentas .NET

```bash
dotnet tool restore
```

### 2. Configurar variáveis de ambiente

Copie o ficheiro de exemplo e ajuste as variáveis (na raiz do repositório ou em `backend/ProductStore.Api/.env` — o último carregado prevalece por chave):

```bash
# Linux / macOS / Git Bash
cp .env.example .env

# Windows (PowerShell)
Copy-Item .env.example .env
```

- **Cosmos** (`Cosmos__Token`): necessário apenas para a pré-visualização por GTIN; sem token, o enriquecimento por código de barras fica desabilitado.
- **Turnstile** (`Turnstile__SecretKey` no `.env` da raiz): chave **secreta** do widget (siteverify), a mesma que no Render. **Desativado em desenvolvimento local**: com `ASPNETCORE_ENVIRONMENT=Development` e `npm run dev` (Vite), o login não exige widget nem domínio no Cloudflare.
- **Frontend** (`frontend/.env`, copiado de `frontend/.env.example`): `VITE_TURNSTILE_SITE_KEY` (chave do site, pública), necessária para builds de produção / `vite preview`. Em `npm run dev` o fluxo ignora o Turnstile no cliente e na API.

Em **produção**, defina `Jwt__Key` (ou `Jwt:Key` no `appsettings`) com pelo menos **32 caracteres** secretos; o valor de desenvolvimento em `appsettings.json` não deve ser usado em produção. Para API exposta (ex.: Render) e front no Vercel, defina `CORS_ORIGINS` com a origem do site.

### 3. Instalar dependências Node

```bash
npm install
npm install --prefix frontend
```

### 4. Iniciar em modo desenvolvimento

```bash
npm run dev
```

Isto inicia em paralelo o backend (`dotnet watch` na API) e o frontend (Vite). Também pode executar cada parte à parte: `dotnet run --project backend/ProductStore.Api` e `npm run dev --prefix frontend`.

Isso inicia simultaneamente:
- **Frontend** → [http://localhost:5173](http://localhost:5173)
- **API** → [http://localhost:5127](http://localhost:5127)
- **OpenAPI/Swagger** → [http://localhost:5127/openapi/v1.json](http://localhost:5127/openapi/v1.json)

Na primeira execução são criados `data/identity.db` (contas) e, por cada registo, `data/users/{id}.db` (produtos e categorias desse utilizador). Em desenvolvimento, os ficheiros ficam em `data/` na raiz do repositório.

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

Compila o frontend (`tsc` + `vite build`). A API publica-se com `dotnet publish` (ver `Dockerfile` para exemplo usado em ambientes como Render).

## Estrutura do projeto

```
product_manager/
├── backend/
│   ├── ProductStore.Api/              # API ASP.NET Core
│   │   ├── Controllers/               # AuthController, ProductsController, CategoriesController, CosmosController
│   │   ├── Services/                  # ProductService, CategoryService, CosmosGtinValidator
│   │   ├── Domain/                    # Regras de negócio (CategoryRules, SkuSource)
│   │   ├── DTOs/                      # Request/Response models
│   │   ├── Models/                    # Entidades EF Core (Product, Category)
│   │   ├── Data/                      # AppDbContext + Migrations; Identity (MigrationsIdentity)
│   │   ├── Middleware/                # GlobalExceptionHandler
│   │   ├── Validation/                # Validators FluentValidation
│   │   └── Exceptions/                # Exceções de domínio tipadas
│   └── ProductStore.Api.Tests/        # Testes de integração (xUnit)
├── frontend/
│   └── src/
│       ├── pages/                     # Login, Register, TurnstileVerify, ProductList, ProductForm, ProductDetail
│       ├── components/                # CosmosPreviewPanel, HttpLogViewer, ProtectedRoute
│       ├── contexts/                  # AuthContext
│       ├── api/                       # authApi, productsApi, categoriesApi, cosmosApi
│       ├── lib/                       # apiClient, authStorage, productCosmos
│       ├── hooks/                     # useTheme
│       └── types/                     # Tipos TypeScript alinhados com a API
├── data/                              # identity.db e users/*.db (gerados em runtime; `users/` versionado só com .gitkeep)
├── scripts/                           # kill-orphan-api.cjs (limpeza de processos)
├── .env.example                       # Modelo de variáveis de ambiente
└── package.json                       # Scripts de orquestração (dev, test)
```

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/auth/register` | Regista utilizador e devolve JWT |
| `POST` | `/api/auth/login` | Valida credenciais e devolve `pendingToken` + prazo (JWT de login pendente; ainda não acede à API de produtos) |
| `POST` | `/api/auth/complete-turnstile` | Troca `pendingToken` + token do widget Turnstile pelo JWT de sessão |
| `GET` | `/api/products` | Lista paginada com filtros |
| `POST` | `/api/products` | Cria produto |
| `GET` | `/api/products/{id}` | Busca produto por ID |
| `PUT` | `/api/products/{id}` | Atualiza produto |
| `DELETE` | `/api/products/{id}` | Remove produto |
| `GET` | `/api/categories` | Lista categorias |
| `POST` | `/api/categories` | Cria categoria |
| `GET` | `/api/cosmos/gtins/{gtin}` | Pré-visualização de produto por GTIN (requer token Cosmos; rota protegida por JWT) |

Rotas `/api/products`, `/api/categories` e `/api/cosmos/*` exigem cabeçalho `Authorization: Bearer <jwt>`.

### Parâmetros de listagem (`GET /api/products`)

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `search` | string | Busca em nome, SKU e descrição (case-insensitive) |
| `categoryId` | GUID | Filtra por categoria |
| `minPrice` / `maxPrice` | decimal | Faixa de preço |
| `stockFilter` | `available` \| `low` | Situação de estoque |
| `page` | int | Página atual (padrão: 1) |
| `pageSize` | int | Itens por página (padrão: 10, máx: 100) |

## Decisões de arquitetura

- **Login em dois passos (Turnstile)**: após palavra-passe válida, a API emite um JWT de *login pendente* (audiência distinta) que não autoriza `/api/products` nem `/api/cosmos`; só após `POST /api/auth/complete-turnstile` com token do widget é emitido o Bearer de sessão. O registo mantém um único passo (JWT imediato). Em **Development**, a API aceita `complete-turnstile` sem token do widget e o front conclui o login sem carregar o script Cloudflare.
- **Multi-tenant por ficheiro**: cada utilizador autenticado (claim `NameIdentifier`) usa um `AppDbContext` em `data/users/{userId}.db`; no registo o ficheiro é criado e migrado sem dados iniciais.
- **SQLite em desenvolvimento**: elimina dependência de banco externo; caminho configurado fora do projeto para o `dotnet watch` não monitorar os arquivos WAL.
- **Proxy Vite**: requisições `/api/*` do frontend são redirecionadas para a API em desenvolvimento, evitando configuração de CORS no browser.
- **DotEnvBootstrap**: carrega `.env` da raiz do repositório e do diretório do projeto API em sequência, com precedência do último — facilita configuração sem alterar `appsettings.json`.
- **GlobalExceptionHandler**: exceções de domínio tipadas mapeadas para respostas HTTP semânticas, mantendo stack traces restritos ao ambiente de desenvolvimento.
- **Testes de integração com `WebApplicationFactory`**: toda a pipeline HTTP é testada contra a API real (com banco em memória), garantindo que serialização, validação e regras de negócio funcionem end-to-end.
