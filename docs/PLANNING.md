# LinkUp — Backend MVP Planning

> Documento de planejamento técnico. Fonte de verdade para sessões de desenvolvimento.
> Stack: .NET 10 · ASP.NET Core · PostgreSQL · Redis
> Escopo: Brasil (PT-BR) · MVP Android (Flutter client)

---

## 1. Visão Geral do Sistema

LinkUp é plataforma social de recomendações humanas. Diferença central: conexões nascem de indicações de pessoas reais, não de algoritmos.

**Fluxo base:** A conhece B e C → A recomenda que B e C se conheçam → B e C recebem notificação → ambos veem perfil completo um do outro → se ambos aceitam → conexão estabelecida → troca de contatos.

### Premissas MVP

- Recomendador DEVE estar conectado às DUAS pessoas indicadas
- Perfil completo visível ao receber recomendação
- Pós-conexão: troca de contato (telefone, email, Instagram, etc.)
- Chat interno: fora do MVP (planejado para v2)
- Limite de recomendações: ilimitado no MVP (regras por plano = futuro)
- Reputação: feedback manual pós-conexão
- Bloqueios: por usuário específico ou desativar completamente
- Tipos de recomendação: FRIENDSHIP | ROMANCE | PROFESSIONAL | MENTORSHIP | PARTNERSHIP
- Idioma: PT-BR, moeda BRL quando aplicável
- Plataforma inicial: Android (Flutter)

---

## 2. Domínios e Entidades Principais

### User (Usuário)
Conta na plataforma. Possui perfil, configurações de privacidade, preferências de notificação.

### UserProfile
Dados públicos do usuário: nome, bio, foto, interesses, localização (cidade/estado), contatos compartilháveis.

### Connection (Conexão Base)
Vínculo de amizade/conexão entre dois usuários na plataforma. Pré-requisito para fazer recomendações.
- Status: PENDING | ACCEPTED | REJECTED | BLOCKED

### Recommendation (Recomendação)
Indicação feita por um usuário (recommender) para que dois outros usuários (target_a, target_b) se conheçam.
- Regra: recommender deve ter Connection ACCEPTED com target_a E com target_b
- Possui um tipo: FRIENDSHIP | ROMANCE | PROFESSIONAL | MENTORSHIP | PARTNERSHIP
- Status: PENDING | PARTIALLY_ACCEPTED | ACCEPTED | REJECTED | EXPIRED | CANCELLED

### RecommendationResponse
Resposta individual de cada destinatário (target_a ou target_b) à recomendação.
- Status: PENDING | ACCEPTED | REJECTED

### EstablishedConnection (Conexão Estabelecida)
Vínculo criado quando ambos os destinatários aceitam a recomendação. Contém referência à recomendação origem.

### ContactExchange
Contatos compartilhados entre os dois usuários após conexão estabelecida.

### RecommendationFeedback
Feedback manual dado pelos envolvidos após conexão. Alimenta reputação do recomendador.
- Campos: success (bool), rating (1–5), comment (opcional)

### ReputationScore
Score agregado do recomendador baseado nos feedbacks recebidos.

### Block
Bloqueio de indicação. Pode ser:
- BLOCK_BY_USER: usuário A bloqueia usuário B de o indicar
- DISABLE_ALL: usuário desativa completamente a possibilidade de ser indicado

### NotificationPreference
Configurações de notificação push por tipo de evento.

---

## 3. Fluxos Críticos

### 3.1 Registro e Onboarding

```
1. POST /auth/register → email + senha + nome
2. Envio de email de verificação (futuro v2, MVP pula)
3. POST /auth/login → JWT access token + refresh token
4. PATCH /users/me/profile → preencher bio, foto, interesses, localização
5. PUT /users/me/contacts → adicionar contatos compartilháveis (telefone, Instagram, etc.)
```

### 3.2 Conexão Base Entre Usuários

```
1. GET /users/search?q={nome} → buscar usuário
2. POST /connections/request → enviar solicitação para userId
   - Valida: não existe connection já, não está bloqueado
3. Destinatário recebe push notification
4. POST /connections/{requestId}/accept → aceitar
   - Cria Connection ACCEPTED bilateral
   OU
   POST /connections/{requestId}/reject → rejeitar
5. GET /connections → listar minhas conexões
```

### 3.3 Fluxo de Recomendação (Core)

