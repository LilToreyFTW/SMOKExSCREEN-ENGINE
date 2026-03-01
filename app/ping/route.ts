import { NextResponse } from 'next/server';

/** GET /ping — ENGINE EXE and MsPingStatus use this for "Vercel: Online" check. */
export async function GET() {
  return NextResponse.json({ status: 'ENGINE_ONLINE' });
}
