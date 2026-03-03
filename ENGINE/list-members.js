const { Client, GatewayIntentBits } = require('discord.js');

// Bot configuration
const BOT_TOKEN = 'MTQ3NzQyOTgzMDYxMzA3ODI0Nw.Gevk0g.M_D-01eoV7KnTvdxNXvP84POn6uM9zmk5EtaEE';
const GUILD_ID = '1455221314653786207'; // Your Discord Guild ID

// Discord client
const client = new Client({
    intents: [
        GatewayIntentBits.Guilds,
        GatewayIntentBits.GuildMembers
    ]
});

client.once('ready', async () => {
    console.log(`[BOT] Logged in as ${client.user.tag}`);
    
    // Get guild
    const guild = client.guilds.cache.get(GUILD_ID);
    if (guild) {
        console.log(`[BOT] Connected to guild: ${guild.name}`);
        console.log(`[BOT] Guild member count: ${guild.memberCount}`);
        
        // Fetch all members
        await guild.members.fetch();
        console.log(`[BOT] Fetched ${guild.members.cache.size} guild members`);
        
        // List all members with their roles
        console.log('\n[BOT] Guild Members:');
        console.log('='.repeat(80));
        
        guild.members.cache.forEach(member => {
            const roles = member.roles.cache.map(role => role.name).join(', ');
            console.log(`ID: ${member.id}`);
            console.log(`Username: ${member.user.username}#${member.user.discriminator}`);
            console.log(`Roles: ${roles}`);
            console.log(`Joined: ${member.joinedAt?.toLocaleString()}`);
            console.log('-'.repeat(40));
        });
        
        // Find users with specific roles
        const ownerRole = guild.roles.cache.find(role => role.name === 'OWNER');
        const communityManagerRole = guild.roles.cache.find(role => role.name === 'COMMUNITY MANAGER');
        const basicAccessRole = guild.roles.cache.find(role => role.name === 'BASIC ACCESS');
        
        console.log('\n[BOT] Role Information:');
        console.log('='.repeat(80));
        console.log(`OWNER Role ID: ${ownerRole?.id || 'Not found'}`);
        console.log(`COMMUNITY MANAGER Role ID: ${communityManagerRole?.id || 'Not found'}`);
        console.log(`BASIC ACCESS Role ID: ${basicAccessRole?.id || 'Not found'}`);
        
        // Find users with these roles
        if (ownerRole) {
            const owners = guild.members.cache.filter(member => member.roles.cache.has(ownerRole.id));
            console.log(`\n[BOT] Users with OWNER role (${owners.size}):`);
            owners.forEach(member => {
                console.log(`  - ${member.user.username}#${member.user.discriminator} (${member.id})`);
            });
        }
        
        if (communityManagerRole) {
            const managers = guild.members.cache.filter(member => member.roles.cache.has(communityManagerRole.id));
            console.log(`\n[BOT] Users with COMMUNITY MANAGER role (${managers.size}):`);
            managers.forEach(member => {
                console.log(`  - ${member.user.username}#${member.user.discriminator} (${member.id})`);
            });
        }
        
        if (basicAccessRole) {
            const basicUsers = guild.members.cache.filter(member => member.roles.cache.has(basicAccessRole.id));
            console.log(`\n[BOT] Users with BASIC ACCESS role (${basicUsers.size}):`);
            basicUsers.forEach(member => {
                console.log(`  - ${member.user.username}#${member.user.discriminator} (${member.id})`);
            });
        }
        
    } else {
        console.log(`[BOT] Guild ${GUILD_ID} not found`);
    }
    
    // Disconnect after listing members
    setTimeout(() => {
        console.log('\n[BOT] Disconnecting...');
        client.destroy();
        process.exit(0);
    }, 5000);
});

// Login to Discord
client.login(BOT_TOKEN).catch(error => {
    console.error('[BOT] Failed to login:', error);
    process.exit(1);
});

// Handle errors
client.on('error', error => {
    console.error('[BOT] Discord client error:', error);
});

process.on('SIGINT', () => {
    console.log('[BOT] Received SIGINT, shutting down...');
    client.destroy();
    process.exit(0);
});
