# Segurança

Este documento descreve as medidas de segurança implementadas no ProductStore e boas práticas para deploy em produção.

## Medidas de Segurança Implementadas

### 1. Autenticação e Autorização
- **JWT Bearer Authentication** com tokens assinados (HMAC-SHA256)
- **ASP.NET Core Identity** para gestão de utilizadores e passwords
- **Password hashing** seguro (PBKDF2)
- **Cloudflare Turnstile** (CAPTCHA) no registo e login
- **Honeypot** anti-bot nos formulários de autenticação
- **Two-step login**: credenciais → Turnstile → JWT de sessão

### 2. Rate Limiting
- **Auth endpoints**: 5-10 requisições/minuto por IP
- **API global**: 100 requisições/minuto por IP em todos os endpoints
- Headers `Retry-After` em respostas 429

### 3. CORS (Cross-Origin Resource Sharing)
- **Desenvolvimento**: `localhost:5173` permitido
- **Produção**: **obrigatório** configurar `CORS_ORIGINS` com origem Vercel
- Validação automática: aplicação **não inicia** se CORS não estiver configurado em produção

### 4. Headers de Segurança (Produção)
- `X-Content-Type-Options: nosniff` - previne MIME sniffing
- `X-Frame-Options: DENY` - previne clickjacking
- `X-XSS-Protection: 1; mode=block` - proteção XSS adicional
- `Referrer-Policy: strict-origin-when-cross-origin` - controlo de referrer
- `Permissions-Policy` - desabilita APIs desnecessárias (geolocation, camera, etc)

### 5. Proteção contra Injeção
- **EF Core** com queries parametrizadas (protegido contra SQL Injection)
- **FluentValidation** em todos os endpoints
- Validação de entrada em frontend e backend
- Sanitização de dados do utilizador

### 6. Multi-Tenancy Seguro
- **Isolamento por utilizador**: cada user tem SQLite próprio (`data/users/{userId}.db`)
- Contexto de tenant validado via JWT claim `NameIdentifier`
- Sem possibilidade de acesso cross-tenant

### 7. Tratamento de Erros
- **GlobalExceptionHandler** com Problem Details (RFC 7807)
- Mensagens de erro detalhadas **apenas em Development**
- Stack traces ocultados em produção
- Logging estruturado sem exposição de dados sensíveis

### 8. HTTPS e Transport Security
- **HTTPS redirect** obrigatório em produção
- Cookies (se implementados) devem usar `Secure` + `HttpOnly` + `SameSite=Strict`

### 9. Reverse Proxy e IP Forwarding
- `ForwardedHeaders` configurado para Render
- `ForwardLimit = 1` - aceita apenas último salto (previne IP spoofing)
- Rate limiting baseado em IP real do cliente

### 10. Docker Security
- **Non-root user** no container (UID 1001)
- Multi-stage build (reduz superfície de ataque)
- Apenas portas necessárias expostas

---

## Checklist de Deploy em Produção

### Variáveis de Ambiente (Render - API)

**OBRIGATÓRIAS:**
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `Jwt__Key` - mínimo 32 caracteres aleatórios (gerar com: `[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))`)
- [ ] `CORS_ORIGINS` - origem Vercel (ex: `https://seu-dominio.vercel.app`)
- [ ] `Turnstile__SecretKey` - chave secreta Cloudflare

**OPCIONAIS:**
- [ ] `Cosmos__Token` - para integração Bluesoft (se não definido, usar SKU interno)
- [ ] `AllowedHosts` - restringir hosts aceites (ex: `seu-app.onrender.com`)

### Variáveis de Ambiente (Vercel - Frontend)

**OBRIGATÓRIAS:**
- [ ] `VITE_API_BASE_URL` - URL pública da API no Render (ex: `https://xxx.onrender.com`)
- [ ] `VITE_TURNSTILE_SITE_KEY` - chave do site Cloudflare (par com `SecretKey`)

### Verificações Finais

- [ ] Ficheiros `.env` **não** estão versionados no Git
- [ ] `.gitignore` inclui `*.env` (já configurado)
- [ ] Todas as credenciais foram geradas exclusivamente para produção
- [ ] HTTPS funcionando corretamente
- [ ] Testes passando (`npm test`)
- [ ] Build frontend OK (`npm run build`)
- [ ] Rate limiting testado (verificar headers `Retry-After`)
- [ ] CORS testado (frontend acede API sem erros)
- [ ] Cloudflare Turnstile testado em produção

---

## Boas Práticas

### Gestão de Credenciais
- **NUNCA** versionar ficheiros `.env` com credenciais reais
- Rotacionar credenciais periodicamente
- Usar variáveis de ambiente distintas para dev/staging/produção
- Gerar `Jwt__Key` com entropia suficiente (min. 32 caracteres)

### Tokens JWT
- Tokens armazenados em `localStorage` (atual)
- **RECOMENDAÇÃO FUTURA**: migrar para cookies `HttpOnly` + `Secure` + `SameSite=Strict`
- Implementar refresh tokens para sessões longas
- Validar `exp` (expiration) no cliente

### Logging
- **NUNCA** logar senhas, tokens ou dados sensíveis
- Em produção, logs devem ser enviados para serviço externo (Sentry, LogRocket, etc)
- Remover `console.error` em produção (usar logging estruturado)

### Monitorização
- Monitorizar rate limit violations (possível ataque)
- Alertas em erros 5xx (falhas inesperadas)
- Monitorizar chamadas à API Cosmos (custo)

### Backup
- Ficheiros SQLite em `data/` devem ter backup regular
- Considerar migração para PostgreSQL/MySQL em produção (se escalar)

---

## Vulnerabilidades Conhecidas e Mitigações

### 1. JWT em localStorage (XSS)
**Risco**: Script malicioso pode roubar token via `localStorage.getItem()`

**Mitigação atual**: Headers de segurança + validação de entrada

**Mitigação futura recomendada**: 
- Migrar para cookies `HttpOnly` (não acessível via JavaScript)
- Implementar CSP (Content-Security-Policy) restritivo
- Short-lived access tokens + refresh token pattern

### 2. Rate Limiting por IP (DDoS distribuído)
**Risco**: Atacante pode usar múltiplos IPs

**Mitigação atual**: 100 req/min por IP

**Mitigação futura**: 
- Cloudflare WAF/DDoS protection
- Rate limiting por utilizador autenticado (além de IP)

### 3. SQLite em Produção
**Risco**: Limitações de concorrência e backup

**Mitigação atual**: Isolamento por tenant (ficheiros separados)

**Mitigação futura**: Migrar para PostgreSQL/MySQL se tráfego crescer

---

## Reporte de Vulnerabilidades

Se descobrir uma vulnerabilidade de segurança, por favor **NÃO** abra issue público. Contacte diretamente o maintainer.

---

## Referências

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://learn.microsoft.com/aspnet/core/security/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Cloudflare Turnstile](https://developers.cloudflare.com/turnstile/)
