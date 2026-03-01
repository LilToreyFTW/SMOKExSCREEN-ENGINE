const { Client, GatewayIntentBits, EmbedBuilder, PermissionFlagsBits } = require('discord.js');
const fs = require('fs');
const path = require('path');

const BOT_TOKEN = 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.Gz9yFk.W8CFCJka03tp3MoFAwYj_f5dXpIaLHojAQVIYw';
const GUILD_ID = '1455221314653786207';
const CATEGORY_ID = '1477430674318299251';
const ROLE_ID = '1477448046873935872'; // Smokescreen-discord-access
const CHAT_CHANNEL_ID = '1477430872021008404';
const NEW_CHANNEL_ID = '1477430901762691259';
const ANNOUNCEMENTS_CHANNEL_ID = '1477430949124767987';
const TOOL_DESCRIPTORS_CHANNEL_ID = '1477431072164941874';
const KEYS_IN_STOCK_CHANNEL_ID = '1477453855485853870';

const SYNC_FILE_PATH = path.join(__dirname, 'ASyncToreyDev.sync');
const ENGINE_EXE_PATH = path.join(__dirname, 'ENGINE', 'bin', 'Release', 'net8.0-windows', 'SmokeScreenEngine.exe');
const WEBSITE_PATH = path.join(__dirname, 'SmokeScreen-ENGINE');

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
    GatewayIntentBits.GuildMembers,
    GatewayIntentBits.DirectMessages,
  ],
});

let lastKnownVersion = null;
let lastKnownTabs = [];
let lastKnownFunctions = [];
let lastKnownWebsiteBuild = null;
let lastKnownGuiBuild = null;
let systemHealth = {
  engine: { status: 'unknown', lastSeen: null },
  website: { status: 'unknown', lastSeen: null },
  gui: { status: 'unknown', lastSeen: null },
  bot: { status: 'running', lastSeen: Date.now() }
};

// Key stock tracking
let keyStock = new Map(); // durationType -> count

function readSyncFile() {
  try {
    const data = fs.readFileSync(SYNC_FILE_PATH, 'utf8');
    return JSON.parse(data);
  } catch {
    return { version: '1.0.0', lastSync: 0, keys: [], updates: [], announcements: [] };
  }
}

function writeSyncFile(data) {
  fs.writeFileSync(SYNC_FILE_PATH, JSON.stringify(data, null, 2));
}

function getEngineVersion() {
  try {
    const stats = fs.statSync(ENGINE_EXE_PATH);
    return stats.mtime.toISOString();
  } catch {
    return null;
  }
}

function getWebsiteBuildTime() {
  try {
    const pkgPath = path.join(WEBSITE_PATH, 'package.json');
    const stats = fs.statSync(pkgPath);
    return stats.mtime.toISOString();
  } catch {
    return null;
  }
}

function getGuiBuildTime() {
  try {
    const projPath = path.join(__dirname, 'ENGINE', 'SmokeScreenEngineGUI.csproj');
    const stats = fs.statSync(projPath);
    return stats.mtime.toISOString();
  } catch {
    return null;
  }
}

function detectNewTabsOrFeatures() {
  const currentTabs = ['ACCOUNT', 'LICENSE', 'ENGINE', 'TOOLS'];
  const currentFunctions = ['ClerkAuth', 'MsPingStatus', 'KeyCache', 'TSyncListener', 'KeySender', 'KeyGenerator', 'KeyExtension'];
  return { tabs: currentTabs, functions: currentFunctions };
}

function createAnnouncementEmbed(title, description, fields = []) {
  return new EmbedBuilder()
    .setColor('#ff3d00')
    .setTitle(title)
    .setDescription(description)
    .addFields(fields)
    .setTimestamp()
    .setFooter({ text: 'SmokeScreen ENGINE — Auto-Bot' });
}

