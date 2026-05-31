# Sessão 2 — Conexões + Perfil

**Data:** 2026-05-30
**Status:** Concluída

## Visão Geral

Objetivo da sessão: implementar o domínio de conexões e perfil de usuário sobre a fundação estabelecida na Sessão 1. Ao final, `dotnet build` passa com 0 erros e 0 warnings em todos os 7 projetos.

---

## 2. Entregues

### 2.1 Domain — Entidades Novas

| Arquivo | Descrição |
|---|---|
| `Entities/Connection.cs` | Par canônico (menor GUID primeiro via `string.Compare`), factory `Create()`, `Reconstitute()` para Dapper |
| `Entities/ConnectionRequest.cs` | `Create()`, `Accept()`, `Reject()` — atualiza `Status` e `UpdatedAt`; `Reconstitute()` |
| `Entities/Block.cs` | `Create()` com `BlockType.BlockByUser` default, `Reconstitute()` |

### 2.2 Domain — Entidade Modificada

| Arquivo | Mudança |
|---|---|
| `Entities/User.cs` | Adicionados `Bio`, `PhotoUrl` (`private set`); método `UpdateProfile(name, bio, photoUrl)`; `Reconstitute()` e `Anonymize()` atualizados |

### 2.3 Domain — Interfaces de Repositório

| Arquivo | Contratos |
|---|---|
| `Interfaces/Repositories/IConnectionRepository.cs` | `GetByUsersAsync`, `GetByUserAsync`, `AddAsync`, `ExistsAsync` |
| `Interfaces/Repositories/IConnectionRequestRepository.cs` | `GetByIdAsync`, `GetPendingAsync`, `AddAsync`, `UpdateAsync` |
| `Interfaces/Repositories/IBlockRepository.cs` | `ExistsAsync`, `AddAsync` |

---

### 2.4 Application — Erros Novos

Adicionados em `Common/Models/Result.cs` → `Errors.Connection`:

| Código | Mensagem | HTTP |
|---|---|---|
| `CONN_004` | Existe um bloqueio entre os usuários. | 403 |
| `CONN_005` | Esta solicitação não está mais pendente. | 409 |

---

### 2.5 Application — Features de Conexão

Pasta: `Features/Connections/`

| Arquivo | Descrição |
|---|---|
| `Commands/SendConnectionRequest/SendConnectionRequestCommand.cs` | Valida: target ativo, não-self, sem conexão existente, sem PENDING duplicado, sem bloqueio bilateral. Persiste `ConnectionRequest`. |
| `Commands/RespondConnectionRequest/RespondConnectionRequestCommand.cs` | Valida: request existe, `TargetId == currentUser`, status `Pending`. Accept → `Connection.Create()` + persiste ambos. Reject → persiste request atualizado. |
| `Queries/GetConnections/GetConnectionsQuery.cs` | Retorna lista `ConnectionDto(UserId, Name, PhotoUrl, ConnectedAt)` do usuário autenticado. |

---

### 2.6 Application — Features de Perfil

Pasta: `Features/Profile/`

| Arquivo | Descrição |
|---|---|
| `Queries/GetUserProfile/GetUserProfileQuery.cs` | Carrega user por ID, verifica `IsActive && !IsDeleted`, retorna `UserProfileResponse(Id, Name, Bio, PhotoUrl, CreatedAt)`. |
| `Commands/UpdateProfile/UpdateProfileCommand.cs` | Validator: `Name` min 2/max 100, `Bio` max 500, `PhotoUrl` max 500. Handler chama `user.UpdateProfile()` e persiste. |

---

### 2.7 Infrastructure — Repositórios Novos

| Arquivo | Destaques |
|---|---|
| `Persistence/Repositories/ConnectionRepository.cs` | Dapper + `CommandDefinition` com CT. `GetByUserAsync`: `WHERE user_id_1 = @UserId OR user_id_2 = @UserId`. `ExistsAsync` ordena canonicamente antes da query. |
| `Persistence/Repositories/ConnectionRequestRepository.cs` | `GetPendingAsync` filtra `status = 'PENDING'`. Enum serializado via `.ToString().ToUpperInvariant()` + cast `::connection_request_status`. |
| `Persistence/Repositories/BlockRepository.cs` | `ExistsAsync` e `AddAsync`, cast `::block_type` no INSERT. |

### 2.8 Infrastructure — Modificações

| Arquivo | Mudança |
|---|---|
| `Persistence/Repositories/UserRepository.cs` | SELECT/INSERT/UPDATE incluem `bio`, `photo_url`; `UserRow` e `ToDomain()` atualizados. |
| `Extensions/InfrastructureServiceExtensions.cs` | Registro Scoped: `IConnectionRepository`, `IConnectionRequestRepository`, `IBlockRepository`. |

