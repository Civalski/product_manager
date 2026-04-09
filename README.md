# ProductStore

Aplicação fullstack de gerenciamento de produtos com integração à API [Bluesoft Cosmos](https://cosmos.bluesoft.com.br/) para enriquecimento de dados por GTIN/código de barras.

## Stack

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | ASP.NET Core 10 · EF Core 10 · SQLite · FluentValidation |
| **Frontend** | React 19 · TypeScript · Vite 8 · React Router 7 |
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
- **Dados de exemplo** inseridos automaticamente na primeira execução

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

Copie o arquivo de exemplo e preencha seu token da API Cosmos (opcional):

```bash
cp .env.example .env
```

O token é necessário apenas para usar a funcionalidade de pré-visualização por GTIN. Sem ele, o sistema funciona normalmente — apenas o enriquecimento por código de barras fica desabilitado.

### 3. Instalar dependências Node

```bash
npm install
npm install --prefix frontend
```

### 4. Iniciar em modo desenvolvimento

```bash
npm run dev
```

Isso inicia simultaneamente:
- **Frontend** → [http://localhost:5173](http://localhost:5173)
- **API** → [http://localhost:5127](http://localhost:5127)
- **OpenAPI/Swagger** → [http://localhost:5127/openapi/v1.json](http://localhost:5127/openapi/v1.json)

O banco SQLite é criado automaticamente em `data/products.db` com categorias e produtos de exemplo na primeira execução.

## Executar testes

```bash
npm test
```

Ou em modo watch:

```bash
npm run test:watch
```

Os testes de integração usam SQLite em memória e um stub da API Cosmos, sem dependências externas.

## Estrutura do projeto

```
product_manager/
├── backend/
│   ├── ProductStore.Api/              # API ASP.NET Core
│   │   ├── Controllers/               # ProductsController, CategoriesController, CosmosController
│   │   ├── Services/                  # ProductService, CategoryService, CosmosGtinValidator
│   │   ├── Domain/                    # Regras de negócio (CategoryRules, SkuSource)
│   │   ├── DTOs/                      # Request/Response models
│   │   ├── Models/                    # Entidades EF Core (Product, Category)
│   │   ├── Data/                      # AppDbContext + Migrations
│   │   ├── Middleware/                # GlobalExceptionHandler
│   │   ├── Validation/                # Validators FluentValidation
│   │   └── Exceptions/                # Exceções de domínio tipadas
│   └── ProductStore.Api.Tests/        # Testes de integração (xUnit)
├── frontend/
│   └── src/
│       ├── pages/                     # ProductListPage, ProductFormPage, ProductDetailPage
│       ├── components/                # CosmosPreviewPanel, HttpLogViewer
│       ├── api/                       # productsApi, categoriesApi, cosmosApi
│       ├── lib/                       # apiClient (ApiError, formatação de erros), productCosmos
│       ├── hooks/                     # useTheme
│       └── types/                     # Tipos TypeScript alinhados com a API
├── data/                              # Banco SQLite (gerado em runtime, não versionado)
├── scripts/                           # kill-orphan-api.cjs (limpeza de processos)
├── .env.example                       # Modelo de variáveis de ambiente
└── package.json                       # Scripts de orquestração (dev, test)
```

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/products` | Lista paginada com filtros |
| `POST` | `/api/products` | Cria produto |
| `GET` | `/api/products/{id}` | Busca produto por ID |
| `PUT` | `/api/products/{id}` | Atualiza produto |
| `DELETE` | `/api/products/{id}` | Remove produto |
| `GET` | `/api/categories` | Lista categorias |
| `POST` | `/api/categories` | Cria categoria |
| `GET` | `/api/cosmos/gtin/{gtin}` | Pré-visualização de produto por GTIN (requer token) |

### Parâmetros de listagem (`GET /api/products`)

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `search` | string | Busca em nome, SKU e descrição (case-insensitive) |
| `categoryId` | GUID | Filtra por categoria |
| `minPrice` / `maxPrice` | decimal | Faixa de preço |
| `stockFilter` | `available` \| `out` \| `low` | Situação de estoque |
| `page` | int | Página atual (padrão: 1) |
| `pageSize` | int | Itens por página (padrão: 10, máx: 100) |

## Decisões de arquitetura

- **SQLite em desenvolvimento**: elimina dependência de banco externo; caminho configurado fora do projeto para o `dotnet watch` não monitorar os arquivos WAL.
- **Proxy Vite**: requisições `/api/*` do frontend são redirecionadas para a API em desenvolvimento, evitando configuração de CORS no browser.
- **DotEnvBootstrap**: carrega `.env` da raiz do repositório e do diretório do projeto API em sequência, com precedência do último — facilita configuração sem alterar `appsettings.json`.
- **GlobalExceptionHandler**: exceções de domínio tipadas mapeadas para respostas HTTP semânticas, mantendo stack traces restritos ao ambiente de desenvolvimento.
- **Testes de integração com `WebApplicationFactory`**: toda a pipeline HTTP é testada contra a API real (com banco em memória), garantindo que serialização, validação e regras de negócio funcionem end-to-end.
