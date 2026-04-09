# TASKS.md

Guia operacional para agentes de IA trabalharem neste projeto com mudancas pequenas, seguras e faceis de revisar.

Este arquivo complementa `AGENT.md`, `RULES.md`, `STACK.md` e `TEST_STRATEGY.md`.

## Objetivo

Antes de editar qualquer coisa:
- entender se a mudanca e de backend, frontend, seguranca, deploy ou testes;
- localizar a feature e a camada correta;
- preservar o comportamento existente, salvo requisito explicito;
- validar o impacto minimo necessario antes de encerrar a tarefa.

## Fluxo base de trabalho

1. Ler rapidamente `README.md`, `AGENT.md`, `RULES.md` e este arquivo.
2. Identificar a area principal afetada:
   - `backend/ProductStore.Api`
   - `backend/ProductStore.Api.Tests`
   - `frontend/src`
3. Localizar implementacoes proximas antes de criar novos padroes.
4. Fazer a menor mudanca coerente possivel.
5. Executar validacoes proporcionais ao risco.
6. Atualizar documentacao quando a mudanca alterar comportamento, fluxo ou setup.

## Tarefas comuns

### 1. Adicionar ou alterar endpoint da API

Checklist:
- localizar o controller relacionado em `backend/ProductStore.Api/Controllers`;
- validar se a regra pertence a controller, service, domain ou data layer;
- criar/ajustar DTOs em `backend/ProductStore.Api/DTOs`;
- criar/ajustar validadores em `backend/ProductStore.Api/Validation`;
- implementar regra em `backend/ProductStore.Api/Services` e, se necessario, em `Domain`;
- manter controller fino: entrada, chamada de servico e resposta HTTP;
- verificar mapeamento de erros e status HTTP;
- atualizar ou criar teste de integracao em `backend/ProductStore.Api.Tests`.

Evitar:
- acessar `DbContext` diretamente no controller;
- colocar regra de negocio em validator;
- retornar formatos de erro ad-hoc fora do fluxo de Problem Details.

### 2. Alterar regra de negocio de produto ou categoria

Checklist:
- localizar regras existentes em `Services` e `Domain`;
- preservar invariantes ja documentadas, como regras de categoria e validacoes de SKU;
- verificar impacto em criacao, atualizacao, listagem e importacao Cosmos;
- atualizar testes de pipeline/integracao quando o comportamento mudar.

Perguntas uteis:
- a regra vale para criar e atualizar?
- a regra deve falhar com `400`, `404`, `409`, `429`, `502` ou `503`?
- existe efeito colateral em filtros, ordenacao, persistencia ou serializacao?

### 3. Alterar autenticacao ou autorizacao

Checklist:
- revisar `AuthController`, servicos JWT e Turnstile;
- preservar o fluxo em dois passos do login quando aplicavel;
- verificar se a mudanca afeta claims, expiracao, protecao de rotas ou armazenamento do token;
- revisar impacto de seguranca em `SECURITY.md`;
- validar cenarios positivos e negativos.

Cuidado extra:
- nao expor credenciais, secrets, tokens reais nem logs sensiveis;
- nao enfraquecer rate limiting, validacao ou isolamento por tenant sem requisito explicito.

### 4. Criar ou ajustar entidade persistida

Checklist:
- localizar model em `backend/ProductStore.Api/Models`;
- revisar `AppDbContext` e configuracoes relacionadas;
- criar migracao EF se a mudanca alterar schema persistido;
- considerar impacto no tenant SQLite por usuario;
- revisar DTOs, servicos, validadores e testes afetados.

Perguntas uteis:
- a coluna nova precisa ser obrigatoria?
- existe valor default ou migracao de dados?
- o schema afeta tenants existentes?

### 5. Alterar integracao com Cosmos / GTIN

Checklist:
- revisar controllers, DTOs e servicos ligados a Cosmos;
- preservar fallback quando `Cosmos__Token` nao estiver configurado;
- evitar dependencia externa em testes;
- manter validacao clara para SKU interno vs GTIN.

### 6. Alterar pagina ou fluxo do frontend

Checklist:
- localizar pagina em `frontend/src/pages`;
- extrair chamadas HTTP para `frontend/src/api`;
- extrair logica reutilizavel para `hooks`, `lib` ou `contexts` quando fizer sentido;
- manter componentes focados em UI e interacao;
- revisar estados de carregamento, erro e sucesso;
- validar rotas protegidas e persistencia de autenticacao quando aplicavel.

Evitar:
- misturar fetch, regra de negocio e renderizacao pesada no mesmo componente;
- duplicar tipos que ja existam em `frontend/src/types`;
- criar cliente HTTP paralelo fora de `frontend/src/lib/apiClient.ts`.

### 7. Corrigir bug

Checklist:
- reproduzir mentalmente ou via testes a falha antes de editar;
- buscar a causa raiz, nao apenas o sintoma;
- preferir correcao localizada;
- adicionar teste quando o bug for regressao relevante ou comportamento critico;
- documentar rapidamente o risco residual se nao for possivel testar.

### 8. Refatorar

Checklist:
- nao misturar refactor com mudanca funcional sem necessidade;
- preservar contratos publicos;
- mover regras para a camada correta;
- reduzir tamanho de arquivos/componentes muito grandes;
- validar comportamento apos a limpeza.

## Quando atualizar documentacao

Atualize algum arquivo de documentacao quando houver mudanca em:
- comandos de setup, build ou teste;
- variaveis de ambiente;
- fluxo de autenticacao;
- endpoints ou contratos principais;
- regras de negocio relevantes;
- estrategia de deploy ou seguranca.

## Definicao de concluido

Uma tarefa so deve ser considerada concluida quando:
- a mudanca esta na camada correta;
- o escopo ficou minimo e coerente;
- validacoes relevantes foram executadas;
- nao foram introduzidos segredos;
- documentacao e testes foram atualizados quando necessario.
