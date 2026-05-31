# LinkUp Backend

Plataforma social de recomendações humanas. API REST em .NET 10 com arquitetura Clean + Vertical Slice.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)

---

## Rodando localmente

### 1. Subir infraestrutura

```bash
docker compose up -d
```

Sobe PostgreSQL 16 e Redis 7. Migrations executadas automaticamente via `docker-entrypoint-initdb.d`.

### 2. Build

```bash
dotnet build
```

### 3. Rodar API

```bash
dotnet run --project src/LinkUp.Api
```

### URLs

| Recurso     | URL                                  |
|-------------|--------------------------------------|
| API         | `http://localhost:5000`              |
| Scalar Docs | `http://localhost:5000/scalar/v1`    |

---

## Migrations

O arquivo `migrations/001_initial_schema.sql` é montado no container PostgreSQL via `docker-entrypoint-initdb.d` e executado na primeira inicialização. Não requer comando manual.

Para recriar o banco do zero:

```bash
docker compose down -v
docker compose up -d
```

---

## Configuração

Configurações de desenvolvimento ficam em `src/LinkUp.Api/appsettings.Development.json`.

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=linkup;Username=linkup;Password=linkup",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "dev-secret-change-in-production",
    "Issuer": "linkup-api",
    "Audience": "linkup-client",
    "ExpiresInMinutes": 60,
    "RefreshExpiresInDays": 7
  }
}
```

> **Producao:** `Jwt:Secret` deve ser substituido por valor longo e aleatorio. Nunca commitar segredos reais.

---

## Endpoints — Auth

| Metodo | Rota                       | Descricao                        |
|--------|----------------------------|----------------------------------|
| POST   | `/api/v1/auth/register`    | Cadastro de novo usuario         |
| POST   | `/api/v1/auth/login`       | Login, retorna JWT + refresh     |
| POST   | `/api/v1/auth/refresh`     | Renova access token via refresh  |
| POST   | `/api/v1/auth/logout`      | Revoga refresh token no Redis    |

---

## Arquitetura

Projeto usa modelo hibrido entre **Clean Architecture** e **Vertical Slice Architecture**.

- `LinkUp.Domain` — entidades, enums, interfaces de repositorio. Sem dependencias externas.
- `LinkUp.Application` — features organizadas por slice (ex: `Users/`, `Auth/`). Cada slice contem MediatR handlers, DTOs, validators (FluentValidation) e contratos de repositorio. Pipeline behaviors centralizam validacao e logging.
- `LinkUp.Infrastructure` — implementacoes concretas: repositorios Dapper (SQL puro, sem ORM), Redis para cache e refresh tokens, servicos JWT e BCrypt.
- `LinkUp.Api` — controllers enxutos, apenas delegam para MediatR. Middleware global de excecoes, configuracao de DI via extension methods.

Dependencias fluem de fora para dentro: `Api` → `Application` → `Domain`. `Infrastructure` implementa contratos de `Domain`.

---

## Convencoes de codigo

- **Result pattern** em todos os use cases — sem `throw` para fluxo de negocio.
- Erros tipados via `Error` record (codigo + mensagem). Controllers mapeiam `Result<T>` para HTTP status.
- `CancellationToken` obrigatorio em todos os metodos async.
- Queries SQL parametrizadas via Dapper — sem concatenacao de strings.
- Validators FluentValidation registrados via `AddValidatorsFromAssembly`. Pipeline behavior valida antes de chegar no handler.
- Logs estruturados com Serilog. Correlation ID em todas as requisicoes.

---

## Estrutura de pastas

```
LinkUp.sln
src/
  LinkUp.Api/
  LinkUp.Application/
  LinkUp.Domain/
  LinkUp.Infrastructure/
tests/
  LinkUp.UnitTests/
  LinkUp.IntegrationTests/
migrations/
  001_initial_schema.sql
docker-compose.yml
```

---

## Roadmap MVP

| Semana | Entregavel                                                         |
|--------|--------------------------------------------------------------------|
| 1      | Setup projeto, Docker Compose, schema inicial, Auth (JWT + Redis)  |
| 2      | CRUD de usuarios, perfil, upload de avatar                         |
| 3      | Conexoes entre usuarios (seguir, aceitar, bloquear)                |
| 4      | Recomendacoes — criar, listar, reagir                              |
| 5      | Feed personalizado, busca de usuarios e recomendacoes              |
| 6      | Notificacoes internas, contadores de atividade                     |
| 7      | Hardening: rate limiting, testes de integracao, revisao de seguranca |
