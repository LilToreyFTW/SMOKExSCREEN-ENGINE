/**
 * POST /api/tsync — ENGINE.exe sends generated keys here. Stored backend-only.
 * GET /api/tsync — ENGINE.exe syncs key list (secret required); never public.
 * Keys are not visible to users; only redeem is public.
 */
import { NextRequest, NextResponse } from 'next/server';
import { query } from '@/lib/db';

const TSYNC_SECRET = process.env.TSYNC_SECRET || 'tsasync-key-2025-02-27-a7f3c9e1';

export async function GET(req: NextRequest) {
  const secret = req.headers.get('x-tsync-key') ?? req.nextUrl.searchParams.get('key') ?? '';
  if (secret !== TSYNC_SECRET) {
    return NextResponse.json({ keys: [], key: '' }, { status: 200 });
  }
  let rows: { key_value: string; duration_type: string; duration_ms: number }[] = [];
  try {
    const result = await query<{ key_value: string; duration_type: string; duration_ms: number }>(
      'SELECT key_value, duration_type, duration_ms FROM license_keys WHERE used = 0 ORDER BY key_value LIMIT 5000'
    );
    rows = result.rows;
  } catch {
    // DB not configured or table missing
  }
  return NextResponse.json({
    keys: rows.map((r) => ({
      key_value: r.key_value,
      duration_type: r.duration_type,
      duration_ms: Number(r.duration_ms),
    })),
    key: '',
  });
}

export async function POST(req: NextRequest) {
  let body: { key?: string; keys?: Array<{ key_value?: string; duration_type?: string; duration_ms?: number }> };
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ ok: false, message: 'Invalid JSON' }, { status: 400 });
  }
  if (body.key !== TSYNC_SECRET) {
    return NextResponse.json({ ok: false, message: 'Invalid key' }, { status: 403 });
  }
  const rawKeys = Array.isArray(body.keys) ? body.keys : [];
  if (rawKeys.length === 0) {
    return NextResponse.json({ ok: true, inserted: 0 });
  }
  const pool = (await import('@/lib/db')).getPool();
  if (!pool) {
    return NextResponse.json({ ok: false, message: 'Database not configured' }, { status: 503 });
  }
  const defaultDurationMs = 30 * 24 * 60 * 60 * 1000;
  const keys: { key_value: string; duration_type: string; duration_ms: number }[] = rawKeys.map((k: unknown) => {
    if (typeof k === 'string') return { key_value: k.trim(), duration_type: '1_MONTH', duration_ms: defaultDurationMs };
    const o = k as { key_value?: string; duration_type?: string; duration_ms?: number };
    return {
      key_value: String(o?.key_value ?? '').trim(),
      duration_type: String(o?.duration_type ?? '1_MONTH').trim(),
      duration_ms: Number(o?.duration_ms) || defaultDurationMs,
    };
  }).filter((k) => k.key_value.length > 0);
  let inserted = 0;
  const client = await pool.connect();
  try {
    for (const k of keys) {
      const keyValue = k.key_value;
      const durationType = k.duration_type;
      const durationMs = k.duration_ms;
      if (!keyValue) continue;
      try {
        const res = await client.query(
          `INSERT INTO license_keys (key_value, duration_type, duration_ms, used)
           VALUES ($1, $2, $3, 0)
           ON CONFLICT (key_value) DO NOTHING`,
          [keyValue, durationType, durationMs]
        );
        if (res.rowCount && res.rowCount > 0) inserted++;
      } catch (_) {
        // skip duplicate or constraint error
      }
    }
  } finally {
    client.release();
  }
  return NextResponse.json({ ok: true, inserted });
}
