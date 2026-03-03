using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class PS5RecoilIntegration : IDisposable
    {
        private readonly PS5ControllerManager _ps5Manager;
        private readonly string _gameType;
        private bool _isActive = false;
        private bool _recoilActive = false;
        private float _recoilStrength = 50.0f;
        private float _recoilSpeed = 50.0f;
        private string _recoilPattern = "Default";
        private Timer _recoilTimer;
        private Random _random = new Random();
        
        public PS5RecoilIntegration(PS5ControllerManager ps5Manager, string gameType)
        {
            _ps5Manager = ps5Manager;
            _gameType = gameType;
            _recoilTimer = new Timer();
            
            // Subscribe to PS5 events
            _ps5Manager.OnRTPressed += OnRTPressed;
            _ps5Manager.OnRTReleased += OnRTReleased;
            _ps5Manager.OnControllerStateChanged += OnControllerStateChanged;
        }
        
        private void OnRTPressed()
        {
            if (!_isActive) return;
            
            _recoilActive = true;
            Console.WriteLine($"[PS5 RECOIL] RT pressed - Activating {_gameType} recoil control");
            
            // Start recoil compensation
            StartRecoilCompensation();
            
            // Provide haptic feedback
            _ps5Manager.SetRumble((int)(_recoilStrength * 1.5), (int)(_recoilStrength * 0.8));
        }
        
        private void OnRTReleased()
        {
            if (!_isActive) return;
            
            _recoilActive = false;
            Console.WriteLine($"[PS5 RECOIL] RT released - Deactivating {_gameType} recoil control");
            
            // Stop recoil compensation
            StopRecoilCompensation();
            
            // Stop rumble
            _ps5Manager.SetRumble(0, 0);
        }
        
        private void OnControllerStateChanged(PS5ControllerManager.PS5State state)
        {
            if (!_isActive) return;
            
            // Update status based on controller state
            if (state.Connected)
            {
                // Process stick movements for aiming assistance
                ProcessStickMovement(state.LeftStickX, state.LeftStickY, state.RightStickX, state.RightStickY);
                
                // Process gyro for advanced aiming
                if (Math.Abs(state.GyroX) > 10 || Math.Abs(state.GyroY) > 10)
                {
                    ApplyGyroAiming(state.GyroX, state.GyroY);
                }
            }
        }
        
        private void StartRecoilCompensation()
        {
            try
            {
                // Configure recoil timer based on pattern
                int interval = GetRecoilInterval();
                _recoilTimer.Interval = interval;
                _recoilTimer.Tick += ApplyRecoilPattern;
                _recoilTimer.Start();
                
                Console.WriteLine($"[PS5 RECOIL] Started recoil compensation - Pattern: {_recoilPattern}, Speed: {_recoilSpeed}%, Strength: {_recoilStrength}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5 RECOIL] Error starting recoil compensation: {ex.Message}");
            }
        }
        
        private void StopRecoilCompensation()
        {
            try
            {
                _recoilTimer.Stop();
                _recoilTimer.Tick -= ApplyRecoilPattern;
                
                // Reset mouse position to neutral
                ResetMousePosition();
                
                Console.WriteLine($"[PS5 RECOIL] Stopped recoil compensation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5 RECOIL] Error stopping recoil Compensation: {ex.Message}");
            }
        }
        
        private void ApplyRecoilPattern(object? sender, EventArgs e)
        {
            if (!_recoilActive) return;
            
            switch (_recoilPattern)
            {
                case "Default":
                    ApplyDefaultRecoil();
                    break;
                case "Aggressive":
                    ApplyAggressiveRecoil();
                    break;
                case "Smooth":
                    ApplySmoothRecoil();
                    break;
                case "Burst":
                    ApplyBurstRecoil();
                    break;
                case "Tap":
                    ApplyTapRecoil();
                    break;
                case "Custom":
                    ApplyCustomRecoil();
                    break;
            }
        }
        
        private void ApplyDefaultRecoil()
        {
            // Default recoil pattern - steady downward pull
            int recoilAmount = (int)(_recoilStrength * 0.3);
            MoveMouse(0, recoilAmount);
        }
        
        private void ApplyAggressiveRecoil()
        {
            // Aggressive pattern - stronger, faster recoil
            int recoilAmount = (int)(_recoilStrength * 0.5);
            int horizontalRecoil = _random.Next(-10, 10); // Random horizontal movement
            MoveMouse(horizontalRecoil, recoilAmount);
        }
        
        private void ApplySmoothRecoil()
        {
            // Smooth pattern - gradual, controlled recoil
            int recoilAmount = (int)(_recoilStrength * 0.2);
            MoveMouse(0, recoilAmount);
        }
        
        private void ApplyBurstRecoil()
        {
            // Burst pattern - multiple quick movements
            for (int i = 0; i < 3; i++)
            {
                int recoilAmount = (int)(_recoilStrength * 0.4);
                MoveMouse(_random.Next(-5, 5), recoilAmount);
                Thread.Sleep(50);
            }
        }
        
        private void ApplyTapRecoil()
        {
            // Tap pattern - quick tap movements
            int recoilAmount = (int)(_recoilStrength * 0.25);
            MoveMouse(0, recoilAmount);
            Thread.Sleep(30);
            MoveMouse(0, -recoilAmount / 2); // Quick recovery
        }
        
        private void ApplyCustomRecoil()
        {
            // Custom pattern - user-defined behavior
            int recoilAmount = (int)(_recoilStrength * 0.35);
            int horizontalRecoil = _random.Next(-15, 15);
            MoveMouse(horizontalRecoil, recoilAmount);
        }
        
        private void ProcessStickMovement(float leftX, float leftY, float rightX, float rightY)
        {
            if (!_isActive) return;
            
            // Convert stick movements to mouse movements for aiming assistance
            // This provides smooth aiming assistance when using PS5 controller
            
            float deadzone = 0.1f;
            float sensitivity = 2.0f;
            
            // Process right stick for aiming
            if (Math.Abs(rightX) > deadzone || Math.Abs(rightY) > deadzone)
            {
                int mouseX = (int)(rightX * sensitivity * 10);
                int mouseY = (int)(rightY * sensitivity * 10);
                
                MoveMouse(mouseX, mouseY);
            }
        }
        
        private void ApplyGyroAiming(float gyroX, float gyroY)
        {
            if (!_isActive) return;
            
            // Apply gyro-based aiming assistance
            // This provides fine-tuned aiming control using gyro sensors
            
            float gyroSensitivity = 0.5f;
            int mouseX = (int)(gyroX * gyroSensitivity);
            int mouseY = (int)(gyroY * gyroSensitivity);
            
            MoveMouse(mouseX, mouseY);
        }
        
        private void MoveMouse(int deltaX, int deltaY)
        {
            try
            {
                // Use Windows API to move mouse
                // This provides precise mouse control for recoil compensation
                var input = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = deltaX,
                        dy = deltaY,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MOVE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                };
                
                SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5 RECOIL] Error moving mouse: {ex.Message}");
            }
        }
        
        private void ResetMousePosition()
        {
            try
            {
                // Reset mouse to neutral position
                var currentPos = Cursor.Position;
                var centerX = Screen.PrimaryScreen.Bounds.Width / 2;
                var centerY = Screen.PrimaryScreen.Bounds.Height / 2;
                
                // Smooth transition back to center
                for (int i = 0; i < 10; i++)
                {
                    int targetX = currentPos.X + (centerX - currentPos.X) / 10;
                    int targetY = currentPos.Y + (centerY - currentPos.Y) / 10;
                    Cursor.Position = new Point(targetX, targetY);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5 RECOIL] Error resetting mouse: {ex.Message}");
            }
        }
        
        private int GetRecoilInterval()
        {
            // Calculate interval based on recoil speed
            // Higher speed = shorter intervals (faster compensation)
            return Math.Max(10, 100 - (int)(_recoilSpeed));
        }
        
        public void SetRecoilSettings(float strength, float speed, string pattern)
        {
            _recoilStrength = strength;
            _recoilSpeed = speed;
            _recoilPattern = pattern;
            
            Console.WriteLine($"[PS5 RECOIL] Settings updated - Strength: {strength}%, Speed: {speed}%, Pattern: {pattern}");
        }
        
        public void Activate()
        {
            _isActive = true;
            Console.WriteLine($"[PS5 RECOIL] Activated for {_gameType}");
        }
        
        public void Deactivate()
        {
            _isActive = false;
            _recoilActive = false;
            StopRecoilCompensation();
            Console.WriteLine($"[PS5 RECOIL] Deactivated for {_gameType}");
        }
        
        public bool IsActive()
        {
            return _isActive;
        }
        
        public bool IsRecoilActive()
        {
            return _recoilActive;
        }
        
        public void Dispose()
        {
            try
            {
                Deactivate();
                _recoilTimer?.Dispose();
                Console.WriteLine($"[PS5 RECOIL] Disposed for {_gameType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5 RECOIL] Error disposing: {ex.Message}");
            }
        }
        
        // Windows API imports for mouse control
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }
        
        private const int INPUT_MOUSE = 0;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        
        [DllImport("user32.dll")]
        private static extern int SendInput(int nInputs, INPUT[] pInputs, int cbSize);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
