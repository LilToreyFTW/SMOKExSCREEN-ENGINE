# Current sources (by file date)

Use this to know which directories are the **current** ones to edit; others may be duplicates or older copies.

**Last checked:** 2026-02-28 (by LastWriteTime on key files)

---

## ENGINE (C# / WinForms)

| Location | Status | Notes |
|----------|--------|--------|
| **`i:\UI_GUI\ENGINE`** | **Current** | Newest: HubForm.cs, KeyExtension.cs, KeyGenerator.cs ~15:57–15:58. Full feature set (KeyCache, MsPingStatus, GenerateKeys, etc.). |
| `i:\UI_GUI\SmokeScreen-ENGINE\ENGINE` | Older duplicate | Key files ~14:01–15:13. Simpler HubForm, older server.ts/vercel.json. |

**When updating ENGINE:** Edit files under **`i:\UI_GUI\ENGINE`** only. Optionally sync to `SmokeScreen-ENGINE\ENGINE` if you need that folder in sync.

---

## Next.js / Vercel (root app)

| Location | Status | Notes |
|----------|--------|--------|
| **`i:\UI_GUI`** (repo root) | **Current** | Next.js app: `app/`, `app/api/`, `next.config.js`, `vercel.json`, `tsconfig.json`. This is what Vercel builds when "Root Directory" is empty. |
| `i:\UI_GUI\SmokeScreen-ENGINE` | Alternate Next | Has its own `next.config.js`, `proxy.ts` (updated 16:04–16:08). Nested `SmokeScreen-ENGINE\SmokeScreen-ENGINE` is **Express** (server.js), not Next. |

**When updating the web app for Vercel:** If the Vercel project builds from repo root, edit **`i:\UI_GUI\app`** and root config files. The EXE calls `smok-ex-screen-engine.vercel.app` for `/api/sync`, `/api/tsync`, `/ping`, etc.; those routes live in the root Next app or in the Express app under `SmokeScreen-ENGINE\SmokeScreen-ENGINE` depending on which project is deployed to that URL.

---

## Express backend (keys, auth, Discord)

| Location | Status | Notes |
|----------|--------|--------|
| **`i:\UI_GUI\SmokeScreen-ENGINE\SmokeScreen-ENGINE`** | Current Express | `server.js` (15:13), `vercel.json` – has `/api/tsync`, `/admin/keys`, Discord OAuth, DB. No GET `/api/sync` in server.js. |

If the same Vercel deployment serves both Next and Express, the root Next app is the one we added; the Express app is a separate stack (different vercel.json with server.js).

---

## Summary

- **ENGINE (C#):** current = **`ENGINE`** at repo root.
- **Next.js / Vercel build:** current = **repo root** (`app/`, `next.config.js`, `vercel.json`).
- **Express API (keys, Discord):** current = **`SmokeScreen-ENGINE\SmokeScreen-ENGINE`** (server.js).
