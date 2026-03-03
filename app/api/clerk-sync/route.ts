/**
 * POST /api/clerk-sync — SmokeScreen-Engine.exe syncs session (token + hwid).
 * Stub: accept and return 200 so the EXE can proceed to redeem.
 */
import { NextRequest, NextResponse } from 'next/server';

export async function POST(req: NextRequest) {
  await req.json().catch(() => ({}));
  return NextResponse.json({ ok: true });
}
