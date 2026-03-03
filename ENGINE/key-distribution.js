const crypto = require('crypto');
const http = require('http');
const fs = require('fs');
const path = require('path');

// Encryption configuration
const ENCRYPTION_KEY = 'SmokeScreenENGINE2026KeyDistribution!@#';
const ALGORITHM = 'aes-256-gcm';

// Key storage
const DISTRIBUTED_KEYS_FILE = path.join(__dirname, 'distributed_keys.json');
let distributedKeys = new Map();

// Load existing distributed keys
function loadDistributedKeys() {
    try {
        if (fs.existsSync(DISTRIBUTED_KEYS_FILE)) {
            const data = fs.readFileSync(DISTRIBUTED_KEYS_FILE, 'utf8');
            const keys = JSON.parse(data);
            distributedKeys = new Map(Object.entries(keys));
            console.log(`[KEY DIST] Loaded ${distributedKeys.size} distributed keys`);
        }
    } catch (error) {
        console.error('[KEY DIST] Error loading distributed keys:', error);
    }
}

// Save distributed keys
function saveDistributedKeys() {
    try {
        const keysObject = Object.fromEntries(distributedKeys);
        fs.writeFileSync(DISTRIBUTED_KEYS_FILE, JSON.stringify(keysObject, null, 2));
    } catch (error) {
        console.error('[KEY DIST] Error saving distributed keys:', error);
    }
}

// Encrypt key data
function encryptKey(keyData) {
    const algorithm = 'aes-256-gcm';
    const key = crypto.scryptSync(ENCRYPTION_KEY, 'salt', 32);
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipheriv(algorithm, key, iv);
    
    let encrypted = cipher.update(JSON.stringify(keyData), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    const authTag = cipher.getAuthTag();
    
    return {
        encrypted: encrypted,
        iv: iv.toString('hex'),
        authTag: authTag.toString('hex')
    };
}

// Decrypt key data
function decryptKey(encryptedData) {
    try {
        const algorithm = 'aes-256-gcm';
        const key = crypto.scryptSync(ENCRYPTION_KEY, 'salt', 32);
        const decipher = crypto.createDecipheriv(algorithm, key, Buffer.from(encryptedData.iv, 'hex'));
        decipher.setAuthTag(Buffer.from(encryptedData.authTag, 'hex'));
        
        let decrypted = decipher.update(encryptedData.encrypted, 'hex', 'utf8');
        decrypted += decipher.final('utf8');
        
        return JSON.parse(decrypted);
    } catch (error) {
        console.error('[KEY DIST] Decryption error:', error);
        return null;
    }
}

// Fetch keys from Discord bot
async function fetchKeysFromBot() {
    try {
        const http = require('http');
        const response = await new Promise((resolve, reject) => {
            const req = http.get('http://localhost:9877/status', (res) => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', () => resolve({ statusCode: res.statusCode, data }));
            });
            req.on('error', reject);
            req.setTimeout(10000, () => {
                req.destroy();
                reject(new Error('Request timeout'));
            });
        });

        if (response.statusCode === 200) {
            const status = JSON.parse(response.data);
            console.log(`[KEY DIST] Bot has ${status.totalKeys} keys, ${status.redeemedKeys} redeemed`);
            return status;
        }
    } catch (error) {
        console.error('[KEY DIST] Error fetching bot status:', error);
    }
    return null;
}

// Get all available keys from bot
async function getAllBotKeys() {
    try {
        // This would need to be implemented in the bot to expose all keys
        // For now, we'll simulate getting the keys
        const keys = [];
        
        // Read from bot's key storage
        const botKeysFile = path.join(__dirname, 'generated_keys.json');
        if (fs.existsSync(botKeysFile)) {
            const botKeys = JSON.parse(fs.readFileSync(botKeysFile, 'utf8'));
            
            Object.entries(botKeys).forEach(([key, data]) => {
                if (!data.redeemed) {
                    keys.push({
                        key: key,
                        game: data.game || 'Unknown',
                        duration: data.duration || '1_MONTH',
                        generatedAt: data.generatedAt || Date.now(),
                        encrypted: false
                    });
                }
            });
        }
        
        return keys;
    } catch (error) {
        console.error('[KEY DIST] Error getting bot keys:', error);
        return [];
    }
}

