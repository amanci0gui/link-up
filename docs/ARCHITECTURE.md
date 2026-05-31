# LinkUp — Arquitetura Backend

> Fonte de verdade arquitetural. Stack: .NET 10 · ASP.NET Core · PostgreSQL · Redis
> Padrões: Clean Architecture · Vertical Slice · CQRS (MediatR) · Result Pattern

---

## 1. Visão Arquitetural

```
┌─────────────────────────────────────────────────┐
│                  Flutter Client                  │
│              (Android MVP - HTTPS)               │
└────────────────────┬────────────────────────────┘
                     │ REST/JSON
┌────────────────────▼────────────────────────────┐
│              ASP.NET Core Web API                │
│  ┌─────────────────────────────────────────┐    │
│  │          Presentation Layer             │    │
│  │  Controllers · Middleware · Filters     │    │
│  └──────────────────┬──────────────────────┘    │
│  ┌──────────────────▼──────────────────────┐    │
│  │          Application Layer              │    │
│  │  MediatR Handlers (Commands/Queries)    │    │
│  │  Validators (FluentValidation)          │    │
│  │  Mappers (Mapperly)                     │    │
│  └──────────────────┬──────────────────────┘    │
│  ┌──────────────────▼──────────────────────┐    │
│  │            Domain Layer                 │    │
│  │  Entities · Value Objects · Enums       │    │
│  │  Domain Rules · Interfaces              │    │
│  └──────────────────┬──────────────────────┘    │
│  ┌──────────────────▼──────────────────────┐    │
│  │        Infrastructure Layer             │    │
│  │  EF Core · Repositories · Redis         │    │
│  │  FCM Service · Email Service            │    │
│  │  Background Jobs (Hangfire/Quartz)      │    │
│  └─────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
   PostgreSQL                   Redis
   (dados principais)    (cache · refresh tokens
                          · distributed locks)
```

---

## 2. Estrutura do Solution

