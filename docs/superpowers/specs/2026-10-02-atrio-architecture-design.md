# Átrio — Architecture Specification

**Versão:** MVP v0.1  
**Data:** 2026-10-02  
**Status:** Architecture Design consolidada — aguardando revisão do usuário  
**Produto:** Átrio  
**Instituição inicial:** Centro Cultural São José Sanchez Del Rio  
**Idioma inicial:** PT-BR  
**Modelo de instalação:** Single-institution

---

## 1. Objetivo arquitetural

O Átrio será a fonte oficial de comunicação e distribuição de conteúdo entre o Centro Cultural, professores e famílias. A arquitetura deve sustentar o fluxo principal:

```text
Administração
    ↓
configura pessoas, famílias, alunos, professores e estrutura acadêmica
    ↓
Professor
    ↓
cria publicação com conteúdo, materiais e orientações individuais
    ↓
Átrio
    ├── valida autorização
    ├── calcula audiência
    ├── persiste grants
    ├── registra auditoria
    └── agenda notificações
             ↓
Família
    ├── vê na Home
    ├── consulta Agenda
    ├── acessa materiais
    └── busca histórico
```

A arquitetura deve priorizar:

- segurança no backend;
- simplicidade operacional;
- mobile-first;
- integridade temporal;
- clareza de limites de domínio;
- baixa complexidade de infraestrutura;
- portabilidade entre host local, Linux dedicado e VPS;
- evolução sem acoplamento prematuro com BiblioFlux;
- YAGNI.

---

## 2. Princípios arquiteturais

### 2.1 Monólito modular

O Átrio será implementado como **monólito modular**, e não como microsserviços.

```text
Web SPA
   ↓
REST API
   ↓
Application / Domain
   ↓
PostgreSQL + FileStorage + Email
```

Os módulos têm limites claros de responsabilidade e contratos explícitos, mas executam no mesmo processo e compartilham um PostgreSQL.

### 2.2 Clean Architecture + DDD pragmático

Estrutura principal:

```text
Atrio.Domain
Atrio.Application
Atrio.Infrastructure
Atrio.Api
```

DDD será aplicado onde houver invariantes e comportamento real. Não haverá abstrações cerimoniais ou `GenericRepository<T>` apenas por padrão arquitetural.

### 2.3 Segurança é servidor-side

A UI pode esconder ações impossíveis, mas não é a camada de autorização.

Toda decisão de acesso depende de:

```text
Authentication
    ↓
Role/Capability
    ↓
Resource/Domain Authorization
```

### 2.4 Estado corrente não destrói história

Relações temporais como matrícula, responsabilidade familiar e atribuição docente são encerradas, não sobrescritas.

### 2.5 Infraestrutura mínima

No v0.1 não haverá:

- Kubernetes;
- RabbitMQ;
- Kafka;
- Redis;
- Elasticsearch/OpenSearch;
- service mesh;
- Event Sourcing;
- blue/green deployment;
- multi-tenant;
- PWA/offline;
- MFA;
- observabilidade distribuída pesada.

---

## 3. Relação com BiblioFlux

### 3.1 Topologia de repositório

Átrio e BiblioFlux permanecerão em **repositórios independentes**.

```text
dierudito/biblioflux
dierudito/atrio
```

Não haverá monorepo neste momento.

### 3.2 Integração futura

Integração futura será por contratos explícitos:

```text
Átrio
   ↕ HTTP API / contratos / eventos futuros
BiblioFlux
```

É proibido integrar por acesso direto ao banco do outro sistema.

### 3.3 Identificadores independentes

`Atrio.StudentId` não precisará ser igual a `BiblioFlux.StudentId`.

Se integração futura exigir relacionamento:

```text
ExternalReference / Mapping
```

será introduzido explicitamente.

---

## 4. Stack tecnológica

### Backend

- .NET 10
- C#
- ASP.NET Core
- EF Core 10
- Npgsql
- xUnit
- FluentAssertions
- Testcontainers para PostgreSQL real

### Frontend

- React 19
- TypeScript
- Vite
- React Router
- TanStack Query
- React Hook Form
- Zod
- Tailwind CSS
- primitives acessíveis no estilo Radix/shadcn
- Lucide Icons
- Vitest
- React Testing Library
- Playwright

### Banco

- PostgreSQL 17 ou versão estável equivalente compatível
- `uuid`
- `timestamptz`
- `date`
- `time`
- `jsonb`
- extensões quando necessárias: `btree_gist`, `unaccent`, `pg_trgm`

### Infraestrutura

- Docker
- Docker Compose
- Caddy como reverse proxy público
- SMTP externo configurável
- FileSystem storage no v0.1, abstraído por `IFileStorage`

---

## 5. Topologia de runtime

### 5.1 Produção inicial

```text
Internet
   ↓
DNS público
   ↓
IP público da conexão
   ↓
Roteador
   ├── TCP 443 → host:443
   └── TCP 80  → host:80
                     ↓
                   Caddy
                  ├── /        → atrio-web
                  └── /api/v1  → atrio-api
                                      ↓
                                  PostgreSQL
```

Somente portas 80 e 443 podem estar expostas externamente.

### 5.2 Portas proibidas ao público

Não expor:

- PostgreSQL;
- Kestrel diretamente;
- Docker daemon;
- portas internas de web/api;
- interfaces administrativas;
- serviços de desenvolvimento.

### 5.3 Same-origin

Produção:

```text
https://atrio.dominio/
https://atrio.dominio/api/v1/...
```

O browser conversa com uma única origem pública.

### 5.4 Desenvolvimento

O Vite poderá fazer proxy de `/api` para o backend, preservando a semântica de mesma origem.

---

