using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class WirelessReceiver
    {
        private readonly string _serverIp = "192.168.1.100"; // Hardcoded server IP
        private readonly int _serverPort = 8080;
        private readonly string _encryptionKey = "Bl0wdart1368087024401252393_SmokeScreen2026";
        private HttpClient _httpClient;
        private System.Threading.Timer _heartbeatTimer;
        private System.Threading.Timer _cleanupTimer;
        
        public class UserSession
        {
            public string UserId { get; set; } = "";
            public string Username { get; set; } = "";
            public string DiscordId { get; set; } = "";
            public string? KeyType { get; set; }
            public string? KeyDuration { get; set; }
            public DateTime? KeyExpiry { get; set; }
            public DateTime LastHeartbeat { get; set; }
            public string? IpAddress { get; set; }
            public string? ComputerName { get; set; }
            public bool IsOnline { get; set; }
            public string SessionId { get; set; } = "";
        }

        private static Dictionary<string, UserSession> _activeUsers = new Dictionary<string, UserSession>();
        private static readonly object _lockObject = new object();

        public WirelessReceiver()
        {
            _httpClient = new HttpClient();
            _heartbeatTimer = new System.Threading.Timer(_ => CleanupInactiveUsers(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _cleanupTimer = new System.Threading.Timer(async _ => await SendHeartbeatToServer(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public string GenerateEncryptionFile()
        {
            var encryptionData = new
            {
                ServerIp = _serverIp,
                ServerPort = _serverPort,
                Timestamp = DateTime.UtcNow,
                SessionId = Guid.NewGuid().ToString(),
                PublicKey = GeneratePublicKey(),
                Version = "1.0.0"
            };

            var json = JsonSerializer.Serialize(encryptionData);
            var encrypted = EncryptData(json);
            
            return Convert.ToBase64String(encrypted);
        }

        private string GeneratePublicKey()
        {
            return $"SS_{DateTime.UtcNow.Ticks}_{_encryptionKey.GetHashCode():X}";
        }

        public bool ValidateConnection(string encryptedData)
        {
            try
            {
                var decrypted = DecryptData(encryptedData);
                var data = JsonSerializer.Deserialize<dynamic>(decrypted);
                
                return data?.ServerIp == _serverIp && data?.ServerPort == _serverPort;
            }
            catch
            {
                return false;
            }
        }

        public async Task RegisterUserAsync(UserSession session)
        {
            try
            {
                session.SessionId = Guid.NewGuid().ToString();
                session.LastHeartbeat = DateTime.UtcNow;
                session.IsOnline = true;

                lock (_lockObject)
                {
                    _activeUsers[session.UserId] = session;
                }

                await SendUserUpdateToServer(session, "REGISTER");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user: {ex.Message}");
            }
        }

        public async Task UpdateUserHeartbeatAsync(string userId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeUsers.ContainsKey(userId))
                    {
                        _activeUsers[userId].LastHeartbeat = DateTime.UtcNow;
                        _activeUsers[userId].IsOnline = true;
                    }
                }

                await SendHeartbeatToServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating heartbeat: {ex.Message}");
            }
        }

        public List<UserSession> GetActiveUsers()
        {
            lock (_lockObject)
            {
                return new List<UserSession>(_activeUsers.Values);
            }
        }

        public int GetActiveUserCount()
        {
            lock (_lockObject)
            {
                return _activeUsers.Count;
            }
        }

        private void CleanupInactiveUsers()
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var toRemove = new List<string>();

                foreach (var kvp in _activeUsers)
                {
                    if (kvp.Value.LastHeartbeat < cutoff)
                    {
                        kvp.Value.IsOnline = false;
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var userId in toRemove)
                {
                    _activeUsers.Remove(userId);
                }
            }
        }

        private async Task SendHeartbeatToServer()
        {
            try
            {
                var activeUsers = GetActiveUsers();
                var heartbeatData = new
                {
                    ServerId = "Bl0wdart1368087024401252393",
                    ActiveUsers = activeUsers.Count,
                    Timestamp = DateTime.UtcNow,
                    Users = activeUsers.Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.DiscordId,
                        u.KeyType,
                        u.KeyDuration,
                        u.LastHeartbeat,
                        u.IpAddress,
                        u.IsOnline
                    })
                };

                var json = JsonSerializer.Serialize(heartbeatData);
                var encrypted = EncryptData(json);
                
                await _httpClient.PostAsync($"http://{_serverIp}:{_serverPort}/api/heartbeat", 
                    new StringContent(Convert.ToBase64String(encrypted), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat: {ex.Message}");
            }
        }

        private async Task SendUserUpdateToServer(UserSession session, string action)
        {
            try
            {
                var userData = new
                {
                    Action = action,
                    ServerId = "Bl0wdart1368087024401252393",
                    User = session,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(userData);
                var encrypted = EncryptData(json);
                
                await _httpClient.PostAsync($"http://{_serverIp}:{_serverPort}/api/user", 
                    new StringContent(Convert.ToBase64String(encrypted), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending user update: {ex.Message}");
            }
        }

        private byte[] EncryptData(string data)
        {
            // Simple XOR encryption with the hardcoded key
            var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encrypted = new byte[dataBytes.Length];

            for (int i = 0; i < dataBytes.Length; i++)
            {
                encrypted[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return encrypted;
        }

        private string DecryptData(string base64Data)
        {
            var encrypted = Convert.FromBase64String(base64Data);
            var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
            var decrypted = new byte[encrypted.Length];

            for (int i = 0; i < encrypted.Length; i++)
            {
                decrypted[i] = (byte)(encrypted[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
