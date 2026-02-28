/**
 * ⚠️  DEPRECATED — DO NOT USE
 *
 * This file was the original in-memory TypeScript auth server.
 * It has been fully replaced by:
 *
 *   server.js  (Node/Express + Neon PostgreSQL)
 *   Deployed to: https://smok-ex-screen-engine.vercel.app
 *
 * The old KEYS.TS registry was in-memory only — keys reset on every restart
 * and there was no persistence, user accounts, Discord auth, or session management.
 *
 * ─── New architecture ────────────────────────────────────────────────────────
 *
 *  Website  →  Vercel (server.js)  →  Neon PostgreSQL
 *  ENGINE.exe  →  Vercel (server.js)  →  Neon PostgreSQL
 *
 *  Auth endpoints:
 *    POST /auth/register              — Email/password sign up
 *    POST /auth/login                 — Email/password sign in
 *    POST /auth/engine-login          — ENGINE EXE login (email/pw or engine token)
 *    POST /auth/discord/callback      — Web Discord OAuth
 *    POST /auth/discord/engine-callback — ENGINE Discord OAuth
 *    GET  /auth/me                    — Get current user (Bearer token)
 *    POST /auth/engine-token          — Generate engine token from web session
 *    POST /auth/logout                — Invalidate session
 *
 *  License endpoints:
 *    POST /keys/redeem                — Redeem a key (Bearer token required)
 *    GET  /keys/mine                  — List my keys (Bearer token required)
 *    GET  /keys/validate              — Check license status (Bearer token required)
 *
 *  Admin endpoint:
 *    POST /admin/keys/add             — Bulk-add keys (ADMIN_SECRET required)
 *
 * ─── KEY.KV — Key generation ─────────────────────────────────────────────────
 *
 *  Keys are generated via the Python script in KEY.KV and imported into the
 *  database with the /admin/keys/add endpoint:
 *
 *    curl -X POST https://smok-ex-screen-engine.vercel.app/admin/keys/add \
 *      -H "Content-Type: application/json" \
 *      -d '{"adminSecret":"YOUR_ADMIN_SECRET","keys":["SS-XXXX...","..."]}'
 *
 * ─────────────────────────────────────────────────────────────────────────────
 */

// This file intentionally left as documentation only.
// Delete it from production builds to avoid confusion.
export {};
