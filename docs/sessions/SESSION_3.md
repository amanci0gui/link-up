# Sessão 3 — Recomendações + Contatos (Core do Produto)

**Data:** 2026-06-04
**Status:** Concluída

## 1. Visão Geral

Objetivo: implementar feature core de Recomendação (Sessão 2, item 3 — Próximos Passos). Ao final, `dotnet build` passa com 0 erros/warnings em 7 projetos e `dotnet test` passa 11/11 testes unitários.

---

## 2. Entregáveis

### 2.1 Domain — Entidades Novas

| Arquivo | Descrição |
|---|---|
| `Entities/Recommendation.cs` | Par canônico (menor GUID string ordinal → `recommended_id`). Factory `Create()`, `Accept()`, `Reject()`, `Expire()`, `IsParticipant()`, `Reconstitute()`. Expira em 30 dias por padrão. |
| `Entities/RecommendationFeedback.cs` | Rating 1–5 (invariante em `Create()`), comentário opcional. `Reconstitute()`. Reservado para Sessão 4 (SubmitFeedback). |
| `Entities/Contact.cs` | `UserId`, `ContactType`, `Value` (trim), `IsPublic`. `Create()`, `Reconstitute()`. |
| `Entities/ContactShare.cs` | Registra consentimento de compartilhamento pós-indicação aceita. `RecommendationId`, `SharerId`, `RecipientId`, `SharedAt`. `Create()`, `Reconstitute()`. |

### 2.2 Domain — Enums Novos

Adicionados em `Enums/Enums.cs`:

| Enum | Valores |
|---|---|
| `RecommendationType` | `Friendship`, `Romance`, `Professional`, `Mentorship`, `Partnership` |
| `RecommendationStatus` | `Pending`, `PartiallyAccepted`, `Accepted`, `Rejected`, `Expired`, `Cancelled` |
| `ContactType` | `Phone`, `Email`, `Instagram`, `LinkedIn`, `WhatsApp`, `Other` |

### 2.3 Domain — Interfaces de Repositório Novas

| Arquivo | Contratos |
|---|---|
| `Interfaces/Repositories/IRecommendationRepository.cs` | `GetByIdAsync`, `GetPendingByRecipientAsync`, `ExistsPendingAsync`, `AddAsync`, `UpdateAsync` |
| `Interfaces/Repositories/IContactRepository.cs` | `GetByUserAsync`, `AddAsync` |
| `Interfaces/Repositories/IContactShareRepository.cs` | `ExistsAsync`, `AddAsync` |

### 2.4 Domain — Interface Modificada

| Arquivo | Mudança |
|---|---|
| `Interfaces/Repositories/IUserRepository.cs` | Adicionado `HasRecommendationsEnabledAsync(Guid userId, CT)` — coluna `recommendations_enabled` na tabela `users` |

---

### 2.5 Application — Erros Novos

Adicionados em `Common/Models/Result.cs`:

**`Errors.Recommendation`**

| Código | Mensagem | HTTP |
|---|---|---|
| `REC_001` | Você precisa estar conectado a ambos os usuários para indicá-los. | 403 |
| `REC_002` | Um dos usuários bloqueou suas indicações. | 403 |
| `REC_003` | Um dos usuários não aceita indicações no momento. | 403 |
| `REC_004` | Já existe uma recomendação pendente entre estes usuários. | 409 |
| `REC_005` | Recomendação não encontrada. | 404 |
| `REC_006` | Você não é participante desta recomendação. | 403 |
| `REC_007` | Você já respondeu esta recomendação. | 409 |

**`Errors.Contact`**

| Código | Mensagem | HTTP |
|---|---|---|
| `CONT_001` | Contatos só podem ser compartilhados após recomendação aceita. | 409 |
| `CONT_002` | Contato já compartilhado nesta recomendação. | 409 |
| `CONT_003` | Contato não encontrado. | 404 |

---

### 2.6 Application — Features de Recomendação

Pasta: `Features/Recommendations/`

