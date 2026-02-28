using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    /// <summary>
    /// Thin wrapper around the Vercel API.
    /// All calls require a valid Bearer session token from DiscordAuth.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _client;
        private readonly string     _token;

        public ApiService(string sessionToken)
        {
            _token  = sessionToken;
            _client = new HttpClient { BaseAddress = new Uri(DiscordAuth.API_BASE) };
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", sessionToken);
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        // ── Dashboard stats ────────────────────────────────────────────────
        public async Task<DashboardData?> GetStatsAsync()
        {
            try
            {
                // /keys/validate returns licensed status + key count
                var licRes  = await _client.GetStringAsync("/keys/validate");
                var licData = JsonConvert.DeserializeObject<LicenseValidateResponse>(licRes);

                // /health returns server + DB status
                var healthRes  = await _client.GetStringAsync("/health");
                var healthData = JsonConvert.DeserializeObject<HealthResponse>(healthRes);

                return new DashboardData
                {
                    CloudStatus = healthData?.Status == "ok" ? "Online" : "Degraded",
                    ActiveUsers = licData?.KeyCount ?? 0,
                    Licensed    = licData?.Licensed ?? false,
                };
            }
            catch { return null; }
        }

        // ── Validate license ───────────────────────────────────────────────
        public async Task<bool> IsLicensedAsync()
        {
            try
            {
                var res  = await _client.GetStringAsync("/keys/validate");
                var data = JsonConvert.DeserializeObject<LicenseValidateResponse>(res);
                return data?.Licensed == true;
            }
            catch { return false; }
        }

        // ── Private response types ─────────────────────────────────────────
        private class LicenseValidateResponse
        {
            [JsonProperty("licensed")]  public bool Licensed  { get; set; }
            [JsonProperty("key_count")] public int  KeyCount  { get; set; }
        }
        private class HealthResponse
        {
            [JsonProperty("status")] public string? Status { get; set; }
            [JsonProperty("db")]     public bool    Db     { get; set; }
        }
    }

    public class DashboardData
    {
        public string CloudStatus { get; set; } = "Offline";
        public int    ActiveUsers { get; set; } = 0;
        public bool   Licensed    { get; set; } = false;
    }
}
