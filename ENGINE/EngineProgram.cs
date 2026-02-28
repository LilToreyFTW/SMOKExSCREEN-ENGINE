using ENGINE.Api;
using ENGINE.Data;
using ENGINE.Services;

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
            var console = new AdminConsole(db, keyService);
            console.Run();
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
