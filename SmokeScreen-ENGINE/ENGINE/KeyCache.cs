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
            
            // Create table with all columns
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS keys (
                    key_value TEXT PRIMARY KEY,
                    duration_type TEXT,
                    used INTEGER NOT NULL DEFAULT 0,
                    redeemed_at INTEGER,
                    expires_at INTEGER,
                    fetched_at INTEGER NOT NULL,
                    game TEXT,
                    encrypted INTEGER NOT NULL DEFAULT 0,
                    key_id TEXT,
                    encrypted_data TEXT
                )";
            cmd.ExecuteNonQuery();
            
            // Add missing columns for legacy databases
            try {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE keys ADD COLUMN encrypted INTEGER NOT NULL DEFAULT 0";
                alterCmd.ExecuteNonQuery();
            } catch { /* Column already exists */ }
            
            try {
                using var alterCmd2 = conn.CreateCommand();
                alterCmd2.CommandText = "ALTER TABLE keys ADD COLUMN key_id TEXT";
                alterCmd2.ExecuteNonQuery();
            } catch { /* Column already exists */ }
            
            try {
                using var alterCmd3 = conn.CreateCommand();
                alterCmd3.CommandText = "ALTER TABLE keys ADD COLUMN encrypted_data TEXT";
                alterCmd3.ExecuteNonQuery();
            } catch { /* Column already exists */ }
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

        public class KeyInfo
        {
            public string Key { get; set; } = "";
            public string Duration { get; set; } = "";
            public bool Used { get; set; }
            public string Game { get; set; } = "";
            public bool Encrypted { get; set; } = false;
            public string KeyId { get; set; } = "";
            public string EncryptedData { get; set; } = "";
        }

        /// <summary>
        /// Add an encrypted key to the cache
        /// </summary>
        public static void AddKey(KeyInfo keyInfo)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT OR REPLACE INTO keys 
                (key_value, duration_type, used, game, encrypted, key_id, encrypted_data, fetched_at) 
                VALUES (@key, @duration, @used, @game, @encrypted, @key_id, @encrypted_data, @fetched_at)";

            cmd.Parameters.AddWithValue("@key", keyInfo.Key);
            cmd.Parameters.AddWithValue("@duration", keyInfo.Duration);
            cmd.Parameters.AddWithValue("@used", keyInfo.Used ? 1 : 0);
            cmd.Parameters.AddWithValue("@game", keyInfo.Game);
            cmd.Parameters.AddWithValue("@encrypted", keyInfo.Encrypted ? 1 : 0);
            cmd.Parameters.AddWithValue("@key_id", keyInfo.KeyId);
            cmd.Parameters.AddWithValue("@encrypted_data", keyInfo.EncryptedData);
            cmd.Parameters.AddWithValue("@fetched_at", DateTimeOffset.Now.ToUnixTimeMilliseconds());

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Get an encrypted key by ID
        /// </summary>
        public static KeyInfo? GetEncryptedKey(string keyId)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT key_value, duration_type, used, game, encrypted, key_id, encrypted_data 
                FROM keys 
                WHERE key_id = @key_id AND encrypted = 1";

            cmd.Parameters.AddWithValue("@key_id", keyId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new KeyInfo
                {
                    Key = reader.GetString(0),
                    Duration = reader.GetString(1),
                    Used = reader.GetBoolean(2),
                    Game = reader.GetString(3),
                    Encrypted = reader.GetBoolean(4),
                    KeyId = reader.GetString(5),
                    EncryptedData = reader.GetString(6)
                };
            }

            return null;
        }

        /// <summary>
        /// Clear all encrypted keys from cache
        /// </summary>
        public static void ClearEncryptedKeys()
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM keys WHERE encrypted = 1";
            cmd.ExecuteNonQuery();
        }
    }
}
