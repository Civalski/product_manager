# Segurança

Este documento descreve o estado atual de segurança do ProductStore, os controles já implementados, limitações conhecidas e o processo recomendado para reporte responsável de vulnerabilidades.

## Escopo

O projeto é composto por:

- frontend React/Vite
- API ASP.NET Core
- autenticação com ASP.NET Core Identity e JWT
- isolamento multi-tenant com SQLite por utilizador
- integrações externas com Cloudflare Turnstile e Bluesoft Cosmos

Este documento cobre o comportamento atual do repositório. Sempre que houver divergência entre a documentação e o código, o código é a fonte de verdade.

## Controlos Implementados

### 1. Autenticação e Autorização

- Autenticação JWT Bearer com assinatura simétrica HMAC.
- ASP.NET Core Identity para gestão de utilizadores, hashing de password e lockout em tentativas inválidas.
- Política mínima de password no backend:
  - pelo menos 8 caracteres
  - pelo menos 1 dígito
  - pelo menos 1 letra minúscula
  - pelo menos 1 símbolo
  - maiúscula não é obrigatória
- Registo protegido por Cloudflare Turnstile fora de `Development`.
- Login em dois passos:
  - `POST /api/auth/login` valida credenciais e devolve um `pendingToken`
  - `POST /api/auth/complete-turnstile` valida Turnstile e emite o JWT final
- O `pendingToken` usa audience diferente da audience do JWT final, para que não seja aceite como bearer token nas APIs protegidas.
- `TestAuth:Enabled` é bloqueado fora de `IntegrationTesting`, mesmo se a flag for configurada por engano.
- Endpoints protegidos exigem `Authorization: Bearer <jwt>`.

### 2. Gestão de Tokens no Cliente

- O JWT final é armazenado no `localStorage`.
- O `pendingToken` do login em dois passos é armazenado no `sessionStorage`.
- O frontend remove tokens expirados ao ler o estado de autenticação.
- O armazenamento atual privilegia simplicidade operacional; ver limitações conhecidas para os riscos e melhorias recomendadas.

### 3. Proteção Contra Abuso

- Rate limit por IP real do cliente após `ForwardedHeaders`.
- Limites atuais:
  - `auth-register`: 5 requisições por minuto por IP
  - `auth-login`: 10 requisições por minuto por IP
  - `api-global`: 100 requisições por minuto por IP
- Respostas `429` incluem header `Retry-After` quando disponível.
- Honeypot nos formulários de registo e login para rejeição simples de bots automatizados.

### 4. CORS e Origem do Frontend

- Em desenvolvimento, apenas `http://localhost:5173` e `http://127.0.0.1:5173` são permitidos.
- Em produção, as origens permitidas devem ser configuradas via `CORS_ORIGINS` ou `Cors:AllowedOrigins`.
- A aplicação falha no arranque se estiver fora de `Development` e `IntegrationTesting` sem CORS de produção configurado.
- Não há suporte a wildcard como `https://*.vercel.app`; cada origem deve ser listada explicitamente.

### 5. Transporte e Cabeçalhos de Segurança