## 6. Hospedagem e portabilidade

### 6.1 Fase inicial

Produção poderá rodar em:

```text
Windows
└── Docker Desktop
```

como estágio operacional inicial.

### 6.2 Destino preferencial

```text
Linux dedicado
ou
VPS Linux
└── Docker Engine
```

### 6.3 Regra de portabilidade

Nenhuma funcionalidade pode depender de:

- caminho `C:\...`;
- comportamento específico de Windows;
- sessão gráfica;
- IP hardcoded;
- hostname hardcoded.

A migração oficial será por backup/restore, não por cópia de diretórios internos do Docker Desktop.

---

## 7. Organização interna do backend

### 7.1 Módulos

```text
Identity & Access
People & Families
Academics
Publications
Files
Notifications
Administration & Audit
Institution
```

### 7.2 Organização por feature

Cada camada seguirá os mesmos limites funcionais.

Exemplo:

```text
Atrio.Application/
├── Identity/
├── People/
├── Academics/
├── Publications/
├── Files/
├── Notifications/
└── Administration/
```

### 7.3 Comunicação entre módulos

Módulos se comunicam por contratos explícitos in-process, por exemplo:

```text
IAcademicContextReader
IPublishingAuthorizationReader
IStudentAudienceReader
IFamilyAccessReader
```

Não haverá:

- HTTP interno entre módulos;
- mensageria interna distribuída;
- acesso arbitrário do módulo consumidor às tabelas internas de outro módulo.

### 7.4 Referências por ID

Agregados não carregarão grafos mutáveis extensos. Referências cross-module serão principalmente por ID.

---

## 8. Módulo Identity & Access

### 8.1 Person ≠ UserAccount

`Person` representa a pessoa institucional.

`UserAccount` representa capacidade de autenticação.

Uma `Person` pode existir sem conta.

### 8.2 UserAccount

Estados principais:

```text
NotYetInvited
InvitationPending
Active
Disabled
```

Estados ortogonais:

- `PasswordResetRequired`
- `PendingEmailChange`

### 8.3 Roles

Papéis fechados:

```text
Guardian
Teacher
Administrator
```

Uma conta pode possuir vários papéis.

Role concede capacidade geral, não escopo contextual.

### 8.4 Bootstrap Administrator

A instalação inicial cria um primeiro administrador por mecanismo local/servidor, com token seguro, uso único e expiração curta.

O Bootstrap Administrator:

- não pode ser excluído;
- não pode perder `Administrator`;
- pode ser Disabled;
- pode ser recuperado somente quando não houver administrador utilizável.

### 8.5 Sessões

Autenticação será por sessão server-side.

Cookie:

- opaco;
- `HttpOnly`;
- `Secure` em produção;
- `SameSite=Lax`.

Nenhum JWT será persistido em `localStorage`.

### 8.6 SecurityVersion

`UserAccount.SecurityVersion` permitirá revogar todas as sessões atomicamente.

Uma sessão só é válida se:

```text
session.revoked_at IS NULL
AND session.expires_at > now()
AND session.security_version = account.security_version
AND account.state = Active
```

### 8.7 Expiração

- sessão normal: 24h absolutas;
- remember me: 30 dias absolutos;
- sem renovação indefinida por atividade.

### 8.8 Reautenticação

Ações críticas exigem senha recente, vinculada à sessão atual, com janela de 15 minutos.

### 8.9 Tokens

Convites, reset, troca de e-mail e recovery:

- token aleatório criptograficamente seguro;
- hash armazenado no banco;
- purpose explícito;
- uso único;
- expiração;
- invalidação por nova emissão quando aplicável.

---

## 9. Módulo People & Families

### 9.1 Person

Campos mínimos:

- `FullName`
- `PreferredName?`
- `Email?`
- `Phone?`
- `BirthDate?`

### 9.2 Family

Representa núcleo institucional de responsabilidade.

### 9.3 Student

Um `Student`:

- referencia exatamente uma `Person`;
- pertence a exatamente uma `Family`;
- não possui `StudentStatus`.

### 9.4 GuardianFamily

Relação temporal:

```text
GuardianPersonId
FamilyId
RelationshipLabel?
StartsAt
EndsAt?
```

Sem sobreposição para o mesmo Guardian+Family.

### 9.5 TeacherContact

Separado dos contatos de login:

```text
PublicEmail?
PublicPhone?
AllowEmail
AllowPhone
AllowWhatsApp
```

---

## 10. Módulo Academics

### 10.1 AcademicPeriod

Estados:

```text
Draft
Active
Closed
```

No máximo:

- 1 `Active`;
- 1 próximo `Draft`;
- N `Closed`.

### 10.2 Class

Turma pertence a um período.

```text
Turma 4 / 2026 ≠ Turma 4 / 2027
```

### 10.3 Subject

Catálogo global institucional.

Pode ser desativado para novos usos sem destruir histórico.

### 10.4 ClassSubject

Representa oferta de disciplina na turma.

Constraint:

```text
UNIQUE(ClassId, SubjectId)
```

### 10.5 StudentEnrollment

Vínculo temporal:

```text
Student
Class
AcademicPeriod
StartsAt
EndsAt?
```

Sem sobreposição para o mesmo Student+AcademicPeriod.

### 10.6 TeachingAssignment

Vínculo temporal:

```text
TeacherPersonId
ClassSubjectId
AcademicPeriodId
StartsAt
EndsAt?
```

É a fonte da autoridade docente contextual.

---

## 11. Módulo Publications

### 11.1 Publication

Principal agregado editorial.

Campos conceituais:

