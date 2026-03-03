const http = require('http');
const fs = require('fs');
const path = require('path');

// Key storage
const KEYS_FILE = path.join(__dirname, 'website_keys.json');
let websiteKeys = [];

// Load existing keys
function loadKeys() {
    try {
        if (fs.existsSync(KEYS_FILE)) {
            const data = fs.readFileSync(KEYS_FILE, 'utf8');
            websiteKeys = JSON.parse(data);
            console.log(`[WEBSITE API] Loaded ${websiteKeys.length} keys from storage`);
        }
    } catch (error) {
        console.error('[WEBSITE API] Error loading keys:', error);
        websiteKeys = [];
    }
}

// Save keys
function saveKeys() {
    try {
        fs.writeFileSync(KEYS_FILE, JSON.stringify(websiteKeys, null, 2));
    } catch (error) {
        console.error('[WEBSITE API] Error saving keys:', error);
    }
}

// Start HTTP server
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
    
    // Key upload endpoint
    if (req.method === 'POST' && req.url === '/api/keys/upload') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', () => {
            try {
                const keyData = JSON.parse(body);
                
                // Add timestamp if not present
                if (!keyData.uploadedAt) {
                    keyData.uploadedAt = Date.now();
                }
                
                // Check if key already exists
                const existingKey = websiteKeys.find(k => k.key === keyData.key);
                if (existingKey) {
                    res.writeHead(409, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ 
                        error: 'Key already exists',
                        key: keyData.key 
                    }));
                    return;
                }
                
                // Add key to storage
                websiteKeys.push(keyData);
                saveKeys();
                
                console.log(`[WEBSITE API] Uploaded key: ${keyData.key} (${keyData.game} - ${keyData.duration})`);
                
                // Notify Discord bot about new key
                try {
                    const http = require('http');
                    const notification = {
                        type: 'new_key',
                        key: keyData.key,
                        game: keyData.game,
                        duration: keyData.duration
                    };
                    
                    const notificationData = JSON.stringify(notification);
                    const req = http.request('http://localhost:9877/api/webhook/new-key', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Content-Length': Buffer.byteLength(notificationData)
                        }
                    }, (res) => {
                        res.on('data', chunk => {});
                        res.on('end', () => {
                            console.log(`[WEBSITE API] Notified Discord bot about new key: ${keyData.key}`);
                        });
                    });
                    
                    req.on('error', (error) => {
                        console.log(`[WEBSITE API] Failed to notify Discord bot: ${error.message}`);
                    });
                    
                    req.write(notificationData);
                    req.end();
                } catch (error) {
                    console.log(`[WEBSITE API] Error notifying Discord bot: ${error.message}`);
                }
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ 
                    success: true,
                    message: 'Key uploaded successfully',
                    key: keyData.key 
                }));
            } catch (error) {
                console.error('[WEBSITE API] Error uploading key:', error);
                res.writeHead(400, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Invalid JSON data' }));
            }
        });
        return;
    }
    
    // Sync endpoint for Discord bot
    if (req.method === 'GET' && req.url === '/api/sync') {
        try {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ 
                success: true,
                keys: websiteKeys.map(key => ({
                    keyValue: key.key,
                    gameType: key.game,
                    durationType: key.duration,
                    generatedAt: key.generatedAt,
                    durationMs: key.expiresAt,
                    used: key.redeemed || false
                }))
            }));
        } catch (error) {
            console.error('[WEBSITE API] Error syncing keys:', error);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to sync keys' }));
        }
        return;
    }
    
    // Status endpoint
    if (req.method === 'GET' && req.url === '/api/status') {
        try {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ 
                success: true,
                status: 'online',
                totalKeys: websiteKeys.length,
                redeemedKeys: websiteKeys.filter(k => k.redeemed).length,
                uptime: process.uptime()
            }));
        } catch (error) {
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to get status' }));
        }
        return;
    }
    
    // 404 for other endpoints
    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'Endpoint not found' }));
});

// Start server
const PORT = 3001;
server.listen(PORT, () => {
    console.log(`[WEBSITE API] Server running on http://localhost:${PORT}`);
    console.log(`[WEBSITE API] Available endpoints:`);
    console.log(`  POST /api/keys/upload - Upload new keys from ENGINE.exe`);
    console.log(`  GET  /api/sync - Sync keys for Discord bot`);
    console.log(`  GET  /api/status - Get server status`);
});

// Load existing keys on startup
loadKeys();

// Handle graceful shutdown
process.on('SIGINT', () => {
    console.log('[WEBSITE API] Shutting down gracefully...');
    server.close(() => {
        console.log('[WEBSITE API] Server closed');
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    console.log('[WEBSITE API] Received SIGTERM, shutting down...');
    server.close(() => {
        console.log('[WEBSITE API] Server closed');
        process.exit(0);
    });
});
