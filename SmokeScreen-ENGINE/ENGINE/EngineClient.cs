// ─────────────────────────────────────────────────────────────────────────────
// ENGINE Client Helper
// Drop this file into your client app.
// Validates the session token (from login) against the Vercel backend.
// Requires: Newtonsoft.Json  (add via NuGet)
//           System.Management  (Windows — for HWID)
// ─────────────────────────────────────────────────────────────────────────────

using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace EngineClient;

public static class LicenseValidator
{
    // Points to the same Vercel backend as DiscordAuth.cs
    private const string ApiBase = "https://smok-ex-screen-engine.vercel.app";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>
    /// Validates the user's session token by calling /keys/validate on the backend.
    /// Returns (true, "OK") if the user has at least one redeemed license key.
    /// Call this after the user has logged in via DiscordAuth / EngineLoginManager.
    /// </summary>
    public static async Task<(bool Valid, string Reason)> ValidateAsync(
        string sessionToken,
        string appVersion = "1.0.0")
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/keys/validate");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
            req.Headers.Add("X-App-Version", appVersion);
            req.Headers.Add("X-HWID", GetHwid());

            var response = await Http.SendAsync(req);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return (false, "SESSION_EXPIRED");

            if (!response.IsSuccessStatusCode)
                return (false, $"SERVER_ERROR_{(int)response.StatusCode}");

            var json   = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ValidationResponse>(json);

            return result?.Licensed == true
                ? (true, "OK")
                : (false, "NO_LICENSE");
        }
        catch (HttpRequestException) { return (false, "SERVER_OFFLINE"); }
        catch (TaskCanceledException) { return (false, "SERVER_TIMEOUT"); }
        catch { return (false, "CLIENT_ERROR"); }
    }

    /// <summary>Checks if the Vercel backend is reachable.</summary>
    public static async Task<bool> PingAsync()
    {
        try
        {
            var res = await Http.GetAsync($"{ApiBase}/ping");
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>
    /// Generates a stable hardware fingerprint for this machine.
    /// Uses CPU ID + Motherboard serial, SHA-256 hashed to 32 hex chars.
    /// </summary>
    public static string GetHwid()
    {
        try
        {
            string cpu   = GetWmiValue("Win32_Processor",  "ProcessorId");
            string board = GetWmiValue("Win32_BaseBoard",  "SerialNumber");
            byte[] hash  = SHA256.HashData(Encoding.UTF8.GetBytes($"{cpu}|{board}"));
            return Convert.ToHexString(hash)[..32];
        }
        catch
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName));
            return Convert.ToHexString(hash)[..32];
        }
    }

    private static string GetWmiValue(string wmiClass, string property)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
        foreach (ManagementObject obj in searcher.Get())
            return obj[property]?.ToString()?.Trim() ?? "UNKNOWN";
        return "UNKNOWN";
    }

    // ─── Human-readable reason descriptions ──────────────────────────────────

    public static string DescribeReason(string reason) => reason switch
    {
        "OK"              => "License is valid.",
        "NO_LICENSE"      => "No license key found. Redeem a key on the website.",
        "SESSION_EXPIRED" => "Your session has expired. Please log in again.",
        "SERVER_OFFLINE"  => "Cannot reach the license server. Check your connection.",
        "SERVER_TIMEOUT"  => "License server timed out. Try again.",
        _                 => $"Validation error: {reason}"
    };

    private class ValidationResponse
    {
        [JsonProperty("licensed")]   public bool Licensed  { get; set; }
        [JsonProperty("key_count")]  public int  KeyCount  { get; set; }
    }
}

// ─── Usage Example ────────────────────────────────────────────────────────────
/*

using EngineClient;
using SmokeScreenEngine; // for TokenStorage

// On application startup, after DiscordAuth / EngineLoginManager login:
var sessionToken = TokenStorage.Load();
if (sessionToken == null)
{
    // Not logged in — show login screen
    return;
}

var (valid, reason) = await LicenseValidator.ValidateAsync(sessionToken, appVersion: "2.1.0");

if (!valid)
{
    MessageBox.Show(LicenseValidator.DescribeReason(reason), "License Error");
    if (reason == "SESSION_EXPIRED") TokenStorage.Clear(); // force re-login
    Application.Exit();
    return;
}

// License is valid — continue loading the app.

*/