```text
Id
Type
ContextType
InstitutionalAudience?
AuthorPersonId
AcademicPeriodId?
SubjectId?
Title
ContentHtml?
ContentPlainText?
RelevantDate?
RelevantTime?
Location?
Status
FirstPublishedAt?
PublishedAt?
UpdatedAt
ArchivedAt?
PinnedUntil?
Version
```

### 11.2 Tipos

```text
Material
Activity
Presentation
Notice
```

### 11.3 Contextos

```text
Academic
ClassGeneral
Institutional
```

#### Academic

Obrigatório:

- AcademicPeriod
- Subject
- 1..N Classes

#### ClassGeneral

Obrigatório:

- AcademicPeriod
- 1..N Classes

Proibido:

- Subject

#### Institutional

Obrigatório:

```text
AllFamilies
AllTeachers
WholeCenter
```

Proibido:

- AcademicPeriod
- Subject
- Class targets

### 11.4 Estados

```text
Draft
Published
```

Arquivamento é ortogonal.

### 11.5 FirstPublishedAt vs PublishedAt

- `FirstPublishedAt`: primeira publicação histórica, imutável;
- `PublishedAt`: publicação corrente.

Unpublish:

```text
Status = Draft
PublishedAt = null
```

Republish:

```text
Status = Published
PublishedAt = now()
```

### 11.6 Relevância

Com `RelevantDate`:

```text
current until RelevantDate + 7 dias
```

Sem `RelevantDate`:

```text
current until PublishedAt + 7 dias
```

### 11.7 Content

Rich text armazenado como HTML sanitizado por whitelist no backend.

Também é derivado e persistido `ContentPlainText` para busca, excerpts e notificações.

---

## 12. PublicationTarget

Classes selecionadas são filhos estruturais da publicação.

```text
PublicationTarget
├── PublicationId
└── ClassId
```

Constraint:

```text
UNIQUE(PublicationId, ClassId)
```

---

## 13. StudentPublicationDetail

Filho editorial de `Publication`.

```text
StudentPublicationDetail
├── PublicationId
├── StudentId
└── Content
```

Constraint:

```text
UNIQUE(PublicationId, StudentId)
```

O backend valida eligibility do Student para o contexto da publicação.

A família recebe somente details de seus próprios Students.

---

## 14. PublicationMaterial

Material pode ser:

```text
UploadedFile
ExternalLink
```

Campos:

```text
DisplayName
SortOrder
StoredFileId?
ExternalUrl?
```

Invariantes:

- UploadedFile → `StoredFileId` obrigatório;
- ExternalLink → `ExternalUrl` obrigatório;
- um material não pode ser simultaneamente arquivo e link.

---

## 15. Controle de acesso a publicações

### 15.1 PublicationStudentAccess

Publicações acadêmicas materializam grants por Student.

```text
PublicationStudentAccess
├── PublicationId
├── StudentId
├── GrantedAt
├── GrantReason
├── RevokedAt?
└── RevocationReason?
```

Apenas um grant vigente por Publication+Student.

### 15.2 Motivo

Exemplos:

```text
InitialAudience
EnteredClassWhileCurrent
HistoricalCorrection
```

### 15.3 Razão para persistir grants

A família deve preservar acesso histórico legítimo mesmo após o Student mudar de turma.

Não recalcular histórico exclusivamente pela matrícula atual.

### 15.4 Guardian não recebe grant direto

Autorização familiar:

```text
Guardian
→ GuardianFamily vigente
→ Students da Family
→ PublicationStudentAccess
```

Quando o vínculo `GuardianFamily` termina, o Guardian perde acesso inclusive ao histórico daquela Family.

### 15.5 Institucionais

Publicações `AllFamilies`, `AllTeachers` e `WholeCenter` não usam `PublicationStudentAccess`; sua elegibilidade é derivada.

---

## 16. Módulo Files

### 16.1 Abstração

```text
IFileStorage
├── FileSystemFileStorage   ← v0.1
└── S3FileStorage           ← futuro
```

Application/Domain nunca conhecem caminho físico.

### 16.2 StoredFile

Metadata:

```text
Id
StorageKey
OriginalFileName
MediaType
SizeBytes
SecurityStatus
CreatedByPersonId
CreatedAt
ValidatedAt?
RejectedAt?
RejectionCode?
```

### 16.3 Estados

```text
PendingValidation
Available
Rejected
```

Somente `Available` pode ser referenciado por publicação disponível.

### 16.4 Quarantine

Upload entra em área de quarentena antes de storage definitivo.

### 16.5 Validação

Pipeline:

```text
Upload
↓
BasicFileSecurityScanner
↓
opcional futuro: ClamAvFileSecurityScanner
↓
Available ou Rejected
```

Validação básica inclui:

- tamanho;
- whitelist de extensão;
- MIME;
- magic bytes quando aplicável;
- coerência extensão/conteúdo;
- validação especial de Office Open XML;
- nome seguro;
- arquivo vazio/corrompido em verificações básicas.

### 16.6 Antivírus

Modo futuro configurável.

Se `FileSecurityMode=Antivirus` e scanner estiver indisponível, comportamento será fail-closed.

### 16.7 Arquivos privados

Não haverá URL pública permanente.

Download:

```text
GET /api/v1/files/{id}/content
↓
authorization
↓
stream
```

### 16.8 Streaming

Suportar:

- `Content-Type`;
- `Content-Length`;
- `Content-Disposition`;
- `Range`;
- `206 Partial Content` quando aplicável.

### 16.9 Imutabilidade

Substituir um arquivo cria novo `StoredFile`. Bytes antigos não são sobrescritos.

### 16.10 Cleanup

Arquivos órfãos não são apagados imediatamente. Cleanup assíncrono remove objetos sem referência após período de segurança.

---

## 17. Módulo Notifications

