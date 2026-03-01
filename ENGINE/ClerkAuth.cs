using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public static class ClerkAuth
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
        private static readonly string API_BASE = "https://smok-ex-screen-engine.vercel.app/api";
        private static string? _sessionToken;
        private static string? _hwid;

        public static string? SessionToken => _sessionToken;
        public static string? Hwid => _hwid;

        public static async Task<bool> SyncSessionAsync()
        {
            if (string.IsNullOrEmpty(_hwid)) _hwid = HardwareId.GetId();
            if (string.IsNullOrEmpty(_sessionToken))
            {
                // In real app, perform Clerk OAuth or token exchange; for demo, generate a mock token
                _sessionToken = Guid.NewGuid().ToString("N");
            }

            try
            {
                var payload = new { token = _sessionToken, hwid = _hwid };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync($"{API_BASE}/clerk-sync", content);
                resp.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RedeemKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(_hwid)) _hwid = HardwareId.GetId();
            if (string.IsNullOrEmpty(_sessionToken)) await SyncSessionAsync();

            try
            {
                var payload = new { key, hwid = _hwid };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync($"{API_BASE}/keys/redeem-clerk", content);
                resp.EnsureSuccessStatusCode();
                var resultJson = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                return result.TryGetProperty("ok", out var ok) && ok.GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        public static void SignOut()
        {
            _sessionToken = null;
        }
    }
}
