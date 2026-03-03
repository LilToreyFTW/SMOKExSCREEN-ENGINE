const http = require('http');
const fs = require('fs');
const path = require('path');

// Bot configuration
const GUILD_ID = '1455221314653786207'; // Your Discord Guild ID

// Key storage
const KEYS_FILE = path.join(__dirname, 'generated_keys.json');
let generatedKeys = new Map();

// User database - this will store real Discord users who authenticate
const USER_DB_FILE = path.join(__dirname, 'users.json');
let userDatabase = new Map();

// Load existing keys from file
function loadKeys() {
    try {
        if (fs.existsSync(KEYS_FILE)) {
            const data = fs.readFileSync(KEYS_FILE, 'utf8');
            const keys = JSON.parse(data);
            generatedKeys = new Map(Object.entries(keys));
            console.log(`[BOT] Loaded ${generatedKeys.size} keys from storage`);
        }
    } catch (error) {
        console.error('[BOT] Error loading keys:', error);
    }
}

// Save keys to file
function saveKeys() {
    try {
        const keysObject = Object.fromEntries(generatedKeys);
        fs.writeFileSync(KEYS_FILE, JSON.stringify(keysObject, null, 2));
    } catch (error) {
        console.error('[BOT] Error saving keys:', error);
    }
}

// Load user database
function loadUsers() {
    try {
        if (fs.existsSync(USER_DB_FILE)) {
            const data = fs.readFileSync(USER_DB_FILE, 'utf8');
            const users = JSON.parse(data);
            userDatabase = new Map(Object.entries(users));
            console.log(`[BOT] Loaded ${userDatabase.size} users from database`);
        }
    } catch (error) {
        console.error('[BOT] Error loading users:', error);
    }
}

// Save user database
function saveUsers() {
    try {
        const usersObject = Object.fromEntries(userDatabase);
        fs.writeFileSync(USER_DB_FILE, JSON.stringify(usersObject, null, 2));
    } catch (error) {
        console.error('[BOT] Error saving users:', error);
    }
}

// Authenticate user with manual verification
async function authenticateUserAsync(discordId, botToken) {
    try {
        console.log(`[BOT] Authenticating Discord user: ${discordId}`);
        
        // Check if user exists in our database
        if (userDatabase.has(discordId)) {
            const user = userDatabase.get(discordId);
            console.log(`[BOT] User found in database: ${user.username}#${user.discriminator} (${user.badge})`);
            return user;
        }
        
        // If not in database, create a new user with basic access
        // This allows users to sign in and then get their roles verified
        console.log(`[BOT] User ${discordId} not found in database, creating new user`);
        
        const newUser = {
            id: discordId,
            username: `User${discordId.slice(-4)}`,
            discriminator: '0000',
            avatar: null,
            badge: '👤 Member',
            isOwner: false,
            hasBasicAccess: false,
            isCommunityManager: false,
            roles: [],
            needsVerification: true
        };
        
        userDatabase.set(discordId, newUser);
        saveUsers();
        
        return newUser;
    } catch (error) {
        console.error(`[BOT] Error authenticating user: ${error.message}`);
        return null;
    }
}

// Manually add user with role verification
async function addUserWithRole(discordId, username, discriminator, role) {
    try {
        let badge = '👤 Member';
        let isOwner = false;
        let hasBasicAccess = false;
        let isCommunityManager = false;
        
        switch (role) {
            case 'OWNER':
                badge = '👑 OWNER';
                isOwner = true;
                hasBasicAccess = true;
                break;
            case 'COMMUNITY_MANAGER':
                badge = '🛡️ COMMUNITY MANAGER';
                isCommunityManager = true;
                hasBasicAccess = true;
                break;
            case 'BASIC_ACCESS':
                badge = '⭐ BASIC ACCESS';
                hasBasicAccess = true;
                break;
        }
        
        const user = {
            id: discordId,
            username: username,
            discriminator: discriminator,
            avatar: null,
            badge: badge,
            isOwner: isOwner,
            hasBasicAccess: hasBasicAccess,
            isCommunityManager: isCommunityManager,
            roles: [role],
            needsVerification: false
        };
        
        userDatabase.set(discordId, user);
        saveUsers();
        
        console.log(`[BOT] Added user: ${username}#${discriminator} (${badge})`);
        return user;
    } catch (error) {
        console.error(`[BOT] Error adding user: ${error.message}`);
        return null;
    }
}