```
1. GET /users/me/connections → listar conexões para selecionar
2. Recommender seleciona target_a e target_b + tipo de recomendação + mensagem opcional
3. POST /recommendations
   Validações server-side:
   - recommender tem Connection ACCEPTED com target_a
   - recommender tem Connection ACCEPTED com target_b
   - target_a não bloqueou recommender (BLOCK_BY_USER)
   - target_b não bloqueou recommender (BLOCK_BY_USER)
   - target_a não desativou ser indicado (DISABLE_ALL)
   - target_b não desativou ser indicado (DISABLE_ALL)
   - não existe recomendação PENDING/PARTIALLY_ACCEPTED entre target_a e target_b pelo mesmo recommender
   Cria:
   - Recommendation (status=PENDING)
   - RecommendationResponse para target_a (status=PENDING)
   - RecommendationResponse para target_b (status=PENDING)
4. Push notification para target_a e target_b
   - Mensagem: "{recommender_name} acha que vocês devem se conhecer"
5. target_a abre recomendação → vê perfil COMPLETO de target_b + tipo + mensagem do recommender
6. POST /recommendations/{id}/respond (target_a)
   body: { response: "ACCEPTED" | "REJECTED" }
   - Atualiza RecommendationResponse de target_a
   - Se REJECTED → Recommendation status = REJECTED, notifica target_b e recommender
   - Se ACCEPTED → verifica resposta de target_b
     - target_b ainda PENDING → Recommendation status = PARTIALLY_ACCEPTED
     - target_b ACCEPTED → Recommendation status = ACCEPTED → dispara fluxo de conexão
7. Mesmo fluxo para target_b
8. Ambos aceitaram → POST interno → cria EstablishedConnection
   - Notifica target_a e target_b: "Conexão estabelecida! Troque seus contatos."
```

### 3.4 Troca de Contato Pós-Conexão

```
1. GET /connections/established/{connectionId}/contacts
   - Valida: usuário é target_a ou target_b da conexão
   - Retorna contatos que o OUTRO usuário cadastrou como compartilháveis
   - Registra evento de visualização (ContactExchange)
2. Contatos visíveis: telefone, email, Instagram, LinkedIn, etc. (conforme o outro preencheu)
```

### 3.5 Feedback de Recomendação (Reputação)

```
1. Após EstablishedConnection criada, agendado: após 7 dias envia push
   "Como foi sua conexão com {nome}? Deixe um feedback"
2. POST /recommendations/{id}/feedback
   body: { success: bool, rating: 1..5, comment?: string }
   - Valida: usuário é target_a ou target_b
   - Valida: não deu feedback ainda para esta recommendation
   - Cria RecommendationFeedback
   - Trigger: recalcular ReputationScore do recommender
3. ReputationScore = média ponderada de todos os feedbacks recebidos
   - success_rate = feedbacks com success=true / total feedbacks
   - avg_rating = média dos ratings
   - total_recommendations = count de recomendações geradas
```

### 3.6 Bloqueio de Recomendadores

```
1. POST /privacy/blocks
   body: { type: "BLOCK_BY_USER", blocked_user_id: uuid }
   → impede que blocked_user_id indique o usuário atual

2. POST /privacy/blocks
   body: { type: "DISABLE_ALL" }
   → desativa completamente ser indicado (cobre todos os usuários)

3. DELETE /privacy/blocks/{blockId} → remover bloqueio
4. GET /privacy/blocks → listar bloqueios ativos

Efeito imediato: validações no fluxo de recomendação consultam tabela blocks antes de criar.
```

---

## 4. Módulos do Backend

| Módulo | Responsabilidade |
|--------|-----------------|
| **Auth** | Registro, login, JWT, refresh token, logout, revogação |
| **Users** | CRUD de perfil, onboarding, busca de usuários |
| **Contacts** | Gerenciar contatos compartilháveis do perfil |
| **Connections** | Solicitação de conexão base, aceite, rejeição, listagem |
| **Recommendations** | Criar, validar, responder, expirar recomendações |
| **EstablishedConnections** | Criar conexão estabelecida, listar, troca de contato |
| **Reputation** | Receber feedback, calcular score, ranking |
| **Privacy** | Bloqueios, configurações de privacidade |
| **Notifications** | Envio de push (FCM), preferências, histórico |
| **Jobs** | Background jobs: expirar recomendações, agendar feedback |

---

## 5. Roadmap MVP — Fases

### Fase 1 — Base (Semana 1–2)
**Objetivo:** Plataforma funcional com auth e perfis.

- [ ] Setup solution .NET 10 + Clean Architecture
- [ ] Configurar PostgreSQL + EF Core + migrations
- [ ] Configurar Redis
- [ ] Módulo Auth: registro, login, JWT, refresh token
- [ ] Módulo Users: CRUD de perfil, onboarding
- [ ] Módulo Contacts: contatos compartilháveis
- [ ] Testes unitários: Auth e Users

### Fase 2 — Conexões Base (Semana 3)
**Objetivo:** Usuários podem se conectar.

- [ ] Módulo Connections: request, accept, reject, list
- [ ] Notificação push básica (FCM)
- [ ] Módulo Privacy: bloqueios
- [ ] Testes: fluxo de conexão

### Fase 3 — Core: Recomendações (Semana 4–5)
**Objetivo:** Fluxo principal funcionando end-to-end.

- [ ] Módulo Recommendations: criar, validar, responder
- [ ] Todas validações de bloqueio e conexão no create
- [ ] EstablishedConnections: criar ao aceite mútuo
- [ ] Troca de contatos pós-conexão
- [ ] Notificações push para cada etapa do fluxo
- [ ] Testes: fluxo completo de recomendação

