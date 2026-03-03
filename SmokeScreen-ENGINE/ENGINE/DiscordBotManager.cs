using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public static class DiscordBotManager
    {
        private static Process? _botProcess;
        private static readonly string _botPath = Path.Combine(AppContext.BaseDirectory, "bot-full-auth.js");
        private static readonly string _nodePath = "node";
        private static bool _isRunning = false;

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
                        // Log bot output for debugging
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
    }
}
