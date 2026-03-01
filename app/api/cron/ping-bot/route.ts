/**
 * Vercel Cron: pings the Discord bot's public URL so it stays online.
 * Runs every 5 min (see vercel.json crons). After each Vercel deploy, the cron
 * keeps running and pinging the bot so the bot's online status is not reset by deploys.
 *
 * Set in Vercel env:
 * - CRON_SECRET (Vercel sends Authorization: Bearer CRON_SECRET)
 * - BOT_PUBLIC_URL = https://your-bot.up.railway.app (or wherever the bot runs)
 */

import { NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';
export const maxDuration = 10;

export async function GET(req: NextRequest) {
  const auth = req.headers.get('authorization');
  const secret = process.env.CRON_SECRET;
  if (secret && auth !== `Bearer ${secret}`) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const botUrl = process.env.BOT_PUBLIC_URL;
  if (!botUrl) {
    return NextResponse.json({ ok: false, reason: 'BOT_PUBLIC_URL not set' }, { status: 200 });
  }

  const url = botUrl.replace(/\/$/, '') + '/health';
  try {
    const res = await fetch(url, { method: 'GET', signal: AbortSignal.timeout(8000) });
    const ok = res.ok;
    const data = await res.json().catch(() => ({}));
    return NextResponse.json({
      ok,
      bot: ok ? 'online' : 'error',
      status: res.status,
      ts: Date.now(),
      ...(data || {}),
    });
  } catch (e) {
    return NextResponse.json({
      ok: false,
      bot: 'unreachable',
      error: e instanceof Error ? e.message : 'fetch failed',
      ts: Date.now(),
    }, { status: 200 });
  }
}
