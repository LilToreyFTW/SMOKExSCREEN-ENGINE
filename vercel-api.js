// Vercel API Routes for SmokeScreen ENGINE
// This should be deployed as api/index.js

const { createClient } = require('@supabase/supabase-js');
const crypto = require('crypto');

// Initialize Supabase client (you'll need to set these environment variables)
const supabaseUrl = process.env.SUPABASE_URL;
const supabaseKey = process.env.SUPABASE_ANON_KEY;
const supabase = createClient(supabaseUrl, supabaseKey);

// Discord Bot Webhook URL
const DISCORD_WEBHOOK_URL = process.env.DISCORD_WEBHOOK_URL;

// CORS headers
const corsHeaders = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
    'Access-Control-Allow-Methods': 'POST, GET, OPTIONS, PUT, DELETE'
};

// Handle CORS preflight requests
function handleCors(req, res) {
    if (req.method === 'OPTIONS') {
        res.writeHead(200, corsHeaders);
        res.end();
        return true;
    }
    return false;
}

// Main handler
export default async function handler(req, res) {
    // Handle CORS
    if (handleCors(req, res)) return;

    try {
        const url = new URL(req.url, `http://${req.headers.host}`);
        const path = url.pathname;

        console.log(`[API] ${req.method} ${path}`);

        switch (path) {
            case '/':
                return handleHome(req, res);
            case '/api/status':
                return handleStatus(req, res);
            case '/api/tsync':
                return handleTsync(req, res);
            case '/api/auth/discord/engine-callback':
                return handleDiscordCallback(req, res);
            case '/api/auth/me':
                return handleAuthMe(req, res);
            case '/api/keys/validate':
                return handleKeysValidate(req, res);
            case '/api/admin/keys/add':
                return handleAdminKeysAdd(req, res);
            default:
                res.writeHead(404, { 'Content-Type': 'application/json', ...corsHeaders });
                res.end(JSON.stringify({ error: 'Endpoint not found' }));
        }
    } catch (error) {
        console.error('[API] Error:', error);
        res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Internal server error' }));
    }
}