async function postAnnouncement(channelId, title, description, fields = []) {
  try {
    const channel = await client.channels.fetch(channelId);
    if (!channel) return;
    await channel.send({ embeds: [createAnnouncementEmbed(title, description, fields)] });
    console.log(`Posted to ${channelId}: ${title}`);
  } catch (err) {
    console.error(`Failed to post to ${channelId}:`, err);
  }
}

// Assign role if user interacts in the category
async function ensureRole(member) {
  if (!member) return;
  const guild = await client.guilds.fetch(GUILD_ID);
  const role = await guild.roles.fetch(ROLE_ID);
  if (!role) return;
  if (!member.roles.cache.has(ROLE_ID)) {
    try {
      await member.roles.add(role, 'Auto-assigned for Smokescreen category interaction');
      console.log(`Assigned role to ${member.user.tag}`);
    } catch (err) {
      console.error(`Failed to assign role to ${member.user.tag}:`, err);
    }
  }
}

// Update key stock display
async function updateKeyStockDisplay() {
  const fields = Array.from(keyStock.entries()).map(([type, count]) => ({
    name: type.replace('_', ' ').toUpperCase(),
    value: `${count} keys`,
    inline: true
  }));
  const embed = new EmbedBuilder()
    .setColor('#ff3d00')
    .setTitle('🔑 Keys in Stock')
    .addFields(...fields)
    .setTimestamp()
    .setFooter({ text: 'Auto-updated by ENGINE.exe' });
  try {
    const channel = await client.channels.fetch(KEYS_IN_STOCK_CHANNEL_ID);
    if (!channel) return;
    const messages = await channel.messages.fetch({ limit: 10 });
    const botMsg = messages.find(m => m.author.id === client.user.id && m.embeds.length > 0 && m.embeds[0].title === '🔑 Keys in Stock');
    if (botMsg) {
      await botMsg.edit({ embeds: [embed] });
    } else {
      await channel.send({ embeds: [embed] });
    }
  } catch (err) {
    console.error('Failed to update key stock display:', err);
  }
}

// Redeem queue
const redeemQueue = new Map();

