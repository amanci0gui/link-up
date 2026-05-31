# Sessão 1 — Fundação do Projeto

**Data:** 2026-05-30
**Status:** Concluída

## Visão Geral

Objetivo da sessão: estabelecer fundação completa do projeto LinkUp — documentação técnica, estrutura de solução .NET 10, autenticação JWT funcional e schema de banco de dados completo. Ao final, `dotnet build` passa com 0 erros e 0 warnings em todos os 7 projetos.

---

## 2. Entregues

### 2.1 Documentação Técnica (`/docs`)

- **`PLANNING.md`** — regras de negócio, 6 fluxos críticos (onboarding, conexão base, recomendação, troca de contato, feedback, bloqueio), 10 entidades de domínio, roadmap 7 semanas, 7 ADRs, tabela de riscos
- **`ARCHITECTURE.md`** — estrutura .NET 10, contratos de API (request/response/erros), Result pattern com erros tipados, config JWT, Redis cache keys+TTL, 2 background jobs, tabela FCM notifications, dependências NuGet, variáveis de ambiente
- **`DATABASE.md`** — ERD Mermaid, schema SQL completo 13 tabelas com constraints/índices, migrations ordenadas, trigger reputation_score, soft delete + estratégia LGPD anonymization, 6 queries críticas, tabela Redis

---

### 2.2 Infraestrutura Base

- Solution `.sln` com 4 projetos: `LinkUp.Api`, `LinkUp.Application`, `LinkUp.Domain`, `LinkUp.Infrastructure`
- 2 projetos de teste: `LinkUp.UnitTests`, `LinkUp.IntegrationTests`
- `docker-compose.yml` — PostgreSQL 16 Alpine + Redis 7 Alpine com healthchecks e volumes persistentes
- `.gitignore` — padrão .NET completo
- Packages: MediatR 14, Dapper 2, Npgsql 10, StackExchange.Redis 2, BCrypt.Net-Next, Serilog, JwtBearer 10, FluentValidation 12, Scalar.AspNetCore, Microsoft.AspNetCore.OpenApi

---

### 2.3 Camada Domain

| Arquivo | Descrição |
|---|---|
| `Entities/User.cs` | Private constructor, factory `Create()`, `Reconstitute()` para Dapper mapping, `Anonymize()` para LGPD |
| `Enums/Enums.cs` | `RecommendationType`, `RecommendationStatus`, `ConnectionRequestStatus`, `BlockType`, `ContactType` |
| `Interfaces/Repositories/IUserRepository.cs` | Contrato do repositório de usuários |

---

### 2.4 Camada Application

| Arquivo | Descrição |
|---|---|
| `Common/Models/Result.cs` | `Result<T>`, `AppError`, `Errors` estático com erros tipados (Auth, User, Recommendation, Connection) |
| `Common/Interfaces/IServices.cs` | `ICurrentUserService`, `IDateTimeService`, `ICacheService`, `ITokenService` |
| `Common/Interfaces/IPasswordService.cs` | Contrato `Hash()`, `Verify()` |
| `Common/Behaviors/ValidationBehavior.cs` | Pipeline MediatR — intercepta todas as requests, executa FluentValidation, retorna `Result<T>.Failure` em erros |
| `Features/Auth/Commands/Register/RegisterCommand.cs` | Command, response, validator (nome/email/senha), handler |
| `Features/Auth/Commands/Login/LoginCommand.cs` | Command, validator, handler |
| `Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs` | Command, handler com token rotation |
| `Features/Auth/Commands/Logout/LogoutCommand.cs` | MVP: client-side; server revocation planejado v2 |

---

### 2.5 Camada Infrastructure

| Arquivo | Descrição |
|---|---|
| `Persistence/Repositories/UserRepository.cs` | Dapper, SQL raw, mapping via `UserRow` → `User.Reconstitute()` |
| `Cache/RedisCacheService.cs` | `GetAsync`, `SetAsync`, `RemoveAsync`, `SetIfNotExistsAsync` |
| `Services/PasswordService.cs` | Wrapper BCrypt implementando `IPasswordService` |
| `Services/TokenService.cs` | Geração JWT, refresh token no Redis como `rt:{userId}:{tokenId}`, token rotation, revogação |
| `Services/CurrentUserService.cs` | Lê `userId`/`email` das claims do `HttpContext` |
| `Extensions/InfrastructureServiceExtensions.cs` | `AddInfrastructure()` isolada — Api não referencia Redis diretamente |

---

### 2.6 Camada API

| Arquivo | Descrição |
|---|---|
| `Program.cs` | Bootstrap com Serilog structured logging, middleware pipeline, JWT, Scalar UI em dev |
| `Controllers/AuthController.cs` | 4 endpoints Auth (register, login, refresh, logout) |
| `Middleware/ExceptionHandlingMiddleware.cs` | Captura exceções não tratadas, retorna 500 JSON padronizado |
| `Extensions/ServiceCollectionExtensions.cs` | `AddApplication()`, `AddInfrastructureDeps()`, `AddJwtAuthentication()`, `AddSwaggerWithJwt()` |
| `appsettings.json` | Configuração base: JWT, connection strings, Serilog |
| `appsettings.Development.json` | Override dev: log level Debug, secrets de desenvolvimento |

