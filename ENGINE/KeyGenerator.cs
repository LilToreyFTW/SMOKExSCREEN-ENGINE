using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmokeScreenEngine
{
    public static class KeyGenerator
    {
        private static readonly Random _rng = new();

        public static string GenerateKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var segments = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                var segment = new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[_rng.Next(s.Length)]).ToArray());
                segments.Add(segment);
            }
            return string.Join("-", segments);
        }

        public static async Task GenerateAndSendBatchAsync(int count = 10, string durationType = "1_MONTH")
        {
            var keys = Enumerable.Range(0, count).Select(_ => GenerateKey()).ToList();
            Console.WriteLine($"Generated {keys.Count} keys ({durationType}) and sent to discord with discord webhook and actually happen.");
            await KeyExtension.SaveKeysToAllAsync(keys, durationType);
        }
    }
}