client.on('messageCreate', async (message) => {
  if (message.author.bot) return;
  if (!message.guild) return;
  if (message.guild.id !== GUILD_ID) return;

  // Auto-assign role for any message in the category
  if (message.channel.parentId === CATEGORY_ID) {
    await ensureRole(message.member);
  }

  const content = message.content.trim();
  const prefix = '!ss';
  if (!content.startsWith(prefix)) return;

  const args = content.slice(prefix.length).trim().split(/\s+/);
  const cmd = args.shift()?.toLowerCase();

  if (cmd === 'redeem' && args.length) {
    const key = args[0];
    if (!key) {
      await message.reply('Usage: `!ss redeem <license_key>`');
      return;
    }
    redeemQueue.set(key, { userId: message.author.id, timestamp: Date.now() });
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('🔑 License Key Submitted')
      .setDescription(`Key \`${key}\` submitted for approval.`)
      .addFields(
        { name: 'Submitted by', value: `<@${message.author.id}>`, inline: true },
        { name: 'Time', value: new Date().toISOString(), inline: true }
      )
      .setFooter({ text: 'Admins: use !ss approve <key> or !ss deny <key>' });
    await message.channel.send({ embeds: [embed] });
    return;
  }

  if (cmd === 'approve' && args.length) {
    const key = args[0];
    const req = redeemQueue.get(key);
    if (!req) {
      await message.reply('No pending redeem request for that key.');
      return;
    }
    try {
      const res = await fetch('https://smok-ex-screen-engine.vercel.app/api/keys/redeem-clerk', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ key, hwid: `discord-${req.userId}` })
      });
      const result = await res.json();
      if (result.ok) {
        const embed = new EmbedBuilder()
          .setColor('#00ff88')
          .setTitle('✅ License Key Approved')
          .setDescription(`Key \`${key}\` approved and redeemed.`)
          .addFields(
            { name: 'User', value: `<@${req.userId}>`, inline: true },
            { name: 'HWID', value: `discord-${req.userId}`, inline: true }
          );
        await message.channel.send({ embeds: [embed] });
        redeemQueue.delete(key);
      } else {
        await message.reply(`Redeem failed: ${result.message || 'Unknown error'}`);
      }
    } catch (err) {
      await message.reply(`Error contacting API: ${err.message}`);
    }
    return;
  }

  if (cmd === 'deny' && args.length) {
    const key = args[0];
    const req = redeemQueue.get(key);
    if (!req) {
      await message.reply('No pending redeem request for that key.');
      return;
    }
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('❌ License Key Denied')
      .setDescription(`Key \`${key}\` denied.`)
      .addFields(
        { name: 'User', value: `<@${req.userId}>`, inline: true },
        { name: 'Reason', value: 'Denied by admin', inline: true }
      );
    await message.channel.send({ embeds: [embed] });
    redeemQueue.delete(key);
    return;
  }

  if (cmd === 'queue') {
    if (redeemQueue.size === 0) {
      await message.reply('No pending redeem requests.');
      return;
    }
    const fields = Array.from(redeemQueue.entries()).map(([k, v]) => ({
      name: k,
      value: `<@${v.userId}> — ${new Date(v.timestamp).toLocaleString()}`,
      inline: false
    }));
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('📋 Pending Redeem Requests')
      .addFields(fields)
      .setFooter({ text: 'Use !ss approve <key> or !ss deny <key>' });
    await message.channel.send({ embeds: [embed] });
    return;
  }

  if (cmd === 'status') {
    const now = Date.now();
    const fields = [
      { name: '🤖 Bot', value: `Running (last seen ${new Date(systemHealth.bot.lastSeen).toLocaleString()})`, inline: false },
      { name: '⚙️ ENGINE.exe', value: `${systemHealth.engine.status} (last seen ${systemHealth.engine.lastSeen ? new Date(systemHealth.engine.lastSeen).toLocaleString() : 'never'})`, inline: false },
      { name: '🌐 Website', value: `${systemHealth.website.status} (last seen ${systemHealth.website.lastSeen ? new Date(systemHealth.website.lastSeen).toLocaleString() : 'never'})`, inline: false },
      { name: '🖥️ GUI', value: `${systemHealth.gui.status} (last seen ${systemHealth.gui.lastSeen ? new Date(systemHealth.gui.lastSeen).toLocaleString() : 'never'})`, inline: false }
    ];
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('📊 System Health')
      .addFields(fields)
      .setFooter({ text: 'Last updated: ' + new Date(now).toLocaleString() });
    await message.channel.send({ embeds: [embed] });
    return;
  }

  if (cmd === 'help') {
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('🛠️ SmokeScreen ENGINE Bot Commands')
      .addFields(
        { name: '!ss redeem <key>', value: 'Submit a license key for admin approval.' },
        { name: '!ss approve <key>', value: '(Admin) Approve and redeem a submitted key.' },
        { name: '!ss deny <key>', value: '(Admin) Deny a submitted key.' },
        { name: '!ss queue', value: 'Show pending redeem requests.' },
        { name: '!ss status', value: 'Show system health and last seen times.' },
        { name: '!ss help', value: 'Show this help.' },
        { name: '🔐 Auto-Role', value: 'Interacting in the Smokescreen category auto-assigns the Smokescreen-discord-access role.' }
      );
    await message.channel.send({ embeds: [embed] });
  }
});

// Listen for key updates from ENGINE via /api/tsync
client.on('ready', () => {
  console.log(`Logged in as ${client.user.tag}`);
  setInterval(updateKeyStockDisplay, 30000); // Update stock display every 30s
});

