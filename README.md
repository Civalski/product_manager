# ProductStore

Aplicação fullstack para gerenciamento de produtos, com cadastro de categorias, autenticação por usuário e integração com a API [Bluesoft Cosmos](https://cosmos.bluesoft.com.br/) para busca de dados por GTIN/código de barras.

## Visão geral

O projeto foi organizado para oferecer:

- cadastro, edição, listagem e remoção de produtos
- busca e filtros por nome, SKU, categoria, preço e estoque
- enriquecimento de dados por GTIN via Bluesoft Cosmos
- autenticação com JWT
- isolamento de dados por usuário com SQLite
- frontend React e API ASP.NET Core executando localmente com um único comando

## Stack

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | ASP.NET Core 10, EF Core 10, SQLite, ASP.NET Core Identity, JWT Bearer, FluentValidation |
| **Frontend** | React 19, TypeScript, Vite 8, React Router 7, Cloudflare Turnstile |
| **Testes** | xUnit, `WebApplicationFactory` |
| **Orquestração** | Node.js com `concurrently` |

## Funcionalidades principais

- CRUD completo de produtos
- listagem paginada com filtros e busca textual
- criação e listagem de categorias
- pré-visualização de produto por GTIN antes de salvar
- validação no frontend e backend
- tratamento global de erros com respostas HTTP consistentes
- tema claro/escuro persistido no navegador
- painel de log HTTP no frontend para apoio ao debug

## Pré-requisitos

| Ferramenta | Versão mínima |
|------------|--------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |
| [Node.js](https://nodejs.org/) | 20 LTS |
| [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) | restaurado via `dotnet-tools.json` |

## Como executar

Na raiz do projeto:

```bash
npm start
```

Esse comando:

- restaura as ferramentas do .NET
- instala as dependências da raiz e de `frontend/`
- cria `.env` e `frontend/.env` a partir dos arquivos de exemplo, se ainda não existirem
- inicia API e frontend em paralelo

Endpoints locais:

- Frontend: [http://localhost:5173](http://localhost:5173)
- API: [http://localhost:5127](http://localhost:5127)
- OpenAPI: [http://localhost:5127/openapi/v1.json](http://localhost:5127/openapi/v1.json)

Se as dependências já estiverem instaladas:

```bash
npm run dev
```

## Configuração

As variáveis podem ser definidas em `.env` na raiz, em `backend/ProductStore.Api/.env` ou em `frontend/.env`, conforme o caso.

### Variáveis mais importantes

| Variável | Onde usar | Quando é necessária |
|----------|-----------|---------------------|
| `Cosmos__Token` | raiz ou API | para consultar GTIN pela Bluesoft Cosmos |
| `Turnstile__SecretKey` | raiz ou API | em produção, para registro e conclusão do login |
| `VITE_TURNSTILE_SITE_KEY` | frontend | em produção, junto com o Turnstile |
| `VITE_API_BASE_URL` | frontend | em deploy; em ambiente local pode ficar vazio |
| `Jwt__Key` | API | obrigatório em produção, com pelo menos 32 caracteres |
| `CORS_ORIGINS` | API | obrigatório em produção com a origem do frontend |
| `TestAuth:Enabled` | API | uso exclusivo dos testes de integração |

### Observações

- Em desenvolvimento local, o fluxo com GTIN só funciona se `Cosmos__Token` estiver configurado.
- Sem token do Cosmos, ainda é possível cadastrar produtos usando SKU interno.
- Em `Development`, a API dispensa o widget do Turnstile.
- O proxy do Vite encaminha `/api` para a porta `5127`, então `VITE_API_BASE_URL` pode ficar vazio localmente.
- Use `.env.example` como referência para preencher os arquivos de ambiente.

Na primeira execução, o projeto cria:

- `data/identity.db` para contas de usuário
- `data/users/{id}.db` para produtos e categorias de cada usuário

## Testes e build

Executar testes:

```bash
npm test
```

Modo watch:

```bash
npm run test:watch
```

Build local:

```bash
npm run build
```

Os testes de integração usam SQLite em memória e um stub da API Cosmos. O build compila o frontend, enquanto a API pode ser publicada com `dotnet publish` ou via `Dockerfile`.

## Deploy

### Frontend no Vercel

- diretório raiz do projeto: `frontend`
- configuração em `frontend/vercel.json`
- variáveis esperadas: `VITE_API_BASE_URL` e `VITE_TURNSTILE_SITE_KEY`

### Backend no Render

- publicação via `Dockerfile` na raiz
- variáveis obrigatórias: `Jwt__Key`, `CORS_ORIGINS` e `Turnstile__SecretKey`
- variáveis opcionais: `Cosmos__Token` e `AllowedHosts`

Rotas de health check:

- `GET /health`
- `GET /ready`

## Estrutura do projeto

```text
product_manager/
├── backend/
│   ├── ProductStore.Api/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── Domain/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Middleware/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Validation/
│   └── ProductStore.Api.Tests/
├── frontend/
│   ├── src/
│   └── vercel.json
├── data/
├── Dockerfile
├── .env.example
└── package.json
```

## API

As rotas principais ficam sob `/api` e a documentação OpenAPI pode ser consultada em `http://localhost:5127/openapi/v1.json`.

Rotas protegidas:

- `/api/products`
- `/api/categories`
- `/api/cosmos/*`

Essas rotas exigem `Authorization: Bearer <jwt>`.