```
LinkUp.sln
│
├── src/
│   ├── LinkUp.Api/                          # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── UsersController.cs
│   │   │   ├── ConnectionsController.cs
│   │   │   ├── RecommendationsController.cs
│   │   │   ├── EstablishedConnectionsController.cs
│   │   │   ├── ReputationController.cs
│   │   │   └── PrivacyController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Program.cs
│   │
│   ├── LinkUp.Application/                  # Application Layer
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── INotificationService.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   └── IDateTimeService.cs
│   │   │   ├── Models/
│   │   │   │   └── Result.cs                # Result<T> pattern
│   │   │   └── Behaviors/
│   │   │       ├── ValidationBehavior.cs
│   │   │       └── LoggingBehavior.cs
│   │   │
│   │   ├── Features/                        # Vertical Slices
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Register/
│   │   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   │   └── RegisterCommandValidator.cs
│   │   │   │   │   ├── Login/
│   │   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   │   │   └── LoginCommandValidator.cs
│   │   │   │   │   ├── RefreshToken/
│   │   │   │   │   └── Logout/
│   │   │   │   └── DTOs/
│   │   │   │       ├── AuthTokenDto.cs
│   │   │   │       └── LoginResponseDto.cs
│   │   │   │
│   │   │   ├── Users/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── UpdateProfile/
│   │   │   │   │   └── UpdateContacts/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetMyProfile/
│   │   │   │   │   ├── GetUserProfile/
│   │   │   │   │   └── SearchUsers/
│   │   │   │   └── DTOs/
│   │   │   │       ├── UserProfileDto.cs
│   │   │   │       └── UserContactDto.cs
│   │   │   │
│   │   │   ├── Connections/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── SendConnectionRequest/
│   │   │   │   │   ├── AcceptConnectionRequest/
│   │   │   │   │   └── RejectConnectionRequest/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetMyConnections/
│   │   │   │   │   └── GetPendingRequests/
│   │   │   │   └── DTOs/
│   │   │   │       └── ConnectionDto.cs
│   │   │   │
│   │   │   ├── Recommendations/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateRecommendation/
│   │   │   │   │   │   ├── CreateRecommendationCommand.cs
│   │   │   │   │   │   ├── CreateRecommendationCommandHandler.cs
│   │   │   │   │   │   └── CreateRecommendationCommandValidator.cs
│   │   │   │   │   └── RespondToRecommendation/
│   │   │   │   │       ├── RespondToRecommendationCommand.cs
│   │   │   │   │       ├── RespondToRecommendationCommandHandler.cs
│   │   │   │   │       └── RespondToRecommendationCommandValidator.cs
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetMyRecommendations/
│   │   │   │   │   ├── GetRecommendationDetail/
│   │   │   │   │   └── GetRecommendationsMade/
│   │   │   │   └── DTOs/
│   │   │   │       ├── RecommendationDto.cs
│   │   │   │       └── RecommendationDetailDto.cs
│   │   │   │
│   │   │   ├── EstablishedConnections/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetMyEstablishedConnections/
│   │   │   │   │   └── GetConnectionContacts/
│   │   │   │   └── DTOs/
│   │   │   │       └── EstablishedConnectionDto.cs
│   │   │   │
│   │   │   ├── Reputation/
│   │   │   │   ├── Commands/
│   │   │   │   │   └── SubmitFeedback/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetUserReputation/
│   │   │   │   │   └── GetReputationRanking/
│   │   │   │   └── DTOs/
│   │   │   │       └── ReputationDto.cs
│   │   │   │
│   │   │   └── Privacy/
│   │   │       ├── Commands/
│   │   │       │   ├── AddBlock/
│   │   │       │   └── RemoveBlock/
│   │   │       ├── Queries/
│   │   │       │   └── GetMyBlocks/
│   │   │       └── DTOs/
│   │   │           └── BlockDto.cs
│   │   │
│   │   └── LinkUp.Application.csproj
│   │
│   ├── LinkUp.Domain/                       # Domain Layer (sem dependências externas)
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── UserProfile.cs
│   │   │   ├── UserContact.cs
│   │   │   ├── Connection.cs
│   │   │   ├── ConnectionRequest.cs
│   │   │   ├── Recommendation.cs
│   │   │   ├── RecommendationResponse.cs
│   │   │   ├── EstablishedConnection.cs
│   │   │   ├── ContactExchange.cs
│   │   │   ├── RecommendationFeedback.cs
│   │   │   ├── ReputationScore.cs
│   │   │   ├── Block.cs
│   │   │   └── NotificationPreference.cs
│   │   ├── Enums/
│   │   │   ├── RecommendationType.cs
│   │   │   ├── RecommendationStatus.cs
│   │   │   ├── RecommendationResponseStatus.cs
│   │   │   ├── ConnectionStatus.cs
│   │   │   └── BlockType.cs
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── IConnectionRepository.cs
│   │   │   │   ├── IRecommendationRepository.cs
│   │   │   │   └── IBlockRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── LinkUp.Domain.csproj
│   │
│   └── LinkUp.Infrastructure/               # Infrastructure Layer
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/              # EF Core Fluent API configs
│       │   │   ├── UserConfiguration.cs
│       │   │   ├── RecommendationConfiguration.cs
│       │   │   └── ...
│       │   ├── Repositories/
│       │   │   ├── UserRepository.cs
│       │   │   ├── ConnectionRepository.cs
│       │   │   └── RecommendationRepository.cs
│       │   ├── Migrations/
│       │   └── UnitOfWork.cs
│       ├── Cache/
│       │   └── RedisCacheService.cs
│       ├── Services/
│       │   ├── JwtTokenService.cs
│       │   ├── RefreshTokenService.cs
│       │   ├── FcmNotificationService.cs
│       │   └── CurrentUserService.cs
│       ├── Jobs/
│       │   ├── ExpireRecommendationsJob.cs
│       │   └── ScheduleFeedbackNotificationJob.cs
│       └── LinkUp.Infrastructure.csproj
│
└── tests/
    ├── LinkUp.UnitTests/
    │   ├── Features/
    │   │   ├── Auth/
    │   │   ├── Connections/
    │   │   └── Recommendations/
    │   └── LinkUp.UnitTests.csproj
    └── LinkUp.IntegrationTests/
        ├── Api/
        └── LinkUp.IntegrationTests.csproj
```

---

## 3. Contratos de API

### Base URL
```
https://api.linkup.app/v1
```

### Autenticação
Header em todas as rotas protegidas:
```
Authorization: Bearer {access_token}
```

---

### Auth

#### POST /auth/register
```json
// Request
{
  "name": "string (2-100 chars)",
  "email": "string (valid email)",
  "password": "string (min 8, 1 maiúscula, 1 número)"
}

// Response 201
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 900
}

// Errors
400 - Validation error
409 - Email já cadastrado
```

#### POST /auth/login
```json
// Request
{
  "email": "string",
  "password": "string"
}

// Response 200
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 900
}

// Errors
401 - Credenciais inválidas
```

#### POST /auth/refresh
```json
// Request
{
  "refreshToken": "string"
}

// Response 200
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 900
}

// Errors
401 - Token inválido ou expirado
```

#### POST /auth/logout
```json
// Request (authenticated)
{}

// Response 204 No Content
// Revoga refresh token no Redis
```