**Endpoints entregues:**

```
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout   [Authorize]
```

---

### 2.7 Banco de Dados

**`migrations/001_initial_schema.sql`** (15.7 KB) — schema completo:

- 5 enums PostgreSQL: `recommendation_type`, `recommendation_status`, `connection_request_status`, `block_type`, `contact_type`
- 12 tabelas:

| Tabela | Propósito |
|---|---|
| `users` | Usuários da plataforma |
| `connections` | Par de usuários conectados (canonical: `user_id_1 < user_id_2`) |
| `connection_requests` | Solicitações de conexão pendentes/respondidas |
| `recommendations` | Recomendações entre pares |
| `recommendation_feedback` | Avaliações pós-recomendação |
| `contacts` | Contatos cadastrados por usuário |
| `contact_shares` | Registro de troca de contatos via recomendação |
| `blocks` | Bloqueios entre usuários |
| `user_plans` | Plano do usuário (FREE, etc) |
| `push_tokens` | FCM tokens Android |
| `notifications` | Notificações in-app |
| `schema_migrations` | Controle de versão de migrations |

- Índices parciais otimizados: `WHERE status = 'PENDING'`, `WHERE is_deleted = false`
- Trigger `trigger_set_updated_at` em `users` e `user_plans`
- Migration executada automaticamente via `docker-entrypoint-initdb.d` no compose

---

### 2.8 Decisões Arquiteturais

| Decisão | Motivo |
|---|---|
| Swashbuckle removido → `AddOpenApi()` + Scalar UI | Swashbuckle 10 usa OpenAPI v2 com interfaces somente-leitura — breaking change sem migration path simples |
| `FrameworkReference` no Infrastructure | `Microsoft.AspNetCore.Http.Abstractions 2.3.10` incompatível com .NET 10; FrameworkReference inclui toda a stack ASP.NET Core |
| `AddInfrastructure` movido para Infrastructure project | Api não depende diretamente de StackExchange.Redis; respeita dependency inversion |
| Índices parciais no PostgreSQL | Queries operacionais tocam só PENDING; histórico (ACCEPTED/REJECTED) não consome IO |
| `CHECK(user_id_1 < user_id_2)` em connections | Par canônico elimina duplicatas `(A,B)` e `(B,A)` sem constraint invertido |

### 2.9 Status de Build

```
dotnet build → 0 erros, 0 warnings — 7 projetos
```

---

## 3. Próximos Passos

### Semana 2 — Conexões + Perfil

**Domain entities a criar:**
- `Connection`, `ConnectionRequest`, `Block`, `Recommendation`

**Features de Conexão:**
- `SendConnectionRequest` — valida existência, duplicatas, usuários ativos
- `RespondConnectionRequest` — accept cria registro em `connections`; reject/cancel atualiza status
- `GetConnections` — lista conexões do usuário autenticado

**Features de Perfil:**
- `GetUserProfile` — endpoint público; retorna perfil completo para conexões
- `UpdateProfile` — bio, foto URL, nome

**Repositories a implementar:**
- `IConnectionRepository`, `IConnectionRequestRepository`, `IBlockRepository`

---

### Semana 3 — Recomendações (core do produto)

**Features de Recomendação:**
- `CreateRecommendation` — valida: recommender conectado a ambos, sem bloqueio, sem duplicata PENDING, recomendações habilitadas no alvo
- `RespondRecommendation` — accept/reject pelo alvo; accept gera notificação
- `GetRecommendations` — inbox de recomendações pendentes recebidas

**Features de Contato:**
- `AddContact` — cadastrar WhatsApp, Instagram, LinkedIn, etc
- `ShareContact` — compartilhar contatos após recomendação aceita

---

### Semana 4-5 — Bloqueio, Feedback, Notificações

- `BlockUser` / `UnblockUser`
- `SubmitFeedback` — avaliação pós-conexão via recomendação; atualiza `reputation_score`
- Push tokens FCM Android: `RegisterPushToken`
- Background jobs:
  - Expirar recomendações vencidas (`expires_at`)
  - Recalcular `reputation_score` agregado

---

### Semana 6-7 — Qualidade e DevOps

Delegações planejadas:

| Agente | Responsabilidade |
|---|---|
| **GithubActionsExpert** | Pipeline CI/CD — build, test, security scan, Docker build/push |
| **TestingExpert** | Testes unitários handlers + integração repositórios Dapper |
| **SecurityExpert** | Rate limiting, security headers (HSTS, X-Frame, CSP), OWASP review |
