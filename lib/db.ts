/**
 * Serverless-safe Postgres pool for key storage.
 * Set DATABASE_URL in Vercel (e.g. Neon, Vercel Postgres).
 * Keys are stored backend-only; never exposed to public.
 */
import { Pool } from 'pg';

let pool: Pool | null = null;

export function getPool(): Pool | null {
  const url = process.env.DATABASE_URL;
  if (!url) return null;
  if (!pool) {
    pool = new Pool({
      connectionString: url,
      ssl: url.includes('sslmode=require') ? { rejectUnauthorized: false } : undefined,
      max: 2,
      idleTimeoutMillis: 10000,
    });
  }
  return pool;
}

export async function query<T = unknown>(
  text: string,
  params?: unknown[]
): Promise<{ rows: T[]; rowCount: number }> {
  const p = getPool();
  if (!p) return { rows: [], rowCount: 0 };
  const result = await p.query(text, params);
  return { rows: result.rows as T[], rowCount: result.rowCount ?? 0 };
}
