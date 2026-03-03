using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SmokeScreenEngine
{
    public class DiscordBotLauncher
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🤖 Starting Ultimate Discord Bot for SmokeScreen Engine...");
            Console.WriteLine("👑 Owner: Bl0wdart (1368087024401252393)");
            Console.WriteLine("🔥 Features: SmokeScreen Monitoring, Key Management, Login Tracking");
            
            var bot = new UltimateDiscordBot();
            
            try
            {
                await bot.StartBotAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Bot error: {ex.Message}");
                Console.WriteLine("🔄 Restarting bot in 30 seconds...");
                await Task.Delay(30000);
                
                // Restart the bot
                await Main(args);
            }
        }
    }
}
