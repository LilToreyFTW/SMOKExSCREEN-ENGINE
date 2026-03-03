using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ENGINE.Services
{
    public class HWIDKernelDriver
    {
        private const string DRIVER_NAME = "Kernel";
        private const string DRIVER_PATH = @"C:\Windows\System32\drivers\Kernel.sys";
        private const string SERVICE_NAME = "KernelHWIDSpoofer";

        [StructLayout(LayoutKind.Sequential)]
        public struct HWID_SPOOF_REQUEST
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string HardwareType;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string NewSerial;
            
            public uint Length;
        }

        // IOCTL codes for HWID spoofing
        private const uint IOCTL_SPOOF_DISK = 0x800;
        private const uint IOCTL_SPOOF_NETWORK = 0x801;
        private const uint IOCTL_SPOOF_GPU = 0x802;
        private const uint IOCTL_SPOOF_BIOS = 0x803;
        private const uint IOCTL_SPOOF_BASEBOARD = 0x804;
        private const uint IOCTL_UNSPOOF_ALL = 0x805;

        private IntPtr driverHandle = IntPtr.Zero;

        public bool LoadDriver()
        {
            try
            {
                // Check if driver file exists
                if (!File.Exists(DRIVER_PATH))
                {
                    Console.WriteLine($"Kernel driver not found at: {DRIVER_PATH}");
                    return false;
                }

                // Create and start the service
                if (!CreateAndStartService())
                {
                    Console.WriteLine("Failed to create/start kernel driver service");
                    return false;
                }

                // Get handle to driver
                driverHandle = CreateFile(
                    $"\\\\.\\{DRIVER_NAME}",
                    0xC0000000, // GENERIC_READ | GENERIC_WRITE
                    0,
                    IntPtr.Zero,
                    3, // OPEN_EXISTING
                    0,
                    IntPtr.Zero);

                if (driverHandle == IntPtr.Zero || driverHandle == new IntPtr(-1))
                {
                    Console.WriteLine($"Failed to get driver handle. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Console.WriteLine("Kernel driver loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading kernel driver: {ex.Message}");
                return false;
            }
        }

        public bool UnloadDriver()
        {
            try
            {
                if (driverHandle != IntPtr.Zero && driverHandle != new IntPtr(-1))
                {
                    CloseHandle(driverHandle);
                    driverHandle = IntPtr.Zero;
                }

                StopAndRemoveService();
                Console.WriteLine("Kernel driver unloaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unloading kernel driver: {ex.Message}");
                return false;
            }
        }

        public bool SpoofHWID(string hardwareType, string newSerial)
        {
            try
            {
                if (driverHandle == IntPtr.Zero || driverHandle == new IntPtr(-1))
                {
                    Console.WriteLine("Driver not loaded");
                    return false;
                }

                var request = new HWID_SPOOF_REQUEST
                {
                    HardwareType = hardwareType,
                    NewSerial = newSerial,
                    Length = (uint)newSerial.Length
                };

                uint ioctlCode = hardwareType.ToLower() switch
                {
                    "disk" => IOCTL_SPOOF_DISK,
                    "network" => IOCTL_SPOOF_NETWORK,
                    "gpu" => IOCTL_SPOOF_GPU,
                    "bios" => IOCTL_SPOOF_BIOS,
                    "baseboard" => IOCTL_SPOOF_BASEBOARD,
                    _ => 0
                };

                if (ioctlCode == 0)
                {
                    Console.WriteLine($"Unknown hardware type: {hardwareType}");
                    return false;
                }

                int bytesReturned;
                bool result = DeviceIoControl(
                    driverHandle,
                    ioctlCode,
                    ref request,
                    Marshal.SizeOf(request),
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);

                if (result)
                {
                    Console.WriteLine($"Successfully spoofed {hardwareType} HWID to: {newSerial}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to spoof {hardwareType} HWID. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error spoofing HWID: {ex.Message}");
                return false;
            }
        }

        public bool UnspoofAllHWIDs()
        {
            try
            {
                if (driverHandle == IntPtr.Zero || driverHandle == new IntPtr(-1))
                {
                    Console.WriteLine("Driver not loaded");
                    return false;
                }

                int bytesReturned;
                bool result = DeviceIoControl(
                    driverHandle,
                    IOCTL_UNSPOOF_ALL,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);

                if (result)
                {
                    Console.WriteLine("Successfully unspoofed all HWIDs");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to unspoof HWIDs. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unspoofing HWIDs: {ex.Message}");
                return false;
            }
        }

        private bool CreateAndStartService()
        {
            try
            {
                IntPtr scm = OpenSCManager(IntPtr.Zero, IntPtr.Zero, 0xF003F); // SC_MANAGER_ALL_ACCESS
                if (scm == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to open service control manager. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                IntPtr service = CreateService(
                    scm,
                    SERVICE_NAME,
                    "Kernel HWID Spoofer",
                    0xF01FF, // SERVICE_ALL_ACCESS
                    0x2, // SERVICE_KERNEL_DRIVER
                    0x3, // SERVICE_DEMAND_START
                    0x1, // SERVICE_ERROR_NORMAL
                    DRIVER_PATH,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (service == IntPtr.Zero)
                {
                    uint error = (uint)Marshal.GetLastWin32Error();
                    if (error == 0x431) // ERROR_SERVICE_EXISTS
                    {
                        service = OpenService(scm, SERVICE_NAME, 0xF01FF);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create service. Error: {error}");
                        CloseServiceHandle(scm);
                        return false;
                    }
                }

                if (service != IntPtr.Zero)
                {
                    bool started = StartService(service, 0, IntPtr.Zero);
                    if (!started)
                    {
                        uint error = (uint)Marshal.GetLastWin32Error();
                        if (error != 0x420) // ERROR_SERVICE_ALREADY_RUNNING
                        {
                            Console.WriteLine($"Failed to start service. Error: {error}");
                            CloseServiceHandle(service);
                            CloseServiceHandle(scm);
                            return false;
                        }
                    }

                    CloseServiceHandle(service);
                    Console.WriteLine("Kernel driver service started successfully");
                }

                CloseServiceHandle(scm);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating service: {ex.Message}");
                return false;
            }
        }

        private void StopAndRemoveService()
        {
            try
            {
                IntPtr scm = OpenSCManager(IntPtr.Zero, IntPtr.Zero, 0xF003F);
                if (scm == IntPtr.Zero) return;

                IntPtr service = OpenService(scm, SERVICE_NAME, 0xF01FF);
                if (service != IntPtr.Zero)
                {
                    SERVICE_STATUS status = new SERVICE_STATUS();
                    ControlService(service, 1, ref status); // SERVICE_CONTROL_STOP
                    DeleteService(service);
                    CloseServiceHandle(service);
                }

                CloseServiceHandle(scm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing service: {ex.Message}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_STATUS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            ref HWID_SPOOF_REQUEST lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenSCManager(
            IntPtr lpMachineName,
            IntPtr lpDatabaseName,
            uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            IntPtr lpLoadOrderGroup,
            IntPtr lpdwTagId,
            IntPtr lpDependencies,
            IntPtr lpServiceStartName,
            IntPtr lpPassword);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(
            IntPtr hSCManager,
            string lpServiceName,
            uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool StartService(
            IntPtr hService,
            int dwNumServiceArgs,
            IntPtr lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DeleteService(IntPtr hService);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ControlService(
            IntPtr hService,
            uint dwControl,
            ref SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);
    }
}
