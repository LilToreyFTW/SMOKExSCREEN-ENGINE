# Donny.AI — SmokeScreen ENGINE AI Manager

Donny.AI is the central **AI Manager** that **manages all systems**: ENGINE.exe, SmokeScreen-ENGINE GUI, web app, and Discord bot.

- **Sync spec:** See `DonnySync.LLM.AI` (same folder or repo root). That file defines how the AI syncs to all systems: automates notifies, login notifies, register notifies, new user live page visit, and Discord permissions for the bot.
- **Integration:** ENGINE.exe and SmokeScreenEngine.exe can report events (e.g. key sync, ping) that Donny aggregates; the Discord bot consumes DonnySync events.
