# TEST_STRATEGY.md

Estrategia pratica de validacao para agentes de IA. O objetivo nao e maximizar quantidade de testes, e sim reduzir risco com custo proporcional.

## Principios

- Testar o comportamento que gera risco real.
- Preferir poucos testes bons a muitos testes redundantes.
- Priorizar a pipeline HTTP e regras de negocio criticas.
- Nao depender de servicos externos em testes automatizados.
- Se uma mudanca pequena puder ser validada por build, lint ou leitura cuidadosa, nao inventar testes de baixo valor.

## Stack de testes atual

- framework principal: xUnit
- estilo predominante: integracao com `WebApplicationFactory`
- persistencia de testes: SQLite em memoria
- dependencias externas: stubs/fakes quando necessario

Diretorio:
- `backend/ProductStore.Api.Tests`

## O que validar por tipo de mudanca

### 1. Mudanca em endpoint, DTO, validacao ou middleware

Validacao recomendada:
- `npm test`

Adicionar ou ajustar teste quando:
- mudar status code;
- mudar contrato de request/response;
- mudar validacao de entrada;
- mudar autenticacao, autorizacao ou tratamento de erro;
- corrigir bug que possa voltar.

Cobrir preferencialmente:
- caso feliz;
- entrada invalida relevante;
- erro de dominio ou autorizacao quando aplicavel.

### 2. Mudanca em regra de negocio no backend

Validacao recomendada:
- `npm test`

Adicionar ou ajustar teste quando:
- a regra impactar criacao, atualizacao, listagem, filtros ou persistencia;
- houver invariantes de negocio importantes;
- o comportamento depender de combinacoes de campos ou contexto.

### 3. Mudanca em model, `DbContext` ou migracao

Validacao recomendada:
- `npm test`

Considerar tambem:
- revisar se o fluxo de criacao de tenant continua valido;
- confirmar que a mudanca nao quebra leitura/escrita nos caminhos principais;
- verificar se a migracao faz sentido para bancos ja existentes.

Adicionar teste quando:
- a mudanca de schema afeta regra de negocio ou serializacao;
- houve correcao de bug de persistencia;
- existe risco real de regressao multi-tenant.

### 4. Mudanca no frontend sem alterar contrato da API

Validacao recomendada:
- `npm run build`
- `frontend`: usar `npm run lint` se a mudanca tocar TypeScript/React de forma relevante

Adicionar teste automatizado apenas se:
- ja existir infraestrutura de teste apropriada para a area;
- a regra na UI for suficientemente importante para justificar manutencao futura.

Na ausencia de teste automatizado, verificar manualmente:
- carregamento;
- estado vazio;
- estado de erro;
- submissao principal do fluxo alterado.

### 5. Mudanca em autenticacao, seguranca ou deploy

Validacao recomendada:
- `npm test`
- `npm run build`

Revisar explicitamente:
- protecao de rotas;
- fluxo `login -> pendingToken -> complete-turnstile -> JWT`;
- ausencia de logs sensiveis;
- compatibilidade com variaveis de ambiente e configuracao de producao.

## Quando NAO adicionar teste novo

Evitar adicionar teste quando:
- a mudanca e apenas textual, de comentarios ou documentacao;
- a mudanca e refactor mecanico sem alteracao funcional;
- o teste apenas repetiria detalhes internos de implementacao;
- o custo de manutencao do teste supera o risco real da mudanca.

## Matriz de decisao rapida

- Mudou endpoint ou resposta HTTP: quase sempre ajustar teste de integracao.
- Mudou regra de negocio: geralmente ajustar teste.
- Mudou schema persistido: testar pipeline afetada.
- Mudou UI sem regra complexa: build + verificacao manual costumam bastar.
- Mudou auth/seguranca: validar com rigor maior e revisar `SECURITY.md`.

## Comandos uteis

Na raiz:

```bash
npm test
```

```bash
npm run test:watch
```

```bash
npm run build
```

No frontend:

```bash
npm run lint
```

## Como relatar validacao ao encerrar

Ao finalizar uma tarefa, deixar claro:
- quais comandos foram executados;
- o que foi validado manualmente, se houver;
- se algum teste nao foi executado;
- qual risco residual permaneceu.

## Regra final

Se a mudanca tocar comportamento critico e nao houver teste automatizado nem validacao manual convincente, a tarefa ainda nao esta suficientemente segura para encerrar.
