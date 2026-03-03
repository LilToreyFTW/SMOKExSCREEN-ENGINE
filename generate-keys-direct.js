const { Pool } = require('pg');

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: { rejectUnauthorized: false }
});

async function main() {
  const client = await pool.connect();
  
  try {
    // Create table if not exists
    await client.query(`
      CREATE TABLE IF NOT EXISTS license_keys (
        id SERIAL PRIMARY KEY,
        key_value VARCHAR(50) UNIQUE NOT NULL,
        duration_type VARCHAR(20) NOT NULL,
        duration_ms BIGINT NOT NULL,
        used BOOLEAN DEFAULT FALSE,
        used_by VARCHAR(50),
        used_at TIMESTAMP,
        created_at TIMESTAMP DEFAULT NOW()
      )
    `);
    console.log('Table created/verified');

    // Generate keys
    const durations = [
      { type: '1_DAY', ms: 1 * 24 * 60 * 60 * 1000 },
      { type: '7_DAY', ms: 7 * 24 * 60 * 60 * 1000 },
      { type: '1_MONTH', ms: 30 * 24 * 60 * 60 * 1000 },
      { type: 'LIFETIME', ms: 0 }
    ];

    function generateKey() {
      const hex = [...Array(16)].map(() => Math.floor(Math.random() * 16).toString(16).toUpperCase()).join('');
      return `SS-${hex}`;
    }

    let inserted = 0;
    for (const dur of durations) {
      console.log(`Inserting 1000 ${dur.type} keys...`);
      for (let i = 0; i < 1000; i++) {
        const keyValue = generateKey();
        await client.query(
          `INSERT INTO license_keys (key_value, duration_type, duration_ms, used) VALUES ($1, $2, $3, FALSE) ON CONFLICT (key_value) DO NOTHING`,
          [keyValue, dur.type, dur.ms]
        );
        inserted++;
      }
    }

    // Insert owner key
    const ownerKey = 'SS-MASTER-99X-QM22-L091-OWNER-PRIME';
    await client.query(
      `INSERT INTO license_keys (key_value, duration_type, duration_ms, used) VALUES ($1, $2, $3, FALSE) ON CONFLICT (key_value) DO NOTHING`,
      [ownerKey, 'OWNER', 0]
    );
    console.log(`\n✓ Owner key inserted: ${ownerKey}`);
    console.log(`✓ Total keys inserted: ${inserted + 1}`);

    // Verify
    const result = await client.query('SELECT duration_type, COUNT(*) FROM license_keys GROUP BY duration_type');
    console.log('\nKeys in database:');
    result.rows.forEach(r => console.log(`  ${r.duration_type}: ${r.count}`));

  } finally {
    client.release();
    await pool.end();
  }
}

main().catch(console.error);
