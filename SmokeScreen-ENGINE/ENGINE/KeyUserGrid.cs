using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SmokeScreenEngine
{
    public class KeyUserGridConfig
    {
        public string ConfigVersion { get; set; } = "1.0.0";
        public bool EnableIPTracking { get; set; } = true;
        public bool EnableAnalytics { get; set; } = true;
        public int PingIntervalMs { get; set; } = 3000;
        public int MaxCachedUsers { get; set; } = 1000;
        public string AdminKey { get; set; } = "";
        public List<string> AllowedIPRanges { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserSessionInfo
    {
        public string SessionId { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string Country { get; set; } = "Unknown";
        public string City { get; set; } = "Unknown";
        public DateTime ConnectedAt { get; set; }
        public DateTime LastPing { get; set; }
        public int PingMs { get; set; }
        public bool IsActive { get; set; }
    }

    public static class KeyUserGridManager
    {
        private static readonly object _lock = new();
        private static List<UserSessionInfo> _activeSessions = new();
        private static string _configPath = "";
        private static KeyUserGridConfig? _config;

        public static KeyUserGridConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = LoadOrCreateConfig();
                }
                return _config;
            }
        }

        public static void Initialize(string exeDirectory)
        {
            _configPath = Path.Combine(exeDirectory, "Engine.keyusergrid.SPL");
            _config = LoadOrCreateConfig();
        }

        private static KeyUserGridConfig LoadOrCreateConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var encrypted = File.ReadAllText(_configPath);
                    var decrypted = DecryptChinese(encrypted);
                    return JsonSerializer.Deserialize<KeyUserGridConfig>(decrypted) ?? new KeyUserGridConfig();
                }
            }
            catch { }
            return new KeyUserGridConfig();
        }

        public static void ExtractConfigFile(string exePath)
        {
            var directory = Path.GetDirectoryName(exePath) ?? "";
            _configPath = Path.Combine(directory, "Engine.keyusergrid.SPL");

            if (!File.Exists(_configPath))
            {
                var defaultConfig = new KeyUserGridConfig
                {
                    ConfigVersion = "1.0.0",
                    EnableIPTracking = true,
                    EnableAnalytics = true,
                    PingIntervalMs = 3000,
                    MaxCachedUsers = 1000,
                    AdminKey = GenerateAdminKey(),
                    AllowedIPRanges = new List<string>(),
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                var encrypted = EncryptChinese(json);
                File.WriteAllText(_configPath, encrypted);
            }
        }

        private static string GenerateAdminKey()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Substring(0, 24);
        }

        public static string EncryptChinese(string plaintext)
        {
            var chineseChars = "赵钱孙李周吴郑王冯陈褚卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜戚谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳酆鲍史";
            var sb = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var keyBytes = Encoding.UTF8.GetBytes("SmokeScreenENGINE2026");
            
            for (int i = 0; i < bytes.Length; i++)
            {
                var b = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);
                var idx1 = (b >> 4) % chineseChars.Length;
                var idx2 = (b & 0x0F) % chineseChars.Length;
                sb.Append(chineseChars[idx1]);
                sb.Append(chineseChars[idx2]);
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        public static string DecryptChinese(string ciphertext)
        {
            try
            {
                var chineseChars = "赵钱孙李周吴郑王冯陈褚卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜戚谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳酆鲍史";
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
                var sb = new StringBuilder();
                var keyBytes = Encoding.UTF8.GetBytes("SmokeScreenENGINE2026");
                
                for (int i = 0; i < decoded.Length; i += 2)
                {
                    var c1 = decoded[i];
                    var c2 = decoded[i + 1];
                    var idx1 = chineseChars.IndexOf(c1);
                    var idx2 = chineseChars.IndexOf(c2);
                    if (idx1 >= 0 && idx2 >= 0)
                    {
                        var b = (byte)((idx1 << 4) | idx2);
                        sb.Append((char)(b ^ keyBytes[(i / 2) % keyBytes.Length]));
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return "{}";
            }
        }

        public static void RegisterSession(string sessionId, string ipAddress)
        {
            lock (_lock)
            {
                var existing = _activeSessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (existing != null)
                {
                    existing.LastPing = DateTime.UtcNow;
                    existing.IsActive = true;
                }
                else
                {
                    if (_activeSessions.Count >= Config.MaxCachedUsers)
                    {
                        _activeSessions.RemoveAt(0);
                    }
                    _activeSessions.Add(new UserSessionInfo
                    {
                        SessionId = sessionId,
                        IPAddress = ipAddress,
                        ConnectedAt = DateTime.UtcNow,
                        LastPing = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }
        }

        public static void UpdatePing(string sessionId, int pingMs)
        {
            lock (_lock)
            {
                var session = _activeSessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session != null)
                {
                    session.PingMs = pingMs;
                    session.LastPing = DateTime.UtcNow;
                }
            }
        }

        public static void RemoveSession(string sessionId)
        {
            lock (_lock)
            {
                _activeSessions.RemoveAll(s => s.SessionId == sessionId);
            }
        }

        public static List<UserSessionInfo> GetActiveSessions()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                foreach (var session in _activeSessions)
                {
                    session.IsActive = (now - session.LastPing).TotalMinutes < 5;
                }
                return _activeSessions.Where(s => s.IsActive).ToList();
            }
        }

        public static int GetActiveUserCount() => GetActiveSessions().Count;

        public static string GetUserIPAddress()
        {
            try
            {
                using var client = new WebClient();
                var json = client.DownloadString("https://ipapi.co/json/");
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("ip", out var ip) ? ip.GetString() ?? "Unknown" : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetCurrentIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
