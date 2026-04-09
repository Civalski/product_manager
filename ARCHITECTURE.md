# Arquitetura do ProductStore

Este documento descreve a arquitetura técnica do ProductStore, incluindo estrutura de componentes, fluxo de dados, decisões arquiteturais e diagramas.

## Visão Geral

ProductStore é uma aplicação fullstack multi-tenant para gestão de produtos com integração à API Bluesoft Cosmos (GTIN/EAN).

```mermaid
graph TB
    subgraph "Cliente"
        Browser[Navegador Web]
    end
    
    subgraph "Frontend - Vercel"
        Vite[Vite Dev Server / Build]
        React[React App]
        Router[React Router]
        Auth[Auth Context]
        API_Client[API Client]
    end
    
    subgraph "Backend - Render"
        API[ASP.NET Core API]
        Auth_Middleware[JWT Auth]
        Rate_Limiter[Rate Limiter]
        CORS[CORS Policy]
        
        subgraph "Controllers"
            Auth_Controller[AuthController]
            Products_Controller[ProductsController]
            Categories_Controller[CategoriesController]
            Cosmos_Controller[CosmosController]
        end
        
        subgraph "Services"
            Product_Service[ProductService]
            Category_Service[CategoryService]
            JWT_Service[JwtTokenService]
            Turnstile_Service[TurnstileService]
            Cosmos_Service[CosmosGtinValidator]
            Tenant_Factory[TenantDbContextFactory]
        end
        
        subgraph "Data Layer"
            Identity_DB[(Identity DB<br/>SQLite)]
            Tenant_DB1[(Tenant 1 DB<br/>SQLite)]
            Tenant_DB2[(Tenant 2 DB<br/>SQLite)]
            Tenant_DBN[(Tenant N DB<br/>SQLite)]
        end
    end
    
    subgraph "Serviços Externos"
        Cloudflare[Cloudflare Turnstile]
        Bluesoft[Bluesoft Cosmos API]
    end
    
    Browser --> React
    React --> Router
    React --> Auth
    React --> API_Client
    API_Client -->|HTTPS + JWT| API
    
    API --> CORS
    CORS --> Rate_Limiter
    Rate_Limiter --> Auth_Middleware
    Auth_Middleware --> Auth_Controller
    Auth_Middleware --> Products_Controller
    Auth_Middleware --> Categories_Controller
    Auth_Middleware --> Cosmos_Controller
    
    Auth_Controller --> JWT_Service
    Auth_Controller --> Turnstile_Service
    Auth_Controller --> Identity_DB
    
    Products_Controller --> Product_Service
    Categories_Controller --> Category_Service
    Cosmos_Controller --> Cosmos_Service
    
    Product_Service --> Tenant_Factory
    Category_Service --> Tenant_Factory
    
    Tenant_Factory -->|userId: abc| Tenant_DB1
    Tenant_Factory -->|userId: def| Tenant_DB2
    Tenant_Factory -->|userId: xyz| Tenant_DBN
    
    Turnstile_Service -->|Verify Token| Cloudflare
    Cosmos_Service -->|Fetch GTIN| Bluesoft
    
    style Browser fill:#e1f5ff
    style React fill:#61dafb
    style API fill:#512bd4
    style Identity_DB fill:#ffeb3b
    style Tenant_DB1 fill:#ffeb3b
    style Tenant_DB2 fill:#ffeb3b
    style Tenant_DBN fill:#ffeb3b
    style Cloudflare fill:#f38020
    style Bluesoft fill:#4285f4
```

---

## Stack Tecnológica

### Frontend
| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Framework | React | 19 |
| Linguagem | TypeScript | 6.0 |
| Build Tool | Vite | 8.0 |
| Routing | React Router | 7 |
| HTTP Client | Fetch API | Nativo |
| Estilo | CSS Modules | Nativo |
| CAPTCHA | Cloudflare Turnstile | - |
| Hospedagem | Vercel | - |

