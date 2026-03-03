using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public class PS5ControllerManager : IDisposable
    {
        private const string JOYSHOCK_LIBRARY = "JoyShockLibrary.dll";
        private bool _isInitialized = false;
        private bool _isConnected = false;
        private bool _rtPressed = false;
        private bool _ltPressed = false;
        private bool _r2Pressed = false;
        private bool _l2Pressed = false;
        private Timer _pollTimer;
        
        // PS5 Controller State
        public struct PS5State
        {
            public bool Connected;
            public float LeftStickX;
            public float LeftStickY;
            public float RightStickX;
            public float RightStickY;
            public bool R2_Pressed;
            public bool L2_Pressed;
            public float R2_Trigger;
            public float L2_Trigger;
            public bool Triangle_Pressed;
            public bool Circle_Pressed;
            public bool Cross_Pressed;
            public bool Square_Pressed;
            public bool L1_Pressed;
            public bool R1_Pressed;
            public bool L3_Pressed;
            public bool R3_Pressed;
            public float GyroX;
            public float GyroY;
            public float GyroZ;
            public float AccelX;
            public float AccelY;
            public float AccelZ;
        }
        
        private PS5State _currentState;
        private PS5State _previousState;
        
        // Events
        public event Action<PS5State>? OnControllerStateChanged;
        public event Action? OnRTPressed;
        public event Action? OnRTReleased;
        public event Action? OnLTPressed;
        public event Action? OnLTReleased;
        public event Action? OnControllerConnected;
        public event Action? OnControllerDisconnected;
        
        // JoyShockLibrary DLL Imports
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern int JslConnectDevices();
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern int JslGetConnectedDeviceHandles(ref IntPtr handles, int max_handles);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslDisconnectAndDisposeAll();
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern int JslPollControllers();
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern int JslGetControllerType(int device_handle);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslGetSimpleState(int device_handle, ref JslSimpleState state);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslSetRumble(int device_handle, int smallRumble, int bigRumble);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslResetContinuousCalibration(int device_handle);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslStartContinuousCalibration(int device_handle);
        
        [DllImport(JOYSHOCK_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
        private static extern void JslCalibrateOrientation(int device_handle);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JslSimpleState
        {
            public int sticks;
            public float leftStickX;
            public float leftStickY;
            public float rightStickX;
            public float rightStickY;
            public int l2;
            public int r2;
            public int buttons;
            public int gyroX;
            public int gyroY;
            public int gyroZ;
            public int accelX;
            public int accelY;
            public int accelZ;
            public int orientationW;
            public int orientationX;
            public int orientationY;
            public int orientationZ;
        }
        
        public PS5ControllerManager()
        {
            _currentState = new PS5State();
            _previousState = new PS5State();
            _pollTimer = new Timer(PollController, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16)); // 60Hz polling
        }
        
        public bool Initialize()
        {
            try
            {
                // Load JoyShockLibrary.dll
                if (!File.Exists(JOYSHOCK_LIBRARY))
                {
                    Console.WriteLine("[PS5] JoyShockLibrary.dll not found. PS5 controller support disabled.");
                    return false;
                }
                
                // Connect to devices
                int result = JslConnectDevices();
                if (result <= 0)
                {
                    Console.WriteLine("[PS5] No PS5 controllers found.");
                    return false;
                }
                
                _isInitialized = true;
                Console.WriteLine($"[PS5] JoyShockLibrary initialized. Found {result} controller(s).");
                
                // Get device handles
                IntPtr[] handles = new IntPtr[4];
                int deviceCount = JslGetConnectedDeviceHandles(ref handles[0], 4);
                
                if (deviceCount > 0)
                {
                    _isConnected = true;
                    OnControllerConnected?.Invoke();
                    Console.WriteLine("[PS5] PS5 controller connected successfully!");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error initializing PS5 controller: {ex.Message}");
                return false;
            }
        }
        
        private void PollController(object? state)
        {
            if (!_isInitialized) return;
            
            try
            {
                // Poll controllers
                JslPollControllers();
                
                // Get device handles
                IntPtr[] handles = new IntPtr[4];
                int deviceCount = JslGetConnectedDeviceHandles(ref handles[0], 4);
                
                if (deviceCount > 0)
                {
                    // Get state from first connected controller
                    JslSimpleState jslState = new JslSimpleState();
                    JslGetSimpleState((int)handles[0], ref jslState);
                    
                    // Convert to our PS5State format
                    _previousState = _currentState;
                    _currentState = new PS5State
                    {
                        Connected = true,
                        LeftStickX = jslState.leftStickX,
                        LeftStickY = jslState.leftStickY,
                        RightStickX = jslState.rightStickX,
                        RightStickY = jslState.rightStickY,
                        R2_Pressed = jslState.r2 > 0,
                        L2_Pressed = jslState.l2 > 0,
                        R2_Trigger = jslState.r2 / 255.0f,
                        L2_Trigger = jslState.l2 / 255.0f,
                        Triangle_Pressed = (jslState.buttons & 0x800) != 0,
                        Circle_Pressed = (jslState.buttons & 0x200) != 0,
                        Cross_Pressed = (jslState.buttons & 0x100) != 0,
                        Square_Pressed = (jslState.buttons & 0x400) != 0,
                        L1_Pressed = (jslState.buttons & 0x1000) != 0,
                        R1_Pressed = (jslState.buttons & 0x2000) != 0,
                        L3_Pressed = (jslState.buttons & 0x4000) != 0,
                        R3_Pressed = (jslState.buttons & 0x8000) != 0,
                        GyroX = jslState.gyroX,
                        GyroY = jslState.gyroY,
                        GyroZ = jslState.gyroZ,
                        AccelX = jslState.accelX,
                        AccelY = jslState.accelY,
                        AccelZ = jslState.accelZ
                    };
                    
                    // Check for RT trigger events
                    if (_currentState.R2_Pressed && !_previousState.R2_Pressed)
                    {
                        _rtPressed = true;
                        OnRTPressed?.Invoke();
                        Console.WriteLine("[PS5] RT trigger pressed - Activating recoil control");
                    }
                    else if (!_currentState.R2_Pressed && _previousState.R2_Pressed)
                    {
                        _rtPressed = false;
                        OnRTReleased?.Invoke();
                        Console.WriteLine("[PS5] RT trigger released - Deactivating recoil control");
                    }
                    
                    // Check for LT trigger events
                    if (_currentState.L2_Pressed && !_previousState.L2_Pressed)
                    {
                        _ltPressed = true;
                        OnLTPressed?.Invoke();
                        Console.WriteLine("[PS5] LT trigger pressed");
                    }
                    else if (!_currentState.L2_Pressed && _previousState.L2_Pressed)
                    {
                        _ltPressed = false;
                        OnLTReleased?.Invoke();
                        Console.WriteLine("[PS5] LT trigger released");
                    }
                    
                    // Fire state changed event
                    OnControllerStateChanged?.Invoke(_currentState);
                }
                else
                {
                    if (_isConnected)
                    {
                        _isConnected = false;
                        OnControllerDisconnected?.Invoke();
                        Console.WriteLine("[PS5] PS5 controller disconnected");
                    }
                    
                    _currentState = new PS5State { Connected = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error polling controller: {ex.Message}");
            }
        }
        
        public PS5State GetCurrentState()
        {
            return _currentState;
        }
        
        public bool IsRTPressed()
        {
            return _rtPressed;
        }
        
        public bool IsLTPressed()
        {
            return _ltPressed;
        }
        
        public bool IsConnected()
        {
            return _isConnected;
        }
        
        public void SetRumble(int smallRumble, int bigRumble)
        {
            if (!_isInitialized || !_isConnected) return;
            
            try
            {
                IntPtr[] handles = new IntPtr[4];
                int deviceCount = JslGetConnectedDeviceHandles(ref handles[0], 4);
                
                if (deviceCount > 0)
                {
                    JslSetRumble((int)handles[0], smallRumble, bigRumble);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error setting rumble: {ex.Message}");
            }
        }
        
        public void CalibrateController()
        {
            if (!_isInitialized || !_isConnected) return;
            
            try
            {
                IntPtr[] handles = new IntPtr[4];
                int deviceCount = JslGetConnectedDeviceHandles(ref handles[0], 4);
                
                if (deviceCount > 0)
                {
                    JslResetContinuousCalibration((int)handles[0]);
                    JslStartContinuousCalibration((int)handles[0]);
                    JslCalibrateOrientation((int)handles[0]);
                    Console.WriteLine("[PS5] Controller calibrated successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error calibrating controller: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            try
            {
                _pollTimer?.Dispose();
                
                if (_isInitialized)
                {
                    JslDisconnectAndDisposeAll();
                    _isInitialized = false;
                    _isConnected = false;
                    Console.WriteLine("[PS5] PS5 controller manager disposed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error disposing controller manager: {ex.Message}");
            }
        }
    }
}