---

### Users

#### GET /users/me
```json
// Response 200
{
  "id": "uuid",
  "name": "string",
  "email": "string",
  "profile": {
    "bio": "string|null",
    "photoUrl": "string|null",
    "interests": ["string"],
    "city": "string|null",
    "state": "string|null",
    "isAcceptingRecommendations": true
  },
  "reputationScore": {
    "avgRating": 4.5,
    "successRate": 0.85,
    "totalRecommendations": 12
  },
  "createdAt": "ISO8601"
}
```

#### PATCH /users/me/profile
```json
// Request
{
  "bio": "string|null (max 500 chars)",
  "photoUrl": "string|null",
  "interests": ["string (max 20 items)"],
  "city": "string|null",
  "state": "string|null (2 chars, UF)"
}

// Response 200 - UserProfileDto
```

#### PUT /users/me/contacts
```json
// Request
{
  "contacts": [
    {
      "type": "PHONE | EMAIL | INSTAGRAM | LINKEDIN | WHATSAPP | OTHER",
      "value": "string",
      "label": "string|null"
    }
  ]
}

// Response 200
```

#### GET /users/search?q={query}&page={n}&pageSize={n}
```json
// Response 200
{
  "items": [
    {
      "id": "uuid",
      "name": "string",
      "photoUrl": "string|null",
      "city": "string|null",
      "state": "string|null",
      "connectionStatus": "NONE | PENDING | CONNECTED"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

#### GET /users/{userId}/profile
```json
// Response 200 - perfil público completo
{
  "id": "uuid",
  "name": "string",
  "photoUrl": "string|null",
  "bio": "string|null",
  "interests": ["string"],
  "city": "string|null",
  "state": "string|null",
  "reputationScore": { ... },
  "connectionStatus": "NONE | PENDING | CONNECTED"
}

// 404 se usuário não encontrado
```

---

### Connections

#### POST /connections/request
```json
// Request
{
  "targetUserId": "uuid"
}

// Response 201
{
  "requestId": "uuid",
  "targetUser": { "id": "uuid", "name": "string", "photoUrl": "string|null" },
  "status": "PENDING",
  "createdAt": "ISO8601"
}