---

### 2.9 API — Controllers Novos

**`Controllers/ConnectionsController.cs`**

```
POST  /api/v1/connections/requests              [Authorize]  → SendConnectionRequest
POST  /api/v1/connections/requests/{id}/respond [Authorize]  → RespondConnectionRequest
GET   /api/v1/connections                       [Authorize]  → GetConnections
```

**`Controllers/ProfileController.cs`**

```
GET   /api/v1/users/{userId}/profile            [Authorize]  → GetUserProfile
PUT   /api/v1/users/me/profile                  [Authorize]  → UpdateProfile
```

Ambos seguem padrão `Problem(AppError)` do `AuthController`.

---

### 2.10 Decisões Arquiteturais

| Decisão | Motivo |
|---|---|
| Canonical order via `string.Compare` em GUIDs | Garante `user_id_1 < user_id_2` sem depender de comparação numérica; espelha constraint `CHECK` no PostgreSQL |
| Verificação de bloqueio bilateral em `SendConnectionRequest` | Bloqueio em qualquer direção impede solicitação; query separada nos dois sentidos evita joins complexos |
| `RespondConnectionRequest` cria `Connection` na mesma transação lógica | Accept é atômico: status atualizado + conexão criada no mesmo handler; sem estado inconsistente |
| `GetConnections` resolve `otherUserId` no handler | Lógica de negócio (quem é "o outro") fica na Application, não vaza para Infrastructure |

---

### 2.11 Correção Pós-Implementação — `photo_url` → `profile_picture_url`

**Problema detectado:** código gerado usou nome de coluna `photo_url` em SQL e prop `PhotoUrl` em C#, mas migration `001_initial_schema.sql` já definia a coluna como `profile_picture_url` (linha 68). Divergência causaria falha silenciosa em runtime — Dapper retornaria `null` para o campo sem lançar exceção.

**Arquivos corrigidos:**

| Arquivo | Mudança |
|---|---|
| `LinkUp.Domain/Entities/User.cs` | `PhotoUrl` → `ProfilePictureUrl`; `UpdateProfile()` e `Anonymize()` atualizados |
| `LinkUp.Infrastructure/Persistence/Repositories/UserRepository.cs` | SQL `photo_url` → `profile_picture_url` em SELECT/INSERT/UPDATE; `UserRow.PhotoUrl` → `ProfilePictureUrl`; `ToDomain()` atualizado |
| `LinkUp.Application/Features/Profile/Queries/GetUserProfile/GetUserProfileQuery.cs` | `UserProfileResponse.PhotoUrl` → `ProfilePictureUrl` |
| `LinkUp.Application/Features/Profile/Commands/UpdateProfile/UpdateProfileCommand.cs` | `UpdateProfileCommand.PhotoUrl` → `ProfilePictureUrl`; `UpdateProfileResponse` e validator atualizados |
| `LinkUp.Application/Features/Connections/Queries/GetConnections/GetConnectionsQuery.cs` | `ConnectionDto.PhotoUrl` → `ProfilePictureUrl` |

**Lição:** nome de propriedade C# deve sempre espelhar exatamente o nome da coluna PostgreSQL (via convenção snake_case ↔ PascalCase do Dapper) para evitar mapeamento silencioso incorreto.

---

### 2.12 Status de Build

```
dotnet build → 0 erros, 0 warnings — 7 projetos
```

---

## 3. Próximos Passos

### Semana 3 — Recomendações (core do produto)

**Domain entities a criar:**
- `Recommendation`, `RecommendationFeedback`, `Contact`, `ContactShare`

**Features de Recomendação:**
- `CreateRecommendation` — valida: recommender conectado a ambos, sem bloqueio, sem duplicata PENDING, recomendações habilitadas no alvo
- `RespondRecommendation` — accept/reject pelo alvo; accept gera conexão entre os dois recomendados
- `GetRecommendations` — inbox de recomendações pendentes recebidas

**Features de Contato:**
- `AddContact` — cadastrar WhatsApp, Instagram, LinkedIn, etc
- `ShareContact` — compartilhar contatos após recomendação aceita

**Repositories a implementar:**
- `IRecommendationRepository`, `IContactRepository`, `IContactShareRepository`

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

| Agente | Responsabilidade |
|---|---|
| **GithubActionsExpert** | Pipeline CI/CD — build, test, security scan, Docker build/push |
| **TestingExpert** | Testes unitários handlers + integração repositórios Dapper |
| **SecurityExpert** | Rate limiting, security headers (HSTS, X-Frame, CSP), OWASP review |
