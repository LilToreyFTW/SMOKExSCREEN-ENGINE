/**
 * import-keys.js
 * 
 * Reads KEY.KV and imports all keys into the Neon PostgreSQL backend
 * via the /admin/keys/add endpoint.
 *
 * Usage:
 *   node import-keys.js
 *
 * Set your ADMIN_SECRET and API_URL below, or pass via env:
 *   ADMIN_SECRET=xxx API_URL=https://... node import-keys.js
 */

const fs = require('fs');
const path = require('path');

const API_URL      = process.env.API_URL      || 'https://smok-ex-screen-engine.vercel.app';
const ADMIN_SECRET = process.env.ADMIN_SECRET || '2ujqdM-k6R4caMtYp2nDzqaN-3blP9vc';
const KEY_FILE     = path.join(__dirname, 'KEY.KV');
const BATCH_SIZE   = 200; // stay well under Vercel's 4 MB body limit

async function main() {
    if (!fs.existsSync(KEY_FILE)) {
        console.error(`KEY.KV not found at ${KEY_FILE}`);
        process.exit(1);
    }

    const lines = fs.readFileSync(KEY_FILE, 'utf8')
        .split('\n')
        .map(l => l.trim())
        .filter(l => l.length > 0 && !l.startsWith('#'));

    // Extract just the key string (everything before the first ':')
    const keys = lines.map(l => l.split(':')[0].trim()).filter(Boolean);
    console.log(`\n📦 Found ${keys.length} keys in KEY.KV`);

    let imported = 0;
    let failed   = 0;

    for (let i = 0; i < keys.length; i += BATCH_SIZE) {
        const batch = keys.slice(i, i + BATCH_SIZE);
        process.stdout.write(`  Importing batch ${Math.floor(i / BATCH_SIZE) + 1}/${Math.ceil(keys.length / BATCH_SIZE)} (${batch.length} keys)... `);

        try {
            const res = await fetch(`${API_URL}/admin/keys/add`, {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ adminSecret: ADMIN_SECRET, keys: batch }),
            });

            const data = await res.json();

            if (res.ok) {
                console.log(`✓ (added: ${data.added ?? batch.length})`);
                imported += batch.length;
            } else {
                console.log(`✗ ${data.message ?? res.status}`);
                failed += batch.length;
            }
        } catch (err) {
            console.log(`✗ Network error: ${err.message}`);
            failed += batch.length;
        }

        // Small delay to avoid hammering the server
        if (i + BATCH_SIZE < keys.length) await new Promise(r => setTimeout(r, 200));
    }

    console.log(`\n✅ Done — imported: ${imported}, failed: ${failed}\n`);
}

main().catch(err => { console.error('Fatal:', err); process.exit(1); });