// Errors
400 - Já existe solicitação ou conexão
404 - Usuário não encontrado
```

#### POST /connections/request/{requestId}/accept
```json
// Response 200
{
  "connectionId": "uuid",
  "status": "ACCEPTED"
}
```

#### POST /connections/request/{requestId}/reject
```json
// Response 200
{
  "requestId": "uuid",
  "status": "REJECTED"
}
```

#### GET /connections?page={n}&pageSize={n}
```json
// Response 200
{
  "items": [
    {
      "connectionId": "uuid",
      "user": { "id": "uuid", "name": "string", "photoUrl": "string|null" },
      "connectedAt": "ISO8601"
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

#### GET /connections/requests/pending
```json
// Response 200 - lista de solicitações recebidas pendentes
{
  "items": [
    {
      "requestId": "uuid",
      "fromUser": { "id": "uuid", "name": "string", "photoUrl": "string|null" },
      "createdAt": "ISO8601"
    }
  ]
}
```

---

### Recommendations

#### POST /recommendations
```json
// Request
{
  "targetAUserId": "uuid",
  "targetBUserId": "uuid",
  "type": "FRIENDSHIP | ROMANCE | PROFESSIONAL | MENTORSHIP | PARTNERSHIP",
  "message": "string|null (max 300 chars)"
}

// Response 201
{
  "recommendationId": "uuid",
  "status": "PENDING",
  "type": "FRIENDSHIP",
  "targetA": { "id": "uuid", "name": "string" },
  "targetB": { "id": "uuid", "name": "string" },
  "createdAt": "ISO8601"
}

// Errors
400 - targetA === targetB
403 - Recomendador não conectado a um dos targets
403 - Target bloqueou recomendador
403 - Target desativou recomendações
409 - Recomendação pendente já existe entre estes dois para este recomendador
```

#### POST /recommendations/{id}/respond
```json
// Request (authenticated como target_a ou target_b)
{
  "response": "ACCEPTED | REJECTED"
}

// Response 200
{
  "recommendationId": "uuid",
  "recommendationStatus": "PARTIALLY_ACCEPTED | ACCEPTED | REJECTED",
  "myResponse": "ACCEPTED",
  "establishedConnectionId": "uuid|null"  // preenchido se ACCEPTED mútuo
}

// Errors
403 - Usuário não é destinatário desta recomendação
409 - Já respondeu
```

#### GET /recommendations/received?status={status}&page={n}
```json
// Response 200 - recomendações recebidas pelo usuário autenticado
{
  "items": [
    {
      "recommendationId": "uuid",
      "recommender": { "id": "uuid", "name": "string", "photoUrl": "string|null" },
      "otherPerson": {
        "id": "uuid",
        "name": "string",
        "photoUrl": "string|null",
        "bio": "string|null",
        "interests": ["string"],
        "city": "string|null"
      },
      "type": "FRIENDSHIP",
      "message": "string|null",
      "status": "PENDING",
      "myResponse": "PENDING | ACCEPTED | REJECTED",
      "createdAt": "ISO8601",
      "expiresAt": "ISO8601"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20
}
```

#### GET /recommendations/made?page={n}
```json
// Response 200 - recomendações feitas pelo usuário autenticado
{
  "items": [
    {
      "recommendationId": "uuid",
      "targetA": { "id": "uuid", "name": "string" },
      "targetB": { "id": "uuid", "name": "string" },
      "type": "PROFESSIONAL",
      "status": "PARTIALLY_ACCEPTED",
      "createdAt": "ISO8601"
    }
  ]
}
```

---

### Established Connections

#### GET /connections/established?page={n}
```json
// Response 200
{
  "items": [
    {
      "connectionId": "uuid",
      "otherUser": { "id": "uuid", "name": "string", "photoUrl": "string|null" },
      "recommendationType": "FRIENDSHIP",
      "recommenderName": "string",
      "establishedAt": "ISO8601",
      "feedbackGiven": false
    }
  ]
}
```

#### GET /connections/established/{connectionId}/contacts
```json
// Response 200 - contatos do outro usuário
{
  "otherUser": { "id": "uuid", "name": "string" },
  "contacts": [
    {
      "type": "INSTAGRAM",
      "value": "@usuario",
      "label": "Instagram pessoal"
    }
  ]
}

// Errors
403 - Usuário não faz parte desta conexão
```

---

### Reputation

#### POST /recommendations/{id}/feedback
```json
// Request
{
  "success": true,
  "rating": 5,
  "comment": "string|null (max 200 chars)"
}

// Response 201
{
  "feedbackId": "uuid",
  "recommenderId": "uuid",
  "recommenderName": "string"
}

// Errors
403 - Usuário não é destinatário desta recomendação
409 - Feedback já enviado
400 - Conexão não estabelecida (não pode dar feedback)
```

#### GET /users/{userId}/reputation
```json
// Response 200
{
  "userId": "uuid",
  "avgRating": 4.3,
  "successRate": 0.78,
  "totalRecommendations": 23,
  "totalFeedbacks": 18,
  "recentFeedbacks": [
    {
      "rating": 5,
      "comment": "string|null",
      "type": "FRIENDSHIP",
      "createdAt": "ISO8601"
    }
  ]
}
```

---

### Privacy

#### GET /privacy/blocks
```json
// Response 200
{
  "disableAllRecommendations": false,
  "blockedUsers": [
    {
      "blockId": "uuid",
      "blockedUser": { "id": "uuid", "name": "string" },
      "createdAt": "ISO8601"
    }
  ]
}
```

#### POST /privacy/blocks
```json
// Request — bloquear usuário específico
{
  "type": "BLOCK_BY_USER",
  "targetUserId": "uuid"
}

// Request — desativar todas as indicações
{
  "type": "DISABLE_ALL"
}

// Response 201
{
  "blockId": "uuid",
  "type": "BLOCK_BY_USER | DISABLE_ALL",
  "createdAt": "ISO8601"
}
```

#### DELETE /privacy/blocks/{blockId}
```json
// Response 204
```

---

## 4. Padrões e Convenções

### Result Pattern
```csharp
// Todos os handlers retornam Result<T>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
}

public record Error(string Code, string Message, int StatusCode);

// Erros de negócio predefinidos
public static class Errors
{
    public static class Recommendation
    {
        public static Error NotConnectedToTarget => new("REC_001", "Você não está conectado a um dos usuários indicados.", 403);
        public static Error TargetBlockedRecommender => new("REC_002", "Um dos usuários bloqueou suas indicações.", 403);
        public static Error DuplicatePending => new("REC_003", "Já existe uma recomendação pendente entre estes usuários.", 409);
    }
}
```

### Paginação
Todos os endpoints de listagem:
```json
// Query params
?page=1&pageSize=20

// Response wrapper
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

### Error Response
```json
{
  "code": "REC_001",
  "message": "Você não está conectado a um dos usuários indicados.",
  "traceId": "uuid"
}
```

### Soft Delete
Entidades com soft delete possuem:
```csharp
public DateTime? DeletedAt { get; private set; }
public bool IsDeleted => DeletedAt.HasValue;
```
EF Core Global Query Filter exclui registros com `DeletedAt != null`.

---

## 5. Autenticação JWT

| Config | Valor |
|--------|-------|
| Access Token TTL | 15 minutos |
| Refresh Token TTL | 30 dias |
| Algorithm | HS256 (MVP) → RS256 (produção) |
| Refresh Token Storage | Redis (key: `rt:{userId}:{tokenId}`) |
| Refresh Token Rotation | Sim — novo par a cada refresh |
| Revogação | DELETE da key no Redis |

Claims no JWT:
```json
{
  "sub": "uuid (userId)",
  "name": "string",
  "iat": 1234567890,
  "exp": 1234568790
}
```

---

## 6. Cache Redis

| Key Pattern | Dados | TTL |
|------------|-------|-----|
| `user:profile:{userId}` | UserProfileDto completo | 5 min |
| `user:reputation:{userId}` | ReputationDto | 10 min |
| `rt:{userId}:{tokenId}` | Refresh token hash | 30 dias |
| `lock:recommendation:{targetA}:{targetB}` | Distributed lock | 10 seg |
| `connections:{userId}` | Lista de connectionIds | 2 min |

**Invalidação:**
- `user:profile:{userId}` → invalidar no PATCH /users/me/profile
- `user:reputation:{userId}` → invalidar ao receber novo feedback
- `connections:{userId}` → invalidar ao aceitar/criar connection

---

## 7. Background Jobs

### ExpireRecommendationsJob
- **Frequência:** diária (02:00 BRT)
- **Ação:** UPDATE recommendations SET status = 'EXPIRED' WHERE status IN ('PENDING','PARTIALLY_ACCEPTED') AND created_at < NOW() - INTERVAL '30 days'
- **Notifica:** recommender sobre expiração

### ScheduleFeedbackNotificationJob
- **Trigger:** EstablishedConnection criada
- **Delay:** 7 dias
- **Ação:** envia push notification para target_a e target_b solicitando feedback

---

## 8. Notificações Push (FCM)

| Evento | Destinatário | Mensagem |
|--------|-------------|---------|
| ConnectionRequest recebido | target | "{nome} quer se conectar com você" |
| ConnectionRequest aceito | requester | "{nome} aceitou sua conexão" |
| Recommendation recebida | target_a, target_b | "{recommender} acha que vocês devem se conhecer" |
| Recommendation rejeitada | recommender + outro target | "A conexão não foi possível desta vez" |
| Conexão estabelecida | target_a, target_b | "Conexão feita! Troque seus contatos com {nome}" |
| Feedback solicitado | target_a, target_b | "Como foi sua conexão com {nome}?" |

---

## 9. Dependências NuGet Principais

```xml
<!-- ASP.NET Core -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />

<!-- MediatR -->
<PackageReference Include="MediatR" Version="12.*" />

<!-- Validação -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />

<!-- EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />

<!-- Redis -->
<PackageReference Include="StackExchange.Redis" Version="2.*" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.*" />

<!-- Mapeamento -->
<PackageReference Include="Mapperly" Version="3.*" />

<!-- Swagger -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />

<!-- Background Jobs -->
<PackageReference Include="Quartz.AspNetCore" Version="3.*" />

<!-- Logging -->
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.*" />

<!-- Testes -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
```

---

## 10. Variáveis de Ambiente

```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=linkup;Username=linkup;Password=secret

# Redis
ConnectionStrings__Redis=localhost:6379

# JWT
Jwt__Secret=<min-32-char-secret>
Jwt__Issuer=linkup-api
Jwt__Audience=linkup-app
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=30

# FCM
Firebase__ProjectId=linkup-firebase
Firebase__ServiceAccountKeyPath=/secrets/firebase-key.json

# App
App__FeedbackDelayDays=7
App__RecommendationExpirationDays=30
App__Environment=Development
```

---

## 11. Segurança

| Medida | Implementação |
|--------|--------------|
| Rate Limiting | `AspNetCoreRateLimit` — 100 req/min por IP, 20 req/min em /auth/* |
| Input Validation | FluentValidation em todos os Commands |
| SQL Injection | EF Core parameterized queries (sem raw SQL desnecessário) |
| LGPD | Soft delete + anonimização de PII na exclusão de conta |
| Passwords | BCrypt (cost factor 12) |
| CORS | Whitelist explícita (sem wildcard em produção) |
| HTTPS | Obrigatório; HSTS em produção |
| Sensitive logs | Nunca logar senha, tokens, contatos pessoais |
