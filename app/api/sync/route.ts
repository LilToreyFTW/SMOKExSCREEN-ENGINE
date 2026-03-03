import { NextRequest, NextResponse } from 'next/server';
import { query } from '@/lib/db';

const TSYNC_SECRET = process.env.TSYNC_SECRET || 'tsasync-key-2025-02-27-a7f3c9e1';

// # ADDED: Final /api/sync implementation backed by license_keys (ENGINE key sync)
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

// # ADDED: Original stub /api/sync implementation kept for reference
// import { NextResponse } from 'next/server';
// /**
//  * GET /api/sync — ENGINE EXE fetches key list to sync into KeyCache.
//  * Current root Next app: returns empty keys; full implementation lives in
//  * SmokeScreen-ENGINE\SmokeScreen-ENGINE\server.js (Express) if that project is deployed.
//  */
// export async function GET() {
//   return NextResponse.json({
//     keys: [],
//   });
// }
