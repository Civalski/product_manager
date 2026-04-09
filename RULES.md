# RULES.md

Regras objetivas para agentes de IA contribuirem neste projeto sem quebrar arquitetura, seguranca ou manutencao.

Se houver conflito entre documentos:
1. requisito explicito do usuario;
2. seguranca e protecao de dados;
3. `AGENT.md`;
4. este arquivo;
5. padroes locais do codigo.

## Regras obrigatorias

- Nao colocar logica de negocio em controllers.
- Nao acessar banco diretamente a partir de controllers.
- Nao acoplar services ao framework HTTP.
- Nao misturar DTO de transporte com entidade de persistencia.
- Nao criar fluxo paralelo de autenticacao sem necessidade explicita.
- Nao versionar segredos, tokens, chaves ou `.env` reais.
- Nao enfraquecer validacoes, rate limit, isolamento por tenant ou tratamento global de erros.

## Regras de arquitetura

- Respeitar separacao por camadas: apresentacao, aplicacao, dominio e infraestrutura.
- Controllers devem apenas receber input, delegar, e devolver resposta HTTP.
- Regras de negocio devem ficar em `Services` e, quando fizer sentido, em `Domain`.
- Persistencia deve ficar concentrada em `Data` e pontos previsiveis de acesso.
- Validacao de entrada deve passar por DTOs e validators.
- Novas abstrações so devem ser criadas quando reduzirem complexidade real.
- Preferir mudancas pequenas, locais e composiveis.

## Regras de backend

- Preservar Problem Details e mapeamento consistente de erros HTTP.
- Manter contratos claros nos servicos.
- Reutilizar validadores existentes antes de criar novas validacoes espalhadas.
- Considerar sempre o contexto multi-tenant por utilizador.
- Ao alterar schema, avaliar impacto em migracoes e tenants existentes.
- Em integracoes externas, prever falha, timeout, indisponibilidade e resposta parcial.

## Regras de frontend

- Componentes devem focar em renderizacao, interacao e composicao.
- Chamadas HTTP devem continuar centralizadas em `frontend/src/api` e `frontend/src/lib/apiClient.ts`.
- Tipos compartilhados devem ficar em `frontend/src/types` quando reutilizaveis.
- Evitar componentes grandes com estado, fetch e regra de negocio no mesmo arquivo.
- Preservar experiencia basica de loading, erro e sucesso.
- Nao duplicar logica que possa viver em hooks, utils ou contextos.

## Regras de seguranca

- Nunca expor credenciais reais em codigo, docs, logs ou commits.
- Nunca logar password, JWT, pending token, turnstile secret ou outros segredos.
- Preservar a protecao Turnstile e o fluxo de autenticacao em dois passos quando aplicavel.
- Manter compatibilidade com CORS de producao e exigencias de `Jwt__Key`.
- Considerar `SECURITY.md` ao alterar auth, headers, logging, rate limiting ou deploy.

## Regras de testes

- Toda mudanca de comportamento critico deve ter verificacao adequada.
- Priorizar testes de integracao para endpoints e fluxos HTTP.
- Nao criar testes redundantes que apenas repitam implementacao.
- Se nao houver teste automatizado, registrar claramente o que foi validado e o risco residual.

## Regras de edicao

- Antes de editar, ler codigo proximo para seguir o padrao local.
- Preferir reutilizar nomes, contratos e estruturas ja existentes.
- Evitar renomeacoes amplas sem necessidade clara.
- Evitar misturar refactor com feature nova.
- Atualizar documentacao quando a mudanca afetar setup, ambiente, fluxo ou contrato publico.

## Regras de qualidade

- Toda mudanca deve ser facil de entender, revisar e reverter.
- Arquivos muito grandes devem ser quebrados quando isso simplificar o entendimento.
- Comentarios devem ser raros e apenas quando o codigo nao for suficientemente claro.
- Otimizacao so deve ser feita com justificativa, nao por suposicao.

## Checklist final

Antes de encerrar:
- a mudanca esta na camada correta;
- o comportamento existente foi preservado onde necessario;
- nao foi criado acoplamento desnecessario;
- nao houve regressao obvia de seguranca;
- testes e validacoes proporcionais foram executados;
- documentacao foi atualizada se o projeto passou a funcionar de outro jeito.
