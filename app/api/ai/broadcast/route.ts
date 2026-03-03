import { NextRequest, NextResponse } from "next/server";

const notifications: Array<{
  title: string;
  message: string;
  source: string;
  type: string;
  timestamp: string;
  userId: string;
}> = [];

export async function POST(req: NextRequest) {
  try {
    const notification = await req.json();
    
    notifications.unshift({
      ...notification,
      timestamp: notification.timestamp || new Date().toISOString()
    });
    
    // Keep only last 100
    if (notifications.length > 100) {
      notifications.pop();
    }
    
    // Also broadcast to Discord
    await broadcastToDiscord(notification);
    
    return NextResponse.json({ success: true });
  } catch (error) {
    return NextResponse.json({ error: "Failed to process notification" }, { status: 500 });
  }
}

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url);
  const source = searchParams.get("source");
  const limit = parseInt(searchParams.get("limit") || "50");
  
  let filtered = notifications;
  if (source) {
    filtered = notifications.filter(n => n.source === source);
  }
  
  return NextResponse.json(filtered.slice(0, limit));
}

async function broadcastToDiscord(notification: any) {
  try {
    const color = notification.type === "error" ? 0xFF0000 
      : notification.type === "warning" ? 0xFFFF00 
      : notification.type === "success" ? 0x00FF00 
      : 0x1F6FEB;
    
    const embed = {
      embeds: [{
        title: `🔔 ${notification.title}`,
        description: notification.message,
        color: color,
        footer: { text: `SmokeScreen ENGINE AI | ${notification.source?.toUpperCase() || "SYSTEM"}` },
        timestamp: notification.timestamp || new Date().toISOString()
      }]
    };
    
    // Send to Discord via bot
    const { Client, GatewayIntentBits, EmbedBuilder } = require('discord.js');
    
    const client = new Client({
      intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages]
    });
    
    client.once('ready', async () => {
      const channel = client.channels.cache.get("1477430872021008404");
      if (channel) {
        await channel.send({ embeds: [embed.embeds[0]] });
      }
      client.destroy();
    });
    
    await client.login(process.env.DISCORD_BOT_TOKEN || "");
  } catch (error) {
    console.error("Discord broadcast failed:", error);
  }
}