// Key redemption command
async function redeemKey(userId, key) {
    try {
        console.log(`[BOT] Key redemption attempt: ${userId} - ${key}`);
        
        // Check if key exists and is valid
        const keyData = generatedKeys.get(key);
        if (!keyData) {
            return { success: false, message: '❌ Invalid key. This key was not generated by ENGINE.exe.' };
        }

        // Check if key is already redeemed
        if (keyData.redeemed) {
            return { success: false, message: '❌ This key has already been redeemed.' };
        }

        // Check if key is expired
        if (keyData.expiresAt && Date.now() > keyData.expiresAt) {
            return { success: false, message: '❌ This key has expired.' };
        }

        // Get user info
        const user = await authenticateUserAsync(userId, BOT_TOKEN);
        if (!user) {
            return { success: false, message: '❌ User not found.' };
        }

        // Check if user has required role
        if (!user.hasBasicAccess && !user.needsVerification) {
            return { success: false, message: '❌ You need the required Discord role to redeem keys. Contact an admin to get your role verified.' };
        }

        // If user needs verification, allow redemption but notify them
        if (user.needsVerification) {
            return { success: false, message: '❌ Your Discord role needs to be verified. Please contact an admin with your Discord User ID: ' + userId };
        }

        // Redeem the key
        keyData.redeemed = true;
        keyData.redeemedBy = userId;
        keyData.redeemedAt = Date.now();
        keyData.redeemedByUsername = user.username;
        
        saveKeys();

        // Validate recoil key format and grant access
        let gameAccess = null;
        let keyType = 'MAIN LICENSE';
        
        if (key.startsWith('R6S-')) {
            gameAccess = 'Rainbow Six Siege';
            keyType = 'RECOIL R6S';
        } else if (key.startsWith('CODW-')) {
            gameAccess = 'Call of Duty Warzone';
            keyType = 'RECOIL CODW';
        } else if (key.startsWith('AR-')) {
            gameAccess = 'Arc Raiders';
            keyType = 'RECOIL AR';
        } else if (key.startsWith('FN-')) {
            gameAccess = 'Fortnite';
            keyType = 'RECOIL FN';
        }

        let successMessage = `✅ Key redeemed successfully!\n\n**Key Details:**\n• Type: ${keyType}\n• Game: ${gameAccess || keyData.game || 'Unknown'}\n• Duration: ${keyData.duration}\n• Redeemed by: ${user.username}#${user.discriminator}\n• Redeemed at: ${new Date().toLocaleString()}`;

        // Add specific game access information
        if (gameAccess) {
            successMessage += `\n\n🎮 **Game Access Granted:**\nYou now have access to ${gameAccess} Recoil V2 features!\n\n💡 **How to use:**\n1. Open SmokeScreen-Engine.exe\n2. Navigate to the ${gameAccess} tab\n3. Your Recoil V2 features are now active`;
        }

        return { 
            success: true, 
            message: successMessage
        };
    } catch (error) {
        console.error('[BOT] Error redeeming key:', error);
        return { success: false, message: '❌ An error occurred while redeeming the key.' };
    }
}

// Generate random key
function generateKey() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let key = '';
    for (let i = 0; i < 3; i++) {
        for (let j = 0; j < 4; j++) {
            key += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        if (i < 2) key += '-';
    }
    return key;
}

// Generate random key part for recoil keys
function generateRandomKeyPart(length) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < length; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
}

