using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public class PythonProcessManager : IDisposable
    {
        private Process? _process;
        private bool _isRunning;
        private readonly string _gameName;
        private StreamWriter? _input;

        public bool IsRunning => _isRunning;

        public event Action<string>? OnOutput;
        public event Action? OnStarted;
        public event Action? OnStopped;

        public PythonProcessManager(string gameName)
        {
            _gameName = gameName;
        }

        public async Task<bool> StartAsync()
        {
            if (_isRunning) return true;

            try
            {
                string scriptName = _gameName.ToLower() switch
                {
                    "warzone" => "warzone_main.py",
                    "r6s" => "r6s_main.py",
                    "arc raiders" => "arc_raiders_main.py",
                    "fortnite" => "fortnite_main.py",
                    _ => null
                };

                if (scriptName == null) return false;

                string scriptPath = Path.Combine(AppContext.BaseDirectory, scriptName);
                if (!File.Exists(scriptPath))
                {
                    OnOutput?.Invoke($"Script not found: {scriptPath}");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "pythonw.exe",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };

                _process = new Process { StartInfo = startInfo };
                _process.OutputDataReceived += (s, e) => OnOutput?.Invoke(e.Data ?? "");
                _process.ErrorDataReceived += (s, e) => OnOutput?.Invoke($"Error: {e.Data}");
                _process.EnableRaisingEvents = true;
                _process.Exited += (s, e) => 
                { 
                    _isRunning = false; 
                    OnStopped?.Invoke();
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                _input = _process.StandardInput;
                _isRunning = true;
                OnStarted?.Invoke();
                OnOutput?.Invoke($"[{_gameName}] Started");

                return true;
            }
            catch (Exception ex)
            {
                OnOutput?.Invoke($"Failed to start: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning || _process == null) return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch { }

            _isRunning = false;
            _input?.Dispose();
            _process?.Dispose();
            _process = null;
            OnStopped?.Invoke();
            OnOutput?.Invoke($"[{_gameName}] Stopped");
        }

        public async Task SendCommandAsync(string command)
        {
            if (_input != null && _isRunning)
            {
                await _input.WriteLineAsync(command);
                await _input.FlushAsync();
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