// Home page - serve the main HTML
async function handleHome(req, res) {
    if (req.method === 'GET') {
        res.writeHead(200, { 'Content-Type': 'text/html', ...corsHeaders });
        res.end(getHomePageHTML());
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Status endpoint
async function handleStatus(req, res) {
    if (req.method === 'GET') {
        const status = {
            cloudStatus: 'Active',
            activeUsers: 1240,
            revenue: '$12,450.00',
            serverLoad: '24%',
            uptime: process.env.uptime || 0,
            timestamp: new Date().toISOString(),
            version: '2.0.1',
            features: {
                hardStackSecurity: true,
                discordAuthentication: true,
                keySystem: true,
                multiGameSupport: true,
                webhooks: true
            }
        };

        res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify(status));
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Tsync endpoint - key synchronization
async function handleTsync(req, res) {
    if (req.method === 'POST') {
        try {
            const body = JSON.parse(req.body);
            const { key, keys } = body;

            // Validate sync key
            if (key !== 'tsasync-key-2025-02-27-a7f3c9e1') {
                res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
                res.end(JSON.stringify({ error: 'Invalid sync key' }));
                return;
            }

            // Process keys
            const processedKeys = [];
            for (const keyData of keys) {
                const { key_value, duration_type, duration_ms } = keyData;
                
                // Store in database
                const { data, error } = await supabase
                    .from('keys')
                    .insert([{
                        key: key_value,
                        duration_type,
                        duration_ms,
                        created_at: new Date().toISOString(),
                        source: 'ENGINE.exe'
                    }])
                    .select();

                if (error) {
                    console.error('[API] Database error:', error);
                    continue;
                }

                processedKeys.push(key_value);
            }

            // Send webhook notification
            if (DISCORD_WEBHOOK_URL && processedKeys.length > 0) {
                await sendWebhook({
                    title: '🔑 Keys Synchronized',
                    description: `${processedKeys.length} keys synchronized from ENGINE.exe`,
                    color: 0x00ff00,
                    fields: [
                        { name: 'Keys Processed', value: processedKeys.length.toString(), inline: true },
                        { name: 'Source', value: 'ENGINE.exe', inline: true },
                        { name: 'Timestamp', value: new Date().toISOString(), inline: false }
                    ]
                });
            }

            res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ 
                success: true, 
                keysProcessed: processedKeys.length,
                keys: processedKeys 
            }));
        } catch (error) {
            console.error('[API] Tsync error:', error);
            res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Failed to sync keys' }));
        }
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Discord OAuth callback
async function handleDiscordCallback(req, res) {
    if (req.method === 'POST') {
        try {
            const body = JSON.parse(req.body);
            const { code, redirect_uri } = body;

            // Exchange code for access token
            const tokenResponse = await fetch('https://discord.com/api/oauth2/token', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: new URLSearchParams({
                    client_id: process.env.DISCORD_CLIENT_ID,
                    client_secret: process.env.DISCORD_CLIENT_SECRET,
                    grant_type: 'authorization_code',
                    code,
                    redirect_uri
                })
            });

            const tokenData = await tokenResponse.json();

            if (tokenData.error) {
                res.writeHead(400, { 'Content-Type': 'application/json', ...corsHeaders });
                res.end(JSON.stringify({ error: tokenData.error }));
                return;
            }

            // Get user info
            const userResponse = await fetch('https://discord.com/api/users/@me', {
                headers: {
                    'Authorization': `Bearer ${tokenData.access_token}`
                }
            });

            const userData = await userResponse.json();

            // Create session token
            const sessionToken = crypto.randomUUID();
            
            // Store session (you might want to use Redis or database for this)
            const { data, error } = await supabase
                .from('sessions')
                .insert([{
                    token: sessionToken,
                    user_id: userData.id,
                    username: userData.username,
                    discriminator: userData.discriminator,
                    avatar: userData.avatar,
                    created_at: new Date().toISOString(),
                    expires_at: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString() // 24 hours
                }])
                .select();

            if (error) {
                console.error('[API] Session storage error:', error);
                res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
                res.end(JSON.stringify({ error: 'Failed to create session' }));
                return;
            }

            res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({
                sessionToken,
                user: {
                    id: userData.id,
                    username: userData.username,
                    discriminator: userData.discriminator,
                    avatar: userData.avatar,
                    discord_id: userData.id,
                    discord_username: userData.username
                }
            }));
        } catch (error) {
            console.error('[API] Discord callback error:', error);
            res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Authentication failed' }));
        }
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Get current user info
async function handleAuthMe(req, res) {
    if (req.method === 'GET') {
        const authHeader = req.headers.authorization;
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'No token provided' }));
            return;
        }

        const token = authHeader.substring(7);

        // Validate session
        const { data, error } = await supabase
            .from('sessions')
            .select('*')
            .eq('token', token)
            .eq('expires_at', 'gt', new Date().toISOString())
            .single();

        if (error || !data) {
            res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Invalid or expired session' }));
            return;
        }

        res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({
            user: {
                id: data.user_id,
                username: data.username,
                discriminator: data.discriminator,
                avatar: data.avatar,
                discord_id: data.user_id,
                discord_username: data.username
            }
        }));
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Validate keys
async function handleKeysValidate(req, res) {
    if (req.method === 'GET') {
        const authHeader = req.headers.authorization;
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'No token provided' }));
            return;
        }

        const token = authHeader.substring(7);

        // Validate session
        const { data: session, error: sessionError } = await supabase
            .from('sessions')
            .select('*')
            .eq('token', token)
            .eq('expires_at', 'gt', new Date().toISOString())
            .single();

        if (sessionError || !session) {
            res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Invalid or expired session' }));
            return;
        }

        // Get user's redeemed keys
        const { data: keys, error: keysError } = await supabase
            .from('key_redemptions')
            .select('*')
            .eq('user_id', session.user_id)
            .eq('active', true);

        if (keysError) {
            console.error('[API] Keys validation error:', keysError);
            res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Failed to validate keys' }));
            return;
        }

        const hasActiveKey = keys && keys.length > 0;
        const activeKey = hasActiveKey ? keys[0] : null;

        res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({
            licensed: hasActiveKey,
            active: hasActiveKey,
            duration_type: activeKey?.duration_type || null,
            duration_label: activeKey?.duration_label || null,
            expires_at: activeKey?.expires_at || null,
            ms_remaining: activeKey ? Math.max(0, new Date(activeKey.expires_at).getTime() - Date.now()) : null,
            days_remaining: activeKey ? Math.max(0, Math.ceil((new Date(activeKey.expires_at).getTime() - Date.now()) / (1000 * 60 * 60 * 24))) : null,
            key_count: keys?.length || 0
        }));
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Admin keys add endpoint
async function handleAdminKeysAdd(req, res) {
    if (req.method === 'POST') {
        try {
            const body = JSON.parse(req.body);
            const { adminSecret, keys } = body;

            // Validate admin secret
            if (adminSecret !== process.env.ADMIN_SECRET) {
                res.writeHead(401, { 'Content-Type': 'application/json', ...corsHeaders });
                res.end(JSON.stringify({ error: 'Invalid admin secret' }));
                return;
            }

            const addedKeys = [];
            for (const key of keys) {
                const { data, error } = await supabase
                    .from('keys')
                    .insert([{
                        key: key,
                        created_at: new Date().toISOString(),
                        source: 'admin'
                    }])
                    .select();

                if (!error) {
                    addedKeys.push(key);
                }
            }

            res.writeHead(200, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ 
                success: true, 
                keysAdded: addedKeys.length 
            }));
        } catch (error) {
            console.error('[API] Admin keys add error:', error);
            res.writeHead(500, { 'Content-Type': 'application/json', ...corsHeaders });
            res.end(JSON.stringify({ error: 'Failed to add keys' }));
        }
    } else {
        res.writeHead(405, { 'Content-Type': 'application/json', ...corsHeaders });
        res.end(JSON.stringify({ error: 'Method not allowed' }));
    }
}

