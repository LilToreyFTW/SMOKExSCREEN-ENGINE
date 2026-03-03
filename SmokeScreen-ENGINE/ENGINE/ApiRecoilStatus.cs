using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SmokeScreenEngine
{
    public static class ApiRecoilStatus
    {
        private static readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "api-recoil-status.log");
        private static readonly List<StatusEntry> _log = new();

        public enum ServiceState
        {
            WORKING,
            MAINTENANCE,
            UPDATING,
            NOT_OPERATIONAL
        }

        public class StatusEntry
        {
            public DateTime Timestamp { get; set; }
            public string Service { get; set; } = "";
            public ServiceState State { get; set; }
            public string Reason { get; set; } = "";
            public int MsPing { get; set; }
        }

        public static void LogStatus(string service, ServiceState state, string reason = "", int msPing = 0)
        {
            var entry = new StatusEntry
            {
                Timestamp = DateTime.UtcNow,
                Service = service,
                State = state,
                Reason = reason,
                MsPing = msPing
            };
            _log.Add(entry);
            Task.Run(() => AppendToFile(entry));
        }

        private static async Task AppendToFile(StatusEntry entry)
        {
            try
            {
                var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | {entry.Service} | {entry.State} | {entry.MsPing}ms | {entry.Reason}";
                await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
            }
            catch { }
        }

        public static List<StatusEntry> GetRecent(int count = 50)
        {
            var all = _log.ToList();
            var start = Math.Max(0, all.Count - count);
            return all.GetRange(start, all.Count - start);
        }

        public static void ExportToFile(string path)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var e in _log)
                {
                    sb.AppendLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} | {e.Service} | {e.State} | {e.MsPing}ms | {e.Reason}");
                }
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }
    }
}
