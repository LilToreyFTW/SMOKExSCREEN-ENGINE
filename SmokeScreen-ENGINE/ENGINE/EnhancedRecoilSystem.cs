using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public static class EnhancedRecoilSystem
    {
        // Game-specific recoil settings
        public static Dictionary<string, RecoilSettings> GameSettings = new()
        {
            ["Warzone"] = new RecoilSettings { Strength = 50, Smoothness = 75, XReduction = 85, YReduction = 90, SpeedMultiplier = 1.0f },
            ["R6S"] = new RecoilSettings { Strength = 45, Smoothness = 80, XReduction = 88, YReduction = 92, SpeedMultiplier = 0.9f },
            ["Arc Raiders"] = new RecoilSettings { Strength = 55, Smoothness = 70, XReduction = 83, YReduction = 88, SpeedMultiplier = 1.1f },
            ["Fortnite"] = new RecoilSettings { Strength = 48, Smoothness = 78, XReduction = 86, YReduction = 91, SpeedMultiplier = 0.95f }
        };

        private static bool _f2ToggleEnabled = false;
        private static bool _leftMousePressed = false;
        private static LowLevelKeyboardProc _keyboardHook;
        private static LowLevelMouseProc _mouseHook;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static string _currentGame = "Warzone";

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int VK_F2 = 0x71;
        private const int VK_LBUTTON = 0x01;

        public static void Initialize()
        {
            _keyboardHook = KeyboardHookCallback;
            _mouseHook = MouseHookCallback;
            
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHook, GetModuleHandle(curModule.ModuleName), 0);
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseHook, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static void Shutdown()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }
            
            if (_mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_F2)
                {
                    _f2ToggleEnabled = !_f2ToggleEnabled;
                    Console.WriteLine($"[RECOIL] F2 Toggle: {(_f2ToggleEnabled ? "ENABLED" : "DISABLED")}");
                    return new IntPtr(1); // Block the key
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    _leftMousePressed = true;
                    if (_f2ToggleEnabled)
                    {
                        ApplyRecoilControl();
                    }
                }
                else if (wParam == (IntPtr)WM_LBUTTONUP)
                {
                    _leftMousePressed = false;
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static void ApplyRecoilControl()
        {
            if (!_f2ToggleEnabled || !_leftMousePressed) return;

            var settings = GameSettings[_currentGame];
            
            // Apply recoil reduction based on current settings
            // This would interface with the actual game's recoil system
            Console.WriteLine($"[RECOIL] Applying {_currentGame} recoil control:");
            Console.WriteLine($"  Strength: {settings.Strength}%");
            Console.WriteLine($"  Smoothness: {settings.Smoothness}%");
            Console.WriteLine($"  X Reduction: {settings.XReduction}%");
            Console.WriteLine($"  Y Reduction: {settings.YReduction}%");
            Console.WriteLine($"  Speed Multiplier: {settings.SpeedMultiplier:F2}");
        }

        public static void SetCurrentGame(string game)
        {
            _currentGame = game;
            Console.WriteLine($"[RECOIL] Current game set to: {game}");
        }

        public static void UpdateSettings(string game, RecoilSettings settings)
        {
            GameSettings[game] = settings;
            Console.WriteLine($"[RECOIL] Updated {game} settings");
        }

        public static bool IsF2Enabled => _f2ToggleEnabled;
        public static bool IsLeftMousePressed => _leftMousePressed;
    }

    public class RecoilSettings
    {
        public int Strength { get; set; }
        public int Smoothness { get; set; }
        public int XReduction { get; set; }
        public int YReduction { get; set; }
        public float SpeedMultiplier { get; set; }
        public int HorizontalRecoil { get; set; } = 50;
        public int VerticalRecoil { get; set; } = 50;
        public int FirstShotRecoil { get; set; } = 30;
        public int RecoverySpeed { get; set; } = 75;
        public int ADSMultiplier { get; set; } = 80;
        public int MovementPenalty { get; set; } = 20;
        public int BreathControl { get; set; } = 60;
    }
}
