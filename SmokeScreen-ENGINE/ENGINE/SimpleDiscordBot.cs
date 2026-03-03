using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public class SimpleDiscordBot
    {
        private Dictionary<string, DateTime> _userSessions = new Dictionary<string, DateTime>();
        private Dictionary<string, string> _cachedKeys = new Dictionary<string, string>();
        private Dictionary<string, DateTime> _keyUsage = new Dictionary<string, DateTime>();
        private System.Threading.Timer? _monitoringTimer;
        private System.Threading.Timer? _heartbeatTimer;
        private readonly string _ownerId = "1368087024401252393";
        private readonly string _webhookUrl = "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN";

        public void StartBot()
        {
            // Load existing keys from ENGINE.exe
            _ = Task.Run(LoadExistingKeysAsync);

            // Start monitoring timers
            _monitoringTimer = new Timer(MonitorSmokeScreenEngine, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            Console.WriteLine("🤖 Simple Discord Bot Started");
            Console.WriteLine("👑 Owner: Bl0wdart (1368087024401252393)");
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
                        var keysArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(keys["keys"]?.ToString() ?? "[]");
                        if (keysArray != null)
                        {
                            foreach (var key in keysArray)
                            {
                                var keyValue = key["key"]?.ToString() ?? "";
                                var duration = key["duration"]?.ToString() ?? "";
                                _cachedKeys[keyValue] = duration;
                                _keyUsage[keyValue] = DateTime.Now;
                            }
                        }
                        
                        await LogToDiscord($"🔑 Loaded {_cachedKeys.Count} existing keys from ENGINE.exe database");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing keys: {ex.Message}");
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
                        Console.WriteLine($"Error monitoring process {process.Id}: {ex.Message}");
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
                Console.WriteLine($"Error in monitoring: {ex.Message}");
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

        private async Task LogToDiscord(string message)
        {
            try
            {
                using var httpClient = new HttpClient();
                var payload = new
                {
                    content = message,
                    username = "SmokeScreen Bot",
                    avatar_url = "https://i.imgur.com/your-avatar.png"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await httpClient.PostAsync(_webhookUrl, content);
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

        public string? GetKeyDuration(string key)
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

        public void StopBot()
        {
            _monitoringTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }
    }
}