### Fase 4 — Reputação e Qualidade (Semana 6)
**Objetivo:** Sistema de reputação funcional.

- [ ] Módulo Reputation: feedback, score, ranking
- [ ] Background job: agendar push de feedback (7 dias)
- [ ] Background job: expirar recomendações (30 dias sem resposta)
- [ ] Rate limiting básico nas rotas
- [ ] Testes: reputação

### Fase 5 — Hardening (Semana 7)
**Objetivo:** MVP pronto para testes com usuários reais.

- [ ] Revisão de segurança (LGPD compliance básico)
- [ ] Paginação em todos os endpoints de listagem
- [ ] Health checks
- [ ] Logs estruturados
- [ ] Documentação Swagger completa
- [ ] Testes de integração end-to-end

---

## 6. Regras de Negócio — Referência Rápida

| Regra | Detalhe |
|-------|---------|
| Recomendador conectado a ambos | Connection ACCEPTED entre recommender↔target_a E recommender↔target_b |
| Perfil visível ao receber | Destinatário vê perfil completo do outro ao abrir recomendação |
| Aceite mútuo obrigatório | Conexão só é estabelecida com ACCEPTED dos dois destinatários |
| Rejeição unilateral encerra | Um REJECTED cancela toda a recomendação |
| Bloqueio por usuário | Usuário A impede B de o indicar |
| Desativar indicações | Usuário desativa completamente o recebimento de recomendações |
| Tipos de recomendação | FRIENDSHIP · ROMANCE · PROFESSIONAL · MENTORSHIP · PARTNERSHIP |
| Feedback = 7 dias depois | Push agendado 7 dias após conexão estabelecida |
| Expiração de recomendação | 30 dias sem resposta → status EXPIRED |
| Soft delete | Dados de usuário: soft delete com anonymization (LGPD) |

---

## 7. Riscos Técnicos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Race condition no aceite mútuo | Média | Alto | Transação DB + Redis distributed lock ao criar EstablishedConnection |
| Spam de recomendações | Baixa (MVP) | Médio | Rate limiting por IP e por usuário; limites por plano no futuro |
| Escalabilidade de notificações | Baixa (MVP) | Médio | Background job com fila; migrar para mensageria em v2 |
| LGPD: exclusão de dados | Média | Alto | Soft delete + anonimização; não deletar fisicamente |
| Token JWT comprometido | Baixa | Alto | Refresh token rotation + revogação em Redis |

---

## 8. Decisões Arquiteturais (ADRs)

### ADR-001: Clean Architecture + Vertical Slice para features
**Decisão:** Estrutura base Clean Architecture, com features organizadas em Vertical Slices.
**Racional:** MVP precisa de velocidade de desenvolvimento. Vertical Slice reduz cerimônia. Clean Architecture garante testabilidade e separação de infraestrutura.

### ADR-002: CQRS com MediatR
**Decisão:** Usar MediatR para Commands e Queries. Sem Event Sourcing no MVP.
**Racional:** Separa intenção de leitura vs escrita, facilita teste unitário de handlers, sem overhead de event sourcing desnecessário para MVP.

### ADR-003: Result Pattern em vez de exceptions para erros de negócio
**Decisão:** Handlers retornam `Result<T>` (ErrorOr ou similar). Exceptions apenas para erros inesperados.
**Racional:** Erros de negócio são previsíveis. Result pattern torna fluxo explícito, evita try/catch desnecessário.

### ADR-004: PostgreSQL como banco principal
**Decisão:** PostgreSQL para todos os dados relacionais.
**Racional:** Modelo de dados do LinkUp é altamente relacional (connections, recommendations). PostgreSQL suporta bem constraints, transações e índices necessários.

### ADR-005: Redis para cache e sessões
**Decisão:** Redis para cache de perfis, sessões de refresh token e distributed lock.
**Racional:** Perfis são lidos com frequência; refresh tokens precisam de revogação rápida; distributed lock para race conditions.

### ADR-006: Soft delete com anonimização
**Decisão:** Nunca deletar fisicamente dados de usuário. Soft delete + anonimizar dados PII após exclusão de conta.
**Racional:** LGPD exige direito ao esquecimento. Anonimização preserva integridade relacional sem expor dados pessoais.

### ADR-007: Push notifications via FCM
**Decisão:** Firebase Cloud Messaging para notificações push Android.
**Racional:** Flutter + FCM é stack padrão para Android. Simples no MVP; migrar para serviço dedicado se volume crescer.

---

## 9. Glossário

| Termo | Significado |
|-------|-------------|
| Recommender | Usuário que faz a recomendação |
| Target A / Target B | Dois usuários sendo recomendados |
| Connection (base) | Vínculo de amizade/conexão na plataforma (pré-requisito) |
| EstablishedConnection | Vínculo criado após aceite mútuo de recomendação |
| Block | Impedimento de ser indicado por alguém |
| Feedback | Avaliação manual pós-conexão |
| ReputationScore | Score do recommender baseado em feedbacks |