### Backend
| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Framework | ASP.NET Core | 10 |
| Linguagem | C# | 13 |
| ORM | Entity Framework Core | 10 |
| Database | SQLite | 3 |
| Autenticação | ASP.NET Identity + JWT | 10 |
| Validação | FluentValidation | 11.3 |
| Containerização | Docker | - |
| Hospedagem | Render | - |

---

## Fluxo de Autenticação

```mermaid
sequenceDiagram
    participant U as Utilizador
    participant F as Frontend
    participant T as Turnstile
    participant API as Backend API
    participant DB as Identity DB
    
    Note over U,DB: 1. REGISTO
    U->>F: Preenche formulário
    F->>T: Solicita widget
    T->>F: Renderiza CAPTCHA
    U->>T: Completa desafio
    T->>F: Token Turnstile
    F->>API: POST /api/auth/register<br/>{userName, password, turnstileToken}
    API->>T: Verifica token
    T->>API: ✓ Válido
    API->>DB: Cria utilizador (hash password)
    API->>API: Cria tenant DB (userId.db)
    API->>API: Gera JWT
    API->>F: {token, userName, expiresAt}
    F->>F: Armazena JWT (localStorage)
    
    Note over U,DB: 2. LOGIN (Dois Passos)
    U->>F: Preenche credenciais
    F->>API: POST /api/auth/login<br/>{userName, password}
    API->>DB: Valida credenciais
    DB->>API: ✓ Válido
    API->>API: Gera pendingToken (10min)
    API->>F: {pendingToken, expiresAt}
    F->>F: Armazena pending (sessionStorage)
    
    F->>T: Solicita widget
    T->>F: Renderiza CAPTCHA
    U->>T: Completa desafio
    T->>F: Token Turnstile
    F->>API: POST /api/auth/complete-turnstile<br/>{pendingToken, turnstileToken}
    API->>API: Valida pendingToken
    API->>T: Verifica turnstileToken
    T->>API: ✓ Válido
    API->>API: Gera JWT final (7 dias)
    API->>F: {token, userName, expiresAt}
    F->>F: Armazena JWT (localStorage)
    F->>F: Limpa pending (sessionStorage)
    
    Note over U,DB: 3. REQUISIÇÕES AUTENTICADAS
    F->>API: GET /api/products<br/>Authorization: Bearer {JWT}
    API->>API: Valida JWT
    API->>API: Extrai userId do claim
    API->>API: Acessa tenant DB (userId.db)
    API->>F: {produtos do tenant}
```

---

## Fluxo de Gestão de Produtos

