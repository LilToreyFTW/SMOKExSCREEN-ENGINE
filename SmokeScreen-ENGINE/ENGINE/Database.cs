using Dapper;
using Microsoft.Data.Sqlite;
using ENGINE.Models;

namespace ENGINE.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(string dbPath = "engine.db")
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private SqliteConnection Connect() => new(_connectionString);

    private void Initialize()
    {
        using var conn = Connect();
        conn.Open();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS LicenseKeys (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Key         TEXT NOT NULL UNIQUE,
                Duration    TEXT NOT NULL,
                DurationDays INTEGER NOT NULL,
                Status      TEXT NOT NULL DEFAULT 'Unused',
                HWID        TEXT,
                CreatedAt   TEXT NOT NULL,
                ActivatedAt TEXT,
                ExpiresAt   TEXT,
                IpAddress   TEXT,
                Country     TEXT,
                AppVersion  TEXT,
                IsOwner     INTEGER NOT NULL DEFAULT 0,
                IsRecoilKey INTEGER NOT NULL DEFAULT 0,
                GameType    TEXT
            );

            CREATE TABLE IF NOT EXISTS AnalyticsEvents (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                EventType   TEXT NOT NULL,
                Key         TEXT,
                HWID        TEXT,
                IpAddress   TEXT,
                AppVersion  TEXT,
                Note        TEXT,
                Timestamp   TEXT NOT NULL,
                Success     INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS HwidBans (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                HWID        TEXT NOT NULL UNIQUE,
                Reason      TEXT NOT NULL,
                BannedAt    TEXT NOT NULL
            );
        ");
        
        // Add new columns if they don't exist (for database migrations)
        try { conn.Execute("ALTER TABLE LicenseKeys ADD COLUMN IsRecoilKey INTEGER NOT NULL DEFAULT 0"); } catch { }
        try { conn.Execute("ALTER TABLE LicenseKeys ADD COLUMN GameType TEXT"); } catch { }
    }

    // ─── Keys ────────────────────────────────────────────────────────────────

    /// <summary>Seeds the owner key into the DB on startup. Safe to call every run — uses INSERT OR IGNORE.</summary>
    public void SeedOwnerKey(string ownerKey)
    {
        using var conn = Connect();
        conn.Execute(@"
            INSERT OR IGNORE INTO LicenseKeys (Key, Duration, DurationDays, Status, CreatedAt, IsOwner)
            VALUES (@key, 'Lifetime', -1, 'Active', @now, 1)",
            new { key = ownerKey, now = DateTime.UtcNow.ToString("o") });
    }

    public void InsertKeys(IEnumerable<LicenseKey> keys)
    {
        using var conn = Connect();
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var k in keys)
        {
            conn.Execute(@"
                INSERT OR IGNORE INTO LicenseKeys (Key, Duration, DurationDays, Status, CreatedAt, IsOwner, IsRecoilKey, GameType)
                VALUES (@Key, @Duration, @DurationDays, @Status, @CreatedAt, @IsOwner, @IsRecoilKey, @GameType)",
                new { k.Key, k.Duration, k.DurationDays, k.Status, CreatedAt = k.CreatedAt.ToString("o"), IsOwner = k.IsOwner ? 1 : 0, IsRecoilKey = k.IsRecoilKey ? 1 : 0, k.GameType }, tx);
        }
        tx.Commit();
    }

    public void InsertRecoilKeys(IEnumerable<LicenseKey> keys)
    {
        using var conn = Connect();
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var k in keys)
        {
            conn.Execute(@"
                INSERT OR IGNORE INTO LicenseKeys (Key, Duration, DurationDays, Status, CreatedAt, IsRecoilKey, GameType)
                VALUES (@Key, @Duration, @DurationDays, @Status, @CreatedAt, 1, @GameType)",
                new { k.Key, k.Duration, k.DurationDays, k.Status, CreatedAt = k.CreatedAt.ToString("o"), k.GameType }, tx);
        }
        tx.Commit();
    }

    public LicenseKey? GetKey(string key)
    {
        using var conn = Connect();
        var row = conn.QueryFirstOrDefault(@"SELECT * FROM LicenseKeys WHERE Key = @key", new { key });
        return row == null ? null : MapKey(row);
    }

    public bool ActivateKey(string key, string hwid, string? ip, string? appVersion)
    {
        using var conn = Connect();
        conn.Open();

        var existing = GetKey(key);
        if (existing == null || existing.Status != "Unused") return false;

        var now = DateTime.UtcNow;
        DateTime? expires = existing.DurationDays == -1 ? null : now.AddDays(existing.DurationDays);

        conn.Execute(@"
            UPDATE LicenseKeys SET
                Status = 'Active',
                HWID = @hwid,
                ActivatedAt = @now,
                ExpiresAt = @expires,
                IpAddress = @ip,
                AppVersion = @appVersion
            WHERE Key = @key",
            new { hwid, now = now.ToString("o"), expires = expires?.ToString("o"), ip, appVersion, key });

        LogEvent(conn, "Activate", key, hwid, ip, appVersion, "Key activated", true);
        return true;
    }

    public (bool valid, string reason) ValidateKey(string key, string hwid, string? ip, string? appVersion)
    {
        using var conn = Connect();

        // Check HWID ban
        var ban = conn.QueryFirstOrDefault("SELECT * FROM HwidBans WHERE HWID = @hwid", new { hwid });
        if (ban != null)
        {
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "HWID banned", false);
            return (false, "HWID_BANNED");
        }

        var existing = GetKey(key);
        if (existing == null)
        {
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "Key not found", false);
            return (false, "INVALID_KEY");
        }

        if (existing.Status == "Revoked")
        {
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "Key revoked", false);
            return (false, "KEY_REVOKED");
        }

        // ── Owner key: bypasses HWID binding and expiry — always valid ──────
        if (existing.IsOwner)
        {
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "Owner key validated", true);
            return (true, "OK");
        }

        if (existing.Status == "Unused")
        {
            // Auto-activate on first validate
            ActivateKey(key, hwid, ip, appVersion);
            existing = GetKey(key)!;
        }

        if (existing.HWID != hwid)
        {
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "HWID mismatch", false);
            return (false, "HWID_MISMATCH");
        }

        if (existing.ExpiresAt.HasValue && existing.ExpiresAt < DateTime.UtcNow)
        {
            conn.Execute("UPDATE LicenseKeys SET Status = 'Expired' WHERE Key = @key", new { key });
            LogEvent(conn, "Validate", key, hwid, ip, appVersion, "Key expired", false);
            return (false, "KEY_EXPIRED");
        }

        LogEvent(conn, "Validate", key, hwid, ip, appVersion, "Valid", true);
        return (true, "OK");
    }

    public bool RevokeKey(string key, string reason)
    {
        using var conn = Connect();
        int rows = conn.Execute("UPDATE LicenseKeys SET Status = 'Revoked' WHERE Key = @key", new { key });
        if (rows > 0) LogEvent(conn, "Revoke", key, null, null, null, reason, true);
        return rows > 0;
    }

    public bool BanHwid(string hwid, string reason)
    {
        using var conn = Connect();
        try
        {
            conn.Execute(@"INSERT OR REPLACE INTO HwidBans (HWID, Reason, BannedAt) VALUES (@hwid, @reason, @now)",
                new { hwid, reason, now = DateTime.UtcNow.ToString("o") });
            // Also revoke all keys tied to this HWID
            conn.Execute("UPDATE LicenseKeys SET Status = 'Revoked' WHERE HWID = @hwid", new { hwid });
            LogEvent(conn, "Ban", null, hwid, null, null, reason, true);
            return true;
        }
        catch { return false; }
    }

    public List<LicenseKey> GetAllKeys(string? statusFilter = null)
    {
        using var conn = Connect();
        var sql = statusFilter == null
            ? "SELECT * FROM LicenseKeys ORDER BY CreatedAt DESC"
            : "SELECT * FROM LicenseKeys WHERE Status = @statusFilter ORDER BY CreatedAt DESC";
        var rows = conn.Query(sql, new { statusFilter });
        return rows.Select(r => (LicenseKey)MapKey(r)).ToList();
    }

    public List<AnalyticsEvent> GetAnalytics(int limit = 500)
    {
        using var conn = Connect();
        var rows = conn.Query("SELECT * FROM AnalyticsEvents ORDER BY Timestamp DESC LIMIT @limit", new { limit });
        var result = new List<AnalyticsEvent>();
        foreach (var r in rows)
            result.Add(MapEvent(r));
        return result;
    }

    public Dictionary<string, int> GetAnalyticsSummary()
    {
        using var conn = Connect();
        return new Dictionary<string, int>
        {
            ["TotalKeys"]      = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM LicenseKeys"),
            ["UnusedKeys"]     = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM LicenseKeys WHERE Status='Unused'"),
            ["ActiveKeys"]     = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM LicenseKeys WHERE Status='Active'"),
            ["ExpiredKeys"]    = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM LicenseKeys WHERE Status='Expired'"),
            ["RevokedKeys"]    = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM LicenseKeys WHERE Status='Revoked'"),
            ["BannedHwids"]    = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM HwidBans"),
            ["TotalEvents"]    = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AnalyticsEvents"),
            ["FailedValidations"] = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AnalyticsEvents WHERE EventType='Validate' AND Success=0"),
        };
    }

    public List<dynamic> GetTopHwids(int top = 10)
    {
        using var conn = Connect();
        return conn.Query(@"
            SELECT HWID, COUNT(*) as Requests, SUM(CASE WHEN Success=0 THEN 1 ELSE 0 END) as Failures
            FROM AnalyticsEvents WHERE HWID IS NOT NULL
            GROUP BY HWID ORDER BY Requests DESC LIMIT @top", new { top })
            .ToList<dynamic>();
    }

    public List<dynamic> GetDailyActivity(int days = 14)
    {
        using var conn = Connect();
        return conn.Query(@"
            SELECT DATE(Timestamp) as Day, COUNT(*) as Events,
                   SUM(CASE WHEN Success=1 THEN 1 ELSE 0 END) as Successes,
                   SUM(CASE WHEN Success=0 THEN 1 ELSE 0 END) as Failures
            FROM AnalyticsEvents
            WHERE Timestamp >= @since
            GROUP BY DATE(Timestamp) ORDER BY Day DESC",
            new { since = DateTime.UtcNow.AddDays(-days).ToString("o") })
            .ToList<dynamic>();
    }

    public int GetActiveUsersCount()
    {
        using var conn = Connect();
        // Count unique HWIDs with active sessions in the last hour
        var oneHourAgo = DateTime.UtcNow.AddHours(-1).ToString("o");
        return conn.ExecuteScalar<int>(@"
            SELECT COUNT(DISTINCT HWID) FROM AnalyticsEvents 
            WHERE HWID IS NOT NULL AND Timestamp >= @since",
            new { since = oneHourAgo });
    }

    public dynamic GetTodayStats()
    {
        using var conn = Connect();
        var today = DateTime.UtcNow.Date.ToString("o");
        var tomorrow = DateTime.UtcNow.Date.AddDays(1).ToString("o");
        
        var generated = conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM LicenseKeys WHERE CreatedAt >= @today",
            new { today });
            
        var activated = conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM LicenseKeys WHERE ActivatedAt >= @today",
            new { today });
            
        var failed = conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM AnalyticsEvents WHERE Timestamp >= @today AND Success=0",
            new { today });
            
        // Estimate revenue (rough calculation)
        var revenue = generated * 5.0m + activated * 10.0m;
        
        return new { Generated = generated, Activated = activated, Failed = failed, Revenue = revenue };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void LogEvent(SqliteConnection conn, string type, string? key, string? hwid,
        string? ip, string? appVersion, string? note, bool success)
    {
        conn.Execute(@"
            INSERT INTO AnalyticsEvents (EventType, Key, HWID, IpAddress, AppVersion, Note, Timestamp, Success)
            VALUES (@type, @key, @hwid, @ip, @appVersion, @note, @now, @success)",
            new { type, key, hwid, ip, appVersion, note, now = DateTime.UtcNow.ToString("o"), success = success ? 1 : 0 });
    }

    private static LicenseKey MapKey(dynamic r) => new()
    {
        Id = (int)r.Id,
        Key = r.Key,
        Duration = r.Duration,
        DurationDays = (int)r.DurationDays,
        Status = r.Status,
        HWID = r.HWID,
        CreatedAt = DateTime.Parse(r.CreatedAt),
        ActivatedAt = r.ActivatedAt == null ? null : DateTime.Parse(r.ActivatedAt),
        ExpiresAt = r.ExpiresAt == null ? null : DateTime.Parse(r.ExpiresAt),
        IpAddress = r.IpAddress,
        Country = r.Country,
        AppVersion = r.AppVersion,
        IsOwner = r.IsOwner == 1,
        IsRecoilKey = r.IsRecoilKey == 1,
        GameType = r.GameType,
    };

    private static AnalyticsEvent MapEvent(dynamic r) => new()
    {
        Id = (int)r.Id,
        EventType = r.EventType,
        Key = r.Key,
        HWID = r.HWID,
        IpAddress = r.IpAddress,
        AppVersion = r.AppVersion,
        Note = r.Note,
        Timestamp = DateTime.Parse(r.Timestamp),
        Success = r.Success == 1,
    };
}
