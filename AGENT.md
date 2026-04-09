# AGENT.md

## Objetivo
Manter o projeto simples de evoluir, com baixo acoplamento, boa separação de responsabilidades e crescimento sustentável no backend e no frontend.

## Princípios
- Priorize código claro, pequeno e fácil de testar.
- Prefira composição a soluções "inteligentes" demais.
- Cada mudança deve ter uma responsabilidade principal.
- Evite duplicação, mas não force abstrações cedo demais.
- Preserve compatibilidade com o comportamento já existente, salvo requisito explícito.

## Regras Obrigatórias (não violar)
- Controllers NÃO devem conter:
- lógica de negócio
- acesso direto ao banco
- Toda regra de negócio deve estar em serviços/domínio
- Toda entrada deve ser validada antes de chegar ao serviço
- Serviços não devem depender de framework HTTP

## Arquitetura
- Respeite separação por camadas: apresentação, aplicação, domínio e infraestrutura.
- Regras de negócio devem ficar no domínio/serviços, não em controllers, componentes ou queries SQL espalhadas.
- Controllers e rotas apenas orquestram entrada, validação superficial e resposta HTTP.
- DTOs servem para transporte de dados; não misture DTO com entidade de persistência.
- Dependências devem apontar para dentro da regra de negócio, nunca o contrário.

## Backend
- Centralize validações, tratamento de erros e regras de negócio.
- Serviços devem expor casos de uso coesos e com contratos claros.
- Acesso a dados deve ficar concentrado em pontos previsíveis, evitando lógica de negócio no EF/DB layer.
- Padronize respostas, nomes e logging para facilitar manutenção e observabilidade.

## Frontend
- Componentes devem focar em renderização e interação.
- Extraia lógica reutilizável para hooks, utilitários ou serviços de API.
- Evite componentes grandes com estado, fetch e regra de negócio no mesmo lugar.
- Mantenha tipagem consistente entre UI, chamadas HTTP e modelos compartilhados.

## Escalabilidade
- Organize código por feature quando fizer sentido, sem quebrar limites de camada.
- Crie extensões pequenas e previsíveis, evitando arquivos "centrais" gigantes.
- Otimize apenas após evidência; priorize legibilidade, contrato estável e baixo acoplamento.
- Sempre que adicionar nova regra importante, avalie impacto em testes, documentação e observabilidade.

## Qualidade
- Toda alteração deve ser fácil de entender, revisar e reverter.
- Adicione ou atualize testes quando a mudança proteger comportamento crítico.
- Não introduza dependências, padrões ou abstrações sem necessidade clara.
- Se uma implementação parecer complexa demais, divida em partes menores.

## Evitar
- Controller acessando banco diretamente
- Componentes frontend fazendo fetch + regra + render ao mesmo tempo
- Services com múltiplas responsabilidades
- Arquivos acima de ~300 linhas sem necessidade clara

## Tarefas Comuns
### Criar novo endpoint
1. Criar DTO de entrada
2. Validar DTO no controller
3. Criar método no service
4. Controller chama service
5. Service executa regra de negócio
6. Repository acessa banco (se necessário)

### Adicionar regra de negócio
1. Implementar no service/domínio
2. NÃO colocar em controller
3. Cobrir com teste se for crítica
