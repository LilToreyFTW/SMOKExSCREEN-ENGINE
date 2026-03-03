using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    public class DiscordAuthService
    {
        private static readonly HttpClient _http = new();
        private const string DISCORD_API_BASE = "https://discord.com/api/v10";

        public static class DiscordRoles
        {
            public const ulong OWNER = 1455256056312631376;
            public const ulong BASIC_ACCESS = 1477448046873935872;
            public const ulong COMMUNITY_MANAGER = 1455256063153803304;
        }

        public class UserProfile
        {
            public string DiscordId { get; set; } = "";
            public string Username { get; set; } = "";
            public string Discriminator { get; set; } = "";
            public string Avatar { get; set; } = "";
            public string Badge { get; set; } = "";
            public bool IsOwner { get; set; }
            public bool HasBasicAccess { get; set; }
            public bool IsCommunityManager { get; set; }
            public DateTime LoginTime { get; set; }
            public string AccessToken { get; set; } = "";
        }

        public class AuthResponse
        {
            public bool success { get; set; }
            public string error { get; set; }
            public BotUser user { get; set; }
        }

        public class BotUser
        {
            public string id { get; set; }
            public string username { get; set; }
            public string discriminator { get; set; }
            public string avatar { get; set; }
            public string badge { get; set; }
            public bool isOwner { get; set; }
            public bool hasBasicAccess { get; set; }
            public bool isCommunityManager { get; set; }
            public List<string> roles { get; set; }
        }

        public static async Task<UserProfile?> AuthenticateUserAsync(string discordId, string botToken)
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {botToken}");
                
                var guildId = DiscordAuth.GUILD_ID;
                
                // Get guild member info
                var memberResponse = await httpClient.GetAsync(
                    $"https://discord.com/api/v10/guilds/{guildId}/members/{discordId}"
                );
                
                if (!memberResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DISCORD] User not found in guild: {memberResponse.StatusCode}");
                    return null;
                }
                
                var memberJson = await memberResponse.Content.ReadAsStringAsync();
                var member = JsonConvert.DeserializeObject<DiscordMember>(memberJson);
                
                if (member == null)
                {
                    return null;
                }
                
                // Get user info
                var userResponse = await httpClient.GetAsync(
                    $"https://discord.com/api/v10/users/{discordId}"
                );
                
                string username = discordId;
                string discriminator = "0000";
                string avatar = "";
                
                if (userResponse.IsSuccessStatusCode)
                {
                    var userJson = await userResponse.Content.ReadAsStringAsync();
                    var userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(userJson);
                    if (userData != null)
                    {
                        username = userData.GetValueOrDefault("username")?.ToString() ?? discordId;
                        discriminator = userData.GetValueOrDefault("discriminator")?.ToString() ?? "0000";
                        avatar = userData.GetValueOrDefault("avatar")?.ToString() ?? "";
                    }
                }
                
                // Check roles
                var roles = member.Roles ?? new List<string>();
                bool isOwner = roles.Contains("1455256056312631376");
                bool hasBasicAccess = roles.Contains("1477448046873935872");
                bool isCommunityManager = roles.Contains("1455256063153803304");
                
                return new UserProfile
                {
                    DiscordId = discordId,
                    Username = username,
                    Discriminator = discriminator,
                    Avatar = avatar,
                    Badge = isOwner ? "OWNER" : (hasBasicAccess ? "MEMBER" : ""),
                    IsOwner = isOwner,
                    HasBasicAccess = hasBasicAccess,
                    IsCommunityManager = isCommunityManager,
                    LoginTime = DateTime.UtcNow,
                    AccessToken = botToken
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCORD] Error authenticating user: {ex.Message}");
                return null;
            }
        }
        
        private class DiscordMember
        {
            [JsonProperty("user")]
            public DiscordUser? User { get; set; }
            
            [JsonProperty("roles")]
            public List<string>? Roles { get; set; }
            
            [JsonProperty("nick")]
            public string? Nick { get; set; }
        }
        
        private class DiscordUser
        {
            [JsonProperty("id")]
            public string Id { get; set; } = "";
            
            [JsonProperty("username")]
            public string Username { get; set; } = "";
            
            [JsonProperty("discriminator")]
            public string Discriminator { get; set; } = "";
            
            [JsonProperty("avatar")]
            public string Avatar { get; set; } = "";
        }

        public static async Task<bool> ValidateSessionAsync(string discordId, string accessToken)
        {
            try
            {
                // In a real implementation, you'd validate against a database or cache
                // For now, we'll just check if the token format is valid
                return !string.IsNullOrEmpty(discordId) && 
                       !string.IsNullOrEmpty(accessToken) && 
                       accessToken.Length == 36; // GUID length
            }
            catch
            {
                return false;
            }
        }
    }
}
