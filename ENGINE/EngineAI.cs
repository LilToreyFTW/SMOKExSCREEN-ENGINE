using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public class EngineAI
    {
        private static EngineAI? _instance;
        private static readonly object _lock = new();
        
        private readonly string _discordBotToken = "MTQ3NzQyOTgzMDYxMzA3ODI0Nw.Gz9yFk.W8CFCJka03tp3MoFAwYj_f5dXpIaLHojAQVIYw";
        private readonly string _logChannelId = "1477430872021008404";
        private readonly string _announceChannelId = "1477430949124767987";
        private readonly HttpClient _http = new();
        
        private readonly List<Notification> _notificationQueue = new();
        
        public class Notification
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Source { get; set; } = ""; // "exe", "engine", "website"
            public string Type { get; set; } = "info"; // info, warning, error, success
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string UserId { get; set; } = "";
        }
        
        public static EngineAI Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new EngineAI();
                    }
                }
                return _instance;
            }
        }
        
        private EngineAI()
        {
            LoadQueue();
        }
        
        private void LoadQueue()
        {
            try
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "ai_notifications.db");
                if (File.Exists(dbPath))
                {
                    var lines = File.ReadAllLines(dbPath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            _notificationQueue.Add(new Notification
                            {
                                Title = parts[0],
                                Message = parts[1],
                                Source = parts[2],
                                Type = parts[3],
                                Timestamp = DateTime.Parse(parts[4])
                            });
                        }
                    }
                }
            }
            catch { }
        }
        
        private void SaveQueue()
        {
            try
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "ai_notifications.db");
                var lines = _notificationQueue.ConvertAll(n => 
                    $"{n.Title}|{n.Message}|{n.Source}|{n.Type}|{n.Timestamp:O}");
                File.WriteAllLines(dbPath, lines);
            }
            catch { }
        }
        
        public async Task NotifyAsync(string title, string message, string source, string type = "info", string userId = "")
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Source = source,
                Type = type,
                Timestamp = DateTime.Now,
                UserId = userId
            };
            
            _notificationQueue.Insert(0, notification);
            if (_notificationQueue.Count > 100)
                _notificationQueue.RemoveAt(_notificationQueue.Count - 1);
            
            SaveQueue();
            
            // Send to Discord
            await SendToDiscordAsync(notification);
            
            // Also send to other systems
            await BroadcastToOtherSystemsAsync(notification);
        }
        
        private async Task SendToDiscordAsync(Notification notification)
        {
            try
            {
                var color = notification.Type switch
                {
                    "error" => 0xFF0000,
                    "warning" => 0xFFFF00,
                    "success" => 0x00FF00,
                    _ => 0x1F6FEB
                };
                
                var embed = new
                {
                    title = $"🔔 {notification.Title}",
                    description = notification.Message,
                    color = color,
                    footer = new { text = $"SmokeScreen ENGINE AI | {notification.Source.ToUpper()}" },
                    timestamp = notification.Timestamp.ToString("o")
                };
                
                var payload = JsonSerializer.Serialize(new
                {
                    embeds = new[] { embed }
                });
                
                // Send to log channel
                await _http.PostAsync(
                    $"https://discord.com/api/channels/{_logChannelId}/messages",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
            }
            catch { }
        }
        
        private async Task BroadcastToOtherSystemsAsync(Notification notification)
        {
            try
            {
                // Broadcast to website API
                var payload = JsonSerializer.Serialize(notification);
                await _http.PostAsync(
                    "https://smokescreen-engine.vercel.app/api/ai/broadcast",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
            }
            catch { }
        }
        
        public List<Notification> GetNotifications(int count = 50)
        {
            var result = new List<Notification>();
            for (int i = 0; i < Math.Min(count, _notificationQueue.Count); i++)
            {
                result.Add(_notificationQueue[i]);
            }
            return result;
        }
        
        public List<Notification> GetNotificationsBySource(string source)
        {
            return _notificationQueue.FindAll(n => n.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
        }
        
        public async Task SendDirectMessageAsync(string userId, string message)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { content = message });
                await _http.PostAsync(
                    $"https://discord.com/api/users/{userId}/messages",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
            }
            catch { }
        }
        
        public async Task AnnounceAsync(string title, string message)
        {
            await NotifyAsync(title, message, "system", "info");
        }
        
        // Quick notification methods
        public Task NotifyKeyRedeemedAsync(string user, string keyType) =>
            NotifyAsync("🎫 Key Redeemed", $"{user} redeemed a {keyType} key", "exe", "success", user);
            
        public Task NotifyUserLoginAsync(string user) =>
            NotifyAsync("👤 User Login", $"{user} logged in", "exe", "info", user);
            
        public Task NotifyKeyGeneratedAsync(string keyType, int count) =>
            NotifyAsync("🔑 Keys Generated", $"{count} x {keyType} keys generated", "engine", "info");
            
        public Task NotifyErrorAsync(string error, string source) =>
            NotifyAsync("❌ Error", $"[{source}] {error}", source, "error");
            
        public Task NotifyPurchaseAsync(string user, string plan) =>
            NotifyAsync("💰 Purchase", $"{user} purchased {plan}", "website", "success", user);
    }
}