// Distribute keys to clients
async function distributeKeysToClients() {
    try {
        console.log('[KEY DIST] Starting key distribution...');
        
        // Get all available keys from bot
        const keys = await getAllBotKeys();
        console.log(`[KEY DIST] Found ${keys.length} available keys`);
        
        // Encrypt and prepare keys for distribution
        const encryptedKeys = [];
        keys.forEach(key => {
            const keyData = {
                keyValue: key.key,
                gameType: key.game,
                durationType: key.duration,
                generatedAt: key.generatedAt,
                expiresAt: key.expiresAt || 0,
                isEncrypted: true,
                distributionTime: Date.now()
            };
            
            const encrypted = encryptKey(keyData);
            encryptedKeys.push({
                id: crypto.randomBytes(16).toString('hex'),
                encryptedData: encrypted,
                metadata: {
                    game: key.game,
                    duration: key.duration,
                    generatedAt: key.generatedAt
                }
            });
        });
        
        // Store encrypted keys
        encryptedKeys.forEach(encryptedKey => {
            distributedKeys.set(encryptedKey.id, encryptedKey);
        });
        
        saveDistributedKeys();
        
        console.log(`[KEY DIST] Distributed ${encryptedKeys.length} encrypted keys`);
        
        return encryptedKeys;
    } catch (error) {
        console.error('[KEY DIST] Error distributing keys:', error);
        return [];
    }
}

// Start HTTP server for key distribution
const server = http.createServer((req, res) => {
    // Set CORS headers
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    
    if (req.method === 'OPTIONS') {
        res.writeHead(200);
        res.end();
        return;
    }
    
    // Endpoint for SmokeScreen-Engine.exe to get encrypted keys
    if (req.method === 'GET' && req.url === '/api/distributed-keys') {
        try {
            const keysArray = Array.from(distributedKeys.values()).map(key => ({
                id: key.id,
                game: key.metadata.game,
                duration: key.metadata.duration,
                generatedAt: key.metadata.generatedAt,
                encryptedData: key.encryptedData
            }));
            
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({
                success: true,
                keys: keysArray,
                total: keysArray.length
            }));
        } catch (error) {
            console.error('[KEY DIST] Error serving distributed keys:', error);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to get distributed keys' }));
        }
        return;
    }
    
    // Endpoint to decrypt a specific key (for redemption)
    if (req.method === 'POST' && req.url === '/api/decrypt-key') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', () => {
            try {
                const data = JSON.parse(body);
                const { keyId } = data;
                
                if (!keyId) {
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'Key ID is required' }));
                    return;
                }
                
                const distributedKey = distributedKeys.get(keyId);
                if (!distributedKey) {
                    res.writeHead(404, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'Key not found' }));
                    return;
                }
                
                const decryptedKey = decryptKey(distributedKey.encryptedData);
                if (!decryptedKey) {
                    res.writeHead(500, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'Failed to decrypt key' }));
                    return;
                }
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({
                    success: true,
                    key: decryptedKey
                }));
            } catch (error) {
                console.error('[KEY DIST] Error decrypting key:', error);
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Failed to decrypt key' }));
            }
        });
        return;
    }
    
    // Manual distribution trigger
    if (req.method === 'POST' && req.url === '/api/distribute') {
        distributeKeysToClients().then(keys => {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({
                success: true,
                message: `Distributed ${keys.length} keys`,
                keysDistributed: keys.length
            }));
        }).catch(error => {
            console.error('[KEY DIST] Error in manual distribution:', error);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Distribution failed' }));
        });
        return;
    }
    
    // Status endpoint
    if (req.method === 'GET' && req.url === '/api/status') {
        try {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({
                success: true,
                status: 'online',
                distributedKeys: distributedKeys.size,
                uptime: process.uptime()
            }));
        } catch (error) {
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to get status' }));
        }
        return;
    }
    
    res.writeHead(404);
    res.end('Not Found');
});

// Start server
const PORT = 3002;
server.listen(PORT, () => {
    console.log(`[KEY DIST] Key distribution server running on http://localhost:${PORT}`);
    console.log(`[KEY DIST] Available endpoints:`);
    console.log(`  GET  /api/distributed-keys - Get encrypted keys for clients`);
    console.log(`  POST /api/decrypt-key - Decrypt a specific key`);
    console.log(`  POST /api/distribute - Manual key distribution`);
    console.log(`  GET  /api/status - Get server status`);
});

// Load existing keys on startup
loadDistributedKeys();

// Auto-distribute keys every 2 minutes
setInterval(async () => {
    console.log('[KEY DIST] Auto-distributing keys...');
    await distributeKeysToClients();
}, 2 * 60 * 1000);

// Initial distribution
setTimeout(async () => {
    console.log('[KEY DIST] Performing initial key distribution...');
    await distributeKeysToClients();
}, 5000);

// Handle graceful shutdown
process.on('SIGINT', () => {
    console.log('[KEY DIST] Shutting down gracefully...');
    server.close(() => {
        console.log('[KEY DIST] Server closed');
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    console.log('[KEY DIST] Received SIGTERM, shutting down...');
    server.close(() => {
        console.log('[KEY DIST] Server closed');
        process.exit(0);
    });
});
