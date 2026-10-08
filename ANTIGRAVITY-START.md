# ÁTRIO — PROMPT DE EXECUÇÃO PARA ANTIGRAVITY — SOMENTE PLANO 01

Você é o agente de engenharia responsável por executar, de forma autônoma, **somente** o Plano 01 (Foundation & Platform Baseline) do projeto Átrio. Trata-se de um projeto novo e **independente do BiblioFlux**. Todo o produto será desenvolvido por fases, e é proibido antecipar funcionalidades de fases posteriores.

## Fontes normativas no workspace

Leia integralmente e na ordem:

1. `docs/superpowers/specs/2026-10-02-atrio-architecture-design.md` — arquitetura aprovada, referência normativa.
2. `docs/superpowers/plans/2026-10-02-atrio-v0.1-implementation-roadmap.md` — fronteiras e dependências dos planos.
3. `docs/superpowers/plans/2026-10-02-atrio-01-foundation-platform.md` — **único plano autorizado para execução nesta rodada**.

Se algum arquivo estiver ausente, incorreto ou inacessível, informe precisamente o problema e pare antes de escrever código. Não tente reconstruir requisitos a partir de memória ou de outro repositório. Se houver contradição, siga a especificação arquitetural para decisões estruturais e sinalize o conflito; não invente escopo.

## Objetivo desta rodada

Ao término do Plano 01, o novo repositório deverá conter a fundação `.NET 10 + Clean Architecture + React 19/TypeScript/Vite + PostgreSQL 17`, as suítes de testes e regras de dependência, API mínima (`ProblemDetails`, `traceId`, `/health/live`, `/health/ready`, `/api/v1/system/version`), infraestrutura de desenvolvimento Docker, scripts de smoke test, CI e documentação. **Nenhuma funcionalidade de Identity, Family, Academics, Files, Publications, Notifications ou Administration deverá ser implementada nesta rodada.**

## 1. Preflight obrigatório, antes de alterar qualquer arquivo

- Identifique o workspace atual, o Git root (`git rev-parse --show-toplevel`, quando disponível), branch, HEAD, alterações não commitadas, presença dos três arquivos normativos e ferramentas (`dotnet --info`, `node --version`, `npm --version`, `docker version`, `docker compose version`, `git --version`, `gh --version` somente se `gh` existir).
- Certifique-se de que o workspace é o **novo Átrio**, e NÃO `dierudito/biblioflux` ou uma pasta interna dele. Se houver risco de editar outro projeto, **pare e descreva o bloqueio**.
- Não modifique nem apague arquivos existentes sem antes inspecionar. Não use `git reset --hard`, `git clean -fd`, `git push --force` ou comandos destrutivos.
- No Windows, use comandos PowerShell compatíveis com a versão disponível; não assuma suporte a `&&`. Use `curl.exe` em vez do alias `curl` quando necessário, com `--fail --show-error --silent --max-time 15`. Não deixe comandos HTTP aguardando entrada interativa sem timeout.
- Faça um relatório curto com caminhos, versões disponíveis e eventuais bloqueios. Se dependência essencial estiver indisponível, instale/configure somente o que for permitido e estritamente necessário; quando exigir privilégio administrativo, aprovação humana ou credenciais, solicite intervenção específica e não improvise.

## 2. Git e criação do repositório

- O repositório desejado é `dierudito/atrio`, **independente** de `dierudito/biblioflux`.
- Se estiver trabalhando numa pasta Átrio nova e sem Git, inicialize Git com branch principal `main`. Faça um commit inicial contendo apenas os documentos fornecidos e um README mínimo, antes de criar branch de feature, para dar histórico verificável à base de trabalho.
- Se já houver repositório Átrio, preserve o histórico e examine `git status`, remotes e branches antes de agir. Sincronize `main` somente quando isso for seguro e não causar perda de alterações.
- Crie branch de trabalho `feat/atrio-v01-foundation` a partir da `main` limpa. Se existir, examine o estado e retome sem recomeçar/destruir trabalho.
- Caso `gh` esteja instalado, autenticado e haja permissão, pode criar no GitHub o repositório **privado** `dierudito/atrio` se ele ainda não existir, configurar `origin` e realizar push. Não altere repositório remoto já existente sem examinar histórico e identidade. Nunca crie repositório público sem autorização explícita.
- Se `gh` não existir ou não estiver autenticado, **não trate como falha do Plano 01**: prossiga com Git local e testes; no relatório final identifique precisamente o que falta para publicar branch/abrir PR. Não solicite senha nem token do GitHub em texto aberto.
- Realize commits pequenos, coesos e rastreáveis após cada task concluída. Uma PR, quando possível, apenas ao final do DoD completo. Não faça merge da PR; aguarde code review. Corrija observações na mesma branch.

