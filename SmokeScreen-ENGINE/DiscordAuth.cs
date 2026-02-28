using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    public static class DiscordAuth
    {
        // ─── CONFIG ───────────────────────────────────────────────────────────
        public const string  API_BASE            = "https://smok-ex-screen-engine.vercel.app";
        private const string DISCORD_CLIENT_ID   = "1476913890620342444";
        private const string ENGINE_REDIRECT_URI = "https://smok-ex-screen-engine.vercel.app/auth/discord/engine-landing";
        private const int    LOCAL_PORT          = 9876;
        private const int    TIMEOUT_SECONDS     = 120;

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        // ─── DISCORD LOGIN ────────────────────────────────────────────────────
        /// <summary>
        /// Starts localhost:9876 listener FIRST, THEN opens the browser.
        /// ENGINE is guaranteed ready before Discord ever redirects back.
        /// </summary>
        public static async Task<LoginResult> LoginWithDiscordAsync(
            IProgress<string>? progress = null,
            CancellationToken  ct       = default)
        {
            var state = Guid.NewGuid().ToString("N");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

            // Step 1 — listener starts NOW, before browser opens
            progress?.Report("Ready. Opening Discord in your browser...");
            var codeTask = WaitForCodeAsync(state, cts.Token);

            // Step 2 — open browser (ENGINE is already listening)
            OpenBrowser(BuildOAuthUrl(state));
            progress?.Report("Authorize in the browser window that just opened...");

            // Step 3 — wait for code
            string code;
            try   { code = await codeTask; }
            catch (OperationCanceledException) { return LoginResult.Fail("Login timed out (2 min). Try again."); }
            catch (Exception ex)               { return LoginResult.Fail($"Auth error: {ex.Message}"); }

            progress?.Report("Signing you in...");
            return await ExchangeCodeAsync(code);
        }

        // ─── AUTO LOGIN ───────────────────────────────────────────────────────
        public static async Task<LoginResult> TryAutoLoginAsync()
        {
            var token = TokenStorage.Load();
            if (token == null) return LoginResult.Fail("No saved session.");
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/auth/me");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode) { TokenStorage.Clear(); return LoginResult.Fail("Session expired."); }
                var data = JsonConvert.DeserializeObject<MeResponse>(await res.Content.ReadAsStringAsync());
                if (data?.User == null) return LoginResult.Fail("Invalid session.");
                return LoginResult.Success(token, data.User);
            }
            catch { return LoginResult.Fail("Could not reach server."); }
        }

        // ─── LICENSE VALIDATE ─────────────────────────────────────────────────
        public static async Task<LicenseStatus> ValidateLicenseAsync(string token)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE}/keys/validate");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var res  = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode) return new LicenseStatus(false, 0);
                var data = JsonConvert.DeserializeObject<LicenseResponse>(await res.Content.ReadAsStringAsync());
                return new LicenseStatus(
                    data?.Licensed      == true,
                    data?.Active        == true,
                    data?.DurationType,
                    data?.DurationLabel,
                    data?.ExpiresAt,
                    data?.MsRemaining,
                    data?.DaysRemaining
                );
            }
            catch { return new LicenseStatus(false, 0); }
        }

        // ─── REDEEM KEY ───────────────────────────────────────────────────────
        public static async Task<(bool ok, string message)> RedeemKeyAsync(string token, string key)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE}/keys/redeem");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                req.Content = new StringContent(JsonConvert.SerializeObject(new { key }), Encoding.UTF8, "application/json");
                var res  = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                return res.IsSuccessStatusCode
                    ? (true,  "License activated!")
                    : (false, (string?)data?.message ?? "Invalid key.");
            }
            catch { return (false, "Could not reach server."); }
        }

        // ─── LOGOUT ───────────────────────────────────────────────────────────
        public static async Task LogoutAsync(string token)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE}/auth/logout");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                req.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                await _http.SendAsync(req);
            }
            catch { }
            finally { TokenStorage.Clear(); }
        }

        // ─── PRIVATE HELPERS ──────────────────────────────────────────────────
        private static string BuildOAuthUrl(string state)
        {
            var p = new System.Collections.Specialized.NameValueCollection
            {
                ["client_id"]     = DISCORD_CLIENT_ID,
                ["redirect_uri"]  = ENGINE_REDIRECT_URI,
                ["response_type"] = "code",
                ["scope"]         = "identify email",
                ["state"]         = state,
                ["prompt"]        = "none",
            };
            var sb = new StringBuilder("https://discord.com/api/oauth2/authorize?");
            foreach (string key in p)
                sb.Append($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(p[key]!)}&");
            return sb.ToString().TrimEnd('&');
        }

        private static void OpenBrowser(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { Process.Start("cmd", $"/c start {url.Replace("&", "^&")}"); }
        }

        private static async Task<string> WaitForCodeAsync(string expectedState, CancellationToken ct)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{LOCAL_PORT}/");
            listener.Start(); // ← UP before browser opens

            while (!ct.IsCancellationRequested)
            {
                var ctxTask = listener.GetContextAsync();
                if (await Task.WhenAny(ctxTask, Task.Delay(300, ct)) != ctxTask) continue;

                var ctx   = await ctxTask;
                var query = ctx.Request.QueryString;
                var code  = query["code"];
                var state = query["state"];

                // Always respond with CORS headers so the page can read the response
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.ContentType = "text/html; charset=utf-8";

                if (string.IsNullOrEmpty(code) || state != expectedState)
                {
                    // Bad request — tell the browser something went wrong
                    var errHtml = System.Text.Encoding.UTF8.GetBytes(
                        "<!DOCTYPE html><html><body style='background:#03040a;color:#ff3b5c;" +
                        "font-family:monospace;display:flex;align-items:center;justify-content:center;height:100vh;'>" +
                        "<div style='text-align:center'><h2>❌ Auth Failed</h2>" +
                        "<p style='color:#4a5470'>Invalid or missing code. Please try again in ENGINE.exe.</p></div></body></html>");
                    ctx.Response.StatusCode    = 400;
                    ctx.Response.ContentLength64 = errHtml.Length;
                    await ctx.Response.OutputStream.WriteAsync(errHtml, ct);
                    ctx.Response.Close();
                    continue; // keep listening — user might retry
                }

                // Success — show a nice "you can close this" page in the browser tab
                var successHtml = System.Text.Encoding.UTF8.GetBytes(@"<!DOCTYPE html>
<html>
<head><title>SmokeScreen ENGINE — Auth</title></head>
<body style='margin:0;font-family:Segoe UI,monospace;background:#03040a;color:#e8ff00;
             display:flex;align-items:center;justify-content:center;height:100vh;'>
<div style='text-align:center;padding:40px;'>
  <div style='font-size:52px;margin-bottom:20px;'>✅</div>
  <h2 style='letter-spacing:3px;margin-bottom:12px;'>LOGIN SUCCESSFUL</h2>
  <p style='color:#7a86a8;font-size:14px;'>
    You are now signed into SmokeScreen ENGINE.<br>
    You can close this window and return to the app.
  </p>
  <div style='margin-top:28px;background:#0b0e18;border-radius:8px;padding:16px 24px;
              display:inline-block;color:#4a5470;font-size:11px;letter-spacing:2px;'>
    WINDOW SAFE TO CLOSE
  </div>
  <script>
    // Auto-close after 3 seconds if the browser allows it
    setTimeout(function() { try { window.close(); } catch(e) {} }, 3000);
  </script>
</div>
</body></html>");

                ctx.Response.StatusCode      = 200;
                ctx.Response.ContentLength64 = successHtml.Length;
                await ctx.Response.OutputStream.WriteAsync(successHtml, ct);
                ctx.Response.Close();
                listener.Stop();

                return code;
            }

            ct.ThrowIfCancellationRequested();
            throw new Exception("Cancelled.");
        }

        private static async Task<LoginResult> ExchangeCodeAsync(string code)
        {
            var payload = JsonConvert.SerializeObject(new { code, redirect_uri = ENGINE_REDIRECT_URI });
            try
            {
                var res  = await _http.PostAsync($"{API_BASE}/auth/discord/engine-callback",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
                var json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    var err = JsonConvert.DeserializeObject<ErrorResponse>(json);
                    return LoginResult.Fail(err?.Message ?? $"Server error {(int)res.StatusCode}");
                }
                var data = JsonConvert.DeserializeObject<AuthResponse>(json);
                if (data?.SessionToken == null) return LoginResult.Fail("No session token received.");
                TokenStorage.Save(data.SessionToken);
                return LoginResult.Success(data.SessionToken, data.User!);
            }
            catch (HttpRequestException) { return LoginResult.Fail("Could not reach server."); }
        }

        // ─── JSON MODELS ──────────────────────────────────────────────────────
        private class MeResponse     { [JsonProperty("user")] public UserInfo? User { get; set; } }
        private class AuthResponse   { [JsonProperty("sessionToken")] public string? SessionToken { get; set; } [JsonProperty("user")] public UserInfo? User { get; set; } }
        private class LicenseResponse
        {
            [JsonProperty("licensed")]        public bool    Licensed      { get; set; }
            [JsonProperty("active")]          public bool    Active        { get; set; }
            [JsonProperty("duration_type")]   public string? DurationType  { get; set; }
            [JsonProperty("duration_label")]  public string? DurationLabel { get; set; }
            [JsonProperty("expires_at")]      public long?   ExpiresAt     { get; set; }
            [JsonProperty("ms_remaining")]    public long?   MsRemaining   { get; set; }
            [JsonProperty("days_remaining")]  public long?   DaysRemaining { get; set; }
            [JsonProperty("key_count")]       public int     KeyCount      { get; set; }
        }
        private class ErrorResponse  { [JsonProperty("message")] public string? Message { get; set; } }
    }

    // ─── LICENSE STATUS ───────────────────────────────────────────────────────
    public class LicenseStatus
    {
        public bool    Licensed      { get; }
        public bool    Active        { get; }
        public string? DurationType  { get; }
        public string? DurationLabel { get; }
        public long?   ExpiresAt     { get; }
        public long?   MsRemaining   { get; }
        public long?   DaysRemaining { get; }
        public bool    IsLifetime    => DurationType == "LIFETIME";
        public bool    HasAccess     => Licensed && Active;

        public LicenseStatus(bool licensed, bool active, string? durationType, string? durationLabel,
                             long? expiresAt, long? msRemaining, long? daysRemaining)
        {
            Licensed      = licensed;
            Active        = active;
            DurationType  = durationType;
            DurationLabel = durationLabel;
            ExpiresAt     = expiresAt;
            MsRemaining   = msRemaining;
            DaysRemaining = daysRemaining;
        }

        // Backwards-compat factory for old code that passes (bool, int)
        public static LicenseStatus FromLegacy(bool licensed, int trialDaysLeft) =>
            new LicenseStatus(licensed, licensed, null, null, null, null, trialDaysLeft > 0 ? (long?)trialDaysLeft : null);

        public string StatusLine()
        {
            if (!Licensed)     return "NO LICENSE";
            if (IsLifetime)    return "LIFETIME ACCESS";
            if (DaysRemaining.HasValue) return $"ACTIVE — {DaysRemaining} day{(DaysRemaining != 1 ? "s" : "")} left";
            return "ACTIVE";
        }
    }

    // ─── LOGIN RESULT ─────────────────────────────────────────────────────────
    public class LoginResult
    {
        public bool           IsSuccess { get; private set; }
        public string?        Token     { get; private set; }
        public UserInfo?      User      { get; private set; }
        public string?        Error     { get; private set; }
        public LicenseStatus? License   { get; set; }
        public static LoginResult Success(string token, UserInfo user) => new() { IsSuccess = true, Token = token, User = user };
        public static LoginResult Fail(string error) => new() { IsSuccess = false, Error = error };
    }

    // ─── USER INFO ────────────────────────────────────────────────────────────
    public class UserInfo
    {
        [JsonProperty("id")]               public string? Id              { get; set; }
        [JsonProperty("username")]         public string? Username        { get; set; }
        [JsonProperty("email")]            public string? Email           { get; set; }
        [JsonProperty("discord_id")]       public string? DiscordId       { get; set; }
        [JsonProperty("discord_username")] public string? DiscordUsername { get; set; }
        [JsonProperty("avatar")]           public string? Avatar          { get; set; }
        public string? AvatarUrl    => (DiscordId != null && Avatar != null) ? $"https://cdn.discordapp.com/avatars/{DiscordId}/{Avatar}.png?size=64" : null;
        public string  DisplayName  => DiscordUsername ?? Username ?? "User";
        public bool    IsOwner      => DiscordId == "1368087024401252393";
    }

    // ─── TOKEN STORAGE ────────────────────────────────────────────────────────
    public static class TokenStorage
    {
        private static readonly string _path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmokeScreenENGINE", "session.token");
        public static void    Save(string token) { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!); System.IO.File.WriteAllText(_path, token); }
        public static string? Load() { if (!System.IO.File.Exists(_path)) return null; var t = System.IO.File.ReadAllText(_path).Trim(); return string.IsNullOrEmpty(t) ? null : t; }
        public static void    Clear() { if (System.IO.File.Exists(_path)) System.IO.File.Delete(_path); }
    }

    // ─── TRIAL STORAGE ────────────────────────────────────────────────────────
    /// <summary>Tracks 24-hour trial start time locally in AppData.</summary>
    public static class TrialStorage
    {
        private static readonly string _path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmokeScreenENGINE", "trial.dat");

        /// <summary>Records first login. Safe to call every startup — only writes once.</summary>
        public static void EnsureStarted()
        {
            if (System.IO.File.Exists(_path)) return;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            System.IO.File.WriteAllText(_path, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }

        public static double HoursRemaining()
        {
            if (!System.IO.File.Exists(_path)) return 24;
            if (!long.TryParse(System.IO.File.ReadAllText(_path).Trim(), out long started)) return 0;
            var remaining = (24 * 3600) - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - started);
            return remaining > 0 ? remaining / 3600.0 : 0;
        }

        public static bool IsExpired() => HoursRemaining() <= 0;
    }
}