### 17.1 NotificationOccurrence

Representa um fato comunicável.

Exemplos:

```text
PublicationPublished
PublicationUpdated
PublicationRepublished
PublicationPinnedAndResent
```

### 17.2 NotificationDelivery

Uma entrega por:

```text
Occurrence + UserAccount + Channel
```

Constraint unique garante deduplicação.

No v0.1:

```text
Channel = Email
```

### 17.3 DestinationSnapshot

O endereço usado é persistido para rastreabilidade histórica da tentativa.

### 17.4 Email

Application não conhece SMTP.

```text
IEmailSender
```

desacopla o provedor.

### 17.5 Sem inbox interna

O v0.1 não possui central de notificações.

- Email chama o usuário para o Átrio;
- Home e Agenda mostram o estado atual.

---

## 18. Transactional Outbox

### 18.1 Objetivo

Publicação e criação da notificação devem ser transacionais, mas SMTP não pode fazer parte da transaction.

Fluxo:

```text
BEGIN
Publication
Audience grants
Audit
NotificationOccurrence
NotificationDelivery
OutboxMessage
COMMIT
↓
BackgroundService
↓
IEmailSender
```

### 18.2 PostgreSQL Outbox

Sem RabbitMQ no v0.1.

Campos principais:

```text
Type
Payload
CreatedAt
AvailableAt
ProcessedAt?
Attempts
LockedUntil?
LastErrorCode?
```

### 18.3 Worker

Executa inicialmente no processo da API.

Usa locking seguro (`FOR UPDATE SKIP LOCKED` ou equivalente) para suportar futura concorrência.

### 18.4 Retry

Retry com backoff. Falhas persistentes permanecem diagnosticáveis.

---

## 19. Módulo Audit

### 19.1 AuditEvent

Append-only.

Campos:

```text
OccurredAt
ActorUserAccountId?
ActorPersonId?
Action
TargetType
TargetId?
ContextData?
TraceId?
```

### 19.2 Separação

```text
AuditEvent = quem fez o quê no negócio
Structured Log = comportamento técnico
```

### 19.3 Eventos obrigatórios

Incluem:

- publication created/published/updated/unpublished/archived;
- account enabled/disabled;
- role granted/revoked;
- GuardianFamily start/end;
- enrollment start/end;
- TeachingAssignment start/end;
- alteração administrativa de TeacherContact;
- bootstrap/recovery;
- ações administrativas sensíveis.

### 19.4 Sem Event Sourcing

Audit log não reconstrói o banco.

---

## 20. InstitutionSettings

Singleton lógico:

```text
Name
ShortName?
LogoStoredFileId?
Email?
Phone?
TimeZoneId
Version
```

Não haverá `TenantId` disseminado no modelo.

Timezone deve ser identificador canônico, por exemplo:

```text
America/Sao_Paulo
```

Instantes técnicos ficam em UTC; datas civis permanecem `date`; horários civis permanecem `time`.

---

## 21. Banco de dados

### 21.1 Um PostgreSQL

Um banco, um `AtrioDbContext`, uma unidade transacional local.

### 21.2 Schemas

```text
institution
identity
people
academics
publications
files
notifications
audit
```

### 21.3 Convenções

```text
snake_case     → banco
PascalCase     → C#
camelCase      → JSON
uuid           → PK/FK
timestamptz    → instante
date           → data civil
time           → horário civil
jsonb          → metadata controlada
```

### 21.4 UUIDv7

Entidades novas usarão UUID, preferencialmente UUIDv7.

UUID não é mecanismo de autorização.

### 21.5 Foreign keys

`RESTRICT/NO ACTION` por padrão.

`CASCADE` somente para filhos sem significado independente em exclusões realmente permitidas.

---

## 22. Tabelas lógicas principais

### Institution

```text
institution.settings
```

### Identity

```text
identity.user_accounts
identity.account_emails
identity.user_account_roles
identity.user_sessions
identity.invitations
identity.security_tokens
```

### People

```text
people.persons
people.families
people.students
people.guardian_families
people.teacher_contacts
```

### Academics

```text
academics.academic_periods
academics.classes
academics.subjects
academics.class_subjects
academics.student_enrollments
academics.teaching_assignments
```

### Publications

```text
publications.publications
publications.publication_targets
publications.student_details
publications.publication_materials
publications.student_access
publications.publication_search
```

### Files

```text
files.stored_files
```

### Notifications

```text
notifications.occurrences
notifications.deliveries
notifications.outbox_messages
```

### Audit

```text
audit.events
```

---

## 23. Constraints de banco

O PostgreSQL deve garantir, quando viável:

- PK/FK;
- e-mail de login globalmente único;
- no máximo um email `Current` por conta;
- no máximo um `PendingChange` por conta;
- no máximo um `AcademicPeriod Active`;
- no máximo um próximo `Draft`;
- `ClassSubject` único por Class+Subject;
- StudentDetail único por Publication+Student;
- Delivery única por Occurrence+Account+Channel;
- um StudentAccess vigente por Publication+Student;
- ausência de sobreposição temporal em GuardianFamily;
- ausência de sobreposição em StudentEnrollment por Student+Period;
- ausência de sobreposição em TeachingAssignment equivalente;
- coerência estrutural de PublicationContext;
- `RelevantTime` somente com `RelevantDate`;
- `PinnedUntil` somente em contextos permitidos;
- coerência UploadedFile vs ExternalLink;
- version fields não nulos.

Regras complexas de negócio permanecem no Domain/Application.

---

## 24. Temporalidade

Relações temporais usam semântica:

```text
[start, end)
```

`StartsAt` incluído; `EndsAt` excluído.

