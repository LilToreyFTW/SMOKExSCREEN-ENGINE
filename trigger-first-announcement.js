const { Client, GatewayIntentBits, EmbedBuilder } = require('discord.js');

const BOT_TOKEN = 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.Gz9yFk.W8CFCJka03tp3MoFAwYj_f5dXpIaLHojAQVIYw';
const TOOL_DESCRIPTORS_CHANNEL_ID = '1477431072164941874';

const client = new Client({
  intents: [GatewayIntentBits.Guilds],
});

client.once('ready', async () => {
  console.log('Bot ready, posting first tool descriptor announcement...');
  try {
    const channel = await client.channels.fetch(TOOL_DESCRIPTORS_CHANNEL_ID);
    if (!channel) {
      console.error('Channel not found');
      process.exit(1);
    }

    const embed = new EmbedBuilder()
      .setColor('#ff3d00')
      .setTitle('⚙️ SmokeScreen ENGINE — Tool Descriptors')
      .setDescription('Welcome to the SmokeScreen ENGINE tool suite. Below are the core components and their capabilities.')
      .addFields(
        { name: '🔐 ClerkAuth', value: 'Handles Clerk session sync, key redemption, and logout between website and GUI.' },
        { name: '📶 MsPingStatus', value: 'Live website ping status display in the GUI top-right corner.' },
        { name: '💾 KeyCache', value: 'Local SQLite cache for keys, prevents reuse, syncs with website.' },
        { name: '🔄 TSyncListener', value: 'Background listener for TSAsync++ key sync from website.' }
      )
      .setTimestamp()
      .setFooter({ text: 'SmokeScreen ENGINE — Auto-Bot' });

    await channel.send({ embeds: [embed] });
    console.log('First tool descriptor announcement posted.');
  } catch (err) {
    console.error('Failed to post announcement:', err);
  } finally {
    client.destroy();
    process.exit(0);
  }
});

client.login(BOT_TOKEN).catch(err => {
  console.error('Bot login failed:', err);
  process.exit(1);
});
