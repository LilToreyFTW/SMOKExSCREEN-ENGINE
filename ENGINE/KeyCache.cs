using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    /// <summary>
    /// Simple local SQLite cache to remember keys between runs.
    /// - Stores key, duration_type, used flag, redeemed_at, expires_at.
    /// - Provides helpers to fetch fresh unused keys from the website (admin session).
    /// - Prevents reuse of already-used/sold keys.
    /// </summary>
    public static class KeyCache
    {
        public static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmokeScreenEngine",
            "keys.db");

        static KeyCache()
        {
            var dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            InitDb();
        }

        private static void InitDb()
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS keys (
                    key_value TEXT PRIMARY KEY,
                    duration_type TEXT,
                    used INTEGER NOT NULL DEFAULT 0,
                    redeemed_at INTEGER,
                    expires_at INTEGER,
                    fetched_at INTEGER NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Insert or update keys from the website admin endpoint.
        /// </summary>
        public static async Task<int> SyncFromWebsiteAsync(string bearerToken, string apiBase = "https://smok-ex-screen-engine.vercel.app")
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{apiBase}/admin/keys?status=unused&limit=500");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            var res = await http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return 0;

            var json = await res.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeAnonymousType(json, new { keys = new List<WebsiteKey>() });
            if (data?.keys == null) return 0;

            int added = 0;
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var tx = conn.BeginTransaction();
            foreach (var wk in data.keys)
            {
                if (string.IsNullOrWhiteSpace(wk.key_value)) continue;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO keys (key_value, duration_type, used, redeemed_at, expires_at, fetched_at)
                    VALUES ($1,$2,$3,$4,$5,$6)";
                cmd.Parameters.AddWithValue("$1", wk.key_value);
                cmd.Parameters.AddWithValue("$2", wk.duration_type ?? "");
                cmd.Parameters.AddWithValue("$3", wk.used ? 1 : 0);
                cmd.Parameters.AddWithValue("$4", wk.redeemed_at);
                cmd.Parameters.AddWithValue("$5", wk.expires_at);
                cmd.Parameters.AddWithValue("$6", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                added += cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return added;
        }

        /// <summary>
        /// Get the next unused key from cache (any duration).
        /// Returns null if none available.
        /// </summary>
        public static CachedKey? GetNextUnusedKey()
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT key_value, duration_type, used, redeemed_at, expires_at, fetched_at
                FROM keys
                WHERE used = 0
                ORDER BY fetched_at ASC
                LIMIT 1";
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new CachedKey
            {
                Key = rdr.GetString(0),
                DurationType = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                Used = rdr.GetInt32(2) != 0,
                RedeemedAt = rdr.IsDBNull(3) ? (long?)null : rdr.GetInt64(3),
                ExpiresAt = rdr.IsDBNull(4) ? (long?)null : rdr.GetInt64(4),
                FetchedAt = rdr.GetInt64(5)
            };
        }

        /// <summary>
        /// Mark a key as used/redeemed locally (after successful redeem).
        /// </summary>
        public static void MarkUsed(string key)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE keys SET used = 1, redeemed_at = $1 WHERE key_value = $2";
            cmd.Parameters.AddWithValue("$1", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            cmd.Parameters.AddWithValue("$2", key);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Return a list of all cached keys (for debugging/inspection).
        /// </summary>
        public static List<CachedKey> GetAll()
        {
            var list = new List<CachedKey>();
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT key_value, duration_type, used, redeemed_at, expires_at, fetched_at FROM keys ORDER BY fetched_at DESC";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new CachedKey
                {
                    Key = rdr.GetString(0),
                    DurationType = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                    Used = rdr.GetInt32(2) != 0,
                    RedeemedAt = rdr.IsDBNull(3) ? (long?)null : rdr.GetInt64(3),
                    ExpiresAt = rdr.IsDBNull(4) ? (long?)null : rdr.GetInt64(4),
                    FetchedAt = rdr.GetInt64(5)
                });
            }
            return list;
        }

        // Types for JSON deserialization from /admin/keys
        private class WebsiteKey
        {
            public string? key_value { get; set; }
            public string? duration_type { get; set; }
            public bool used { get; set; }
            public long? redeemed_at { get; set; }
            public long? expires_at { get; set; }
        }

        public class CachedKey
        {
            public string Key { get; set; } = "";
            public string DurationType { get; set; } = "";
            public bool Used { get; set; }
            public long? RedeemedAt { get; set; }
            public long? ExpiresAt { get; set; }
            public long FetchedAt { get; set; }
        }
    }
}
