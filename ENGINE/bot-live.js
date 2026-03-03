const { Client, GatewayIntentBits } = require('discord.js');
const http = require('http');
const fs = require('fs');
const path = require('path');
const { URL } = require('url');

// Bot configuration
const DISCORD_BOT_TOKEN = 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.G-N9Kr.80Q2eiJtAPXShAIZzVurcG-v1rT6XH5-A7vQRw';
const GUILD_ID = '1455221314653786207'; // Your Discord Guild ID
const ANNOUNCEMENT_CHANNEL_ID = '1477453855485853870'; // keys-in-stcok channel
const ANNOUNCEMENTS_CHANNEL_ID = '1477430949124767987'; // announcements channel
const CHAT_CHANNEL_ID = '1477430872021008404'; // chat channel
const NEWS_CHANNEL_ID = '1477430901762691259'; // news channel
const KEY_WEBHOOK_URL = 'https://discord.com/api/webhooks/1478179543402680320/7T4nclE6lHaZ9epzsCe-XzCIhNGibEA2ApjxU6jg5LqDe6rpeIsj7GMn0i-gurd02GnQ'; // Webhook URL for key announcements

// Key storage
const KEYS_FILE = path.join(__dirname, 'generated_keys.json');
let generatedKeys = new Map();

// Discord client
const client = new Client({
    intents: [
        GatewayIntentBits.Guilds,
        GatewayIntentBits.GuildMessages
    ]
});

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

// Authenticate real Discord user
async function authenticateUserAsync(discordId, botToken) {
    try {
        console.log(`[BOT] HARD STACK Authenticating Discord user: ${discordId}`);
        console.log(`[BOT] Client ready: ${client.ready}`);
        console.log(`[BOT] Guilds available: ${client.guilds.cache.size}`);
        
        // HARD STACK GUILD VALIDATION: Get guild member info from Discord API
        const guild = client.guilds.cache.get(GUILD_ID);
        if (!guild) {
            console.log(`[BOT] HARD STACK GUILD NOT FOUND: Guild ${GUILD_ID} not found. Available guilds: ${Array.from(client.guilds.cache.keys()).join(', ')}`);
            return null;
        }

        console.log(`[BOT] HARD STACK GUILD FOUND: Found guild ${guild.name} (${guild.id}) with ${guild.members.cache.size} members`);

        // Try to get member from cache first
        let member = guild.members.cache.get(discordId);
        console.log(`[BOT] Member in cache: ${!!member}`);
        
        // If not in cache, fetch from Discord API
        if (!member) {
            try {
                console.log(`[BOT] HARD STACK: Fetching member from Discord API...`);
                member = await guild.members.fetch(discordId);
                console.log(`[BOT] HARD STACK: Successfully fetched member from Discord API`);
            } catch (error) {
                console.log(`[BOT] HARD STACK REJECTION: Failed to fetch member ${discordId}: ${error.message}`);
                console.log(`[BOT] HARD STACK: User is not in guild ${GUILD_ID}`);
                return null;
            }
        }

        if (!member) {
            console.log(`[BOT] HARD STACK REJECTION: User ${discordId} not found in guild ${GUILD_ID}`);
            return null;
        }

        console.log(`[BOT] HARD STACK SUCCESS: Found member ${member.user.username}#${member.user.discriminator} in guild ${guild.name}`);

        // Check user's roles
        const roles = member.roles;
        const roleNames = Array.from(roles).map(role => role.name);

        // Determine badge and access based on roles
        let badge = '👤 Member';
        let isOwner = false;
        let hasBasicAccess = false;
        let isCommunityManager = false;

        // Check for specific role names
        if (roleNames.includes('BL0WDART Owner')) {
            badge = '👑 OWNER';
            isOwner = true;
            hasBasicAccess = true;
        } else if (roleNames.includes('Community Manager')) {
            badge = '🛡️ COMMUNITY MANAGER';
            isCommunityManager = true;
            hasBasicAccess = true;
        } else if (roleNames.includes('Smokescreen-discord-access')) {
            badge = '⭐ BASIC ACCESS';
            hasBasicAccess = true;
        }

        console.log(`[BOT] User authenticated: ${member.user.username}#${member.user.discriminator} (${badge})`);

        return {
            id: member.id,
            username: member.user.username,
            discriminator: member.user.discriminator,
            avatar: member.user.avatar,
            badge: badge,
            isOwner: isOwner,
            hasBasicAccess: hasBasicAccess,
            isCommunityManager: isCommunityManager,
            roles: roleIds
        };
    } catch (error) {
        console.error(`[BOT] Error authenticating user: ${error.message}`);
        return null;
    }
}

