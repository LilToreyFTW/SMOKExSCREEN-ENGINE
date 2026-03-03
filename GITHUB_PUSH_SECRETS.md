# GitHub Push – Allow Discord Bot Tokens (Push Protection)

The repo is **clean of large files** (bin/, obj/, node_modules/, zips removed from history).  
The only thing blocking `git push origin main --force` is **GitHub Push Protection** detecting Discord Bot Tokens.

You asked to **keep** real secrets in the repo. To push without removing them:

## Option A – Allow each detected secret (recommended if you want tokens in the repo)

1. Open each URL below in your browser (while logged into GitHub as the repo owner).
2. Click **“Allow secret”** (or equivalent) for that detection.
3. After allowing **all** listed detections, run again:
   ```bash
   git push origin main --force
   ```

### Unblock URLs from the last push error

- https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE/security/secret-scanning/unblock-secret/3ASJd3UE1fUs0HzcJzKGMaPHSYj  
  (SmokeScreen-ENGINE/ENGINE/send-discord-update.js)
- https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE/security/secret-scanning/unblock-secret/3ASJczsmNyti7DKAvTDQoZay2ei  
  (ENGINE/list-members.js)
- https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE/security/secret-scanning/unblock-secret/3ASJd26Oe6kDHl0A9RFf3a6ATE0  
  (bot-full-auth.js, bot.js, bot-with-role.js, etc.)
- https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE/security/secret-scanning/unblock-secret/3ASJd0T2TRUdDUMsUe8uQqT4sUe  
  (ENGINE/bot-full-auth.js, ENGINE/bot-mock.js)
- https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE/security/secret-scanning/unblock-secret/3ASJd08WrCVkZM04fafN3F4v0qk  
  (SmokeScreen-ENGINE/ENGINE/bot-full-auth.js)

GitHub reported **2 more** secrets; after you allow the ones above, push again—GitHub will then show the remaining unblock links.

## Option B – Use environment variables (no secrets in repo)

If you prefer not to allow secrets in the repo, replace hardcoded Discord tokens with e.g. `process.env.DISCORD_BOT_TOKEN` and set the value in Vercel / local `.env` (do not commit `.env`).

---

After the push succeeds, Vercel will deploy from `main` and smok-ex-screen-engine.vercel.app will update.
