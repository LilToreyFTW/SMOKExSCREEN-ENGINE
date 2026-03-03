const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

class BotManager {
    constructor() {
        this.botProcess = null;
        this.isRunning = false;
        this.restartAttempts = 0;
        this.maxRestartAttempts = 50;
        this.restartDelay = 5000; // 5 seconds
    }

    async startBot() {
        if (this.isRunning) {
            console.log('[BOT MANAGER] Bot is already running');
            return;
        }

        console.log('[BOT MANAGER] Starting Discord bot...');
        this.isRunning = true;
        this.restartAttempts = 0;
        
        await this.launchBot();
    }

    async launchBot() {
        if (!this.isRunning) return;

        try {
            console.log(`[BOT MANAGER] Launching bot (attempt ${this.restartAttempts + 1}/${this.maxRestartAttempts})`);
            
            this.botProcess = spawn('node', ['bot-full-auth.js'], {
                cwd: __dirname,
                stdio: ['pipe', 'pipe', 'pipe'],
                env: { ...process.env }
            });

            this.botProcess.stdout.on('data', (data) => {
                console.log(`[BOT] ${data.toString().trim()}`);
            });

            this.botProcess.stderr.on('data', (data) => {
                console.log(`[BOT ERROR] ${data.toString().trim()}`);
            });

            this.botProcess.on('close', (code) => {
                console.log(`[BOT] Process exited with code ${code}`);
                this.isRunning = false;
                this.botProcess = null;

                if (this.restartAttempts < this.maxRestartAttempts) {
                    this.restartAttempts++;
                    console.log(`[BOT MANAGER] Restarting bot in ${this.restartDelay/1000} seconds...`);
                    setTimeout(() => this.launchBot(), this.restartDelay);
                } else {
                    console.log('[BOT MANAGER] Max restart attempts reached. Stopping bot manager.');
                }
            });

            this.botProcess.on('error', (error) => {
                console.error(`[BOT ERROR] ${error.message}`);
                this.isRunning = false;
                this.botProcess = null;
            });

        } catch (error) {
            console.error(`[BOT MANAGER] Failed to launch bot: ${error.message}`);
            this.isRunning = false;
        }
    }

    stopBot() {
        console.log('[BOT MANAGER] Stopping Discord bot...');
        this.isRunning = false;
        
        if (this.botProcess) {
            this.botProcess.kill('SIGTERM');
            this.botProcess = null;
        }
    }

    getStatus() {
        return {
            isRunning: this.isRunning,
            restartAttempts: this.restartAttempts,
            maxRestartAttempts: this.maxRestartAttempts
        };
    }
}

// Create global bot manager instance
const botManager = new BotManager();

// Handle process termination
process.on('SIGINT', () => {
    console.log('[BOT MANAGER] Received SIGINT, shutting down...');
    botManager.stopBot();
    process.exit(0);
});

process.on('SIGTERM', () => {
    console.log('[BOT MANAGER] Received SIGTERM, shutting down...');
    botManager.stopBot();
    process.exit(0);
});

// Start the bot
botManager.startBot();

// Export for external use
module.exports = botManager;
