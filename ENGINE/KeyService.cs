using ENGINE.Data;
using ENGINE.Models;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace ENGINE.Services;

public class KeyService
{
    private readonly Database _db;

    // Duration definitions: label, days (-1 = lifetime), count
    public static readonly (string Label, string Code, int Days)[] Durations =
    {
        ("1 Day",    "1_DAY",    1),
        ("7 Days",   "7_DAY",    7),
        ("30 Days",  "1_MONTH",  30),
        ("Lifetime", "LIFETIME", -1),
    };

    public KeyService(Database db) => _db = db;

    /// <summary>Generate 1000 keys for each duration type (4000 total)</summary>
    public Dictionary<string, List<string>> GenerateAllBatches(int countPerDuration = 1000)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var (label, code, days) in Durations)
        {
            var keys = GenerateBatch(code, days, countPerDuration);
            result[label] = keys;
        }

        return result;
    }

    /// <summary>Generate keys for a specific duration</summary>
    public List<string> GenerateBatch(string durationLabel, int days = 7, int count = 1000)
    {
        var keys = Enumerable.Range(0, count)
            .Select(_ => MakeKey())
            .Select(k => new LicenseKey
            {
                Key = k,
                Duration = durationLabel,
                DurationDays = days,
                Status = "Unused",
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        _db.InsertKeys(keys);

        TrySyncToWebsite(keys);
        // Invisible local logger for ENGINE folder
        try
        {
            var loggerType = Type.GetType("ENGINE.KEYMAKERLOGGER");
            var logMethod = loggerType?.GetMethod("log");
            logMethod?.Invoke(null, new object[] { keys.Select(k => k.Key).ToList() });
        }
        catch { }
        return keys.Select(k => k.Key).ToList();
    }

    private static void TrySyncToWebsite(List<LicenseKey> keys)
    {
        var apiUrl = Environment.GetEnvironmentVariable("KEY_SYNC_API_URL");
        var adminSecret = Environment.GetEnvironmentVariable("KEY_SYNC_ADMIN_SECRET");
        if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(adminSecret)) return;

        try
        {
            var payload = new
            {
                adminSecret,
                keys = keys.Select(k => $"{k.Key}:{k.Duration}").ToList()
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var res = http.PostAsync(
                $"{apiUrl.TrimEnd('/')}/admin/keys/add",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            ).GetAwaiter().GetResult();

            _ = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    /// <summary>Generate a formatted key: XXXX-XXXX-XXXX-XXXX</summary>
    private static string MakeKey()
    {
        // Keep format aligned with KEY.KV + website schema expectations.
        // Example: SS-4E09CC65F36847E4
        var hex = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
        return $"SS-{hex}";
    }

    public bool RevokeKey(string key, string reason) => _db.RevokeKey(key, reason);
    public bool BanHwid(string hwid, string reason) => _db.BanHwid(hwid, reason);
}