| Arquivo | Descrição |
|---|---|
| `Commands/CreateRecommendation/CreateRecommendationCommand.cs` | Validações: recommender conectado a ambos, não-self, sem bloqueio bilateral, target com `recommendations_enabled=true`, sem PENDING duplicado (par canônico). Persiste `Recommendation`. |
| `Commands/RespondRecommendation/RespondRecommendationCommand.cs` | Somente participante responde. Accept: `recommendation.Accept()` + `Connection.Create()` + persiste ambos (transação lógica). Reject: atualiza status. |
| `Queries/GetRecommendations/GetRecommendationsQuery.cs` | Inbox PENDING para `currentUser`. Resolve "outro usuário" no handler (lógica de negócio não vaza para Infrastructure). Retorna `RecommendationDto` com info de recommender e outro usuário. |

### 2.7 Application — Features de Contato

Pasta: `Features/Contacts/`

| Arquivo | Descrição |
|---|---|
| `Commands/AddContact/AddContactCommand.cs` | Cadastra contato (`Type`, `Value`, `IsPublic`) para `currentUser`. Validator: `Value` obrigatório, max 255 chars. |
| `Commands/ShareContact/ShareContactCommand.cs` | Compartilha contato por `RecommendationId`. Valida: participante, status `ACCEPTED`, não duplicado. Persiste `ContactShare`. |

---

### 2.8 Infrastructure — Repositórios Novos

| Arquivo | Destaques |
|---|---|
| `Persistence/Repositories/RecommendationRepository.cs` | Dapper + `CommandDefinition` + CT. `GetPendingByRecipientAsync`: `WHERE (recommended_id = ? OR target_id = ?) AND status = 'PENDING'`. `ExistsPendingAsync`: ordenação canônica antes da query. Cast `::recommendation_type`, `::recommendation_status` no INSERT/UPDATE. |
| `Persistence/Repositories/ContactRepository.cs` | `GetByUserAsync` + `AddAsync`. Cast `::contact_type` no INSERT. |
| `Persistence/Repositories/ContactShareRepository.cs` | `ExistsAsync(recommendationId, sharerId)` + `AddAsync`. |

### 2.9 Infrastructure — Modificações

| Arquivo | Mudança |
|---|---|
| `Persistence/Repositories/UserRepository.cs` | Adicionado `HasRecommendationsEnabledAsync` — query `SELECT recommendations_enabled FROM users WHERE id = @Id`. |
| `Extensions/InfrastructureServiceExtensions.cs` | Registro Scoped: `IRecommendationRepository`, `IContactRepository`, `IContactShareRepository`. |

---

### 2.10 API — Controllers Novos

**`Controllers/RecommendationsController.cs`**

```
POST  /api/v1/recommendations                   [Authorize]  → CreateRecommendation
POST  /api/v1/recommendations/{id}/respond      [Authorize]  → RespondRecommendation
GET   /api/v1/recommendations                   [Authorize]  → GetRecommendations (inbox PENDING)
POST  /api/v1/recommendations/{id}/share-contact[Authorize]  → ShareContact
```

**`Controllers/ContactsController.cs`**

```
POST  /api/v1/contacts                          [Authorize]  → AddContact
```

Ambos seguem padrão `Problem(AppError)` dos controllers anteriores.

---

### 2.11 Migration SQL

Arquivo: `migrations/002_recommendation_feature.sql`

**Operações:**
1. `ALTER TABLE connections RENAME COLUMN created_at TO connected_at` — corrige divergência silenciosa entre `ConnectionRepository.cs` e migration 001.
2. `ALTER TABLE recommendations ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ` + trigger `trg_recommendations_set_updated_at`.
3. Índice único parcial anti-duplicata PENDING:
   ```sql
   CREATE UNIQUE INDEX IF NOT EXISTS ux_recommendation_pending
       ON recommendations (recommender_id, recommended_id, target_id)
       WHERE status = 'PENDING';
   ```
4. Índices de inbox (dois parciais separados, mais eficientes que expressão OR):
   ```sql
   CREATE INDEX IF NOT EXISTS idx_recommendations_recommended_pending
       ON recommendations (recommended_id) WHERE status = 'PENDING';

   CREATE INDEX IF NOT EXISTS idx_recommendations_target_pending
       ON recommendations (target_id) WHERE status = 'PENDING';
   ```
5. Índices para `contacts` (`user_id`) e `contact_shares` (`recommendation_id`, `sharer_id`, `recipient_id`).

**Aplicar:**
```sh
psql -U <user> -d <db> -f migrations/002_recommendation_feature.sql
```