## 3. Execução: Plano 01, Task 1 até Task 7

- Leia os blocos `Global Constraints`, `Review Focus`, `Target Repository Structure`, `Interfaces`, critérios de testes e `Plan Completion Gate` do plano.
- Execute **em ordem** as Tasks 1–7, respeitando arquivos, contratos, comandos, verificações e commits previstos no plano. Use nomes equivalentes apenas quando houver justificativa técnica objetiva documentada.
- Use TDD sempre que a task descrever teste: teste falhando pelo comportamento ausente, implementação mínima, teste passando. Não introduza dependência proibida nem abstração especulativa.
- Respeite restrições arquiteturais: `Domain` não depende das outras camadas, `Application` depende somente de `Domain`, `Infrastructure` depende das camadas internas e `Api` compõe o sistema. Garanta isso em architecture tests.
- Preserve `same-origin` em arquitetura e proxy `/api` de desenvolvimento. Não introduza cookies de autenticação, sessões ou telas de login agora: essas pertencem ao Plano 02.
- Para PostgreSQL, use Testcontainers nos testes de integração. Não substitua o banco por EF Core InMemory. Se Docker/Testcontainers não puder executar, registre bloqueio com evidência e não declare o DoD cumprido.
- Nunca embuta segredo, senha real, hostname externo específico ou caminho Windows na fonte. Crie `.env.example` com valores de exemplo não secretos.
- Atenção ao contrato de `/health/live` versus `/health/ready`: liveness independe de PostgreSQL, readiness verifica PostgreSQL. Teste indisponibilidade e recuperação, quando possível.
- Testes de exceção devem demonstrar `ProblemDetails` seguro, `code=internal_error` e `traceId`, sem stack trace no response em produção.
- Em caso de conflito entre comandos do plano e ambiente real, ajuste somente o necessário para obter comportamento equivalente, explique e preserve a intenção do plano; não avance silenciosamente diante de falha.

## 4. Gates finais obrigatórios

Execute e registre saída/resultado de:

- `dotnet build apps/api/Atrio.slnx`
- `dotnet test apps/api/Atrio.slnx`
- arquitetura (incluída nos testes acima)
- integração PostgreSQL/Testcontainers real
- frontend typecheck/test/build usando scripts exatos do `package.json`
- `docker compose up -d --build`
- `docker compose ps`
- chamadas HTTP bounded para `/health/live`, `/health/ready`, `/api/v1/system/version` e web root
- validação do estado final do Git, commits da branch, ausência de secrets e documentação.

Não declare teste PASS sem executá-lo. Diferencie explicitamente PASS, FAIL, SKIPPED e BLOCKED. Se CI remoto ainda não puder ser executado por falta de GitHub, registre que o gate de CI remoto está **pendente**, mesmo que os comandos locais tenham passado.

## 5. Limite estrito e Definition of Done

A rodada termina quando a fundação está buildável, testável, dockerizada, documentada e revisável. **É proibido avançar para Plano 02 ou outros planos, implementar Product Domain, simular testes ausentes, publicar produção na internet ou aplicar configuração de roteador/domínio.**

Ao finalizar, entregue relatório objetivo com:

1. Resumo do que foi criado (estrutura e interfaces).
2. Tabela Tasks 1–7: concluída/parcial/bloqueada + commit correspondente.
3. Resultado real de build, testes, Testcontainers, smoke HTTP e CI.
4. `git status`, branch, commits, remote e URL da PR **somente se realmente criada**.
5. Divergências justificadas em relação ao Plano 01.
6. Bloqueios, riscos remanescentes e passos exatos para resolvê-los.
7. Declaração explícita: **“Plano 01 encerrado; aguardando code review e autorização antes do Plano 02.”**

Não faça merge automaticamente. Não continue executando trabalho após esse relatório.
