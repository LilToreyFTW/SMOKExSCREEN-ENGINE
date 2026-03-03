require('dotenv').config({ path: '.env.local' });
const { Pool } = require('pg');

console.log('DATABASE_URL:', process.env.DATABASE_URL ? 'set' : 'NOT SET');

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: { rejectUnauthorized: false }
});

async function main() {
  const client = await pool.connect();
  
  try {
    // Create/upgrade table 
    await client.query(`
      CREATE TABLE IF NOT EXISTS license_keys (
        id SERIAL PRIMARY KEY,
        key_value VARCHAR(50) UNIQUE NOT NULL,
        duration_type VARCHAR(20) NOT NULL,
        duration_ms BIGINT NOT NULL,
        used BOOLEAN DEFAULT FALSE,
        used_by VARCHAR(50),
        used_at TIMESTAMP,
        redeemed_at BIGINT,
        expires_at BIGINT,
        user_id VARCHAR(50),
        created_at TIMESTAMP DEFAULT NOW()
      )
    `);
    console.log('Table created/verified');

    // Add columns if they don't exist (for existing tables)
    try {
      await client.query(`ALTER TABLE license_keys ADD COLUMN IF NOT EXISTS redeemed_at BIGINT`);
      await client.query(`ALTER TABLE license_keys ADD COLUMN IF NOT EXISTS expires_at BIGINT`);
      await client.query(`ALTER TABLE license_keys ADD COLUMN IF NOT EXISTS user_id VARCHAR(50)`);
      console.log('Columns added/verified');
    } catch (e) {
      // columns may already exist
    }

    // Verify current keys
    const result = await client.query('SELECT duration_type, COUNT(*) FROM license_keys GROUP BY duration_type');
    console.log('\nKeys in database:');
    result.rows.forEach(r => console.log(`  ${r.duration_type}: ${r.count}`));

  } finally {
    client.release();
    await pool.end();
  }
}

main().catch(console.error);