```mermaid
sequenceDiagram
    participant U as Utilizador
    participant F as Frontend
    participant API as Backend
    participant PS as ProductService
    participant CS as CosmosService
    participant Cosmos as Bluesoft API
    participant TF as TenantFactory
    participant DB as Tenant DB
    
    Note over U,DB: CRIAR PRODUTO (SKU Interno)
    U->>F: Preenche formulário (SKU interno)
    F->>API: POST /api/products<br/>{sku, name, price, categoryId}
    API->>PS: CreateAsync(request)
    PS->>TF: Obtém DbContext do tenant
    TF->>TF: Identifica userId via JWT claim
    TF->>DB: Conecta a userId.db
    PS->>DB: Valida SKU único
    PS->>DB: Valida categoria existe
    PS->>PS: Aplica regras de negócio<br/>(ex: eletrônico min R$50)
    PS->>DB: INSERT produto
    DB->>PS: Produto criado
    PS->>API: ProductResponse
    API->>F: 201 Created + produto
    F->>U: Exibe sucesso
    
    Note over U,DB: CRIAR PRODUTO (SKU GTIN/Cosmos)
    U->>F: Cola código de barras GTIN
    F->>F: Detecta formato numérico 8-14 dígitos
    F->>API: GET /api/cosmos/gtins/{gtin}
    API->>CS: FetchProductAsync(gtin)
    CS->>Cosmos: GET /gtins/{gtin}.json<br/>X-Cosmos-Token: {token}
    Cosmos->>CS: {description, brand, avgPrice, images, ncm, gpc}
    CS->>API: CosmosGtinProductDto
    API->>F: Dados do produto
    F->>F: Pré-preenche formulário<br/>(nome, descrição, preço, imagem)
    U->>F: Ajusta e submete
    F->>API: POST /api/products<br/>{skuSource: "CosmosGtin", sku: gtin, ...}
    API->>PS: CreateAsync(request)
    PS->>CS: FetchProductAsync(gtin)<br/>(busca novamente para garantir dados atuais)
    CS->>Cosmos: GET /gtins/{gtin}.json
    Cosmos->>CS: Dados do produto
    PS->>PS: Mescla dados Cosmos + input do utilizador
    PS->>DB: INSERT produto + cosmosMetadataJson
    DB->>PS: Produto criado
    PS->>API: ProductResponse
    API->>F: 201 Created
    F->>U: Exibe sucesso
    
    Note over U,DB: LISTAR PRODUTOS (Paginação + Filtros)
    U->>F: Acessa lista / busca / filtra
    F->>API: GET /api/products?search=termo&categoryId=guid&minPrice=50&page=1&pageSize=20
    API->>PS: GetListAsync(query)
    PS->>TF: Obtém DbContext do tenant
    TF->>DB: Conecta a userId.db
    PS->>DB: SELECT com filtros + paginação<br/>(LIKE %search%, JOIN category, WHERE price)
    DB->>PS: Lista de produtos + total
    PS->>API: PagedResult<ProductResponse>
    API->>F: {items: [...], totalCount, page, pageSize}
    F->>U: Renderiza grid paginado
    
    Note over U,DB: ATUALIZAR PRODUTO
    U->>F: Edita produto
    F->>API: PUT /api/products/{id}<br/>{sku, name, price, ...}
    API->>PS: UpdateAsync(id, request)
    PS->>DB: SELECT produto WHERE id = {id}
    PS->>DB: Valida SKU único (exceto próprio produto)
    PS->>PS: Aplica regras de negócio
    PS->>DB: UPDATE produto
    DB->>PS: Produto atualizado
    PS->>API: ProductResponse
    API->>F: 200 OK + produto
    F->>U: Exibe sucesso
    
    Note over U,DB: ELIMINAR PRODUTO
    U->>F: Clica eliminar
    F->>F: Confirma ação
    F->>API: DELETE /api/products/{id}
    API->>PS: DeleteAsync(id)
    PS->>DB: DELETE FROM products WHERE id = {id}
    DB->>PS: Produto eliminado
    PS->>API: 204 No Content
    API->>F: Sucesso
    F->>U: Remove da lista
```

---

## Arquitetura Multi-Tenant

O ProductStore utiliza uma estratégia de **Database-per-Tenant** para isolamento completo de dados.

```mermaid
graph TB
    subgraph "Camada de Aplicação"
        Request[HTTP Request]
        JWT_Middleware[JWT Middleware]
        Controller[Controller]
    end
    
    subgraph "Tenant Resolution"
        HTTP_Context[HttpContext]
        Claims[JWT Claims]
        UserId[NameIdentifier Claim]
    end
    
    subgraph "TenantAppDbContextFactory"
        Factory[CreateDbContext]
        Resolve[Resolve Data Directory]
        Connection[Build Connection String]
        Migrate[Auto Migrate if First Access]
    end
    
    subgraph "File System - data/users/"
        DB1[abc123.db<br/>User 1 Products]
        DB2[def456.db<br/>User 2 Products]
        DB3[xyz789.db<br/>User 3 Products]
    end
    
    subgraph "Identity DB - data/"
        Identity[(identity.db<br/>Users & Auth)]
    end
    
    Request --> JWT_Middleware
    JWT_Middleware --> HTTP_Context
    HTTP_Context --> Claims
    Claims --> UserId
    
    Controller --> Factory
    Factory --> UserId
    UserId -->|userId: abc123| Resolve
    Resolve --> Connection
    Connection -->|Data Source=data/users/abc123.db| DB1
    Connection -->|Data Source=data/users/def456.db| DB2
    Connection -->|Data Source=data/users/xyz789.db| DB3
    
    Factory --> Migrate
    Migrate -->|First access after deploy| DB1
    Migrate -->|First access after deploy| DB2
    Migrate -->|First access after deploy| DB3
    
    JWT_Middleware -.->|Registo/Login| Identity
    
    style Request fill:#e1f5ff
    style UserId fill:#ff9800
    style DB1 fill:#4caf50
    style DB2 fill:#4caf50
    style DB3 fill:#4caf50
    style Identity fill:#2196f3
```

