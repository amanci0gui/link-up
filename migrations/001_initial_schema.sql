BEGIN;

-- ---------------------------------------------------------------------------
-- Extension
-- ---------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ---------------------------------------------------------------------------
-- Enums
-- ---------------------------------------------------------------------------
DO $$ BEGIN
    CREATE TYPE recommendation_type AS ENUM (
        'FRIENDSHIP', 'ROMANCE', 'PROFESSIONAL', 'MENTORSHIP', 'PARTNERSHIP'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE recommendation_status AS ENUM (
        'PENDING', 'ACCEPTED', 'REJECTED', 'EXPIRED'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE connection_request_status AS ENUM (
        'PENDING', 'ACCEPTED', 'REJECTED', 'CANCELLED'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE block_type AS ENUM (
        'RECOMMENDATION', 'ALL'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE contact_type AS ENUM (
        'WHATSAPP', 'INSTAGRAM', 'LINKEDIN', 'EMAIL', 'PHONE', 'OTHER'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- ---------------------------------------------------------------------------
-- Trigger function — updated_at
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION trigger_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- Table: users
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id                      UUID            NOT NULL DEFAULT gen_random_uuid(),
    email                   VARCHAR(255)    NOT NULL,
    password_hash           VARCHAR(255)    NOT NULL,
    name                    VARCHAR(100)    NOT NULL,
    bio                     TEXT,
    profile_picture_url     TEXT,
    reputation_score        DECIMAL(3,2)    NOT NULL DEFAULT 0.00,
    is_active               BOOLEAN         NOT NULL DEFAULT true,
    is_deleted              BOOLEAN         NOT NULL DEFAULT false,
    recommendations_enabled BOOLEAN         NOT NULL DEFAULT true,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at              TIMESTAMPTZ,

    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT chk_users_reputation_score
        CHECK (reputation_score >= 0.00 AND reputation_score <= 5.00)
);

CREATE TRIGGER trg_users_set_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at();

-- ---------------------------------------------------------------------------
-- Table: connections
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS connections (
    id          UUID        NOT NULL DEFAULT gen_random_uuid(),
    user_id_1   UUID        NOT NULL,
    user_id_2   UUID        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_connections PRIMARY KEY (id),
    CONSTRAINT fk_connections_user_id_1
        FOREIGN KEY (user_id_1) REFERENCES users (id),
    CONSTRAINT fk_connections_user_id_2
        FOREIGN KEY (user_id_2) REFERENCES users (id),
    CONSTRAINT uq_connections_pair
        UNIQUE (user_id_1, user_id_2),
    -- canonical ordering prevents (A,B) + (B,A) duplicates
    CONSTRAINT chk_connections_canonical_order
        CHECK (user_id_1 < user_id_2)
);

-- ---------------------------------------------------------------------------
-- Table: connection_requests
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS connection_requests (
    id              UUID                        NOT NULL DEFAULT gen_random_uuid(),
    requester_id    UUID                        NOT NULL,
    target_id       UUID                        NOT NULL,
    status          connection_request_status   NOT NULL DEFAULT 'PENDING',
    message         TEXT,
    created_at      TIMESTAMPTZ                 NOT NULL DEFAULT NOW(),
    responded_at    TIMESTAMPTZ,

    CONSTRAINT pk_connection_requests PRIMARY KEY (id),
    CONSTRAINT fk_connection_requests_requester_id
        FOREIGN KEY (requester_id) REFERENCES users (id),
    CONSTRAINT fk_connection_requests_target_id
        FOREIGN KEY (target_id) REFERENCES users (id),
    CONSTRAINT uq_connection_requests_pair
        UNIQUE (requester_id, target_id)
);

-- ---------------------------------------------------------------------------
-- Table: recommendations
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS recommendations (
    id              UUID                    NOT NULL DEFAULT gen_random_uuid(),
    recommender_id  UUID                    NOT NULL,
    recommended_id  UUID                    NOT NULL,
    target_id       UUID                    NOT NULL,
    type            recommendation_type     NOT NULL,
    status          recommendation_status   NOT NULL DEFAULT 'PENDING',
    message         TEXT,
    created_at      TIMESTAMPTZ             NOT NULL DEFAULT NOW(),
    expires_at      TIMESTAMPTZ             NOT NULL DEFAULT NOW() + INTERVAL '30 days',
    responded_at    TIMESTAMPTZ,

    CONSTRAINT pk_recommendations PRIMARY KEY (id),
    CONSTRAINT fk_recommendations_recommender_id
        FOREIGN KEY (recommender_id) REFERENCES users (id),
    CONSTRAINT fk_recommendations_recommended_id
        FOREIGN KEY (recommended_id) REFERENCES users (id),
    CONSTRAINT fk_recommendations_target_id
        FOREIGN KEY (target_id) REFERENCES users (id)
);

-- ---------------------------------------------------------------------------
-- Table: recommendation_feedback
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS recommendation_feedback (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid(),
    recommendation_id   UUID        NOT NULL,
    reviewer_id         UUID        NOT NULL,
    reviewee_id         UUID        NOT NULL,
    rating              INTEGER,
    comment             TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_recommendation_feedback PRIMARY KEY (id),
    CONSTRAINT fk_recommendation_feedback_recommendation_id
        FOREIGN KEY (recommendation_id) REFERENCES recommendations (id),
    CONSTRAINT fk_recommendation_feedback_reviewer_id
        FOREIGN KEY (reviewer_id) REFERENCES users (id),
    CONSTRAINT fk_recommendation_feedback_reviewee_id
        FOREIGN KEY (reviewee_id) REFERENCES users (id),
    CONSTRAINT uq_recommendation_feedback_reviewer
        UNIQUE (recommendation_id, reviewer_id),
    CONSTRAINT chk_recommendation_feedback_rating
        CHECK (rating >= 1 AND rating <= 5)
);

-- ---------------------------------------------------------------------------
-- Table: contacts
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS contacts (
    id          UUID            NOT NULL DEFAULT gen_random_uuid(),
    user_id     UUID            NOT NULL,
    type        contact_type    NOT NULL,
    value       VARCHAR(255)    NOT NULL,
    is_public   BOOLEAN         NOT NULL DEFAULT false,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_contacts PRIMARY KEY (id),
    CONSTRAINT fk_contacts_user_id
        FOREIGN KEY (user_id) REFERENCES users (id)
);

-- ---------------------------------------------------------------------------
-- Table: contact_shares
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS contact_shares (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid(),
    recommendation_id   UUID        NOT NULL,
    sharer_id           UUID        NOT NULL,
    recipient_id        UUID        NOT NULL,
    shared_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_contact_shares PRIMARY KEY (id),
    CONSTRAINT fk_contact_shares_recommendation_id
        FOREIGN KEY (recommendation_id) REFERENCES recommendations (id),
    CONSTRAINT fk_contact_shares_sharer_id
        FOREIGN KEY (sharer_id) REFERENCES users (id),
    CONSTRAINT fk_contact_shares_recipient_id
        FOREIGN KEY (recipient_id) REFERENCES users (id),
    CONSTRAINT uq_contact_shares_unique
        UNIQUE (recommendation_id, sharer_id, recipient_id)
);

-- ---------------------------------------------------------------------------
-- Table: blocks
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS blocks (
    id          UUID        NOT NULL DEFAULT gen_random_uuid(),
    blocker_id  UUID        NOT NULL,
    blocked_id  UUID        NOT NULL,
    block_type  block_type  NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_blocks PRIMARY KEY (id),
    CONSTRAINT fk_blocks_blocker_id
        FOREIGN KEY (blocker_id) REFERENCES users (id),
    CONSTRAINT fk_blocks_blocked_id
        FOREIGN KEY (blocked_id) REFERENCES users (id),
    CONSTRAINT uq_blocks_pair
        UNIQUE (blocker_id, blocked_id),
    CONSTRAINT chk_blocks_no_self_block
        CHECK (blocker_id != blocked_id)
);

-- ---------------------------------------------------------------------------
-- Table: user_plans
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_plans (
    id                      UUID            NOT NULL DEFAULT gen_random_uuid(),
    user_id                 UUID            NOT NULL,
    plan_name               VARCHAR(50)     NOT NULL DEFAULT 'FREE',
    recommendations_limit   INTEGER,        -- NULL = unlimited
    valid_until             TIMESTAMPTZ,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_user_plans PRIMARY KEY (id),
    CONSTRAINT fk_user_plans_user_id
        FOREIGN KEY (user_id) REFERENCES users (id),
    CONSTRAINT uq_user_plans_user_id
        UNIQUE (user_id)
);

CREATE TRIGGER trg_user_plans_set_updated_at
    BEFORE UPDATE ON user_plans
    FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at();

-- ---------------------------------------------------------------------------
-- Table: push_tokens
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS push_tokens (
    id          UUID            NOT NULL DEFAULT gen_random_uuid(),
    user_id     UUID            NOT NULL,
    token       TEXT            NOT NULL,
    platform    VARCHAR(20)     NOT NULL DEFAULT 'android',
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_push_tokens PRIMARY KEY (id),
    CONSTRAINT fk_push_tokens_user_id
        FOREIGN KEY (user_id) REFERENCES users (id),
    CONSTRAINT uq_push_tokens_user_token
        UNIQUE (user_id, token)
);

-- ---------------------------------------------------------------------------
-- Table: notifications
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS notifications (
    id          UUID            NOT NULL DEFAULT gen_random_uuid(),
    user_id     UUID            NOT NULL,
    type        VARCHAR(50)     NOT NULL,
    title       VARCHAR(255)    NOT NULL,
    body        TEXT,
    data        JSONB,
    is_read     BOOLEAN         NOT NULL DEFAULT false,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_notifications PRIMARY KEY (id),
    CONSTRAINT fk_notifications_user_id
        FOREIGN KEY (user_id) REFERENCES users (id)
);

-- ---------------------------------------------------------------------------
-- Table: schema_migrations
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS schema_migrations (
    version     VARCHAR(20)     NOT NULL,
    applied_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_schema_migrations PRIMARY KEY (version)
);

-- ---------------------------------------------------------------------------
-- Indexes — users
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_users_email
    ON users (email);

-- partial: hot path for active, non-deleted user lookups
CREATE INDEX IF NOT EXISTS idx_users_active
    ON users (is_deleted, is_active)
    WHERE is_deleted = false;

-- ---------------------------------------------------------------------------
-- Indexes — connections
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_connections_user_id_1
    ON connections (user_id_1);

CREATE INDEX IF NOT EXISTS idx_connections_user_id_2
    ON connections (user_id_2);

-- ---------------------------------------------------------------------------
-- Indexes — connection_requests
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_connection_requests_requester_id
    ON connection_requests (requester_id);

CREATE INDEX IF NOT EXISTS idx_connection_requests_target_id
    ON connection_requests (target_id);

-- partial: only PENDING rows are actionable; avoids scanning resolved history
CREATE INDEX IF NOT EXISTS idx_connection_requests_status_pending
    ON connection_requests (status)
    WHERE status = 'PENDING';

-- ---------------------------------------------------------------------------
-- Indexes — recommendations
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_recommendations_recommender_id
    ON recommendations (recommender_id);

CREATE INDEX IF NOT EXISTS idx_recommendations_recommended_id
    ON recommendations (recommended_id);

CREATE INDEX IF NOT EXISTS idx_recommendations_target_id
    ON recommendations (target_id);

-- partial: only PENDING rows need expiry checks and inbox queries
CREATE INDEX IF NOT EXISTS idx_recommendations_status_pending
    ON recommendations (status)
    WHERE status = 'PENDING';

-- supports expiry job: WHERE expires_at < NOW() AND status = 'PENDING'
CREATE INDEX IF NOT EXISTS idx_recommendations_expires_at
    ON recommendations (expires_at);

-- ---------------------------------------------------------------------------
-- Indexes — blocks
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_blocks_blocker_id
    ON blocks (blocker_id);

CREATE INDEX IF NOT EXISTS idx_blocks_blocked_id
    ON blocks (blocked_id);

-- ---------------------------------------------------------------------------
-- Indexes — notifications
-- ---------------------------------------------------------------------------
-- covering index for unread inbox query: WHERE user_id = ? AND is_read = false
CREATE INDEX IF NOT EXISTS idx_notifications_user_id_is_read
    ON notifications (user_id, is_read);

CREATE INDEX IF NOT EXISTS idx_notifications_created_at
    ON notifications (created_at);

-- ---------------------------------------------------------------------------
-- Indexes — push_tokens
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_push_tokens_user_id
    ON push_tokens (user_id);

-- ---------------------------------------------------------------------------
-- Seed: migration version record
-- ---------------------------------------------------------------------------
INSERT INTO schema_migrations (version)
VALUES ('001')
ON CONFLICT (version) DO NOTHING;

COMMIT;
