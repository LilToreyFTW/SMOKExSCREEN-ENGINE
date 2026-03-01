const { Client, GatewayIntentBits, EmbedBuilder } = require('discord.js');
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
  // Placeholder: In real implementation, parse HubForm.cs or reflection
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

async function checkForUpdates() {
  const now = Date.now();
  const sync = readSyncFile();
  const engineVersion = getEngineVersion();
  const { tabs, functions } = detectNewTabsOrFeatures();

  // Detect version change
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

  // Detect new tabs
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

  // Detect new functions
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
  // Check every 30 seconds
  setInterval(checkForUpdates, 30000);
});

client.login(BOT_TOKEN).catch(err => {
  console.error('Bot login failed:', err);
});
