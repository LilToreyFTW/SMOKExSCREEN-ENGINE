# SmokeScreen-ENGINE Auth System Setup

## Tech Stack

- Backend: Node.js + Express
- Database: PostgreSQL (Neon or any Postgres)
- Auth:
  - Passwords hashed with `bcryptjs`
  - Sessions stored in DB (`sessions` table)
  - Session cookie: HTTP-only cookie `sse_session`
  - CSRF protection via `csurf` (token fetched from `GET /auth/csrf` and sent in `csrf-token` header)
- Frontend: Static HTML/CSS/JS served by Express

## Database Schema

Run `schema.sql` in your Postgres database (Neon SQL editor recommended).

Tables used for this feature set:

- `users` (email/password_hash/role)
- `sessions` (server-side session store)
- `password_reset_tokens` (secure reset flow; stores *hash* of the token)
- `activity_logs` (system/activity feed)
- `app_settings` (admin-editable settings)

## Environment Variables

Edit `.env` in `SmokeScreen-ENGINE/`:

- `DATABASE_URL` (required)
- `PORT` (optional, default `4000`)
- `FRONTEND_URL` (required for reset link generation; use your deployment URL in prod)
- `COOKIE_SECURE` (optional)
  - `true` in production HTTPS
  - `false` for local http

## Install & Run Locally

From `i:\UI_GUI\SmokeScreen-ENGINE`:

1. Install dependencies:

   - `npm install`

2. Ensure your Postgres DB has the schema:

   - Run `schema.sql`

3. Start the server:

   - `node server.js`

4. Open:

- `http://localhost:4000/auth/register`
- `http://localhost:4000/auth/login`
- `http://localhost:4000/dashboard`

## Create an Admin User

New users default to role `user`.

To promote a user:

- Update the user in the DB:

  - `UPDATE users SET role = 'admin' WHERE email = 'you@example.com';`

Then visit:

- `http://localhost:4000/admin`

## Security Notes

- SQL injection: prevented via parameterized queries (`pg` with `$1` params)
- XSS: mitigated by avoiding inserting untrusted HTML; admin UI escapes inputs
- CSRF: enforced on mutating routes (`POST/PATCH/PUT/DELETE`) via `csurf` tokens
- Rate limiting: enabled on `POST /auth/login`
- HTTPS: set `COOKIE_SECURE=true` in production

## Password Reset Flow

1. User goes to `/auth/forgot` and submits email.
2. Server creates a random token, stores only `SHA256(token)` in DB.
3. In this implementation, the API returns `resetUrl` for development/testing.
   - In production, you should email this link instead of returning it.
4. User opens `/auth/reset?token=...` and sets a new password.

