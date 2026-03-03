using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public class VersionChecker
    {
        private static readonly HttpClient _http = new HttpClient();
        
        public static async Task<string> GetWarzoneVersionAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("https://steamdb.info/app/1938090/patchnotes/");
                var match = Regex.Match(response, @"(\d+\.\d+\.\d+\.\d+)");
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch { return "Error"; }
        }

        public static async Task<string> GetArcRaidersVersionAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("https://steamdb.info/app/1808500/patchnotes/");
                var match = Regex.Match(response, @"(\d+\.\d+\.\d+\.\d+)");
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch { return "Error"; }
        }

        public static async Task<string> GetR6SVersionAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("https://steamdb.info/app/359550/patchnotes/");
                var match = Regex.Match(response, @"(\d+\.\d+\.\d+)");
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch { return "Error"; }
        }

        public static async Task<string> GetFortniteVersionAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("https://fortnite.fandom.com/wiki/Fortnite_Wiki");
                var match = Regex.Match(response, @"v?(\d+\.\d+(?:\.\d+)?)");
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch { return "Error"; }
        }
    }
}
