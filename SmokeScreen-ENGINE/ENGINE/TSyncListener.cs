using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    /// <summary>
    /// Tiny listener that receives key updates from the website via /api/tsync.
    /// Runs in the background and updates the local KeyCache.
    /// </summary>
    public static class TSyncListener
    {
        private const string SYNC_KEY = "tsasync-key-2025-02-27-a7f3c9e1";
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly System.Threading.Timer _timer = new System.Threading.Timer(_ => Tick(), null, Timeout.Infinite, Timeout.Infinite);

        public static void Start()
        {
            // Poll every 12 seconds (offset from website’s 10s)
            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(12));
        }

        private static async void Tick()
        {
            try
            {
                var res = await _http.GetAsync("https://smok-ex-screen-engine.vercel.app/api/tsync");
                if (!res.IsSuccessStatusCode) return;
                var json = await res.Content.ReadAsStringAsync();
                var payload = JsonConvert.DeserializeAnonymousType(json, new { keys = new List<string>(), key = "" });
                if (payload?.key != SYNC_KEY) return;
                if (payload?.keys == null) return;

                // Insert/merge into KeyCache
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={KeyCache.DbPath}");
                conn.Open();
                int added = 0;
                foreach (var k in payload.keys)
                {
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT OR IGNORE INTO keys (key_value, duration_type, used, redeemed_at, expires_at, fetched_at) VALUES ($1,'',0,NULL,NULL,$2)";
                    cmd.Parameters.AddWithValue("$1", k.Trim());
                    cmd.Parameters.AddWithValue("$2", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    added += cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Silently ignore errors; this is invisible background sync
            }
        }
    }
}