- `UseHttpsRedirection()` ativo fora de `Development`.
- `ForwardedHeaders` configurado para respeitar `X-Forwarded-For` e `X-Forwarded-Proto`.
- `ForwardLimit = 1` para reduzir risco de spoofing de IP atrás do proxy esperado.
- Em ambientes não `Development`, a API adiciona:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: geolocation=(), microphone=(), camera=()`

### 6. Validação e Tratamento de Entrada

- FluentValidation aplicado aos requests da API.
- EF Core utiliza queries parametrizadas, reduzindo risco de SQL injection.
- Respostas inválidas usam `ValidationProblemDetails`.
- O frontend sanitiza pré-visualizações do painel de logs HTTP para ocultar campos sensíveis conhecidos antes de os exibir na UI de debug.

### 7. Isolamento de Dados

- Base de identidade separada em `data/identity.db`.
- Cada utilizador possui base própria em `data/users/{userId}.db`.
- O tenant é resolvido a partir do claim `NameIdentifier` do JWT.
- Este modelo reduz superfície de acesso cross-tenant e simplifica backup/remoção por utilizador.

### 8. Tratamento de Erros e Observabilidade

- `GlobalExceptionHandler` com Problem Details (RFC 7807).
- Detalhes mais verbosos de erro apenas em `Development`.
- Resumos de requests com status `4xx` e `5xx` são registados no backend.
- O painel de logs HTTP do frontend fica desligado por padrão em produção e só é ativado com `VITE_ENABLE_HTTP_LOG_VIEWER=true`.

### 9. Segurança de Deploy e Execução

- O backend exige `Jwt__Key` seguro fora de `Development` e `IntegrationTesting`.
- A aplicação falha no arranque se `Jwt__Key` estiver vazio, curto ou igual ao placeholder do repositório.
- O `Dockerfile` usa build multi-stage e execução com utilizador não-root.
- Endpoints de health check expostos:
  - `GET /health`
  - `GET /ready`

## Checklist de Produção

### Variáveis obrigatórias da API

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `Jwt__Key` com pelo menos 32 caracteres secretos
- [ ] `CORS_ORIGINS` com a(s) origem(ns) públicas do frontend
- [ ] `Turnstile__SecretKey` configurada

Sugestão para gerar segredo forte no PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### Variáveis opcionais da API

- [ ] `Cosmos__Token` para integração com a Bluesoft Cosmos
- [ ] `AllowedHosts` para restringir hosts aceites

### Variáveis obrigatórias do frontend

- [ ] `VITE_API_BASE_URL` com a URL pública da API
- [ ] `VITE_TURNSTILE_SITE_KEY` com a chave pública do Cloudflare Turnstile

### Verificações antes de publicar

- [ ] Nenhum `.env` com segredos reais está versionado
- [ ] HTTPS funcional entre frontend e backend
- [ ] CORS validado com a origem real de produção
- [ ] Turnstile validado em produção
- [ ] `npm test` a passar
- [ ] `npm run build` a passar
- [ ] Rate limiting verificado com respostas `429`
- [ ] `VITE_ENABLE_HTTP_LOG_VIEWER` desativado, salvo necessidade operacional explícita
- [ ] Backups do diretório de dados definidos para o ambiente de produção

## Limitações Conhecidas

### 1. JWT final em `localStorage`

Risco:

- Em caso de XSS com execução de JavaScript no browser, o token pode ser lido e exfiltrado.

Mitigação atual:

- validação de entrada
- cabeçalhos de segurança no backend
- painel de logs com redação de campos sensíveis conhecidos

Melhoria recomendada:

- migrar para cookies `HttpOnly` + `Secure` + `SameSite`
- reduzir a vida útil do access token
- introduzir refresh tokens com rotação

### 2. `pendingToken` em `sessionStorage`

Risco:

- O token pendente tem vida curta, mas continua exposto a JavaScript da página durante a sessão.

Mitigação atual:

- audience separada do JWT final
- expiração curta, limitada a no máximo 60 minutos e com valor default de 10 minutos

Melhoria recomendada:

- migrar o fluxo de autenticação para armazenamento não acessível por JavaScript

### 3. Ausência atual de CSP restritiva

Risco:

- Sem `Content-Security-Policy`, a contenção de impacto de XSS depende de outras camadas.

Mitigação atual:

- validação de entrada
- cabeçalhos defensivos adicionais

Melhoria recomendada:

- definir CSP compatível com Vite/React e com o script do Turnstile

### 4. Rate limit baseado apenas em IP

Risco:

- Ataques distribuídos ou múltiplos utilizadores atrás do mesmo NAT podem contornar ou sofrer injustamente o limite.

Mitigação atual:

- limites conservadores por endpoint
- resolução do IP após proxy reverso

Melhoria recomendada:

- combinar rate limit por IP com rate limit por utilizador autenticado
- considerar WAF/CDN com proteção anti-bot e anti-DDoS

### 5. SQLite em produção

Risco:

- Concorrência limitada, operação de backup mais sensível e ausência de cifragem em repouso fornecida pela aplicação.

Mitigação atual:

- isolamento por utilizador
- estrutura simples para backup por ficheiro

Melhoria recomendada:

- usar cifragem ao nível do disco/host
- avaliar PostgreSQL/MySQL se houver crescimento de tráfego, concorrência ou requisitos operacionais mais fortes

## Boas Práticas Operacionais

- Nunca versionar ficheiros `.env` com credenciais reais.
- Rodar segredos periodicamente, especialmente `Jwt__Key`, Turnstile e tokens de terceiros.
- Usar segredos distintos para desenvolvimento, staging e produção.
- Monitorizar erros `5xx`, picos de `429` e falhas de verificação Turnstile.
- Rever regularmente dependências do frontend, do backend e da imagem Docker.
- Manter backups testados do diretório `data/`.
- Tratar `GET /health` e `GET /ready` como endpoints públicos de infraestrutura e evitar incluir dados sensíveis neles.

## Reporte Responsável de Vulnerabilidades

Se descobrir uma vulnerabilidade:

- não abra issue público
- contacte o maintainer por canal privado disponível na plataforma do repositório
- inclua impacto, pré-condições, passos de reprodução e versão/commit afetado
- se possível, envie prova de conceito mínima e segura
- se a falha envolver exposição de segredo, indique quais credenciais devem ser rotacionadas

Não há SLA formal definido neste repositório. Os reports serão analisados assim que possível.

## Referências

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://learn.microsoft.com/aspnet/core/security/)
- [JWT Best Current Practices (RFC 8725)](https://www.rfc-editor.org/rfc/rfc8725)
- [Cloudflare Turnstile](https://developers.cloudflare.com/turnstile/)
