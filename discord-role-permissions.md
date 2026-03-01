# Discord Role & Bot Permissions Setup

## Role: Smokescreen-discord-access (ID: 1477448046873935872)

### Recommended Role Permissions (in Discord Server Settings > Roles)
- **View Channels** ✅ (required)
- **Send Messages** ✅ (required)
- **Embed Links** ✅ (for bot embeds)
- **Attach Files** ✅ (optional, for logs/screenshots)
- **Read Message History** ✅ (required)
- **Use External Emojis** ✅ (optional)
- **Add Reactions** ✅ (optional)

**Deny** (disable):
- Administrator
- Manage Server
- Manage Channels
- Manage Roles
- Manage Messages
- Kick/Ban Members
- Mention Everyone/@here
- Create Events
- Create Invites

### Category: Smokescreen-ENGINE X (ID: 1477430674318299251)
- Override: Allow **View Channels** for Smokescreen-discord-access
- Override: Allow **Send Messages** for Smokescreen-discord-access
- Override: Allow **Read Message History** for Smokescreen-discord-access

### Specific Channel Overrides (if needed)
- **#chat (1477430872021008404)**: Allow Send Messages
- **#new (1477430901762691259)**: Allow Send Messages
- **#announcements (1477430949124767987)**: Allow Read Message History, Deny Send Messages (admins only)
- **#tool-descriptors (1477431072164941874)**: Allow Send Messages

## Bot Permissions (Bot User in Server Settings > Bot)
Required for the bot to function:
- **View Channels**
- **Send Messages**
- **Embed Links**
- **Read Message History**
- **Manage Roles** (to assign Smokescreen-discord-access)
- **Use External Emojis** (optional)

### Bot Channel Permissions
- In **Smokescreen-ENGINE X** category: Allow all above
- In **#announcements**: Allow Send Messages (for bot posts)
- In **#tool-descriptors**: Allow Send Messages

## How to Apply in Discord
1. Go to Server Settings > Roles
2. Select “Smokescreen-discord-access”
3. Toggle permissions as listed above
4. Go to Category Settings > Permissions
5. Add the role and set overrides
6. Repeat for specific channels if needed

## Bot Code Notes
- The bot automatically assigns the role when a user sends any message in the category
- Role ID is hardcoded: `1477448046873935872`
- Category ID is hardcoded: `1477430674318299251`
- Ensure the bot has “Manage Roles” permission in the server
