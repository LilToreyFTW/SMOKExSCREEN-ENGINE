using ENGINE.Api;
using ENGINE.Data;
using ENGINE.Services;
using SmokeScreenEngine;
using System.Windows.Forms;

namespace ENGINE;

internal static class EngineProgram
{
    public static async Task Main(string[] args)
    {
        var db = new Database();

        var ownerKey = Environment.GetEnvironmentVariable("ENGINE_OWNER_KEY")
            ?? "SS-MASTER-99X-QM22-L091-OWNER-PRIME";
        db.SeedOwnerKey(ownerKey);

        var app = ApiServer.Build(db, port: 5150);
        await app.StartAsync();

        try
        {
            var keyService = new KeyService(db);
            
            // Start Live User Dashboard in background thread
            var dashboardThread = new System.Threading.Thread(() => {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LiveUserDashboard());
            });
            dashboardThread.SetApartmentState(System.Threading.ApartmentState.STA);
            dashboardThread.Start();
            
            var console = new AdminConsole(db, keyService);
            console.Run();
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