async function checkForUpdates() {
  const now = Date.now();
  const sync = readSyncFile();
  const engineVersion = getEngineVersion();
  const websiteBuild = getWebsiteBuildTime();
  const guiBuild = getGuiBuildTime();
  const { tabs, functions } = detectNewTabsOrFeatures();

  // ENGINE.exe version change
  if (engineVersion && engineVersion !== lastKnownVersion) {
    lastKnownVersion = engineVersion;
    systemHealth.engine = { status: 'updated', lastSeen: now };
    await postAnnouncement(
      ANNOUNCEMENTS_CHANNEL_ID,
      '🚀 SmokeScreen ENGINE GUI Updated',
      `New build detected: **${engineVersion}**`,
      [
        { name: 'Download', value: 'Check your releases folder or the ENGINE/bin/Release/net8.0-windows directory.' },
        { name: 'Sync Status', value: 'ASyncToreyDev.sync updated across all systems.' }
      ]
    );
    sync.updates.push({ type: 'version', value: engineVersion, timestamp: now });
  }

  // Website build change
  if (websiteBuild && websiteBuild !== lastKnownWebsiteBuild) {
    lastKnownWebsiteBuild = websiteBuild;
    systemHealth.website = { status: 'updated', lastSeen: now };
    await postAnnouncement(
      ANNOUNCEMENTS_CHANNEL_ID,
      '🌐 Website Updated',
      `New website build: **${websiteBuild}**`,
      [{ name: 'Deploy URL', value: 'https://smok-ex-screen-engine.vercel.app' }]
    );
    sync.updates.push({ type: 'website', value: websiteBuild, timestamp: now });
  }

  // GUI build change
  if (guiBuild && guiBuild !== lastKnownGuiBuild) {
    lastKnownGuiBuild = guiBuild;
    systemHealth.gui = { status: 'updated', lastSeen: now };
    await postAnnouncement(
      ANNOUNCEMENTS_CHANNEL_ID,
      '🖥️ GUI Project Updated',
      `New GUI build: **${guiBuild}**`,
      [{ name: 'Project', value: 'SmokeScreenEngineGUI.csproj' }]
    );
    sync.updates.push({ type: 'gui', value: guiBuild, timestamp: now });
  }

  // New tabs
  const newTabs = tabs.filter(t => !lastKnownTabs.includes(t));
  if (newTabs.length > 0) {
    lastKnownTabs = tabs;
    await postAnnouncement(
      NEW_CHANNEL_ID,
      '🧩 New Tab(s) Added',
      `New tabs detected: **${newTabs.join(', ')}**`,
      [{ name: 'Tabs', value: tabs.join(', ') }]
    );
    sync.updates.push({ type: 'tabs', value: newTabs, timestamp: now });
  }

  // New functions
  const newFunctions = functions.filter(f => !lastKnownFunctions.includes(f));
  if (newFunctions.length > 0) {
    lastKnownFunctions = functions;
    await postAnnouncement(
      TOOL_DESCRIPTORS_CHANNEL_ID,
      '⚙️ New Functions/Features',
      `New functions: **${newFunctions.join(', ')}**`,
      [{ name: 'Functions', value: functions.join(', ') }]
    );
    sync.updates.push({ type: 'functions', value: newFunctions, timestamp: now });
  }

  // Periodic health report
  if (now % 300000 < 30000) { // Every 5 minutes
    systemHealth.bot = { status: 'running', lastSeen: now };
    const fields = [
      { name: '🤖 Bot', value: 'Running', inline: true },
      { name: '⚙️ ENGINE.exe', value: systemHealth.engine.status, inline: true },
      { name: '🌐 Website', value: systemHealth.website.status, inline: true },
      { name: '🖥️ GUI', value: systemHealth.gui.status, inline: true }
    ];
    await postAnnouncement(
      CHAT_CHANNEL_ID,
      '📊 System Health Report',
      'Automated health check',
      fields
    );
  }

  sync.lastSync = now;
  writeSyncFile(sync);
}

// API endpoint to receive key updates from ENGINE
const express = require('express');
const app = express();
app.use(express.json());

// Keep-alive: Vercel cron pings these so the bot stays online across deploys
app.get('/health', (req, res) => {
  res.json({ ok: true, status: 'online', ts: Date.now() });
});
app.get('/', (req, res) => {
  res.json({ ok: true, service: 'SmokeScreen Discord Bot', status: 'online' });
});
app.get('/wake', (req, res) => {
  res.json({ ok: true, woken: Date.now() });
});