Isso permite encerrar e iniciar novo vínculo no mesmo instante sem sobreposição.

PostgreSQL poderá usar exclusion constraints com `btree_gist`.

---

## 25. Concorrência

Entidades mutáveis relevantes usam:

```text
Version bigint
```

Update:

```text
WHERE id = @id
AND version = @expectedVersion
```

Se nenhuma linha for alterada:

```text
409 Conflict
code = publication.concurrent_change
```

Não haverá overwrite silencioso.

---

## 26. Busca

### 26.1 PostgreSQL

Busca do v0.1 usa PostgreSQL, sem search engine externa.

Extensões possíveis:

- `unaccent`
- `pg_trgm`

### 26.2 Conteúdo geral

Documento de busca geral inclui:

- Title;
- ContentPlainText;
- Material DisplayName.

### 26.3 StudentDetails

StudentDetail privado não entra no documento geral.

A busca privada faz:

```text
general match
OR
authorized student detail match
```

com autorização aplicada dentro da query.

---

## 27. API HTTP

### 27.1 Base

```text
/api/v1
```

### 27.2 Tipos de endpoint

#### Recursos

```text
GET /publications/{id}
POST /publications
PUT /publications/{id}
DELETE /publications/{id}
```

#### Commands

```text
POST /publications/{id}/publish
POST /publications/{id}/unpublish
POST /publications/{id}/archive
POST /accounts/{id}/disable
```

#### Read models

```text
GET /me/family/home
GET /me/family/agenda
GET /me/teacher/home
GET /me/teacher/classes
```

### 27.3 GET sem side effects

GET e HEAD nunca modificam estado de negócio.

---

## 28. Semântica HTTP

| Caso | Status |
|---|---:|
| Consulta OK | 200 |
| Criado | 201 |
| Processamento assíncrono | 202 |
| Sem body | 204 |
| Request inválido | 400 |
| Não autenticado | 401 |
| Sem permissão | 403 |
| Não encontrado/invisível | 404 |
| Conflito | 409 |
| Regra de negócio | 422 |
| Rate limit | 429 |
| Erro interno | 500 |

Recursos sensíveis podem responder `404` para evitar enumeração.

---

## 29. ProblemDetails

Todos os erros seguem contrato uniforme baseado em RFC Problem Details.

Extensões:

```text
code
traceId
validationErrors
```

Frontend toma decisões por `code`, não pela mensagem em português.

---

## 30. Endpoints de autenticação

Baseline:

```text
POST /api/v1/auth/login
POST /api/v1/auth/logout
GET  /api/v1/me

POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
POST /api/v1/auth/change-password
POST /api/v1/auth/reauthenticate

POST /api/v1/invitations/activate
POST /api/v1/invitations/request-new
```

Troca de e-mail possui fluxo próprio.

---

## 31. CSRF

Como a autenticação usa cookie:

- same-origin;
- SameSite;
- antiforgery token explícito.

A SPA obtém token em endpoint apropriado e envia header nas mutações.

POST/PUT/PATCH/DELETE autenticados exigem antiforgery, exceto endpoints públicos especificamente desenhados para login/reset.

---

## 32. Tokens em URLs

Links de convite/reset preferem fragmento:

```text
https://atrio/.../activate#token=...
```

O fragmento não é enviado ao servidor durante a navegação.

SPA lê o token e o envia no body de POST.

Páginas sensíveis usam:

```text
Referrer-Policy: no-referrer
```

---

## 33. Rate limiting

Aplicar especialmente a:

- login;
- forgot password;
- request new invitation;
- activation;
- endpoints públicos de segurança.

Rate limit considera IP e identificador normalizado de conta quando apropriado.

Atingir rate limit nunca altera conta para `Disabled`.

---

## 34. Authorization Design

### 34.1 Camadas

```text
Authentication
↓
Role/Capability
↓
Resource/Domain authorization
```

### 34.2 Policies de capacidade

Papéis tratam apenas capacidade geral.

### 34.3 Autorização contextual

Serviços especializados:

```text
IPublicationAuthorizationService
IFamilyAuthorizationService
IAcademicAuthorizationService
IFileAuthorizationService
```

### 34.4 Linguagem de domínio

Preferir:

```text
CanCreateAcademicPublication(teacher, subject, classes)
```

a permissões string genéricas quando o escopo depende de vínculos temporais.

### 34.5 Actor

Cliente nunca fornece `ActorId`. Ator vem da sessão autenticada.

---

## 35. APIs por contexto

### Family

```text
GET /api/v1/me/family/home
GET /api/v1/me/family/agenda
GET /api/v1/me/family/children
GET /api/v1/me/family/children/{studentId}
GET /api/v1/me/family/publications
```

### Teacher

```text
GET /api/v1/me/teacher/home
GET /api/v1/me/teacher/agenda
GET /api/v1/me/teacher/classes
GET /api/v1/me/teacher/classes/{classId}
GET /api/v1/me/teacher/students
GET /api/v1/me/teacher/publications
```

### Admin

```text
/api/v1/admin/people
/api/v1/admin/families
/api/v1/admin/students
/api/v1/admin/teachers
/api/v1/admin/accounts
/api/v1/admin/academic-periods
/api/v1/admin/classes
/api/v1/admin/subjects
/api/v1/admin/publications
/api/v1/admin/audit
```

### Files

```text
POST /api/v1/files
GET  /api/v1/files/{id}
GET  /api/v1/files/{id}/content
```

---

## 36. OpenAPI

A API ASP.NET é a fonte formal do contrato HTTP.

Frontend pode gerar types/client TypeScript a partir de OpenAPI.

Swagger UI:

- habilitado em desenvolvimento;
- desabilitado ou protegido em produção.

