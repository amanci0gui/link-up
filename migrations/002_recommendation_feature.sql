BEGIN;

-- ---------------------------------------------------------------------------
-- Correção: renomeia created_at → connected_at na tabela connections
-- O repositório ConnectionRepository.cs referencia a coluna como connected_at
-- e a entidade Connection possui a propriedade ConnectedAt. A migration 001
-- criou a coluna com nome created_at, gerando divergência silenciosa no Dapper.
-- ---------------------------------------------------------------------------
ALTER TABLE connections RENAME COLUMN created_at TO connected_at;

-- ---------------------------------------------------------------------------
-- Adiciona updated_at à tabela recommendations (ausente em 001)
-- Necessário para rastrear quando status muda (Accept/Reject/Expire).
-- ---------------------------------------------------------------------------
ALTER TABLE recommendations
    ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();

CREATE TRIGGER trg_recommendations_set_updated_at
    BEFORE UPDATE ON recommendations
    FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at();

-- ---------------------------------------------------------------------------
-- Índice único parcial: previne duplicata PENDING por recommender + par
--
-- A aplicação usa ordenação canônica (menor GUID → recommended_id), então
-- (recommender_id, recommended_id, target_id) é suficiente para unicidade.
-- WHERE status = 'PENDING' → índice cobre apenas linhas ativas, minimizando
-- tamanho e custo de manutenção.
-- ---------------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS ux_recommendation_pending
    ON recommendations (recommender_id, recommended_id, target_id)
    WHERE status = 'PENDING';

-- ---------------------------------------------------------------------------
-- Índices de inbox: recipient pode ser recommended_id OU target_id.
-- Dois índices parciais separados são mais eficientes que índice em expressão
-- para as queries WHERE (recommended_id = ? OR target_id = ?) AND status = 'PENDING'.
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_recommendations_recommended_pending
    ON recommendations (recommended_id)
    WHERE status = 'PENDING';

CREATE INDEX IF NOT EXISTS idx_recommendations_target_pending
    ON recommendations (target_id)
    WHERE status = 'PENDING';

-- ---------------------------------------------------------------------------
-- Indexes — contacts
-- Suporta GetByUserAsync: WHERE user_id = ?
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_contacts_user_id
    ON contacts (user_id);

-- ---------------------------------------------------------------------------
-- Indexes — contact_shares
-- Suporta ExistsAsync e queries de compartilhamento por recomendação.
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_contact_shares_recommendation_id
    ON contact_shares (recommendation_id);

CREATE INDEX IF NOT EXISTS idx_contact_shares_sharer_id
    ON contact_shares (sharer_id);

CREATE INDEX IF NOT EXISTS idx_contact_shares_recipient_id
    ON contact_shares (recipient_id);

-- ---------------------------------------------------------------------------
-- Seed: versão da migration
-- ---------------------------------------------------------------------------
INSERT INTO schema_migrations (version)
VALUES ('002')
ON CONFLICT (version) DO NOTHING;

COMMIT;