### Características:

1. **Isolamento Total**: Cada utilizador tem SQLite próprio
2. **Segurança**: Impossível acesso cross-tenant (validado via claim JWT)
3. **Auto-Migração**: Migrações EF aplicadas automaticamente no primeiro acesso
4. **Cache**: `ConcurrentDictionary` rastreia tenants já migrados
5. **Escalabilidade**: Fácil migração futura para PostgreSQL multi-tenant

---

## Modelo de Dados

### Identity Database (`identity.db`)

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserClaims : has
    AspNetUsers ||--o{ AspNetUserLogins : has
    AspNetUsers ||--o{ AspNetUserTokens : has
    AspNetUsers ||--o{ AspNetUserRoles : has
    AspNetRoles ||--o{ AspNetUserRoles : has
    AspNetRoles ||--o{ AspNetRoleClaims : has
    
    AspNetUsers {
        string Id PK
        string UserName UK
        string NormalizedUserName
        string Email
        string NormalizedEmail
        bool EmailConfirmed
        string PasswordHash
        string SecurityStamp
        string ConcurrencyStamp
        string PhoneNumber
        bool PhoneNumberConfirmed
        bool TwoFactorEnabled
        DateTime LockoutEnd
        bool LockoutEnabled
        int AccessFailedCount
    }
```

### Tenant Database (`{userId}.db`)

```mermaid
erDiagram
    Category ||--o{ Product : has
    Category ||--o{ CategoryFieldDefinition : has
    Product ||--o{ ProductCustomFieldValue : "stored as JSON"
    CategoryFieldDefinition ||--o{ ProductCustomFieldValue : defines
    
    Category {
        guid Id PK
        string Name
        string NormalizedName UK
        datetime CreatedAt
    }
    
    CategoryFieldDefinition {
        guid Id PK
        guid CategoryId FK
        string Name
        string NormalizedName
        string Type
        bool IsRequired
        int DisplayOrder
        string Options
    }
    
    Product {
        guid Id PK
        string Sku UK
        string Name
        string Description
        decimal Price
        decimal PaidAmount
        int Stock
        guid CategoryId FK
        string CosmosMetadataJson
        string CosmosGtin
        string CosmosThumbnailUrl
        string CosmosBrandName
        string CosmosBrandPictureUrl
        decimal CosmosAvgPrice
        decimal CosmosMaxPrice
        decimal CosmosMinPrice
        string CosmosPriceLabel
        string CosmosNcmCode
        string CosmosNcmDescription
        string CosmosGpcCode
        string CosmosGpcDescription
        string CosmosCommercialDescription
        string CustomFieldValuesJson
        datetime CreatedAt
        datetime UpdatedAt
    }
```

### Relacionamentos:

- **Category → Product**: 1:N (Restrict on delete - categoria não pode ser eliminada se tiver produtos)
- **Category → CategoryFieldDefinition**: 1:N (Cascade - campos eliminados com categoria)
- **Product.CustomFieldValuesJson**: Armazena valores dos campos personalizados em JSON

---

## Camadas da Aplicação

### 1. Presentation Layer (Controllers)

```
Controllers/
├── AuthController.cs         # Registo, login, Turnstile
├── ProductsController.cs     # CRUD produtos + export/import
├── CategoriesController.cs   # CRUD categorias + campos personalizados
└── CosmosController.cs       # Preview GTIN
```

**Responsabilidades:**
- Validação de entrada (FluentValidation)
- Autenticação/Autorização (JWT)
- Rate Limiting
- Mapeamento DTOs
- Retorno de Problem Details (RFC 7807)

### 2. Business Layer (Services)

```
Services/
├── IProductService.cs / ProductService.cs
├── ICategoryService.cs / CategoryService.cs
├── ICosmosGtinValidator.cs / CosmosGtinValidator.cs
├── ITurnstileVerificationService.cs / TurnstileVerificationService.cs
├── JwtTokenService.cs
├── TenantAppDbContextFactory.cs
├── TenantDatabaseProvisioner.cs
└── CategoryNormalizedNameSync.cs
```

**Responsabilidades:**
- Lógica de negócio
- Regras de domínio (ex: `CategoryRules.ElectronicsMinPrice`)
- Integração com APIs externas (Cosmos, Turnstile)
- Orquestração de operações complexas

### 3. Domain Layer

```
Domain/
├── CategoryRules.cs          # Regras de negócio de categorias
└── SkuSource.cs             # Enum: Internal vs CosmosGtin
```

**Responsabilidades:**
- Regras de negócio centralizadas
- Validações de domínio
- Enums e constantes

### 4. Data Layer

```
Data/
├── AppDbContext.cs                    # Contexto EF do tenant
├── AppIdentityDbContext.cs           # Contexto EF do identity
├── Migrations/                       # Migrações do tenant
└── MigrationsIdentity/              # Migrações do identity
```

**Responsabilidades:**
- Acesso a dados via EF Core
- Configuração de entidades
- Migrações de schema

### 5. Cross-Cutting Concerns

```
Middleware/
└── GlobalExceptionHandler.cs        # Tratamento global de erros

Validation/
├── RegisterRequestValidator.cs
├── LoginRequestValidator.cs
├── CreateProductRequestValidator.cs
└── ...

Exceptions/
└── AppExceptions.cs                 # Exceções de domínio customizadas

Configuration/
├── JwtOptions.cs
├── TurnstileOptions.cs
└── CosmosOptions.cs
```

---

## Fluxo de Dados - Requisição Típica

```mermaid
flowchart TD
    Start([Cliente faz requisição]) --> HTTPS{HTTPS?}
    HTTPS -->|Não| Redirect[Redirect para HTTPS]
    HTTPS -->|Sim| Forwarded[ForwardedHeaders Middleware]
    
    Forwarded --> Security[Security Headers Middleware]
    Security --> CORS{CORS válido?}
    CORS -->|Não| CORS_Error[403 Forbidden]
    CORS -->|Sim| RateLimit{Rate Limit OK?}
    
    RateLimit -->|Não| RateLimit_Error[429 Too Many Requests]
    RateLimit -->|Sim| Auth{JWT válido?}
    
    Auth -->|Não| Auth_Error[401 Unauthorized]
    Auth -->|Sim| Extract[Extrai userId do claim]
    
    Extract --> Routing[Routing para Controller]
    Routing --> Validation{Validação FluentValidation?}
    
    Validation -->|Não| Validation_Error[400 Bad Request]
    Validation -->|Sim| Service[Chama Service Layer]
    
    Service --> TenantFactory[TenantAppDbContextFactory]
    TenantFactory --> Resolve{Tenant DB existe?}
    
    Resolve -->|Não| Create[Cria e migra DB]
    Resolve -->|Sim| Connect[Conecta a userId.db]
    Create --> Connect
    
    Connect --> Business[Executa lógica de negócio]
    Business --> Rules{Regras de negócio OK?}
    
    Rules -->|Não| Business_Error[400/409 com Problem Details]
    Rules -->|Sim| External{Precisa API externa?}
    
    External -->|Cosmos| Cosmos_Call[Chama Bluesoft API]
    External -->|Turnstile| Turnstile_Call[Chama Cloudflare]
    External -->|Não| DB_Op[Operação no DB]
    
    Cosmos_Call --> DB_Op
    Turnstile_Call --> DB_Op
    
    DB_Op --> Success{Sucesso?}
    Success -->|Não| DB_Error[404/409/500 com Problem Details]
    Success -->|Sim| Response[Monta Response DTO]
    
    Response --> Log[Log de sucesso]
    Log --> Return[200/201/204 + JSON]
    
    CORS_Error --> ErrorLog[Log de erro]
    RateLimit_Error --> ErrorLog
    Auth_Error --> ErrorLog
    Validation_Error --> ErrorLog
    Business_Error --> ErrorLog
    DB_Error --> ErrorLog
    ErrorLog --> ErrorResponse[Retorna Problem Details]
    
    Redirect --> End([Fim])
    Return --> End
    ErrorResponse --> End
    
    style Start fill:#e1f5ff
    style End fill:#c8e6c9
    style CORS_Error fill:#ffcdd2
    style RateLimit_Error fill:#ffcdd2
    style Auth_Error fill:#ffcdd2
    style Validation_Error fill:#ffcdd2
    style Business_Error fill:#ffcdd2
    style DB_Error fill:#ffcdd2
    style Return fill:#c8e6c9
```

---

## Decisões Arquiteturais

### 1. **Multi-Tenant com Database-per-Tenant**

**Decisão:** Cada utilizador tem SQLite próprio (`data/users/{userId}.db`)

**Razões:**
- ✅ Isolamento total de dados (segurança máxima)
- ✅ Fácil backup/restore por utilizador
- ✅ Conformidade GDPR (delete total de dados do utilizador)
- ✅ Performance previsível (sem queries cross-tenant)
- ❌ Migrações mais complexas (múltiplos DBs)
- ❌ Não ideal para centenas de milhares de users

**Alternativas consideradas:**
- **Schema-per-Tenant**: Complexidade desnecessária para SQLite
- **Row-Level Security**: Risco de vazamento de dados

### 2. **JWT em localStorage (não em cookies)**

**Decisão:** JWT armazenado em `localStorage`

**Razões:**
- ✅ Simplicidade de implementação
- ✅ Funciona bem com CORS e múltiplas origens
- ✅ Sem problemas com SameSite policies
- ❌ Vulnerável a XSS (mitigado com headers de segurança)
- ❌ Não pode ser invalidado no servidor

**Mitigações:**
- Headers de segurança (X-XSS-Protection, CSP)
- Validação rigorosa de entrada
- Short-lived tokens (7 dias)

**Roadmap futuro:**
- Migrar para cookies HttpOnly + Secure
- Implementar refresh token pattern

### 3. **SQLite em Produção**

**Decisão:** SQLite para MVP/pequena escala

**Razões:**
- ✅ Zero configuração
- ✅ Custo zero (sem serviço de DB separado)
- ✅ Adequado para centenas de utilizadores
- ✅ File-based = fácil backup
- ❌ Limitações de concorrência
- ❌ Não ideal para alta escala

**Quando migrar para PostgreSQL:**
- Mais de 1000 utilizadores ativos
- Necessidade de replicação
- Queries complexas com JOINs pesados
- Necessidade de full-text search avançado

### 4. **React SPA com Client-Side Routing**

**Decisão:** React SPA hospedado na Vercel

**Razões:**
- ✅ Deploy automático via Git
- ✅ CDN global
- ✅ HTTPS gratuito
- ✅ Separação clara front/back
- ❌ SEO limitado (não é crítico para app interna)

### 5. **Rate Limiting por IP**

**Decisão:** Rate limiting global em todos endpoints autenticados

**Razões:**
- ✅ Proteção contra abuse/DDoS
- ✅ Controlo de custos (API Cosmos)
- ✅ `ForwardedHeaders` garante IP real via Render
- ❌ Usuários atrás de NAT compartilham limite
- ❌ VPNs podem ser bloqueados injustamente

**Configuração:**
- Auth endpoints: 5-10 req/min
- APIs gerais: 100 req/min
- Headers `Retry-After` em respostas 429

### 6. **Cloudflare Turnstile (não reCAPTCHA)**

**Decisão:** Turnstile para CAPTCHA

**Razões:**
- ✅ Melhor UX (challenge invisível na maioria dos casos)
- ✅ Privacy-friendly (não rastreia users)
- ✅ Gratuito até 1M req/mês
- ✅ API simples
- ❌ Vendor lock-in (menos crítico que Google)

---

## Performance e Escalabilidade

### Estratégias Atuais:

1. **Paginação**: Todas listagens com `page` + `pageSize` (max 100)
2. **Índices DB**: 
   - `Sku` (unique)
   - `CategoryId` (foreign key)
   - `Category.NormalizedName` (unique)
3. **Lazy Loading**: EF Core não carrega relações desnecessárias
4. **HTTP Caching**: Frontend cacheia categorias (mudam raramente)
5. **Connection Pooling**: EF Core reutiliza conexões SQLite

### Roadmap de Escalabilidade:

| Utilizadores | Estratégia |
|--------------|-----------|
| 0-1K | Atual (SQLite multi-tenant) |
| 1K-10K | Migrar para PostgreSQL multi-tenant (schema-per-tenant) |
| 10K-100K | PostgreSQL + Read replicas + Redis cache |
| 100K+ | Microserviços + Event sourcing + CQRS |

---

## Segurança - Camadas de Defesa

```mermaid
graph TB
    subgraph "Camada 1: Network"
        HTTPS[HTTPS Only]
        Cloudflare_WAF[Cloudflare WAF]
    end
    
    subgraph "Camada 2: Application Gateway"
        CORS_Policy[CORS Policy]
        Rate_Limiter[Rate Limiter]
        Security_Headers[Security Headers]
    end
    
    subgraph "Camada 3: Authentication"
        JWT_Validation[JWT Validation]
        Turnstile_Verify[Turnstile Verification]
        Password_Hash[Password Hashing]
    end
    
    subgraph "Camada 4: Authorization"
        Tenant_Isolation[Tenant Isolation]
        Resource_Owner[Resource Ownership Check]
    end
    
    subgraph "Camada 5: Input Validation"
        FluentValidation[FluentValidation]
        Parameterized_Queries[EF Core Parameterized Queries]
        Sanitization[Data Sanitization]
    end
    
    subgraph "Camada 6: Business Logic"
        Domain_Rules[Domain Rules]
        Category_Rules[Category Rules]
    end
    
    subgraph "Camada 7: Data"
        DB_Permissions[File System Permissions]
        Encrypted_Fields[Sensitive Data Hashing]
    end
    
    HTTPS --> CORS_Policy
    Cloudflare_WAF --> Rate_Limiter
    CORS_Policy --> Rate_Limiter
    Rate_Limiter --> Security_Headers
    Security_Headers --> JWT_Validation
    JWT_Validation --> Turnstile_Verify
    Turnstile_Verify --> Tenant_Isolation
    Tenant_Isolation --> FluentValidation
    FluentValidation --> Domain_Rules
    Domain_Rules --> DB_Permissions
    
    style HTTPS fill:#4caf50
    style CORS_Policy fill:#4caf50
    style JWT_Validation fill:#4caf50
    style Tenant_Isolation fill:#4caf50
    style FluentValidation fill:#4caf50
```

Ver `SECURITY.md` para detalhes completos.

---

## Deployment Architecture

```mermaid
graph TB
    subgraph "Developer"
        Git[Git Push]
    end
    
    subgraph "GitHub"
        Repo[Repository]
        Actions[GitHub Actions<br/>opcional]
    end
    
    subgraph "Vercel - Frontend"
        Vercel_Build[Build: npm run build]
        Vercel_Deploy[Deploy to CDN]
        Vercel_Edge[Edge Network]
    end
    
    subgraph "Render - Backend"
        Render_Docker[Docker Build<br/>Dockerfile]
        Render_Deploy[Deploy Container]
        Render_Service[Web Service<br/>Port :8080]
    end
    
    subgraph "Cloudflare"
        Turnstile_Service[Turnstile API]
    end
    
    subgraph "Bluesoft"
        Cosmos_API[Cosmos API]
    end
    
    subgraph "Client Browser"
        User[Utilizador Final]
    end
    
    Git --> Repo
    Repo -.->|Webhook| Vercel_Build
    Repo -.->|Webhook| Render_Docker
    
    Vercel_Build --> Vercel_Deploy
    Vercel_Deploy --> Vercel_Edge
    
    Render_Docker --> Render_Deploy
    Render_Deploy --> Render_Service
    
    User --> Vercel_Edge
    Vercel_Edge -->|API Calls| Render_Service
    
    Render_Service -->|Verify CAPTCHA| Turnstile_Service
    Render_Service -->|Fetch GTIN| Cosmos_API
    
    style Git fill:#f05032
    style Repo fill:#181717
    style Vercel_Build fill:#000000
    style Render_Docker fill:#46e3b7
    style User fill:#2196f3
```

### Variáveis de Ambiente por Ambiente:

#### Desenvolvimento Local:
```bash
# .env (raiz)
Cosmos__Token=<dev_token>
Jwt__Key=DEV_ONLY...
Turnstile__SecretKey=<dev_secret>

# frontend/.env
VITE_API_BASE_URL=          # vazio = proxy Vite
VITE_TURNSTILE_SITE_KEY=<dev_site_key>
```

#### Produção Vercel (Frontend):
```bash
VITE_API_BASE_URL=https://product-store-api.onrender.com
VITE_TURNSTILE_SITE_KEY=<prod_site_key>
```

#### Produção Render (Backend):
```bash
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=<48_bytes_base64_random>
CORS_ORIGINS=https://product-store.vercel.app
Turnstile__SecretKey=<prod_secret>
Cosmos__Token=<prod_token>
AllowedHosts=product-store-api.onrender.com
```

---

## Monitorização e Observabilidade

### Logs Estruturados:

```csharp
logger.LogInformation("Criando produto SKU={Sku}", request.Sku);
logger.LogWarning("Conflito de SKU: {Sku}", dup.Sku);
logger.LogError(exception, "Falha ao migrar tenant {UserId}", userId);
```

### Métricas Recomendadas:

- **Taxa de erro** por endpoint (alertar se > 5%)
- **Latência p95** das APIs (alertar se > 500ms)
- **Rate limit violations** (possível ataque)
- **Chamadas API Cosmos** (controlo de custos)
- **Tamanho dos DBs** SQLite (backup quando > 100MB)

### Ferramentas Recomendadas:

- **Sentry** - Error tracking
- **LogRocket** - Session replay
- **Render Metrics** - Infraestrutura
- **Vercel Analytics** - Performance frontend

---

## Testes

### Estratégia de Testes:

```
Tests/
└── ProductStore.Api.Tests/
    ├── ApiWebApplicationFactory.cs    # Setup WebApplicationFactory
    ├── ProductCrudPipelineTests.cs    # Testes de integração
    └── ...
```

**Cobertura Atual:**
- ✅ Testes de integração HTTP (3 testes)
- ✅ SQLite in-memory para testes
- ✅ Stub da API Cosmos
- ❌ Testes unitários (TODO)
- ❌ Testes E2E com Playwright (TODO)

---

## Glossário

| Termo | Definição |
|-------|-----------|
| **Tenant** | Utilizador/organização com dados isolados |
| **GTIN** | Global Trade Item Number (código de barras EAN/UPC) |
| **SKU** | Stock Keeping Unit (identificador único de produto) |
| **Cosmos** | API Bluesoft para consulta de produtos por GTIN |
| **Turnstile** | CAPTCHA da Cloudflare (alternativa ao reCAPTCHA) |
| **Problem Details** | RFC 7807 - formato padronizado de erros HTTP |
| **JWT** | JSON Web Token - formato de autenticação stateless |

---

## Referências

- [ASP.NET Core Architecture](https://learn.microsoft.com/aspnet/core/fundamentals/)
- [React Architecture Best Practices](https://react.dev/learn/thinking-in-react)
- [Multi-Tenancy Patterns](https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/overview)
- [API Design Guidelines](https://learn.microsoft.com/azure/architecture/best-practices/api-design)
- [Bluesoft Cosmos API](https://cosmos.bluesoft.com.br/api)
- [Cloudflare Turnstile](https://developers.cloudflare.com/turnstile/)