---

## 37. Frontend Architecture

### 37.1 Feature-based

```text
src/
├── app/
├── auth/
├── family/
├── teacher/
├── admin/
├── publications/
├── files/
├── institution/
└── shared/
```

### 37.2 Router

Rotas refletem os contextos:

```text
/family/...
/teacher/...
/admin/...
```

Troca de contexto é navegação, não troca de identidade.

### 37.3 Bootstrap

Ao iniciar:

```text
GET /api/v1/me
```

retorna identidade mínima e `availableContexts`.

### 37.4 Estado

```text
Server state → TanStack Query
Form state   → React Hook Form
Local UI     → React state
Identity     → pequeno session context
```

Sem Redux no v0.1.

---

## 38. Frontend API client

Um único client centraliza:

- base URL;
- `credentials: include`;
- CSRF header;
- ProblemDetails;
- TraceId;
- cancellation.

Não haverá `fetch()` arbitrário espalhado em componentes.

---

## 39. Tratamento de erros na UI

- `401` → sessão encerrada/login;
- `403` → sem permissão;
- `404` → conteúdo inexistente/invisível;
- `409` → conflito de edição;
- `422` → erros de formulário/regra;
- `500` → erro genérico com `traceId`.

Error Boundaries impedem tela branca.

---

## 40. Publication Editor

Feature própria:

```text
PublicationEditor
├── TypeSelector
├── ContextSelector
├── Title
├── RichTextEditor
├── RelevantMoment
├── Materials
├── StudentDetails
└── Actions
```

### Draft

Autosave permitido, com debounce e feedback real.

### Published

Sem autosave server-side durante edição; atualização só ocorre ao `Salvar alterações`.

### Conflict

`Version` acompanha o form e qualquer `409` interrompe o autosave/edição segura.

---

## 41. Upload Manager

Estados:

```text
Queued
Uploading
Validating
Available
Rejected
Failed
```

Responsabilidades:

- progress;
- cancel;
- retry;
- associação com `StoredFileId`.

Publicação só pode referenciar arquivo `Available`.

---

## 42. Responsive Design

### Mobile

- single column;
- cards;
- drawers/sheets;
- ações principais sticky quando útil;
- navegação compacta;
- sem dependência de hover.

### Desktop

- sidebar;
- múltiplas colunas úteis;
- tabelas quando apropriadas;
- maior superfície do editor.

Family e Teacher recebem prioridade máxima de UX mobile.

---

## 43. Design System

Baseline:

- Tailwind;
- primitives acessíveis;
- tokens de spacing, typography, radius, shadow e semantic colors;
- Lucide como biblioteca única de ícones;
- nenhuma dependência de cor como único indicador.

---

## 44. Acessibilidade

Referência: **WCAG 2.2 AA**.

Requisitos:

- keyboard navigation;
- focus management;
- contrast;
- labels;
- ARIA somente quando necessário;
- screen-reader names;
- reduced motion onde aplicável;
- alternativa acessível ao drag-and-drop.

---

## 45. PWA/offline

Não haverá Service Worker/PWA offline no v0.1.

Conteúdo privado usa cache conservador.

Assets estáticos versionados podem receber cache agressivo.

---

## 46. Headers de segurança

Baseline:

```text
Strict-Transport-Security     ← após validação inicial
X-Content-Type-Options: nosniff
Referrer-Policy
Permissions-Policy
Content-Security-Policy
```

CSP deverá ser restritiva, preferencialmente próxima de:

```text
default-src 'self'
frame-ancestors 'none'
```

com aberturas explícitas apenas quando necessárias.

---

## 47. Threat Model

### 47.1 IDOR/BOLA

Ameaça prioritária.

Mitigação:

- autorização por recurso;
- queries filtradas;
- testes negativos;
- 404 quando apropriado.

### 47.2 XSS

Mitigação em camadas:

```text
restricted editor
↓
server-side sanitization
↓
safe rendering
↓
CSP
```

### 47.3 CSRF

Mitigação:

- same-origin;
- SameSite;
- antiforgery token.

### 47.4 Upload malicioso

Mitigação:

- whitelist;
- size limit;
- MIME/magic bytes;
- quarantine;
- scanner abstraction;
- private storage;
- `nosniff`.

Não aceitar:

- executáveis;
- scripts;
- SVG;
- formatos Office com macro como DOCM/XLSM/PPTM.

### 47.5 SSRF

Átrio não fará fetch server-side automático de links externos no v0.1.

### 47.6 Brute force

- password hashing do ASP.NET Identity;
- progressive throttling;
- generic responses.

### 47.7 Session theft

- HttpOnly;
- Secure;
- SameSite;
- CSP;
- expiração absoluta;
- revogação server-side;
- SecurityVersion.

Não vincular sessão rigidamente a IP/User-Agent.

### 47.8 Enumeration

Forgot password e request invitation retornam resposta neutra.

### 47.9 Open redirect

Somente return URLs internas relativas.

### 47.10 Logs

Nunca registrar:

- passwords;
- cookies;
- tokens;
- CSRF;
- conteúdo privado integral;
- StudentDetails;
- secrets.

### 47.11 Backups

Tratados como dados sensíveis.

### 47.12 Dados de menores

Reduzir coleta e reforçar autorização. Não coletar CPF/RG/endereço/documentação sem caso de uso aprovado.

---

## 48. Infraestrutura de produção

### 48.1 Compose

Produção será definida em composição explícita, por exemplo:

```text
docker-compose.prod.yml
```

### 48.2 Containers

```text
caddy
atrio-web
atrio-api
postgres
```

### 48.3 Redes

```text
public
internal
```

Somente Caddy publica portas no host.

### 48.4 Volumes