app.post('/api/bot/keys', (req, res) => {
  const { keys, durationType } = req.body;
  if (!Array.isArray(keys) || !durationType) return res.status(400).json({ error: 'Invalid payload' });
  const current = keyStock.get(durationType) || 0;
  keyStock.set(durationType, current + keys.length);
  updateKeyStockDisplay();
  res.json({ ok: true, received: keys.length });
});

// Donny.AI sync — receive events from server/ENGINE and post to Discord
const DONNY_CHANNELS = {
  LOGIN_NOTIFY: ANNOUNCEMENTS_CHANNEL_ID,
  REGISTER_NOTIFY: ANNOUNCEMENTS_CHANNEL_ID,
  NEW_USER_LIVE_PAGE_VISIT: CHAT_CHANNEL_ID,
  KEY_REDEEM_NOTIFY: ANNOUNCEMENTS_CHANNEL_ID,
  AUTOMATES_NOTIFYS: ANNOUNCEMENTS_CHANNEL_ID,
  SYSTEM_HEALTH: CHAT_CHANNEL_ID,
  ENGINE_PING: CHAT_CHANNEL_ID,
};
app.post('/api/bot/donny', async (req, res) => {
  const { event, payload } = req.body || {};
  if (!event) return res.status(400).json({ error: 'Missing event' });
  const channelId = DONNY_CHANNELS[event] || ANNOUNCEMENTS_CHANNEL_ID;
  try {
    const channel = await client.channels.fetch(channelId);
    if (!channel) {
      res.status(502).json({ error: 'Channel not found' });
      return;
    }
    const title = {
      LOGIN_NOTIFY: '🔐 Login',
      REGISTER_NOTIFY: '📝 New Registration',
      NEW_USER_LIVE_PAGE_VISIT: '👁️ Live Page Visit',
      KEY_REDEEM_NOTIFY: '🔑 Key Redeemed',
      AUTOMATES_NOTIFYS: '🤖 Automate Notify',
      SYSTEM_HEALTH: '📊 System Health',
      ENGINE_PING: '⚙️ ENGINE Ping',
    }[event] || `Donny: ${event}`;
    const desc = payload?.message || (payload ? JSON.stringify(payload, null, 2) : '');
    const fields = [];
    if (payload?.username) fields.push({ name: 'Username', value: String(payload.username), inline: true });
    if (payload?.email) fields.push({ name: 'Email', value: String(payload.email).replace(/(.{2}).*@/, '$1***@'), inline: true });
    if (payload?.path) fields.push({ name: 'Path', value: String(payload.path), inline: true });
    if (payload?.userId) fields.push({ name: 'User ID', value: String(payload.userId).slice(0, 8) + '…', inline: true });
    await channel.send({ embeds: [createAnnouncementEmbed(title, desc, fields)] });
    res.json({ ok: true, channelId });
  } catch (err) {
    console.error('Donny post failed:', err);
    res.status(500).json({ error: err.message });
  }
});

app.listen(9877, () => console.log('Bot API listening on port 9877'));

client.once('ready', () => {
  console.log(`Logged in as ${client.user.tag}`);
  const sync = readSyncFile();
  lastKnownVersion = sync.updates.find(u => u.type === 'version')?.value || null;
  lastKnownTabs = sync.updates.find(u => u.type === 'tabs')?.value || [];
  lastKnownFunctions = sync.updates.find(u => u.type === 'functions')?.value || [];
  lastKnownWebsiteBuild = sync.updates.find(u => u.type === 'website')?.value || null;
  lastKnownGuiBuild = sync.updates.find(u => u.type === 'gui')?.value || null;
  setInterval(checkForUpdates, 30000);
});

client.login(BOT_TOKEN).catch(err => {
  console.error('Bot login failed:', err);
});
