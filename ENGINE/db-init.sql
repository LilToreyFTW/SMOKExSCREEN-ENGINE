-- ─────────────────────────────────────────────────────────────────────────────
-- SmokeScreen ENGINE — Neon PostgreSQL Schema
-- Run this once against your Neon database to initialize all tables.
--
-- How to run:
--   psql "your-neon-connection-string" -f db-init.sql
--
-- Or paste into the Neon SQL editor in the dashboard.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id              TEXT        PRIMARY KEY,          -- UUID
    username        TEXT        UNIQUE NOT NULL,
    email           TEXT        UNIQUE,
    password_hash   TEXT,                             -- NULL for Discord-only accounts
    discord_id      TEXT        UNIQUE,
    discord_username TEXT,
    discord_avatar  TEXT,
    created_at      BIGINT      NOT NULL              -- Unix ms
);

-- ── Sessions ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS sessions (
    token       TEXT        PRIMARY KEY,              -- UUID
    user_id     TEXT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type        TEXT        NOT NULL DEFAULT 'web',   -- 'web' | 'engine'
    expires     BIGINT      NOT NULL,                 -- Unix ms
    created_at  BIGINT      NOT NULL
);

CREATE INDEX IF NOT EXISTS sessions_user_id_idx ON sessions(user_id);
CREATE INDEX IF NOT EXISTS sessions_expires_idx ON sessions(expires);

-- ── License Keys ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS license_keys (
    id          SERIAL      PRIMARY KEY,
    key_value   TEXT        UNIQUE NOT NULL,          -- e.g. SS-XXXX-XXXX-XXXX
    used        INTEGER     NOT NULL DEFAULT 0,       -- 0 = available, 1 = redeemed
    user_id     TEXT        REFERENCES users(id) ON DELETE SET NULL,
    redeemed_at BIGINT                                -- Unix ms
);

CREATE INDEX IF NOT EXISTS license_keys_user_id_idx ON license_keys(user_id);
CREATE INDEX IF NOT EXISTS license_keys_used_idx    ON license_keys(used);

-- ── Engine Tokens (optional — for token-based login from ENGINE.exe) ─────────
CREATE TABLE IF NOT EXISTS engine_tokens (
    token       TEXT        PRIMARY KEY,
    user_id     TEXT        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at  BIGINT      NOT NULL,
    expires     BIGINT      NOT NULL
);

CREATE INDEX IF NOT EXISTS engine_tokens_user_id_idx ON engine_tokens(user_id);

-- ─────────────────────────────────────────────────────────────────────────────
-- Done. Tables created:
--   users          — accounts (email/password or Discord OAuth)
--   sessions       — Bearer tokens for API auth
--   license_keys   — SS-XXXX keys, imported from KEY.KV via import-keys.js
--   engine_tokens  — short-lived tokens for ENGINE.exe quick-login
-- ─────────────────────────────────────────────────────────────────────────────