```text
postgres-data
atrio-files
caddy-data
caddy-config
```

Quarantine é separada logicamente do storage definitivo.

### 48.5 Imagens

- multi-stage;
- runtime mínimo;
- sem root quando viável;
- filesystem read-only quando possível.

---

## 49. HTTPS

Caddy gerencia certificados ACME automaticamente.

Produção externa exige HTTPS real.

HTTP apenas redireciona para HTTPS e pode servir ao challenge ACME.

HSTS será ativado após validação inicial do domínio, sem `preload` no começo.

---

## 50. DNS e IP

Produção usa hostname DNS como `PublicBaseUrl`.

Mudança de IP é resolvida pela infraestrutura/DDDNS, não por código do Átrio.

CGNAT é restrição operacional. Se ocorrer:

- solicitar IP público;
- migrar para VPS;
- reconsiderar tunnel.

---

## 51. Migrations

### 51.1 Versionadas

EF Core Migrations no Git.

### 51.2 Produção

API não aplica migration silenciosamente no startup.

Fluxo:

```text
backup/precheck
↓
migration job
↓
start/update app
↓
health check
```

### 51.3 Credenciais

- `atrio_migrator` → DDL;
- `atrio_app` → DML.

### 51.4 Expand/contract

Migrations destrutivas seguem estratégia compatível com rollback de aplicação.

---

## 52. Releases

Containers versionados:

```text
atrio-api:0.1.0
atrio-web:0.1.0
```

Não usar `latest` como única referência de produção.

Release identifica:

- versão;
- commit;
- build.

---

## 53. Deploy

Runbook mínimo:

```text
1. verificar versão atual
2. executar backup/precheck
3. obter imagens da release
4. executar migrations
5. atualizar containers
6. health checks
7. smoke test
8. registrar versão
```

Atualização automática sem controle não faz parte do v0.1.

---

## 54. Rollback

Rollback de aplicação e rollback de banco são operações distintas.

Migrations devem ser desenhadas para permitir, quando possível, voltar a versão da aplicação sem reverter imediatamente o schema.

---

## 55. Backup

### 55.1 PostgreSQL

Backup lógico via:

```text
pg_dump
```

### 55.2 Files

Backup preserva `StorageKey → bytes`.

### 55.3 RPO

Meta inicial:

```text
RPO ≤ 24h
```

### 55.4 Retenção

Baseline:

- 7 diários;
- 4 semanais;
- 3 mensais.

### 55.5 Destino

Backup deve existir fora da máquina de produção.

---

## 56. Restore

Restore é parte do requisito operacional.

Teste mínimo:

```text
máquina vazia
↓
Docker/config
↓
restore PostgreSQL
↓
restore files
↓
subir
↓
health
↓
login
↓
abrir publicação com anexo
```

Backup sem restore testado não é considerado suficiente.

---

## 57. Logs e observabilidade

### 57.1 Logs estruturados

stdout/stderr.

Campos úteis:

- Timestamp;
- Level;
- TraceId;
- RequestId;
- Event;
- ErrorCode;
- UserAccountId quando apropriado.

### 57.2 Rotação

Host deve limitar tamanho e quantidade de logs.

### 57.3 Health

```text
/health/live
/health/ready
```

Readiness inclui PostgreSQL.

SMTP não precisa derrubar readiness porque envio é assíncrono.

### 57.4 FileStorage

Health específico para storage.

### 57.5 Outbox

Diagnóstico mínimo:

- pending count;
- failed count;
- oldest pending message.

### 57.6 OpenTelemetry

Aplicação pode ser preparada para OpenTelemetry, mas sem stack adicional obrigatória no v0.1.

---

## 58. Test Architecture

### Backend

- unit tests;
- application/domain tests;
- integration tests com PostgreSQL real via Testcontainers;
- authorization positive/negative tests;
- migration validation;
- concurrency tests.

### Frontend

- Vitest;
- Testing Library;
- MSW ou equivalente para API;
- accessibility checks baratos com axe;
- Playwright para jornadas críticas.

### E2E críticos

- bootstrap/login;
- convite;
- professor publica;
- família visualiza;
- StudentDetail protegido;
- upload/download;
- admin desativa conta.

---

## 59. CI

PR deve executar, no mínimo:

### Backend

- restore/build;
- unit tests;
- integration tests.

### Frontend

- typecheck;
- tests;
- build.

### Arquitetura

- dependency rules.

### Containers

- smoke test quando aplicável.

### Segurança

Progressivamente:

- NuGet vulnerability scan;
- npm dependency scan;
- container image scan;
- secret scanning.

---

## 60. Definition of Done arquitetural do MVP v0.1

A arquitetura é considerada corretamente implementada quando:

- repo Átrio independente existe;
- solução respeita Clean Architecture e módulos;
- PostgreSQL possui migrations versionadas;
- sessão server-side funciona;
- cookie seguro e CSRF funcionam;
- roles e autorização contextual funcionam;
- nenhuma família acessa dados de outra família;
- nenhum professor publica fora de seus TeachingAssignments;
- PublicationStudentAccess preserva histórico;
- arquivos são privados e autorizados;
- upload passa por quarantine e validação;
- Outbox garante que falha de SMTP não invalida publicação;
- NotificationDelivery deduplica por ocorrência;
- logs e audit têm finalidades separadas;
- health checks funcionam;
- backup e restore foram testados;
- produção externa usa HTTPS;
- somente Caddy é exposto;
- containers e dados são separados;
- migrations não rodam silenciosamente na API;
- mobile principal foi validado;
- testes negativos de autorização existem;
- E2E principal passa.

---

## 61. Decisões conscientemente adiadas

Fora do v0.1:

