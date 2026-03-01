# Keep Discord Bot Online When Vercel Updates

So the bot stays **online** and does **not** reset when you push to Vercel:

1. **Run the bot on an always-on host** (not on Vercel).  
   Vercel is serverless and cannot run a persistent Discord bot. Use one of:
   - [Railway](https://railway.app) (recommended)
   - [Render](https://render.com)
   - A VPS (e.g. DigitalOcean, Linode)

2. **Expose the bot with a public URL**  
   - Railway/Render give you a URL like `https://your-app.up.railway.app`.  
   - The bot already has `GET /health` and `GET /wake`; your host will keep the process running.

3. **Set Vercel env vars** (Project → Settings → Environment Variables):
   - **`BOT_PUBLIC_URL`** = your bot’s public URL (e.g. `https://your-bot.up.railway.app`).  
     No trailing slash.
   - **`CRON_SECRET`** = a random string (e.g. 32 chars).  
     Vercel sends it as `Authorization: Bearer <CRON_SECRET>` when calling the cron.

4. **Vercel cron**  
   Every **5 minutes** Vercel calls `GET /api/cron/ping-bot`, which requests your bot’s `/health`.  
   That keeps the bot process (and thus Discord “online” status) alive and unaffected by Vercel deploys.

After each successful Vercel deploy, the same cron keeps pinging the bot, so the bot never goes offline because of a deploy.

## Deploying the bot on Railway (example)

1. Push the repo (or the folder with `bot-full-auth.js`) to GitHub.
2. In Railway: New Project → Deploy from GitHub → select repo.
3. Set **Start Command**: `node bot-full-auth.js` (or `node path/to/bot-full-auth.js`).
4. Add env var **BOT_TOKEN** (your Discord bot token).
5. Open **Settings → Generate Domain** to get the public URL.
6. In Vercel, set **BOT_PUBLIC_URL** to that URL (e.g. `https://your-project.up.railway.app`).