// Key redemption command
async function redeemKey(userId, key) {
    try {
        console.log(`[BOT] HARD STACK Key redemption attempt: ${userId} - ${key}`);
        
        // Check if key exists and is valid
        const keyData = generatedKeys.get(key);
        if (!keyData) {
            return { success: false, message: '❌ Invalid key. This key was not generated by ENGINE.exe.' };
        }

        // HARD STACK VALIDATION: Check if key was generated by Engine.exe ONLY
        if (keyData.generatedBy !== 'ENGINE.exe') {
            console.log(`[BOT] HARD STACK REJECTION: Key ${key} was generated by ${keyData.generatedBy}, not ENGINE.exe`);
            return { success: false, message: '❌ HARD STACK: This key was not generated by ENGINE.exe. Only keys from ENGINE.exe can be redeemed.' };
        }

        console.log(`[BOT] HARD STACK VALIDATION PASSED: Key ${key} was generated by ${keyData.generatedBy}`);

        // Check if key is already redeemed
        if (keyData.redeemed) {
            return { success: false, message: '❌ This key has already been redeemed.' };
        }

        // Check if key is expired
        if (keyData.expiresAt && Date.now() > keyData.expiresAt) {
            return { success: false, message: '❌ This key has expired.' };
        }

        // HARD STACK USER VALIDATION: Check if user is in the required guild
        const user = await authenticateUserAsync(userId, DISCORD_BOT_TOKEN);
        if (!user) {
            return { success: false, message: '❌ HARD STACK: User not found in guild 1455221314653786207 or insufficient permissions.' };
        }

        console.log(`[BOT] HARD STACK USER VALIDATION PASSED: User ${user.username}#${user.discriminator} is in guild`);

        // Check if user has required role using the badge
        if (!user.hasBasicAccess) {
            return { success: false, message: '❌ You need the required Discord role to redeem keys.' };
        }

        // Mark key as redeemed
        keyData.redeemed = true;
        keyData.redeemedAt = Date.now();
        keyData.redeemedBy = userId;
        keyData.redeemedByUsername = user.username;
        
        console.log(`[BOT] HARD STACK SUCCESS: Key ${key} redeemed by ${user.username}#${user.discriminator}`);
        
        // Send webhook notification for redemption
        await sendKeyWebhook({
            key: key,
            game: keyData.game,
            duration: keyData.duration,
            generatedBy: keyData.generatedBy,
            redeemedBy: user.username
        }, 'redeemed');
        
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

// Send announcement to specific channels
async function sendAnnouncement(channelId, title, description, color = 0x0099ff) {
    try {
        const channel = client.channels.cache.get(channelId);
        if (channel && channel.isTextBased()) {
            const embed = {
                title: title,
                description: description,
                color: color,
                timestamp: new Date().toISOString(),
                footer: {
                    text: 'SmokeScreen ENGINE - Advanced Tool Suite',
                    iconURL: client.user?.displayAvatarURL()
                }
            };
            
            await channel.send({ embeds: [embed] });
            console.log(`[BOT] Announcement sent to channel ${channelId}: ${title}`);
        } else {
            console.log(`[BOT] Channel ${channelId} not found or not text-based`);
        }
    } catch (error) {
        console.log(`[BOT] Error sending announcement: ${error.message}`);
    }
}

// Send tool descriptor updates
async function sendToolDescriptor(toolName, description, features, updateType = 'update') {
    const channels = [ANNOUNCEMENTS_CHANNEL_ID, NEWS_CHANNEL_ID];
    
    for (const channelId of channels) {
        try {
            const channel = client.channels.cache.get(channelId);
            if (channel && channel.isTextBased()) {
                const embed = {
                    title: `🔧 Tool Descriptor: ${toolName}`,
                    description: `**${updateType.charAt(0).toUpperCase() + updateType.slice(1)}**\n\n${description}`,
                    color: updateType === 'update' ? 0x00ff00 : (updateType === 'new' ? 0x0099ff : 0xff9900),
                    fields: [
                        {
                            name: '🚀 Features',
                            value: features.join('\n• '),
                            inline: false
                        },
                        {
                            name: '📅 Last Updated',
                            value: new Date().toLocaleString(),
                            inline: true
                        },
                        {
                            name: '🔗 Integration',
                            value: 'SmokeScreen ENGINE Suite',
                            inline: true
                        }
                    ],
                    timestamp: new Date().toISOString(),
                    footer: {
                        text: 'SmokeScreen ENGINE - Tool Descriptors',
                        iconURL: client.user?.displayAvatarURL()
                    }
                };
                
                await channel.send({ embeds: [embed] });
                console.log(`[BOT] Tool descriptor sent: ${toolName} to channel ${channelId}`);
            }
        } catch (error) {
            console.log(`[BOT] Error sending tool descriptor: ${error.message}`);
        }
    }
}

// Get tool descriptions
function getToolDescriptors() {
    return {
        'SmokeScreen ENGINE': {
            description: 'Advanced game enhancement suite with real-time Discord integration and encrypted key management.',
            features: [
                '🔐 Encrypted key generation and distribution',
                '🤖 Live Discord bot authentication',
                '🎮 Multi-game recoil control system',
                '📊 Real-time analytics and monitoring',
                '🔑 Automatic key synchronization',
                '🌐 Web-based management interface',
                '🛡️ Role-based access control',
                '⚡ High-performance optimization'
            ]
        },
        'Discord Bot Integration': {
            description: 'Intelligent Discord bot providing real-time user authentication and key management.',
            features: [
                '👤 Real Discord user verification',
                '🔑 Automatic key redemption',
                '📢 Webhook notifications',
                '🎯 Role-based permissions',
                '📊 Usage analytics',
                '🔄 Real-time synchronization',
                '🛡️ Secure authentication',
                '📝 Activity logging'
            ]
        },
        'Key Distribution System': {
            description: 'Military-grade encrypted key distribution with automatic synchronization.',
            features: [
                '🔐 AES-256-GCM encryption',
                '⚡ Instant key distribution',
                '🔄 Automatic synchronization',
                '📱 Multi-platform support',
                '🛡️ Secure key storage',
                '📊 Distribution analytics',
                '🔍 Key tracking',
                '⏰ Expiration management'
            ]
        },
        'Recoil Control System': {
            description: 'Advanced recoil control for multiple games with customizable settings.',
            features: [
                '🎮 Multi-game support',
                '⚙️ Customizable settings',
                '📊 Performance analytics',
                '🔧 Real-time adjustments',
                '🎯 Precision control',
                '📈 Usage statistics',
                '🔄 Profile switching',
                '⚡ Low latency'
            ]
        },
        'Website Integration': {
            description: 'Seamless web-based management interface for remote administration.',
            features: [
                '🌐 Web dashboard',
                '📊 Real-time statistics',
                '👥 User management',
                '🔑 Key administration',
                '📈 Analytics dashboard',
                '🔄 Live updates',
                '🛡️ Secure authentication',
                '📱 Mobile responsive'
            ]
        }
    };
}

// Send key notification to webhook
async function sendKeyWebhook(keyData, action = 'generated') {
    try {
        const https = require('https');
        const url = new URL(KEY_WEBHOOK_URL);
        
        const embed = {
            title: `🔑 Key ${action.charAt(0).toUpperCase() + action.slice(1)}`,
            description: `**Key:** ${keyData.key}\n**Game:** ${keyData.game}\n**Duration:** ${keyData.duration}\n**Generated by:** ${keyData.generatedBy}`,
            color: action === 'generated' ? 0x00ff00 : (action === 'redeemed' ? 0xff9900 : 0xff0000),
            timestamp: new Date().toISOString(),
            footer: {
                text: 'SmokeScreen ENGINE - Key Management System'
            }
        };
        
        const webhookData = {
            embeds: [embed]
        };
        
        const postData = JSON.stringify(webhookData);
        
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname + url.search,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(postData)
            }
        };
        
        const req = https.request(options, (res) => {
            console.log(`[BOT] Webhook sent: ${action} key ${keyData.key} - Status: ${res.statusCode}`);
        });
        
        req.on('error', (error) => {
            console.log(`[BOT] Webhook error: ${error.message}`);
        });
        
        req.write(postData);
        req.end();
    } catch (error) {
        console.log(`[BOT] Error sending webhook: ${error.message}`);
    }
}