- S3/MinIO obrigatório;
- ClamAV obrigatório;
- Redis;
- broker;
- MFA;
- PWA/offline;
- push notifications;
- calendar integration;
- semantic search;
- multi-tenant;
- CQRS framework;
- MediatR obrigatório;
- Event Sourcing;
- read database separado;
- cache distribuído;
- Kubernetes;
- zero-downtime deployment;
- service mesh;
- OpenSearch/Elastic;
- observability stack dedicada;
- central de notificações;
- inbox interna;
- student accounts;
- comments/chat;
- read receipts.

---

## 62. Architecture Decision Register — resumo

### Repositório e runtime
- AD-001: repositórios independentes com integração futura por contratos.
- AD-002: monólito modular.
- AD-003: .NET 10/C#.
- AD-004: React 19/TypeScript/Vite.
- AD-005: PostgreSQL.
- AD-006: Clean Architecture + DDD pragmático.
- AD-007: REST.
- AD-008: frontend/backend separados em runtime.
- AD-009: Docker padrão.
- AD-010: sem Kubernetes.

### Hospedagem e borda
- AD-011: produção local inicialmente, arquitetura portável.
- AD-013: reverse proxy como única entrada pública.
- AD-014: HTTPS obrigatório externamente.
- AD-015: `PublicBaseUrl` explícita.
- AD-070: Windows inicialmente, Linux/VPS depois.
- AD-082: exposição direta pelo roteador.
- AD-088: Caddy.
- AD-089: TLS automático.

### Persistência e storage
- AD-021: filesystem v0.1 por abstração.
- AD-022: arquivos privados.
- AD-025: bytes imutáveis.
- AD-026: EF Core principal.
- AD-029: migrations explícitas.
- AD-051: um PostgreSQL/DbContext.
- AD-053: optimistic concurrency com version.
- AD-178: schemas funcionais.

### Auth e autorização
- AD-017: cookie session, sem JWT localStorage.
- AD-018: ASP.NET Identity como primitive, não domínio.
- AD-019: sessões revogáveis.
- AD-020: policy + autorização contextual.
- AD-046: sessões no PostgreSQL.
- AD-047: CSRF explícito.
- AD-048: HttpOnly/Secure/SameSite=Lax.
- AD-159: SecurityVersion.
- AD-160/161: tokens hashed e com purpose.

### Publicações e audiência
- AD-054: audiência acadêmica não recalculada puramente ao vivo.
- AD-055: grants persistidos.
- AD-056: Guardian não recebe grant direto.
- AD-057: nova matrícula herda apenas conteúdo corrente.
- AD-135–150: agregado Publication, contextos, materials, student details e grants.

### Notifications
- AD-030: e-mail assíncrono.
- AD-031: Transactional Outbox em PostgreSQL.
- AD-032: sem RabbitMQ/Redis.
- AD-033: `IEmailSender`.
- AD-059/060: occurrence + delivery + outbox.
- AD-061: worker na API inicialmente.

### Frontend
- AD-035: SPA.
- AD-036: TanStack Query.
- AD-037: RHF + Zod.
- AD-066: Tailwind + primitives acessíveis.
- AD-316–374: feature architecture, routing, editor, upload manager, mobile, accessibility, sem PWA.

### Segurança
- AD-417–463: threat model, IDOR, XSS, CSRF, upload safety, brute force, session security, token security, enumeration, logs, backups, dados de menores e go-live checklist.

---

## 63. Diagramas consolidados

### 63.1 Arquitetura lógica

```text
Browser / React
      │
      ▼
Caddy / HTTPS
      │
      ├─────────────── static web
      │
      ▼
ASP.NET Core API
      │
      ├── Identity & Access
      ├── People & Families
      ├── Academics
      ├── Publications
      ├── Files
      ├── Notifications
      ├── Administration & Audit
      └── Institution
      │
      ├────────── PostgreSQL
      ├────────── FileStorage
      └────────── SMTP
```

### 63.2 Publicação

```text
Teacher
  │
  ▼
POST publish
  │
  ▼
Auth + CSRF + Authorization
  │
  ▼
Application
  ├── Publication validation
  ├── Academic authorization
  ├── Audience calculation
  │
  ▼
Transaction
  ├── Publication
  ├── StudentAccess
  ├── AuditEvent
  ├── NotificationOccurrence
  ├── NotificationDelivery
  └── OutboxMessage
  │
  ▼
COMMIT
  │
  ├──────────► HTTP response
  │
  ▼
Background Worker
  │
  ▼
IEmailSender
```

### 63.3 Leitura familiar

```text
Guardian
  │
  ▼
GET publication/home
  │
  ▼
Session
  │
  ▼
GuardianFamily vigente
  │
  ▼
Students da Family
  │
  ▼
PublicationStudentAccess
  │
  ▼
Authorized read model
  ├── General content
  ├── Materials
  └── StudentDetails dos próprios filhos
```

### 63.4 Storage

```text
Upload
  │
  ▼
Quarantine
  │
  ▼
Basic scanner
  │
  ├── Rejected
  │
  └── Available
        │
        ▼
FileSystemFileStorage
        │
        ▼
StoredFile metadata
```

---

## 64. Pendências antes de implementação

A Architecture Design está fechada em nível suficiente para planejamento.

Antes de escrever código ainda é necessário:

1. revisar e aprovar esta spec;
2. transformar a arquitetura em Implementation Plan;
3. decompor o plano em tarefas TDD;
4. definir ordem de entrega do v0.1;
5. somente então preparar os prompts do Antigravity.

Não deve ser iniciado scaffolding, código, dependências ou criação de repositório antes da aprovação formal desta especificação e do plano de implementação.
