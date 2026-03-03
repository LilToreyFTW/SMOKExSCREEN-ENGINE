const { Client, GatewayIntentBits, EmbedBuilder } = require('discord.js');

const BOT_TOKEN = process.env.DISCORD_BOT_TOKEN || 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.GTHiWx.yWZ7ZpZ7wr-p1xdu7zp4Y_WVcAbY7UIqKES4mA';
const CHANNEL_ID = '1477430872021008404';

const client = new Client({
    intents: [
        GatewayIntentBits.Guilds,
        GatewayIntentBits.GuildMessages
    ]
});

client.once('ready', async () => {
    console.log(`Logged in as ${client.user.tag}`);
    
    const channel = client.channels.cache.get(CHANNEL_ID);
    if (!channel) {
        console.log('Channel not found!');
        process.exit(1);
    }
    
    const embed = new EmbedBuilder()
        .setTitle('🎉 SmokeScreen ENGINE v4.2 - Major Update!')
        .setColor(0xFF3D00)
        .setDescription('New features and improvements have been deployed!')
        .addFields(
            { name: '🌐 New Website Features', value: '• Game tabs: WARZONE, ARC RAIDERS, R6S, FN\n• LIVE status indicators with colored dots\n• Real-time ping display\n• New subscription plans for each game' },
            { name: '🖥️ Desktop App Updates', value: '• LIVE status indicators on all game tabs\n• Real-time ping display (5s refresh)\n• API SERVICE STATUS diagnostics panel\n• Improved auto-updater with GitHub integration' },
            { name: '🎮 Recoil Key Generator', value: '• New in Engine.exe: Recoil Game Key Subscriptions Generator\n• Generate R6S-, CODW-, AR-, FN- keys\n• Multiple durations: 1mo, 6mo, 12mo, Lifetime\n• Keys auto-saved to database for user redemption' },
            { name: '💰 Pricing', value: 'Main Engine: $5/day, $15/week, $35/month, $150 lifetime\nRecoil Scripts: $9.99/mo, $35.99/6mo, $65.99/yr, $149.99 lifetime' },
            { name: '📥 Download', value: 'Get the latest version from the website!' }
        )
        .setTimestamp()
        .setFooter({ text: 'SmokeScreen ENGINE', iconURL: 'https://i.imgur.com/someicon.png' });
    
    await channel.send({ embeds: [embed] });
    console.log('Message sent successfully!');
    process.exit(0);
});

client.on('error', (error) => {
    console.error('Discord bot error:', error);
    process.exit(1);
});

client.login(BOT_TOKEN);
