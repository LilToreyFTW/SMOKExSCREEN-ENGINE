using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public static class DiscordBotManager
    {
        private static Process? _botProcess;
        private static readonly string _botPath = Path.Combine(AppContext.BaseDirectory, "bot-full-auth.js");
        private static readonly string _nodePath = "node";
        private static bool _isRunning = false;
        private static readonly HttpClient _http = new();

        public static bool IsRunning => _isRunning && _botProcess != null && !_botProcess.HasExited;

        public static async Task<bool> StartBotAsync()
        {
            if (IsRunning) return true;

            try
            {
                if (!File.Exists(_botPath))
                {
                    throw new FileNotFoundException("Bot script not found", _botPath);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = _nodePath,
                    Arguments = $"\"{_botPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };

                _botProcess = new Process { StartInfo = startInfo };
                
                _botProcess.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BOT] {e.Data}");
                    }
                };

                _botProcess.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BOT ERROR] {e.Data}");
                    }
                };

                _botProcess.Exited += (s, e) => {
                    _isRunning = false;
                    Console.WriteLine("[BOT] Process exited");
                };

                _botProcess.EnableRaisingEvents = true;
                
                var started = _botProcess.Start();
                if (started)
                {
                    _botProcess.BeginOutputReadLine();
                    _botProcess.BeginErrorReadLine();
                    _isRunning = true;
                    
                    // Give bot time to initialize
                    await Task.Delay(3000);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOT] Failed to start: {ex.Message}");
                _isRunning = false;
            }

            return false;
        }

        public static async Task<bool> StopBotAsync()
        {
            if (!IsRunning) return true;

            try
            {
                if (_botProcess != null && !_botProcess.HasExited)
                {
                    _botProcess.Kill();
                    await _botProcess.WaitForExitAsync();
                }
                
                _isRunning = false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOT] Failed to stop: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> RestartBotAsync()
        {
            await StopBotAsync();
            await Task.Delay(2000); // Wait for cleanup
            return await StartBotAsync();
        }

        public static string GetStatus()
        {
            if (!IsRunning) return "Offline";
            if (_botProcess == null) return "Unknown";
            
            try
            {
                return $"Running (PID: {_botProcess.Id})";
            }
            catch
            {
                return "Error";
            }
        }

        public static async Task<BotStatusInfo?> GetDetailedStatusAsync()
        {
            try
            {
                var response = await _http.GetAsync("http://localhost:9877/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<BotStatusInfo>(json);
                }
            }
            catch
            {
                // Bot might not be running or HTTP server not ready
            }
            
            return null;
        }

        public static async Task<KeyGenerationResult?> GenerateKeysAsync(string game, int duration, int keyCount)
        {
            try
            {
                if (!IsRunning) {
                    return new KeyGenerationResult { 
                        Success = false, 
                        Error = "Bot is not running. Please start the bot first." 
                    };
                }

                var request = new
                {
                    game = game,
                    duration = duration,
                    keys = keyCount,
                    source = "ENGINE.exe"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _http.PostAsync("http://localhost:9877/generate-key", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<KeyGenerationResult>(responseJson);
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    return new KeyGenerationResult { 
                        Success = false, 
                        Error = $"HTTP {response.StatusCode}: {errorJson}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new KeyGenerationResult { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }
    }

    public class BotStatusInfo
    {
        public string Status { get; set; } = "";
        public long Uptime { get; set; }
        public string Guild { get; set; } = "";
        public int ReconnectAttempts { get; set; }
        public long LastHeartbeat { get; set; }
        public int WsStatus { get; set; }
        public int Ping { get; set; }
        public int TotalKeys { get; set; }
        public int RedeemedKeys { get; set; }
    }

    public class KeyGenerationResult
    {
        public bool Success { get; set; }
        public string[]? Keys { get; set; }
        public string Message { get; set; } = "";
        public string Error { get; set; } = "";
    }
}
