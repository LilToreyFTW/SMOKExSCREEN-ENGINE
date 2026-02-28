using ENGINE.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ENGINE.Api;

public static class ApiServer
{
    public static WebApplication Build(Database db, int port = 5150)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(db);
        builder.WebHost.UseUrls($"http://localhost:{port}");

        // Silence ASP.NET startup noise
        builder.Logging.ClearProviders();

        var app = builder.Build();

        // ── POST /validate ───────────────────────────────────────────────────
        // Body: { "key": "XXXX-XXXX-XXXX-XXXX", "hwid": "...", "version": "..." }
        app.MapPost("/validate", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<ValidateRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.Key) || string.IsNullOrWhiteSpace(body.Hwid))
                return Results.BadRequest(new { success = false, reason = "MISSING_FIELDS" });

            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            var (valid, reason) = db.ValidateKey(body.Key, body.Hwid, ip, body.Version);

            return Results.Ok(new
            {
                success = valid,
                reason,
                timestamp = DateTime.UtcNow
            });
        });

        // ── GET /ping ────────────────────────────────────────────────────────
        app.MapGet("/ping", () => Results.Ok(new { status = "ENGINE_ONLINE", timestamp = DateTime.UtcNow }));

        return app;
    }

    private record ValidateRequest(string Key, string Hwid, string? Version);
}
