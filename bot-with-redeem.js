const { Client, GatewayIntentBits, EmbedBuilder, PermissionFlagsBits } = require('discord.js');
const fs = require('fs');
const path = require('path');

const BOT_TOKEN = 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.Gz9yFk.W8CFCJka03tp3MoFAwYj_f5dXpIaLHojAQVIYw';
const GUILD_ID = '1455221314653786207';
const CATEGORY_ID = '1477430674318299251';
const CHAT_CHANNEL_ID = '1477430872021008404';
const NEW_CHANNEL_ID = '1477430901762691259';
const ANNOUNCEMENTS_CHANNEL_ID = '1477430949124767987';
const TOOL_DESCRIPTORS_CHANNEL_ID = '1477431072164941874';

const SYNC_FILE_PATH = path.join(__dirname, 'ASyncToreyDev.sync');
const ENGINE_EXE_PATH = path.join(__dirname, 'ENGINE', 'bin', 'Release', 'net8.0-windows', 'SmokeScreenEngine.exe');
const WEBSITE_PATH = path.join(__dirname, 'SmokeScreen-ENGINE');

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
  ],
});

let lastKnownVersion = null;
let lastKnownTabs = [];
let lastKnownFunctions = [];

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

function detectNewTabsOrFeatures() {
  const currentTabs = ['ACCOUNT', 'LICENSE', 'ENGINE', 'TOOLS'];
  const currentFunctions = ['ClerkAuth', 'MsPingStatus', 'KeyCache', 'TSyncListener'];
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

// Redeem queue
const redeemQueue = new Map(); // key -> { userId, timestamp }

client.on('messageCreate', async (message) => {
  if (message.author.bot) return;
  if (!message.guild) return;
  if (message.guild.id !== GUILD_ID) return;

  const content = message.content.trim();
  const prefix = '!ss';
  if (!content.startsWith(prefix)) return;

  const args = content.slice(prefix.length).trim().split(/\s+/);
  const cmd = args.shift()?.toLowerCase();

  // !ss redeem <key>
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

  // !ss approve <key>
  if (cmd === 'approve' && args.length) {
    const key = args[0];
    const req = redeemQueue.get(key);
    if (!req) {
      await message.reply('No pending redeem request for that key.');
      return;
    }
    // Call website API to redeem
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

  // !ss deny <key>
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

  // !ss queue
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

  // Help
  if (cmd === 'help') {
    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('🛠️ SmokeScreen ENGINE Bot Commands')
      .addFields(
        { name: '!ss redeem <key>', value: 'Submit a license key for admin approval.' },
        { name: '!ss approve <key>', value: '(Admin) Approve and redeem a submitted key.' },
        { name: '!ss deny <key>', value: '(Admin) Deny a submitted key.' },
        { name: '!ss queue', value: 'Show pending redeem requests.' },
        { name: '!ss help', value: 'Show this help.' }
      );
    await message.channel.send({ embeds: [embed] });
  }
});

async function checkForUpdates() {
  const now = Date.now();
  const sync = readSyncFile();
  const engineVersion = getEngineVersion();
  const { tabs, functions } = detectNewTabsOrFeatures();

  if (engineVersion && engineVersion !== lastKnownVersion) {
    lastKnownVersion = engineVersion;
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

  sync.lastSync = now;
  writeSyncFile(sync);
}

client.once('ready', () => {
  console.log(`Logged in as ${client.user.tag}`);
  const sync = readSyncFile();
  lastKnownVersion = sync.updates.find(u => u.type === 'version')?.value || null;
  lastKnownTabs = sync.updates.find(u => u.type === 'tabs')?.value || [];
  lastKnownFunctions = sync.updates.find(u => u.type === 'functions')?.value || [];
  setInterval(checkForUpdates, 30000);
});

client.login(BOT_TOKEN).catch(err => {
  console.error('Bot login failed:', err);
});
