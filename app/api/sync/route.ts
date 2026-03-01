import { NextResponse } from 'next/server';

/**
 * GET /api/sync — ENGINE EXE fetches key list to sync into KeyCache.
 * Current root Next app: returns empty keys; full implementation lives in
 * SmokeScreen-ENGINE\SmokeScreen-ENGINE\server.js (Express) if that project is deployed.
 */
export async function GET() {
  return NextResponse.json({
    keys: [],
  });
}
