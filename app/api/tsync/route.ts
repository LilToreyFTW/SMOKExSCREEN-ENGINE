import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/tsync — TSyncListener in ENGINE EXE polls for key updates.
 * POST /api/tsync — KeyExtension/KeySender send key list from ENGINE.
 * Current root Next app: stub responses; full logic in SmokeScreen-ENGINE\SmokeScreen-ENGINE\server.js if deployed.
 */
export async function GET() {
  return NextResponse.json({ keys: [], key: '' });
}

export async function POST(req: NextRequest) {
  await req.json().catch(() => ({}));
  return NextResponse.json({ ok: true });
}
