/**
 * POST /api/keys/redeem-clerk — SmokeScreen-Engine.exe (and bots) redeem a key.
 * Body: { key: string, hwid?: string }
 * Keys are backend-only; users never see the key list, only redeem with a key you send them.
 */
import { NextRequest, NextResponse } from 'next/server';
import { getPool } from '@/lib/db';

export async function POST(req: NextRequest) {
  let body: { key?: string; hwid?: string };
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ ok: false, message: 'Invalid JSON' }, { status: 400 });
  }
  const key = String(body?.key ?? '').trim();
  if (!key) {
    return NextResponse.json({ ok: false, message: 'Missing key' }, { status: 400 });
  }
  const pool = getPool();
  if (!pool) {
    return NextResponse.json({ ok: false, message: 'Service unavailable' }, { status: 503 });
  }
  const client = await pool.connect();
  try {
    const select = await client.query(
      'SELECT key_value, duration_type, duration_ms, used FROM license_keys WHERE key_value = $1 FOR UPDATE',
      [key]
    );
    if (select.rowCount === 0) {
      return NextResponse.json({ ok: false, message: 'Invalid key' }, { status: 400 });
    }
    const row = select.rows[0] as { used: number; duration_ms: number };
    if (Number(row.used) === 1) {
      return NextResponse.json({ ok: false, message: 'Key already redeemed' }, { status: 400 });
    }
    const now = Date.now();
    const durationMs = Number(row.duration_ms) || 0;
    const expiresAt = durationMs > 0 ? now + durationMs : null;
    await client.query(
      'UPDATE license_keys SET used = 1, redeemed_at = $2, expires_at = $3, user_id = NULL WHERE key_value = $1',
      [key, now, expiresAt]
    );
    return NextResponse.json({ ok: true, expiresAt });
  } finally {
    client.release();
  }
}