---

### 2.12 Testes Unitários

Projeto: `tests/LinkUp.UnitTests/`

**Novos arquivos:**

| Arquivo | Cenários |
|---|---|
| `Features/Recommendations/CreateRecommendationHandlerTests.cs` | Sem conexão → `REC_001`; bloqueio bilateral → `REC_002`; recommendations_enabled=false → `REC_003`; duplicata PENDING → `REC_004`; happy path → cria recomendação. |
| `Features/Recommendations/RespondRecommendationHandlerTests.cs` | Not found → `REC_005`; não-participante → `REC_006`; já respondida → `REC_007`; accept → status `ACCEPTED` + conexão criada; reject → status `REJECTED`. |
| `Fakes/TestFakes.cs` | `FakeCurrentUser`, `FakeUserRepository` (com flag `recommendationsEnabled`), `FakeConnectionRepository`, `FakeBlockRepository`, `FakeRecommendationRepository`. |

**Rodar testes:**
```sh
dotnet test tests/LinkUp.UnitTests/ --verbosity normal
```

**Resultado:** 11/11 Passed

---

### 2.13 Decisões Arquiteturais

| Decisão | Motivo |
|---|---|
| Canonical ordering no par (recommended_id < target_id) | Espelha padrão de Connection — garante unicidade sem ordenação na query, INDEX parcial eficiente. |
| `ExistsPendingAsync` com canonical ordering no repositório | Duplicata é detectada independente da direção do par enviado pelo caller. |
| Transação lógica `RespondRecommendation` → Accept cria Connection na mesma handler | Sem estado inconsistente: Recommendation ACCEPTED e Connection criada atomicamente (sem transação distribuída — monolito modular). |
| `IsParticipant(userId)` na entidade, não no handler | Lógica de negócio encapsulada no domínio — handler apenas delega. |
| Dois índices parciais separados em vez de OR index | PostgreSQL usa um índice por condição de filtro; dois índices parciais permitem index-only scan para cada side do OR, eliminando seq scan no inbox. |
| `recommendations_enabled` em `users` em vez de nova tabela | Preferência simples sobre escalabilidade prematura — flag booleana suficiente para MVP; migração futura para tabela de settings não quebra contrato. |
| `ContactShare` como entidade separada de `Contact` | Consentimento de compartilhamento é evento imutável — registra **quem** autorizou **quem** a ver seus contatos **em qual contexto** (RecommendationId). |

---

### 2.14 Status de Build e Testes

```
dotnet build → 0 erros, 0 warnings — 7 projetos
dotnet test  → 11 Passed, 0 Failed, 0 Skipped
```

---

## 3. Próximos Passos

### Semana 4–5 — Bloqueio, Feedback, Notificações

| Feature | Descrição |
|---|---|
| `BlockUser` / `UnblockUser` | Commands + repositório + endpoints |
| `SubmitFeedback` | Avaliação pós-conexão via recomendação; atualiza `reputation_score` em `users`; usa `RecommendationFeedback` já modelado |
| `RegisterPushToken` | FCM Android — salva token por usuário |
| Background job: expirar recomendações | Query `WHERE status = 'PENDING' AND expires_at < NOW()`; chama `Expire()`, persiste |
| Background job: recalcular `reputation_score` | Agrega ratings de `recommendation_feedbacks` por `reviewee_id` |

### Semana 6–7 — Qualidade e DevOps

| Agente | Responsabilidade |
|---|---|
| **GithubActionsExpert** | Pipeline CI/CD — build, test, security scan, Docker build/push |
| **TestingExpert** | Testes integração repositórios Dapper (Testcontainers PostgreSQL) |
| **SecurityExpert** | Rate limiting, security headers (HSTS, X-Frame, CSP), OWASP review |
| **PerformanceExpert** | Profiling inbox query em escala; avaliar cache Redis para `GetRecommendations` (TTL curto, invalidar on Accept/Reject) |

### Consultas Pendentes

- **PostgreSQLExpert** — revisar estratégia de índices para query inbox com volume alto (>100k recomendações por usuário popular).
- **RedisExpert** — cache de inbox PENDING: chave `rec:pending:{userId}`, TTL 60s, invalidar on `CreateRecommendation` / `RespondRecommendation` por `userId`.