// Start HTTP server for API endpoints
const server = http.createServer(async (req, res) => {
    // Set CORS headers
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    
    if (req.method === 'OPTIONS') {
        res.writeHead(200);
        res.end();
        return;
    }
    
    if (req.method === 'GET' && req.url === '/status') {
        const uptime = process.uptime();
        
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
            status: 'online',
            uptime: uptime,
            guild: 'SmokeScreen ENGINE',
            totalKeys: generatedKeys.size,
            redeemedKeys: Array.from(generatedKeys.values()).filter(k => k.redeemed).length,
            totalUsers: userDatabase.size,
            verifiedUsers: Array.from(userDatabase.values()).filter(u => !u.needsVerification).length
        }));
        return;
    }
    
    // Key generation endpoint (for ENGINE.exe)
    if (req.method === 'POST' && req.url === '/generate-key') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                const data = JSON.parse(body);
                const { game, duration, keys, source } = data;
                
                // Verify source is ENGINE.exe
                if (source !== 'ENGINE.exe') {
                    res.writeHead(403, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'Unauthorized source. Only ENGINE.exe can generate keys.' }));
                    return;
                }
                
                const generatedKeyList = [];
                
                // Calculate expiration based on duration
                let expirationMs = Date.now();
                switch (duration) {
                    case '1_MONTH':
                        expirationMs += 30 * 24 * 60 * 60 * 1000; // 30 days
                        break;
                    case '6_MONTHS':
                        expirationMs += 180 * 24 * 60 * 60 * 1000; // 180 days
                        break;
                    case '12_MONTHS':
                        expirationMs += 365 * 24 * 60 * 60 * 1000; // 365 days
                        break;
                    case 'LIFETIME':
                        expirationMs = 0; // No expiration
                        break;
                    default:
                        expirationMs += 30 * 24 * 60 * 60 * 1000; // Default 30 days
                        break;
                }
                
                for (let i = 0; i < keys; i++) {
                    let key;
                    
                    // Generate different key formats based on game type
                    if (game.startsWith('R6S')) {
                        key = `R6S-${generateRandomKeyPart(8)}`;
                    } else if (game.startsWith('CODW')) {
                        key = `CODW-${generateRandomKeyPart(8)}`;
                    } else if (game.startsWith('AR')) {
                        key = `AR-${generateRandomKeyPart(8)}`;
                    } else if (game.startsWith('FN')) {
                        key = `FN-${generateRandomKeyPart(8)}`;
                    } else {
                        // Default main license key format
                        key = generateKey();
                    }
                    
                    const keyData = {
                        key: key,
                        game: game,
                        duration: duration,
                        generatedAt: Date.now(),
                        generatedBy: 'ENGINE.exe',
                        redeemed: false,
                        expiresAt: expirationMs
                    };
                    
                    generatedKeys.set(key, keyData);
                    generatedKeyList.push(key);
                }
                
                saveKeys();
                
                console.log(`[BOT] Generated ${keys} keys for ${game} (${duration}) from ENGINE.exe`);
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ 
                    success: true, 
                    keys: generatedKeyList,
                    message: `Generated ${keys} keys for ${game} (${duration})`
                }));
                
            } catch (error) {
                console.error('[BOT] Error generating keys:', error);
                res.writeHead(400, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Invalid request format' }));
            }
        });
        return;
    }
    
    // User authentication endpoint
    if (req.method === 'POST' && req.url === '/auth/user') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                const data = JSON.parse(body);
                const { discordId } = data;
                
                if (!discordId) {
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'Discord ID is required'
                    }));
                    return;
                }
                
                const user = await authenticateUserAsync(discordId, BOT_TOKEN);
                
                if (user) {
                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: true,
                        user: user
                    }));
                } else {
                    res.writeHead(404, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'User not found'
                    }));
                }
            } catch (error) {
                console.error('[BOT] Error authenticating user:', error);
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Authentication failed' }));
            }
        });
        return;
    }
    
    // Admin endpoint to add users with roles
    if (req.method === 'POST' && req.url === '/admin/add-user') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                const data = JSON.parse(body);
                const { discordId, username, discriminator, role } = data;
                
                if (!discordId || !username || !role) {
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'Discord ID, username, and role are required'
                    }));
                    return;
                }
                
                const user = await addUserWithRole(discordId, username, discriminator, role);
                
                if (user) {
                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: true,
                        message: `User ${username}#${discriminator} added with role ${role}`,
                        user: user
                    }));
                } else {
                    res.writeHead(500, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'Failed to add user'
                    }));
                }
            } catch (error) {
                console.error('[BOT] Error adding user:', error);
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Failed to add user' }));
            }
        });
        return;
    }
    
    // List users endpoint
    if (req.method === 'GET' && req.url === '/admin/users') {
        try {
            const users = Array.from(userDatabase.values()).map(user => ({
                id: user.id,
                username: user.username,
                discriminator: user.discriminator,
                badge: user.badge,
                hasBasicAccess: user.hasBasicAccess,
                needsVerification: user.needsVerification
            }));
            
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({
                success: true,
                users: users
            }));
        } catch (error) {
            console.error('[BOT] Error listing users:', error);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to list users' }));
        }
        return;
    }
    
    res.writeHead(404);
    res.end('Not Found');
});

// Start HTTP server
server.listen(9877, () => {
    console.log('[BOT] Discord Bot API listening on port 9877');
    console.log('[BOT] Available endpoints:');
    console.log('  GET  /status - Bot status');
    console.log('  POST /generate-key - Generate keys (ENGINE.exe)');
    console.log('  POST /auth/user - Authenticate user');
    console.log('  POST /admin/add-user - Add user with role');
    console.log('  GET  /admin/users - List all users');
    console.log('[BOT] Ready for user authentication!');
});

// Load existing data
loadKeys();
loadUsers();

// Handle graceful shutdown
process.on('SIGINT', () => {
    console.log('[BOT] Shutting down gracefully...');
    server.close(() => {
        console.log('[BOT] Server closed');
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    console.log('[BOT] Received SIGTERM, shutting down...');
    server.close(() => {
        console.log('[BOT] Server closed');
        process.exit(0);
    });
});
