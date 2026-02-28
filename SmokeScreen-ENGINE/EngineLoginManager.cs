// EngineLoginManager.cs
// Thin coordinator — delegates to DiscordAuth.cs for all real logic.
// Kept for backward compatibility; prefer calling DiscordAuth directly.
using System.Threading;
using System.Threading.Tasks;

public class EngineLoginManager
{
    public static Task<LoginResult> TryAutoLoginAsync()
        => SmokeScreenEngine.DiscordAuth.TryAutoLoginAsync();

    public static Task<LoginResult> LoginWithDiscordAsync(
        IProgress<string>? progress = null, CancellationToken ct = default)
        => SmokeScreenEngine.DiscordAuth.LoginWithDiscordAsync(progress, ct);

    public static async Task<LoginResult> LoginWithEngineTokenAsync(string engineToken)
    {
        using var http = new System.Net.Http.HttpClient();
        var payload    = Newtonsoft.Json.JsonConvert.SerializeObject(new { engineToken });
        var res        = await http.PostAsync(
            $"{SmokeScreenEngine.DiscordAuth.API_BASE}/auth/engine-login",
            new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            var err = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            return LoginResult.Fail((string?)err?.message ?? "Invalid token");
        }
        var data    = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json)!;
        string tok  = (string)data.sessionToken;
        var userJson= Newtonsoft.Json.JsonConvert.SerializeObject(data.user);
        var user    = Newtonsoft.Json.JsonConvert.DeserializeObject<SmokeScreenEngine.UserInfo>(userJson)!;
        SmokeScreenEngine.TokenStorage.Save(tok);
        return LoginResult.Success(tok, user);
    }

    public static Task LogoutAsync(string token)
        => SmokeScreenEngine.DiscordAuth.LogoutAsync(token);
}