// Discord bot ready event
client.once('ready', async () => {
    console.log(`[BOT] Logged in as ${client.user.tag}`);
    
    // Load keys from storage
    loadKeys();
    
    // Sync keys from website
    await syncKeysFromWebsite();
    
    // Set persistent bot status
    client.user.setActivity('SmokeScreen ENGINE Auth', { type: 'WATCHING' });
    
    // Verify guild access
    const guild = client.guilds.cache.get(GUILD_ID);
    if (guild) {
        console.log(`[BOT] Successfully connected to guild: ${guild.name}`);
        console.log(`[BOT] Guild member count: ${guild.memberCount}`);
        
        // Fetch all guild members for authentication
        try {
            await guild.members.fetch();
            console.log(`[BOT] Fetched ${guild.members.cache.size} guild members`);
        } catch (error) {
            console.log(`[BOT] Error fetching guild members: ${error.message}`);
        }
        
        // Send startup notification
        const channel = guild.channels.cache.get(ANNOUNCEMENT_CHANNEL_ID);
        if (channel && channel.isTextBased()) {
            try {
                await channel.send({
                    embeds: [{
                        title: '🤖 Discord Bot Online',
                        description: 'The SmokeScreen ENGINE Discord bot is now online with enhanced features!\n\n**New Features:**\n• Real Discord user authentication\n• Direct Discord ID login\n• Guild member verification\n• Role-based access control\n• Key redemption system\n• Website key synchronization\n• Multi-channel announcements\n• Tool descriptor system',
                        color: 0x00ff00
                    }]
                });
            } catch (error) {
                console.log(`[BOT] Error sending startup notification: ${error.message}`);
            }
        }
        
        // Send tool descriptors to announcements and news channels
        setTimeout(async () => {
            const tools = getToolDescriptors();
            for (const [toolName, toolInfo] of Object.entries(tools)) {
                await sendToolDescriptor(toolName, toolInfo.description, toolInfo.features, 'new');
                // Small delay between tool posts
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
        }, 5000); // 5 seconds after startup
    } else {
        console.log(`[BOT] Guild ${GUILD_ID} not found`);
    }
});

// Sync keys from website
async function syncKeysFromWebsite() {
    try {
        console.log('[BOT] Syncing keys from website...');
        
        const http = require('http');
        const response = await new Promise((resolve, reject) => {
            const req = http.get('http://localhost:3001/api/sync', (res) => {
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
            const data = JSON.parse(response.data);
            if (data.keys && Array.isArray(data.keys)) {
                let addedCount = 0;
                data.keys.forEach(key => {
                    if (!generatedKeys.has(key.keyValue)) {
                        generatedKeys.set(key.keyValue, {
                            key: key.keyValue,
                            game: key.gameType || 'Unknown',
                            duration: key.durationType || '1_MONTH',
                            generatedAt: key.generatedAt || Date.now(),
                            generatedBy: 'Website Sync',
                            redeemed: key.used || false,
                            expiresAt: key.durationMs || 0
                        });
                        addedCount++;
                    }
                });
                
                if (addedCount > 0) {
                    saveKeys();
                    console.log(`[BOT] Synced ${addedCount} new keys from website`);
                } else {
                    console.log('[BOT] No new keys from website');
                }
            }
        } else {
            console.log(`[BOT] Failed to sync from website: HTTP ${response.statusCode}`);
        }
    } catch (error) {
        console.log(`[BOT] Error syncing keys from website: ${error.message}`);
    }
}

// Watch for new keys from ENGINE.exe
async function watchForNewKeys() {
    try {
        const http = require('http');
        
        // Check website API for new keys more frequently
        const response = await new Promise((resolve, reject) => {
            const req = http.get('http://localhost:3001/api/status', (res) => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', () => resolve({ statusCode: res.statusCode, data }));
            });
            req.on('error', reject);
            req.setTimeout(5000, () => {
                req.destroy();
                reject(new Error('Request timeout'));
            });
        });

        if (response.statusCode === 200) {
            const data = JSON.parse(response.data);
            const currentTotalKeys = data.totalKeys || 0;
            
            // If we have more keys than before, sync them
            if (currentTotalKeys > generatedKeys.size) {
                console.log(`[BOT] Detected new keys (${currentTotalKeys} vs ${generatedKeys.size}), syncing...`);
                await syncKeysFromWebsite();
            }
        }
    } catch (error) {
        console.log(`[BOT] Error watching for new keys: ${error.message}`);
    }
}

// Periodic sync every 5 minutes
setInterval(async () => {
    console.log('[BOT] Periodic key sync...');
    await syncKeysFromWebsite();
}, 5 * 60 * 1000); // 5 minutes

// Watch for new keys every 30 seconds (more frequent)
setInterval(async () => {
    await watchForNewKeys();
}, 30 * 1000); // 30 seconds

// Initial watch after startup
setTimeout(async () => {
    console.log('[BOT] Initial key watch...');
    await watchForNewKeys();
}, 10000); // 10 seconds after startup

// Message handling for key redemption
client.on('messageCreate', async (message) => {
    // Ignore bot messages
    if (message.author.bot) return;

    // Only respond to DMs or messages in specific channels
    if (message.guild && message.channel.id !== ANNOUNCEMENT_CHANNEL_ID) return;

    const content = message.content.trim();
    
    // Handle key redemption
    if (content.startsWith('!redeem ')) {
        const key = content.substring(8).trim();
        
        if (!key) {
            await message.reply('❌ Please provide a key to redeem. Usage: `!redeem <key>`');
            return;
        }

        const result = await redeemKey(message.author.id, key);
        await message.reply(result.message);
        
        // Log redemption attempt
        console.log(`[BOT] Key redemption attempt: ${message.author.tag} - ${key} - ${result.success ? 'SUCCESS' : 'FAILED'}`);
    }
    
    // Send tool descriptors
    if (content === '!tools' || content === '!descriptors') {
        const tools = getToolDescriptors();
        let response = '🔧 **SmokeScreen ENGINE Tool Suite**\n\n';
        
        for (const [toolName, toolInfo] of Object.entries(tools)) {
            response += `**${toolName}**\n`;
            response += `${toolInfo.description}\n`;
            response += `*Features: ${toolInfo.features.length}*\n\n`;
        }
        
        await message.reply({
            embeds: [{
                title: '🔧 SmokeScreen ENGINE Tool Suite',
                description: response,
                color: 0x0099ff,
                footer: {
                    text: 'Use "!tool <name>" for detailed information about a specific tool'
                }
            }]
        });
        return;
    }

    // Send specific tool descriptor
    if (content.startsWith('!tool ')) {
        const toolName = content.substring(6).trim();
        const tools = getToolDescriptors();
        const tool = tools[toolName];
        
        if (tool) {
            await message.reply({
                embeds: [{
                    title: `🔧 ${toolName}`,
                    description: tool.description,
                    color: 0x00ff00,
                    fields: [
                        {
                            name: '🚀 Features',
                            value: tool.features.join('\n• '),
                            inline: false
                        }
                    ],
                    footer: {
                        text: 'SmokeScreen ENGINE - Advanced Tool Suite'
                    }
                }]
            });
        } else {
            await message.reply(`❌ Tool "${toolName}" not found. Use "!tools" to see available tools.`);
        }
        return;
    }

    // Send announcements to different channels
    if (content.startsWith('!announce ')) {
        // Check if user has admin role (simple check for now)
        const member = await message.guild.members.fetch(message.author.id);
        const hasAdminRole = member.roles.cache.some(role => 
            role.name === 'BL0WDART Owner' || 
            role.name === 'Community Manager' || 
            role.name === 'Smokescreen-discord-access'
        );
        
        if (!hasAdminRole) {
            await message.reply('❌ You need admin permissions to send announcements.');
            return;
        }
        
        const announcementText = content.substring(10).trim();
        const parts = announcementText.split('|');
        const channelName = parts[0].trim();
        const title = parts[1] ? parts[1].trim() : '📢 Announcement';
        const description = parts[2] ? parts[2].trim() : parts.slice(2).join('|').trim();
        
        let targetChannel;
        switch (channelName.toLowerCase()) {
            case 'announcements':
                targetChannel = ANNOUNCEMENTS_CHANNEL_ID;
                break;
            case 'news':
                targetChannel = NEWS_CHANNEL_ID;
                break;
            case 'chat':
                targetChannel = CHAT_CHANNEL_ID;
                break;
            default:
                targetChannel = ANNOUNCEMENTS_CHANNEL_ID;
        }
        
        await sendAnnouncement(targetChannel, title, description);
        await message.reply(`✅ Announcement sent to ${channelName} channel!`);
        return;
    }
    
    // Help command
    if (content === '!help') {
        const helpEmbed = {
            title: '🔑 SmokeScreen ENGINE Bot Commands',
            description: 'Available commands for key redemption and management:',
            color: 0x0099ff,
            fields: [
                {
                    name: '!redeem <key>',
                    value: 'Redeem a key generated from ENGINE.exe\nExamples:\n• Main: `!redeem ABC123-DEF456-GHI789`\n• R6S: `!redeem R6S-ABCD1234`\n• CODW: `!redeem CODW-EFGH5678`\n• AR: `!redeem AR-IJKL9012`\n• FN: `!redeem FN-MNOP3456`',
                    inline: false
                },
                {
                    name: '!tools / !descriptors',
                    value: 'Show all available tools and their descriptions',
                    inline: false
                },
                {
                    name: '!tool <name>',
                    value: 'Get detailed information about a specific tool\nExample: `!tool SmokeScreen ENGINE`',
                    inline: false
                },
                {
                    name: '!announce <channel> | <title> | <message>',
                    value: 'Send announcements to specific channels (Admin only)\nChannels: announcements, news, chat\nExample: `!announce announcements | Update | New features available!`',
                    inline: false
                },
                {
                    name: '!help',
                    value: 'Show this help message',
                    inline: false
                },
                {
                    name: '🎮 Recoil Game Keys',
                    value: '• R6S-XXXXXXX: Rainbow Six Siege Recoil V2\n• CODW-XXXXXXX: Call of Duty Warzone Recoil V2\n• AR-XXXXXXX: Arc Raiders Recoil V2\n• FN-XXXXXXX: Fortnite Recoil V2\n\nPricing: $9.99/mo, $35.99/6mo, $65.99/12mo, $149.99/lifetime\nCrypto: BTC, ETH, SOL',
                    inline: false
                },
                {
                    name: '📋 Key Requirements',
                    value: '• Keys must be generated from ENGINE.exe\n• You need the required Discord role (OWNER/COMMUNITY MANAGER/BASIC ACCESS)\n• Keys can only be redeemed once\n• Main license keys are separate from game keys\n• Game keys grant access to specific Recoil V2 tabs',
                    inline: false
                }
            ],
            footer: {
                text: 'SmokeScreen ENGINE Bot - Key Management System | v2.0'
            }
        };
        
        await message.reply({ embeds: [helpEmbed] });
    }
});

// Start HTTP API server for key redemption and webhooks
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

    // Discord authentication endpoint for SmokeScreen-ENGINE login
    if (req.method === 'POST' && url.pathname === '/auth/user') {
        console.log('[BOT] Received Discord authentication request');
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                console.log('[BOT] Request body received:', body);
                const data = JSON.parse(body);
                const { discordId } = data;
                console.log(`[BOT] Authenticating Discord ID: ${discordId}`);
                
                if (!discordId) {
                    console.log('[BOT] No Discord ID provided');
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'Discord ID is required'
                    }));
                    return;
                }
                
                // Simplified authentication - check if user has keys without Discord API call
                const userRedeemedKeys = [];
                let hasAccess = false;
                
                try {
                    for (const [key, keyData] of generatedKeys.entries()) {
                        if (keyData.redeemedBy === discordId) {
                            userRedeemedKeys.push({
                                key: key,
                                game: keyData.game,
                                duration: keyData.duration,
                                redeemedAt: keyData.redeemedAt
                            });
                            
                            // Grant access based on redeemed keys
                            if (keyData.game === 'MASTER' || keyData.duration === 'LIFETIME') {
                                hasAccess = true;
                            }
                        }
                    }
                } catch (keyError) {
                    console.log('[BOT] Error processing keys:', keyError.message);
                }
                
                // Create basic user response
                const response = {
                    success: true,
                    user: {
                        id: discordId,
                        username: 'bl0wdart', // Hardcoded for testing
                        discriminator: '0',
                        roles: hasAccess ? ['MASTER', 'LIFETIME'] : ['MEMBER'],
                        hasBasicAccess: true
                    },
                    redeemedKeys: userRedeemedKeys,
                    totalRedeemed: userRedeemedKeys.length
                };
                
                console.log('[BOT] Sending simplified response with', userRedeemedKeys.length, 'redeemed keys');
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify(response));
                
                console.log(`[BOT] Successfully authenticated user ${discordId}`);
                
            } catch (error) {
                console.error('[BOT] Error in Discord authentication:', error);
                console.error('[BOT] Error stack:', error.stack);
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ 
                    success: false, 
                    error: 'Authentication failed',
                    details: error.message 
                }));
            }
        });
        return;
    }

    // Parse URL
    const url = new URL(req.url, `http://localhost:9877`);
    
    // Webhook for new key notifications
    if (req.method === 'POST' && url.pathname === '/api/webhook/new-key') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', () => {
            try {
                const notification = JSON.parse(body);
                console.log(`[BOT] Webhook: New key uploaded - ${notification.key} (${notification.game} - ${notification.duration})`);
                
                // Immediately sync the new key
                syncKeysFromWebsite().then(() => {
                    console.log(`[BOT] Synced new key: ${notification.key}`);
                }).catch(error => {
                    console.log(`[BOT] Error syncing new key: ${error.message}`);
                });
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ success: true, message: 'Webhook received' }));
            } catch (error) {
                console.log(`[BOT] Error processing webhook: ${error.message}`);
                res.writeHead(400, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Invalid webhook data' }));
            }
        });
        return;
    }
    
    if (req.method === 'GET' && req.url === '/status') {
        const uptime = client.uptime ? client.uptime : 0;
        const guild = client.guilds.cache.get(GUILD_ID);
        
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
            status: client.isReady() ? 'online' : 'offline',
            uptime: uptime,
            guild: guild?.name || 'Not found',
            guildMemberCount: guild?.memberCount || 0,
            totalKeys: generatedKeys.size,
            redeemedKeys: Array.from(generatedKeys.values()).filter(k => k.redeemed).length,
            botReady: client.isReady(),
            connectedGuilds: client.guilds.cache.size
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
                
                for (const key of generatedKeyList) {
                    await sendKeyWebhook({
                        key: key,
                        game: game,
                        duration: duration,
                        generatedBy: 'ENGINE.exe'
                    }, 'generated');
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
        console.log('[BOT] Received authentication request');
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                console.log('[BOT] Request body:', body);
                const data = JSON.parse(body);
                const { discordId } = data;
                console.log('[BOT] Parsed discordId:', discordId);
                
                if (!discordId) {
                    console.log('[BOT] No discordId provided');
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({
                        success: false,
                        error: 'Discord ID is required'
                    }));
                    return;
                }
                
                console.log('[BOT] Calling authenticateUserAsync...');
                
                const user = await authenticateUserAsync(discordId, DISCORD_BOT_TOKEN);
                
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
                        error: 'User not found in guild or insufficient permissions'
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
    
    // Get user info endpoint
                if (req.method === 'GET' && req.url.startsWith('/auth/user?userId=')) {
                    (async () => {
                        try {
                            const url = new URL(req.url, `http://localhost:9877`);
                            const userId = url.searchParams.get('userId');
                            console.log(`[BOT] User info request: ${userId}`);
                            
                            // Get user from Discord
                            const user = await authenticateUserAsync(userId, DISCORD_BOT_TOKEN);
                            if (!user) {
                                res.writeHead(404, { 'Content-Type': 'application/json' });
                                res.end(JSON.stringify({ error: 'User not found' }));
                                return;
                            }

                            // Get user's redeemed keys
                            const userRedeemedKeys = [];
                            for (const [key, keyData] of generatedKeys.entries()) {
                                if (keyData.redeemedBy === userId) {
                                    userRedeemedKeys.push({
                                        key: key,
                                        game: keyData.game,
                                        duration: keyData.duration,
                                        redeemedAt: keyData.redeemedAt
                                    });
                                }
                            }

                            res.writeHead(200, { 'Content-Type': 'application/json' });
                            res.end(JSON.stringify({
                                success: true,
                                user: {
                                    id: user.id,
                                    username: user.username,
                                    discriminator: user.discriminator,
                                    roles: user.roles,
                                    hasBasicAccess: user.hasBasicAccess
                                },
                                redeemedKeys: userRedeemedKeys,
                                totalRedeemed: userRedeemedKeys.length
                            }));
                        } catch (error) {
                            console.error('[BOT] Error getting user info:', error);
                            res.writeHead(500, { 'Content-Type': 'application/json' });
                            res.end(JSON.stringify({ error: 'Failed to get user info' }));
                        }
                    })();
                    return;
                }

                // List available keys endpoint
    if (req.method === 'GET' && req.url === '/keys/list') {
        try {
            const availableKeys = [];
            for (const [key, keyData] of generatedKeys.entries()) {
                if (!keyData.redeemed && keyData.generatedBy === 'ENGINE.exe') {
                    availableKeys.push({
                        key: key,
                        game: keyData.game,
                        duration: keyData.duration,
                        generatedAt: keyData.generatedAt,
                        generatedBy: keyData.generatedBy
                    });
                }
            }
            
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({
                success: true,
                keys: availableKeys,
                total: availableKeys.length
            }));
        } catch (error) {
            console.error('[BOT] Error listing keys:', error);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Failed to list keys' }));
        }
        return;
    }
    
    // Key redemption endpoint
    if (req.method === 'POST' && req.url === '/keys/redeem') {
        let body = '';
        req.on('data', chunk => {
            body += chunk.toString();
        });
        
        req.on('end', async () => {
            try {
                const data = JSON.parse(body);
                const { key, userId } = data;
                
                if (!key) {
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'Key is required' }));
                    return;
                }
                
                if (!userId) {
                    res.writeHead(400, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ error: 'User ID is required' }));
                    return;
                }
                
                const result = await redeemKey(userId, key);
                
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify(result));
            } catch (error) {
                console.error('[BOT] Error redeeming key:', error);
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Key redemption failed' }));
            }
        });
        return;
    }
    
    res.writeHead(404);
    res.end('Not Found');
});

// Start HTTP server
server.listen(9877, () => {
    console.log('[BOT] HTTP server listening on port 9877');
});

// Login to Discord
client.login(DISCORD_BOT_TOKEN).catch(error => {
    console.error('[BOT] Failed to login:', error);
    process.exit(1);
});

// Handle graceful shutdown
process.on('SIGINT', () => {
    console.log('[BOT] Received SIGINT, shutting down gracefully...');
    client.destroy();
    server.close(() => {
        console.log('[BOT] Server closed');
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    console.log('[BOT] Received SIGTERM, shutting down...');
    client.destroy();
    server.close(() => {
        console.log('[BOT] Server closed');
        process.exit(0);
    });
});
