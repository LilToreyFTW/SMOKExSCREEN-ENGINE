using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public static class KeyExtension
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private const string ApiUrl = "https://smok-ex-screen-engine.vercel.app/api/tsync";
        private const string SyncKey = "tsasync-key-2025-02-27-a7f3c9e1";
        private static readonly string _sourcelinkPath = Path.Combine(AppContext.BaseDirectory, "SmokeScreenEngineGUI.sourcelink.json");

        public static async Task SaveKeysToAllAsync(IEnumerable<string> keys, string durationType)
        {
            var keyList = new List<object>();
            foreach (var key in keys)
            {
                keyList.Add(new { key_value = key, duration_type = durationType, duration_ms = GetDurationMs(durationType) });
            }

            // 1. Send to website server via /api/tsync
            await SendToWebsiteAsync(keyList);

            // 2. Send to Discord bot via webhook (if configured)
            await SendToDiscordAsync(keys, durationType);

            // 3. Save to local sourcelink.json
            await SaveToSourcelinkAsync(keys, durationType);
        }

        private static async Task SendToWebsiteAsync(List<object> keys)
        {
            try
            {
                var payload = new { key = SyncKey, keys };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(ApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Keys sent to website: {result}");
                }
                else
                {
                    Console.WriteLine($"Failed to send keys to website: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Website send error: {ex.Message}");
            }
        }

        private static async Task SendToDiscordAsync(IEnumerable<string> keys, string durationType)
        {
            try
            {
                var payload = new { keys, durationType };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("http://localhost:9877/api/bot/keys", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Keys sent to Discord bot API: {result}");
                }
                else
                {
                    Console.WriteLine($"Failed to send keys to Discord bot API: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discord bot API send error: {ex.Message}");
            }
        }

        private static async Task SaveToSourcelinkAsync(IEnumerable<string> keys, string durationType)
        {
            try
            {
                var data = new Dictionary<string, object>();
                if (File.Exists(_sourcelinkPath))
                {
                    var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(await File.ReadAllTextAsync(_sourcelinkPath));
                    if (existing != null) data = existing;
                }

                var keyInfo = new List<object>();
                if (data.ContainsKey("keys"))
                {
                    keyInfo = JsonSerializer.Deserialize<List<object>>(data["keys"].ToString());
                }

                foreach (var key in keys)
                {
                    keyInfo.Add(new { key, duration_type = durationType, generated_at = DateTime.UtcNow });
                }

                data["keys"] = keyInfo;
                data["last_generated"] = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_sourcelinkPath, json);
                Console.WriteLine($"Saved {keys.Count()} keys to sourcelink.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sourcelink save error: {ex.Message}");
            }
        }

        private static long GetDurationMs(string durationType)
        {
            return durationType switch
            {
                "1_MONTH" => 30L * 24 * 60 * 60 * 1000,
                "3_MONTHS" => 90L * 24 * 60 * 60 * 1000,
                "6_MONTHS" => 180L * 24 * 60 * 60 * 1000,
                "1_YEAR" => 365L * 24 * 60 * 60 * 1000,
                _ => 30L * 24 * 60 * 60 * 1000
            };
        }
    }
}
