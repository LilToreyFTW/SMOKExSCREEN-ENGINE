using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace SmokeScreenEngine
{
    public class UltimateDiscordBot
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private Dictionary<string, DateTime> _userSessions = new Dictionary<string, DateTime>();
        private Dictionary<string, string> _cachedKeys = new Dictionary<string, string>();
        private Dictionary<string, DateTime> _keyUsage = new Dictionary<string, DateTime>();
        private System.Threading.Timer _monitoringTimer;
        private System.Threading.Timer _heartbeatTimer;
        private readonly string _ownerId = "1368087024401252393";
        private readonly string _botToken = "MTQ3NzQyOTgzMDYxMzA3ODI0Nw.GBxfJB.yKFr8PG4wOpmma8SaqFoXvBoZeDMCn61WrGhQw";
        private readonly string _logChannelId = "YOUR_LOG_CHANNEL_ID";
        private readonly string _keyChannelId = "YOUR_KEY_CHANNEL_ID";

        public async Task StartBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1000
            });

            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            _client.Log += LogAsync;
            _client.Ready += OnReadyAsync;
            _client.MessageReceived += HandleCommandAsync;

            await _client.LoginAsync(TokenType.Bot, _botToken);
            await _client.StartAsync();

            // Load existing keys from ENGINE.exe
            await LoadExistingKeysAsync();

            // Start monitoring timers
            _monitoringTimer = new Timer(MonitorSmokeScreenEngine, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task LoadExistingKeysAsync()
        {
            try
            {
                var keysFile = "I:\\UI_GUI\\ENGINE\\generated_keys.json";
                if (File.Exists(keysFile))
                {
                    var json = await File.ReadAllTextAsync(keysFile);
                    var keys = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (keys != null && keys.ContainsKey("keys"))
                    {
                        var keysArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(keys["keys"].ToString());
                        foreach (var key in keysArray)
                        {
                            var keyValue = key["key"].ToString();
                            var duration = key["duration"].ToString();
                            _cachedKeys[keyValue] = duration;
                            _keyUsage[keyValue] = DateTime.Now;
                        }
                        
                        await LogToDiscord($"🔑 Loaded {_cachedKeys.Count} existing keys from ENGINE.exe database");
                    }
                }
            }
            catch (Exception ex)
            {
                await LogToDiscord($"❌ Error loading existing keys: {ex.Message}");
            }
        }

        private async Task MonitorSmokeScreenEngine(object state)
        {
            try
            {
                var processes = Process.GetProcessesByName("SmokeScreenEngine");
                var currentUsers = new HashSet<string>();

                foreach (var process in processes)
                {
                    try
                    {
                        var startTime = process.StartTime;
                        var sessionKey = $"{process.Id}_{startTime:yyyyMMddHHmmss}";
                        
                        if (!_userSessions.ContainsKey(sessionKey))
                        {
                            _userSessions[sessionKey] = startTime;
                            currentUsers.Add(sessionKey);
                            
                            await LogToDiscord($"🚀 **SmokeScreenEngine.exe Started**\n" +
                                $"📅 **Time:** {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                                $"🆔 **Process ID:** {process.Id}\n" +
                                $"👤 **Session Key:** {sessionKey}\n" +
                                $"💻 **Machine:** {Environment.MachineName}\n" +
                                $"👑 **Owner:** Bl0wdart (1368087024401252393)");
                        }
                        else
                        {
                            currentUsers.Add(sessionKey);
                        }
                    }
                    catch (Exception ex)
                    {
                        await LogToDiscord($"⚠️ Error monitoring process {process.Id}: {ex.Message}");
                    }
                }

                // Check for closed sessions
                var closedSessions = _userSessions.Keys.Where(k => !currentUsers.Contains(k)).ToList();
                foreach (var closedSession in closedSessions)
                {
                    var endTime = DateTime.Now;
                    var duration = endTime - _userSessions[closedSession];
                    
                    await LogToDiscord($"🛑 **SmokeScreenEngine.exe Closed**\n" +
                        $"📅 **End Time:** {endTime:yyyy-MM-dd HH:mm:ss}\n" +
                        $"⏱️ **Duration:** {duration.TotalMinutes:F1} minutes\n" +
                        $"🆔 **Session Key:** {closedSession}");
                    
                    _userSessions.Remove(closedSession);
                }
            }
            catch (Exception ex)
            {
                await LogToDiscord($"❌ Error in monitoring: {ex.Message}");
            }
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            var argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private async Task OnReadyAsync()
        {
            await LogToDiscord($"🤖 **Ultimate Discord Bot Online**\n" +
                $"👑 **Owner:** Bl0wdart (1368087024401252393)\n" +
                $"🔑 **Cached Keys:** {_cachedKeys.Count}\n" +
                $"👥 **Active Sessions:** {_userSessions.Count}\n" +
                $"📅 **Started:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        private async Task LogAsync(LogMessage message)
        {
            if (message.Severity <= LogSeverity.Warning)
            {
                await LogToDiscord($"📊 **Bot Log:** {message.Message}");
            }
        }

        private async Task SendHeartbeat(object state)
        {
            try
            {
                await LogToDiscord($"💓 **Bot Heartbeat**\n" +
                    $"👥 **Active Sessions:** {_userSessions.Count}\n" +
                    $"🔑 **Cached Keys:** {_cachedKeys.Count}\n" +
                    $"📅 **Time:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat error: {ex.Message}");
            }
        }

        public async Task LogKeyRedemption(string discordId, string key, string duration, bool success)
        {
            try
            {
                var timestamp = DateTime.Now;
                var status = success ? "✅ **SUCCESS**" : "❌ **FAILED**";
                
                await LogToDiscord($"🔑 **Key Redemption Attempt**\n" +
                    $"{status}\n" +
                    $"👤 **Discord ID:** {discordId}\n" +
                    $"🔑 **Key:** `{key}`\n" +
                    $"⏰ **Duration:** {duration}\n" +
                    $"📅 **Time:** {timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                    $"👑 **Owner:** Bl0wdart (1368087024401252393)");

                if (success)
                {
                    _keyUsage[key] = timestamp;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key redemption log error: {ex.Message}");
            }
        }

        public async Task LogLoginAttempt(string discordId, string username, bool success, string reason = "")
        {
            try
            {
                var timestamp = DateTime.Now;
                var status = success ? "✅ **SUCCESS**" : "❌ **FAILED**";
                
                await LogToDiscord($"🔐 **Login Attempt**\n" +
                    $"{status}\n" +
                    $"👤 **Discord ID:** {discordId}\n" +
                    $"👨 **Username:** {username}\n" +
                    $"📅 **Time:** {timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                    $"{(!string.IsNullOrEmpty(reason) ? $"📝 **Reason:** {reason}\n" : "")}" +
                    $"👑 **Owner:** Bl0wdart (1368087024401252393)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login log error: {ex.Message}");
            }
        }

        public async Task LogToDiscord(string message)
        {
            try
            {
                if (ulong.TryParse(_logChannelId, out var channelId))
                {
                    var channel = _client.GetChannel(channelId) as SocketTextChannel;
                    if (channel != null)
                    {
                        await channel.SendMessageAsync($"```{message}```");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discord log error: {ex.Message}");
            }
        }

        public async Task AddKeyToCache(string key, string duration)
        {
            try
            {
                _cachedKeys[key] = duration;
                _keyUsage[key] = DateTime.Now;
                
                await LogToDiscord($"🔑 **New Key Added to Cache**\n" +
                    $"🔑 **Key:** `{key}`\n" +
                    $"⏰ **Duration:** {duration}\n" +
                    $"📅 **Added:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"👑 **Owner:** Bl0wdart (1368087024401252393)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key cache error: {ex.Message}");
            }
        }

        public bool IsKeyCached(string key)
        {
            return _cachedKeys.ContainsKey(key);
        }

        public string GetKeyDuration(string key)
        {
            return _cachedKeys.TryGetValue(key, out var duration) ? duration : null;
        }

        public Dictionary<string, string> GetAllCachedKeys()
        {
            return new Dictionary<string, string>(_cachedKeys);
        }

        public int GetActiveSessionCount()
        {
            return _userSessions.Count;
        }

        public int GetCachedKeyCount()
        {
            return _cachedKeys.Count;
        }

        public async Task StopBotAsync()
        {
            await _client.StopAsync();
            _monitoringTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }
    }

    // Bot Commands Module
    public class BotCommands : ModuleBase<SocketCommandContext>
    {
        private UltimateDiscordBot _bot;

        public BotCommands(UltimateDiscordBot bot)
        {
            _bot = bot;
        }

        [Command("status")]
        [Summary("Show bot status and statistics")]
        public async Task StatusAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("🤖 Ultimate Bot Status")
                .WithColor(Color.Blue)
                .AddField("👑 Owner", "Bl0wdart (1368087024401252393)", true)
                .AddField("👥 Active Sessions", _bot.GetActiveSessionCount().ToString(), true)
                .AddField("🔑 Cached Keys", _bot.GetCachedKeyCount().ToString(), true)
                .AddField("📅 Uptime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), true)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("keys")]
        [Summary("Show all cached keys")]
        public async Task KeysAsync()
        {
            var keys = _bot.GetAllCachedKeys();
            if (keys.Count == 0)
            {
                await ReplyAsync("🔑 No cached keys found.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🔑 Cached Keys")
                .WithColor(Color.Green)
                .WithDescription($"Total keys: {keys.Count}")
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("sessions")]
        [Summary("Show active SmokeScreenEngine sessions")]
        public async Task SessionsAsync()
        {
            var count = _bot.GetActiveSessionCount();
            var embed = new EmbedBuilder()
                .WithTitle("🚀 Active Sessions")
                .WithColor(Color.Orange)
                .AddField("Active SmokeScreenEngine.exe Instances", count.ToString(), true)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("addkey")]
        [Summary("Add a key to cache (Owner only)")]
        public async Task AddKeyAsync(string key, string duration)
        {
            if (Context.User.Id.ToString() != "1368087024401252393")
            {
                await ReplyAsync("❌ Owner only command!");
                return;
            }

            await _bot.AddKeyToCache(key, duration);
            await ReplyAsync($"✅ Key `{key}` added to cache!");
        }

        [Command("help")]
        [Summary("Show available commands")]
        public async Task HelpAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("🤖 Bot Commands")
                .WithColor(Color.Purple)
                .AddField("!status", "Show bot status and statistics", true)
                .AddField("!keys", "Show all cached keys", true)
                .AddField("!sessions", "Show active SmokeScreenEngine sessions", true)
                .AddField("!addkey <key> <duration>", "Add key to cache (Owner only)", true)
                .AddField("!help", "Show this help message", true)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed);
        }
    }
}