// Send webhook notification
async function sendWebhook(embed) {
    if (!DISCORD_WEBHOOK_URL) return;

    try {
        await fetch(DISCORD_WEBHOOK_URL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                embeds: [embed]
            })
        });
    } catch (error) {
        console.error('[API] Webhook error:', error);
    }
}

// HTML content for home page
function getHomePageHTML() {
    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SmokeScreen ENGINE - Advanced Recoil Control System</title>
    <meta name="description" content="Professional recoil control system with Discord authentication, key management, and advanced gaming features">
    <link href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@400;700;900&family=Inter:wght@300;400;600;700&display=swap');
        
        body {
            font-family: 'Inter', sans-serif;
            background: linear-gradient(135deg, #0f0f23 0%, #1a1a3e 50%, #0f0f23 100%);
            min-height: 100vh;
        }
        
        .orbitron {
            font-family: 'Orbitron', monospace;
        }
        
        .glow-text {
            text-shadow: 0 0 20px rgba(59, 130, 246, 0.8);
        }
        
        .card-glow {
            box-shadow: 0 0 30px rgba(59, 130, 246, 0.3);
            border: 1px solid rgba(59, 130, 246, 0.5);
        }
        
        .btn-glow {
            box-shadow: 0 0 20px rgba(59, 130, 246, 0.5);
            transition: all 0.3s ease;
        }
        
        .btn-glow:hover {
            box-shadow: 0 0 30px rgba(59, 130, 246, 0.8);
            transform: translateY(-2px);
        }
        
        .feature-card {
            background: rgba(30, 30, 60, 0.8);
            backdrop-filter: blur(10px);
            border: 1px solid rgba(59, 130, 246, 0.3);
            transition: all 0.3s ease;
        }
        
        .feature-card:hover {
            transform: translateY(-5px);
            border-color: rgba(59, 130, 246, 0.6);
            box-shadow: 0 10px 40px rgba(59, 130, 246, 0.4);
        }
        
        .discord-badge {
            background: linear-gradient(135deg, #5865F2 0%, #7289DA 100%);
        }
        
        .hard-stack-badge {
            background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        }
        
        .pulse-animation {
            animation: pulse 2s infinite;
        }
        
        @keyframes pulse {
            0% { opacity: 1; }
            50% { opacity: 0.7; }
            100% { opacity: 1; }
        }
        
        .gradient-border {
            background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
            padding: 2px;
            border-radius: 12px;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1.5rem;
        }
        
        .stat-card {
            background: rgba(30, 30, 60, 0.9);
            border: 1px solid rgba(59, 130, 246, 0.3);
            border-radius: 12px;
            padding: 1.5rem;
            text-align: center;
        }
        
        .key-features {
            background: linear-gradient(135deg, rgba(59, 130, 246, 0.1) 0%, rgba(139, 92, 246, 0.1) 100%);
            border: 1px solid rgba(59, 130, 246, 0.3);
            border-radius: 12px;
            padding: 2rem;
        }
    </style>
</head>
<body class="text-white">
    <!-- Navigation -->
    <nav class="fixed top-0 w-full bg-black/80 backdrop-blur-lg border-b border-blue-500/30 z-50">
        <div class="container mx-auto px-4 py-4">
            <div class="flex justify-between items-center">
                <div class="flex items-center space-x-3">
                    <div class="w-10 h-10 bg-gradient-to-r from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-fire text-white"></i>
                    </div>
                    <h1 class="orbitron text-2xl font-bold glow-text">SmokeScreen ENGINE</h1>
                </div>
                <div class="hidden md:flex space-x-6">
                    <a href="#features" class="hover:text-blue-400 transition">Features</a>
                    <a href="#authentication" class="hover:text-blue-400 transition">Authentication</a>
                    <a href="#keys" class="hover:text-blue-400 transition">Key System</a>
                    <a href="#download" class="hover:text-blue-400 transition">Download</a>
                </div>
            </div>
        </div>
    </nav>

    <!-- Hero Section -->
    <section class="min-h-screen flex items-center justify-center px-4 pt-20">
        <div class="text-center max-w-4xl">
            <div class="mb-8">
                <span class="hard-stack-badge px-4 py-2 rounded-full text-sm font-semibold pulse-animation">
                    <i class="fas fa-shield-alt mr-2"></i>HARD STACK SECURITY
                </span>
            </div>
            <h1 class="orbitron text-5xl md:text-7xl font-bold mb-6 glow-text">
                SmokeScreen ENGINE
            </h1>
            <p class="text-xl md:text-2xl text-gray-300 mb-8">
                Advanced Recoil Control System with Discord Authentication
            </p>
            <p class="text-lg text-gray-400 mb-12 max-w-2xl mx-auto">
                Professional recoil management with HARD STACK security. Only authenticated Discord users can access premium features.
            </p>
            
            <div class="flex flex-col sm:flex-row gap-4 justify-center mb-12">
                <a href="#download" class="btn-glow bg-blue-600 hover:bg-blue-700 px-8 py-4 rounded-lg font-semibold transition">
                    <i class="fas fa-download mr-2"></i>Download Now
                </a>
                <a href="#authentication" class="btn-glow bg-purple-600 hover:bg-purple-700 px-8 py-4 rounded-lg font-semibold transition">
                    <i class="fas fa-discord mr-2"></i>Discord Login
                </a>
            </div>

            <!-- Live Stats -->
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="text-3xl font-bold text-blue-400">1,240+</div>
                    <div class="text-gray-400">Active Users</div>
                </div>
                <div class="stat-card">
                    <div class="text-3xl font-bold text-green-400">99.9%</div>
                    <div class="text-gray-400">Uptime</div>
                </div>
                <div class="stat-card">
                    <div class="text-3xl font-bold text-purple-400">43</div>
                    <div class="text-gray-400">Keys Generated</div>
                </div>
                <div class="stat-card">
                    <div class="text-3xl font-bold text-yellow-400">6</div>
                    <div class="text-gray-400">Keys Redeemed</div>
                </div>
            </div>
        </div>
    </section>

    <!-- Features Section -->
    <section id="features" class="py-20 px-4">
        <div class="container mx-auto">
            <h2 class="orbitron text-4xl font-bold text-center mb-16 glow-text">Advanced Features</h2>
            
            <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-blue-400 text-3xl mb-4">
                        <i class="fas fa-crosshairs"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">Precision Recoil Control</h3>
                    <p class="text-gray-400">Advanced algorithms for perfect recoil management across multiple games.</p>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-purple-400 text-3xl mb-4">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">HARD STACK Security</h3>
                    <p class="text-gray-400">Multi-layer authentication with Discord guild validation and role-based access.</p>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-green-400 text-3xl mb-4">
                        <i class="fas fa-key"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">Secure Key System</h3>
                    <p class="text-gray-400">Only keys generated by ENGINE.exe can be redeemed with complete audit trail.</p>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-yellow-400 text-3xl mb-4">
                        <i class="fas fa-gamepad"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">Multi-Game Support</h3>
                    <p class="text-gray-400">Support for Rainbow Six Siege, COD Warzone, Apex Legends, and Fortnite.</p>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-red-400 text-3xl mb-4">
                        <i class="fas fa-chart-line"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">Real-time Analytics</h3>
                    <p class="text-gray-400">Live statistics and performance monitoring for optimal gaming experience.</p>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-indigo-400 text-3xl mb-4">
                        <i class="fas fa-sync"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">Cloud Sync</h3>
                    <p class="text-gray-400">Automatic synchronization across devices with secure cloud storage.</p>
                </div>
            </div>
        </div>
    </section>

    <!-- HARD STACK Authentication Section -->
    <section id="authentication" class="py-20 px-4">
        <div class="container mx-auto max-w-4xl">
            <h2 class="orbitron text-4xl font-bold text-center mb-16 glow-text">HARD STACK Authentication</h2>
            
            <div class="key-features">
                <div class="grid md:grid-cols-2 gap-8 mb-12">
                    <div>
                        <h3 class="text-2xl font-semibold mb-6 text-blue-400">
                            <i class="fas fa-discord mr-2"></i>Discord Integration
                        </h3>
                        <ul class="space-y-3 text-gray-300">
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Guild-only access (1455221314653786207)</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Role-based permissions</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Real-time user validation</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Secure OAuth2 authentication</li>
                        </ul>
                    </div>
                    
                    <div>
                        <h3 class="text-2xl font-semibold mb-6 text-purple-400">
                            <i class="fas fa-lock mr-2"></i>Security Features
                        </h3>
                        <ul class="space-y-3 text-gray-300">
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Multi-layer validation</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Audit trail logging</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Engine.exe key verification</li>
                            <li><i class="fas fa-check text-green-400 mr-2"></i>Automatic threat detection</li>
                        </ul>
                    </div>
                </div>
                
                <div class="text-center">
                    <div class="gradient-border inline-block rounded-lg p-1">
                        <div class="bg-gray-900 rounded-lg p-6">
                            <h4 class="text-xl font-semibold mb-4">Authentication Flow</h4>
                            <div class="flex flex-wrap justify-center gap-4 text-sm">
                                <span class="bg-blue-600/20 border border-blue-500/50 px-3 py-1 rounded">Discord ID</span>
                                <i class="fas fa-arrow-right text-blue-400 self-center"></i>
                                <span class="bg-purple-600/20 border border-purple-500/50 px-3 py-1 rounded">Guild Validation</span>
                                <i class="fas fa-arrow-right text-purple-400 self-center"></i>
                                <span class="bg-green-600/20 border border-green-500/50 px-3 py-1 rounded">Role Check</span>
                                <i class="fas fa-arrow-right text-green-400 self-center"></i>
                                <span class="bg-yellow-600/20 border border-yellow-500/50 px-3 py-1 rounded">Access Granted</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- Key System Section -->
    <section id="keys" class="py-20 px-4">
        <div class="container mx-auto max-w-4xl">
            <h2 class="orbitron text-4xl font-bold text-center mb-16 glow-text">Secure Key System</h2>
            
            <div class="grid md:grid-cols-2 gap-8">
                <div class="feature-card p-6 rounded-lg">
                    <h3 class="text-xl font-semibold mb-4 text-green-400">
                        <i class="fas fa-shield-alt mr-2"></i>Engine.exe Only Keys
                    </h3>
                    <p class="text-gray-300 mb-4">
                        Only keys generated by ENGINE.exe are accepted for redemption. Each key is cryptographically signed and validated.
                    </p>
                    <div class="bg-green-600/20 border border-green-500/50 rounded-lg p-4">
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check-circle mr-2"></i>Source validation
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check-circle mr-2"></i>Audit trail
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check-circle mr-2"></i>Anti-tampering
                        </div>
                    </div>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <h3 class="text-xl font-semibold mb-4 text-blue-400">
                        <i class="fas fa-key mr-2"></i>Key Redemption
                    </h3>
                    <p class="text-gray-300 mb-4">
                        Secure key redemption with multi-factor validation. Keys are single-use and permanently tracked.
                    </p>
                    <div class="bg-blue-600/20 border border-blue-500/50 rounded-lg p-4">
                        <div class="text-sm text-blue-400">
                            <i class="fas fa-check-circle mr-2"></i>One-time use
                        </div>
                        <div class="text-sm text-blue-400">
                            <i class="fas fa-check-circle mr-2"></i>User binding
                        </div>
                        <div class="text-sm text-blue-400">
                            <i class="fas fa-check-circle mr-2"></i>Expiration control
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="mt-12 text-center">
                <div class="inline-flex items-center space-x-4 bg-gray-800 rounded-lg p-4">
                    <div class="text-left">
                        <div class="text-sm text-gray-400">Current Status</div>
                        <div class="text-lg font-semibold text-green-400">
                            <i class="fas fa-circle text-green-400 text-xs mr-2 pulse-animation"></i>
                            System Online
                        </div>
                    </div>
                    <div class="text-left">
                        <div class="text-sm text-gray-400">Keys Available</div>
                        <div class="text-lg font-semibold text-blue-400">37 Active</div>
                    </div>
                    <div class="text-left">
                        <div class="text-sm text-gray-400">Redeemed</div>
                        <div class="text-lg font-semibold text-purple-400">6 Total</div>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <!-- Download Section -->
    <section id="download" class="py-20 px-4">
        <div class="container mx-auto text-center">
            <h2 class="orbitron text-4xl font-bold mb-8 glow-text">Download SmokeScreen ENGINE</h2>
            <p class="text-xl text-gray-300 mb-12 max-w-2xl mx-auto">
                Get the latest version of SmokeScreen ENGINE with HARD STACK security and advanced recoil control.
            </p>
            
            <div class="grid md:grid-cols-2 gap-8 max-w-4xl mx-auto">
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-blue-400 text-3xl mb-4">
                        <i class="fas fa-windows"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">SmokeScreen-ENGINE.exe</h3>
                    <p class="text-gray-400 mb-4">Main application with Discord authentication and key management</p>
                    <div class="space-y-2">
                        <div class="text-sm text-gray-500">Version 2.0.1</div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>HARD STACK Security
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>Discord Integration
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>Multi-Game Support
                        </div>
                    </div>
                </div>
                
                <div class="feature-card p-6 rounded-lg">
                    <div class="text-purple-400 text-3xl mb-4">
                        <i class="fas fa-terminal"></i>
                    </div>
                    <h3 class="text-xl font-semibold mb-3">ENGINE.exe Console</h3>
                    <p class="text-gray-400 mb-4">Console version for key generation and system management</p>
                    <div class="space-y-2">
                        <div class="text-sm text-gray-500">Version 2.0.1</div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>Key Generation
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>Batch Operations
                        </div>
                        <div class="text-sm text-green-400">
                            <i class="fas fa-check mr-1"></i>Webhook Integration
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="mt-12">
                <p class="text-gray-400 mb-4">
                    <i class="fas fa-info-circle mr-2"></i>
                    Download links available after Discord authentication
                </p>
                <div class="flex flex-col sm:flex-row gap-4 justify-center">
                    <button class="btn-glow bg-blue-600 hover:bg-blue-700 px-8 py-4 rounded-lg font-semibold transition">
                        <i class="fas fa-download mr-2"></i>Download SmokeScreen-ENGINE.exe
                    </button>
                    <button class="btn-glow bg-purple-600 hover:bg-purple-700 px-8 py-4 rounded-lg font-semibold transition">
                        <i class="fas fa-terminal mr-2"></i>Download ENGINE.exe Console
                    </button>
                </div>
            </div>
        </div>
    </section>

    <!-- Footer -->
    <footer class="bg-black/80 border-t border-blue-500/30 py-12 px-4">
        <div class="container mx-auto">
            <div class="grid md:grid-cols-4 gap-8">
                <div>
                    <div class="flex items-center space-x-3 mb-4">
                        <div class="w-8 h-8 bg-gradient-to-r from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
                            <i class="fas fa-fire text-white text-sm"></i>
                        </div>
                        <h3 class="orbitron font-bold">SmokeScreen ENGINE</h3>
                    </div>
                    <p class="text-gray-400 text-sm">
                        Advanced recoil control system with HARD STACK security
                    </p>
                </div>
                
                <div>
                    <h4 class="font-semibold mb-4">Features</h4>
                    <ul class="space-y-2 text-gray-400 text-sm">
                        <li><a href="#" class="hover:text-blue-400">Recoil Control</a></li>
                        <li><a href="#" class="hover:text-blue-400">Discord Auth</a></li>
                        <li><a href="#" class="hover:text-blue-400">Key System</a></li>
                        <li><a href="#" class="hover:text-blue-400">Multi-Game</a></li>
                    </ul>
                </div>
                
                <div>
                    <h4 class="font-semibold mb-4">Support</h4>
                    <ul class="space-y-2 text-gray-400 text-sm">
                        <li><a href="#" class="hover:text-blue-400">Documentation</a></li>
                        <li><a href="#" class="hover:text-blue-400">Discord Server</a></li>
                        <li><a href="#" class="hover:text-blue-400">Troubleshooting</a></li>
                        <li><a href="#" class="hover:text-blue-400">Contact</a></li>
                    </ul>
                </div>
                
                <div>
                    <h4 class="font-semibold mb-4">Legal</h4>
                    <ul class="space-y-2 text-gray-400 text-sm">
                        <li><a href="#" class="hover:text-blue-400">Privacy Policy</a></li>
                        <li><a href="#" class="hover:text-blue-400">Terms of Service</a></li>
                        <li><a href="#" class="hover:text-blue-400">License</a></li>
                        <li><a href="#" class="hover:text-blue-400">Security</a></li>
                    </ul>
                </div>
            </div>
            
            <div class="border-t border-gray-800 mt-8 pt-8 text-center">
                <p class="text-gray-400 text-sm">
                    © 2026 SmokeScreen ENGINE. All rights reserved. | 
                    <span class="text-green-400">
                        <i class="fas fa-circle text-xs mr-1"></i>System Online
                    </span>
                </p>
            </div>
        </div>
    </footer>

    <script>
        // Smooth scrolling for navigation links
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                e.preventDefault();
                document.querySelector(this.getAttribute('href')).scrollIntoView({
                    behavior: 'smooth'
                });
            });
        });

        // Add scroll effect to navigation
        window.addEventListener('scroll', function() {
            const nav = document.querySelector('nav');
            if (window.scrollY > 50) {
                nav.classList.add('bg-black/90');
            } else {
                nav.classList.remove('bg-black/90');
            }
        });

        // Animate stats on scroll
        const observerOptions = {
            threshold: 0.5,
            rootMargin: '0px'
        };

        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        document.querySelectorAll('.stat-card').forEach(card => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';
            card.style.transition = 'all 0.6s ease';
            observer.observe(card);
        });
    </script>
</body>
</html>`;
}
