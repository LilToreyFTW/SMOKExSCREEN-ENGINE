using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public static class KeySender
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private const string ApiUrl = "https://smok-ex-screen-engine.vercel.app/api/tsync";
        private const string SyncKey = "tsasync-key-2025-02-27-a7f3c9e1";

        public static async Task SendKeysToBotAsync(IEnumerable<string> keys)
        {
            try
            {
                var payload = new
                {
                    key = SyncKey,
                    keys = keys
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(ApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Keys sent to bot: {result}");
                }
                else
                {
                    Console.WriteLine($"Failed to send keys: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeySender error: {ex.Message}");
            }
        }
    }
}
