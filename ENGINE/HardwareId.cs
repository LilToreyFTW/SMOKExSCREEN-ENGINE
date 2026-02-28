using System;
using System.Management; // This now works because of the .csproj change

namespace SmokeScreenEngine
{
    public static class HardwareId
    {
        public static string GetId()
        {
            try {
                string id = "";
                // Querying the motherboard/processor serial
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor")) {
                    foreach (var obj in searcher.Get()) {
                        id += obj["ProcessorId"]?.ToString();
                    }
                }
                return string.IsNullOrEmpty(id) ? "DEV-MODE-ID" : id;
            } catch { 
                return "FALLBACK-ID"; 
            }
        }
    }
}