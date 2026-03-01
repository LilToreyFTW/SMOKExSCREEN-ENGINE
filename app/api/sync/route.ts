/**
 * GET /api/sync — ENGINE.exe fetches key list (secret required). Keys never public.
 * Same backend as /api/tsync; requires X-TSync-Key header so only ENGINE.exe can pull.
 */
import { NextRequest, NextResponse } from 'next/server';
import { query } from '@/lib/db';

const TSYNC_SECRET = process.env.TSYNC_SECRET || 'tsasync-key-2025-02-27-a7f3c9e1';

export async function GET(req: NextRequest) {
  const secret = req.headers.get('x-tsync-key') ?? req.nextUrl.searchParams.get('key') ?? '';
  if (secret !== TSYNC_SECRET) {
    return NextResponse.json({ keys: [] });
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
  });
}
