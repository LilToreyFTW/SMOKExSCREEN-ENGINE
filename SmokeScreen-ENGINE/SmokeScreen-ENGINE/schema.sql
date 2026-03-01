-- ═══════════════════════════════════════════════════════════════════════════
--  SmokeScreen ENGINE — Neon PostgreSQL Schema v2
--
--  Setup:
--    1. Go to neon.tech → New Project → copy the connection string
--    2. Open the Neon SQL Editor and paste + run this entire file
--    3. Add DATABASE_URL to Vercel environment variables
-- ═══════════════════════════════════════════════════════════════════════════

-- ── users ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
  id               VARCHAR(36)   PRIMARY KEY,
  username         VARCHAR(100)  NOT NULL UNIQUE,
  email            VARCHAR(255)  UNIQUE,
  password_hash    VARCHAR(255),
  role             VARCHAR(20)   NOT NULL DEFAULT 'user',
  discord_id       VARCHAR(30)   UNIQUE,
  discord_username VARCHAR(100),
  discord_avatar   VARCHAR(100),
  created_at       BIGINT        NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_users_email      ON users (email);
CREATE INDEX IF NOT EXISTS idx_users_discord_id ON users (discord_id);
CREATE INDEX IF NOT EXISTS idx_users_role       ON users (role);

-- ── password_reset_tokens ────────────────────────────────────────────────────
-- token_hash is a SHA256 hex digest of the raw token (never store raw tokens).
CREATE TABLE IF NOT EXISTS password_reset_tokens (
  id          VARCHAR(36)  PRIMARY KEY,
  user_id     VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash  VARCHAR(64)  NOT NULL UNIQUE,
  expires_at  BIGINT       NOT NULL,
  used        SMALLINT     NOT NULL DEFAULT 0,
  created_at  BIGINT       NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_user_id    ON password_reset_tokens (user_id);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_expires_at ON password_reset_tokens (expires_at);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_used       ON password_reset_tokens (used);

-- ── activity_logs ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS activity_logs (
  id          VARCHAR(36)  PRIMARY KEY,
  user_id     VARCHAR(36)  REFERENCES users(id) ON DELETE SET NULL,
  action      VARCHAR(100) NOT NULL,
  detail      TEXT,
  ip          VARCHAR(64),
  user_agent  TEXT,
  created_at  BIGINT       NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_activity_logs_user_id    ON activity_logs (user_id);
CREATE INDEX IF NOT EXISTS idx_activity_logs_created_at ON activity_logs (created_at);

-- ── app_settings ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS app_settings (
  key         VARCHAR(100) PRIMARY KEY,
  value       TEXT         NOT NULL,
  updated_at  BIGINT       NOT NULL
);

-- ── sessions ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS sessions (
  token      VARCHAR(36)  PRIMARY KEY,
  user_id    VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  type       VARCHAR(10)  NOT NULL DEFAULT 'web',
  expires    BIGINT       NOT NULL,
  created_at BIGINT       NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_sessions_user_id ON sessions (user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expires  ON sessions (expires);

-- ── license_keys ──────────────────────────────────────────────────────────────
-- duration_type : '1_DAY' | '3_DAY' | '7_DAY' | '1_MONTH' | '3_MONTH' | 'LIFETIME'
-- duration_ms   : exact milliseconds valid after redemption (0 = LIFETIME = never expires)
-- used          : 0 = available, 1 = redeemed
-- expires_at    : NULL until redeemed, then redeemed_at + duration_ms (NULL for LIFETIME)
CREATE TABLE IF NOT EXISTS license_keys (
  key_value     VARCHAR(50)  PRIMARY KEY,
  duration_type VARCHAR(20)  NOT NULL DEFAULT '1_MONTH',
  duration_ms   BIGINT       NOT NULL DEFAULT 2592000000,
  used          SMALLINT     NOT NULL DEFAULT 0,
  user_id       VARCHAR(36)  REFERENCES users(id) ON DELETE SET NULL,
  redeemed_at   BIGINT,
  expires_at    BIGINT
);

CREATE INDEX IF NOT EXISTS idx_license_keys_user_id       ON license_keys (user_id);
CREATE INDEX IF NOT EXISTS idx_license_keys_used          ON license_keys (used);
CREATE INDEX IF NOT EXISTS idx_license_keys_duration_type ON license_keys (duration_type);
CREATE INDEX IF NOT EXISTS idx_license_keys_expires_at    ON license_keys (expires_at);

-- ── key_pool_config ───────────────────────────────────────────────────────────
-- Controls auto-refill: when available keys for a type hit 0,
-- the server generates `refill_target` new keys automatically.
CREATE TABLE IF NOT EXISTS key_pool_config (
  duration_type VARCHAR(20)  PRIMARY KEY,
  duration_ms   BIGINT       NOT NULL,
  refill_target INT          NOT NULL DEFAULT 1000,
  label         VARCHAR(50)  NOT NULL,
  sort_order    INT          NOT NULL DEFAULT 0
);

INSERT INTO key_pool_config (duration_type, duration_ms, refill_target, label, sort_order) VALUES
  ('1_DAY',    86400000,      1000, '1 Day',    1),
  ('3_DAY',    259200000,     1000, '3 Days',   2),
  ('7_DAY',    604800000,     1000, '7 Days',   3),
  ('1_MONTH',  2592000000,    1000, '1 Month',  4),
  ('3_MONTH',  7776000000,    1000, '3 Months', 5),
  ('LIFETIME', 0,             1000, 'Lifetime', 6)
ON CONFLICT (duration_type) DO NOTHING;

-- ── view: key pool status ─────────────────────────────────────────────────────
CREATE OR REPLACE VIEW key_pool_status AS
  SELECT
    c.duration_type,
    c.label,
    c.refill_target,
    COUNT(k.key_value) FILTER (WHERE k.used = 0) AS available,
    COUNT(k.key_value) FILTER (WHERE k.used = 1) AS redeemed
  FROM key_pool_config c
  LEFT JOIN license_keys k USING (duration_type)
  GROUP BY c.duration_type, c.label, c.refill_target, c.sort_order
  ORDER BY c.sort_order;
