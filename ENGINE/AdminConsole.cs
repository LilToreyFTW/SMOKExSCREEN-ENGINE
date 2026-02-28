using ENGINE.Data;
using ENGINE.Services;
using Spectre.Console;

namespace ENGINE.Services;

public class AdminConsole
{
    private readonly Database _db;
    private readonly KeyService _keyService;

    public AdminConsole(Database db, KeyService keyService)
    {
        _db = db;
        _keyService = keyService;
    }

    public void Run()
    {
        while (true)
        {
            Console.Clear();
            DrawHeader();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Select an option:[/]")
                    .AddChoices(
                        "📦  Generate Keys (1000 per duration)",
                        "🔑  View All Keys",
                        "🔍  Lookup Key",
                        "❌  Revoke Key",
                        "🚫  Ban HWID",
                        "📤  Export Keys to .txt",
                        "📊  Analytics Dashboard",
                        "📋  Recent Events Log",
                        "🏠  Exit"
                    ));

            Console.Clear();

            if (choice.Contains("Generate"))       GenerateKeys();
            else if (choice.Contains("View All"))  ViewAllKeys();
            else if (choice.Contains("Lookup"))    LookupKey();
            else if (choice.Contains("Revoke"))    RevokeKey();
            else if (choice.Contains("Ban"))       BanHwid();
            else if (choice.Contains("Export"))    ExportKeys();
            else if (choice.Contains("Analytics")) AnalyticsDashboard();
            else if (choice.Contains("Events"))    RecentEvents();
            else if (choice.Contains("Exit"))      break;

            if (!choice.Contains("Exit"))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
                Console.ReadKey(true);
            }
        }
    }

    // ─── Generate Keys ───────────────────────────────────────────────────────

    private void GenerateKeys()
    {
        AnsiConsole.MarkupLine("[bold yellow]📦 Generate Key Batches[/]\n");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What do you want to generate?")
                .AddChoices("All durations (1000 each = 4000 total)", "Specific duration only"));

        if (choice.Contains("All"))
        {
            AnsiConsole.Status().Start("Generating 4000 keys...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                var batches = _keyService.GenerateAllBatches(1000);
                ctx.Status("Done!");
                Thread.Sleep(500);
                AnsiConsole.MarkupLine("\n[green]✓ Keys generated:[/]");
                foreach (var (label, keys) in batches)
                    AnsiConsole.MarkupLine($"  [cyan]{label,-12}[/] → [white]{keys.Count}[/] keys");
            });
        }
        else
        {
            var duration = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select duration:")
                    .AddChoices("1 Day", "7 Days", "30 Days", "Lifetime"));

            var (_, code, days) = KeyService.Durations.First(d => d.Label == duration);
            var count = AnsiConsole.Ask<int>("How many keys?", 1000);

            AnsiConsole.Status().Start($"Generating {count} keys...", _ =>
            {
                _keyService.GenerateBatch(code, days, count);
            });
            AnsiConsole.MarkupLine($"[green]✓ {count} × {duration} keys generated.[/]");
        }
    }

    // ─── View All Keys ───────────────────────────────────────────────────────

    private void ViewAllKeys()
    {
        AnsiConsole.MarkupLine("[bold yellow]🔑 All Keys[/]\n");

        var filter = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Filter by status:")
                .AddChoices("All", "Unused", "Active", "Expired", "Revoked"));

        var keys = _db.GetAllKeys(filter == "All" ? null : filter);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Key")
            .AddColumn("Duration")
            .AddColumn("Status")
            .AddColumn("HWID")
            .AddColumn("Activated")
            .AddColumn("Expires");

        foreach (var k in keys.Take(200))
        {
            var statusColor = k.Status switch
            {
                "Active"  => "green",
                "Unused"  => "grey",
                "Expired" => "yellow",
                "Revoked" => "red",
                _         => "white"
            };

            var keyLabel = k.IsOwner ? $"[gold1]★ OWNER  {k.Key}[/]" : $"[white]{k.Key}[/]";

            table.AddRow(
                keyLabel,
                $"[cyan]{k.Duration}[/]",
                k.IsOwner ? "[gold1]Owner[/]" : $"[{statusColor}]{k.Status}[/]",
                k.HWID ?? "[grey]-[/]",
                k.ActivatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "[grey]-[/]",
                k.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ?? (k.DurationDays == -1 ? "[green]Lifetime[/]" : "[grey]-[/]")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[grey]Showing up to 200 of {keys.Count} total keys.[/]");
    }

    // ─── Lookup Key ──────────────────────────────────────────────────────────

    private void LookupKey()
    {
        AnsiConsole.MarkupLine("[bold yellow]🔍 Lookup Key[/]\n");
        var key = AnsiConsole.Ask<string>("Enter key:");
        var k = _db.GetKey(key);

        if (k == null)
        {
            AnsiConsole.MarkupLine("[red]Key not found.[/]");
            return;
        }

        var panel = new Panel(
            $"[grey]ID:[/]        {k.Id}\n" +
            $"[grey]Key:[/]       [white]{k.Key}[/]\n" +
            $"[grey]Duration:[/]  {k.Duration}\n" +
            $"[grey]Status:[/]    {k.Status}\n" +
            $"[grey]HWID:[/]      {k.HWID ?? "Not activated"}\n" +
            $"[grey]IP:[/]        {k.IpAddress ?? "-"}\n" +
            $"[grey]Version:[/]   {k.AppVersion ?? "-"}\n" +
            $"[grey]Created:[/]   {k.CreatedAt:yyyy-MM-dd HH:mm}\n" +
            $"[grey]Activated:[/] {k.ActivatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-"}\n" +
            $"[grey]Expires:[/]   {k.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ?? (k.DurationDays == -1 ? "Never (Lifetime)" : "-")}"
        ).Header("[yellow]Key Details[/]").Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
    }

    // ─── Revoke Key ──────────────────────────────────────────────────────────

    private void RevokeKey()
    {
        AnsiConsole.MarkupLine("[bold yellow]❌ Revoke Key[/]\n");
        var key = AnsiConsole.Ask<string>("Enter key to revoke:");
        var reason = AnsiConsole.Ask<string>("Reason:", "Admin revoked");

        if (!AnsiConsole.Confirm($"Revoke [red]{key}[/]?")) return;

        var existing = _db.GetKey(key);
        if (existing?.IsOwner == true)
        {
            AnsiConsole.MarkupLine("[red]Cannot revoke the owner key.[/]");
            return;
        }

        bool ok = _keyService.RevokeKey(key, reason);
        AnsiConsole.MarkupLine(ok ? "[green]✓ Key revoked.[/]" : "[red]Key not found.[/]");
    }

    // ─── Ban HWID ────────────────────────────────────────────────────────────

    private void BanHwid()
    {
        AnsiConsole.MarkupLine("[bold yellow]🚫 Ban HWID[/]\n");
        var hwid = AnsiConsole.Ask<string>("Enter HWID to ban:");
        var reason = AnsiConsole.Ask<string>("Reason:", "Banned by admin");

        if (!AnsiConsole.Confirm($"Ban HWID [red]{hwid}[/] and revoke all associated keys?")) return;

        bool ok = _keyService.BanHwid(hwid, reason);
        AnsiConsole.MarkupLine(ok ? "[green]✓ HWID banned and keys revoked.[/]" : "[red]Failed.[/]");
    }

    // ─── Export Keys ─────────────────────────────────────────────────────────

    private void ExportKeys()
    {
        AnsiConsole.MarkupLine("[bold yellow]📤 Export Keys[/]\n");

        var filter = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Export which keys?")
                .AddChoices("Unused only", "All keys", "Active only"));

        var statusFilter = filter switch
        {
            "Unused only" => "Unused",
            "Active only" => "Active",
            _             => (string?)null
        };

        var keys = _db.GetAllKeys(statusFilter);

        var exportPath = AnsiConsole.Ask<string>("Save path:", "keys_export.txt");

        var lines = new List<string>
        {
            $"# ENGINE Key Export — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
            $"# Filter: {filter} | Total: {keys.Count}",
            ""
        };

        // Group by duration
        foreach (var group in keys.GroupBy(k => k.Duration))
        {
            lines.Add($"## {group.Key} ({group.Count()} keys)");
            lines.AddRange(group.Select(k => k.Status == "Unused"
                ? k.Key
                : $"{k.Key}  [{k.Status}] HWID={k.HWID ?? "-"}"));
            lines.Add("");
        }

        File.WriteAllLines(exportPath, lines);
        AnsiConsole.MarkupLine($"[green]✓ Exported {keys.Count} keys to [white]{exportPath}[/][/]");
    }

    // ─── Analytics Dashboard ─────────────────────────────────────────────────

    private void AnalyticsDashboard()
    {
        AnsiConsole.MarkupLine("[bold yellow]📊 Analytics Dashboard[/]\n");

        var summary = _db.GetAnalyticsSummary();

        // Summary bar
        var grid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();
        grid.AddRow(
            StatCard("Total Keys",   summary["TotalKeys"].ToString(),    "white"),
            StatCard("Unused",       summary["UnusedKeys"].ToString(),   "grey"),
            StatCard("Active",       summary["ActiveKeys"].ToString(),   "green"),
            StatCard("Expired",      summary["ExpiredKeys"].ToString(),  "yellow")
        );
        grid.AddRow(
            StatCard("Revoked",      summary["RevokedKeys"].ToString(),  "red"),
            StatCard("Banned HWIDs", summary["BannedHwids"].ToString(), "red"),
            StatCard("Total Events", summary["TotalEvents"].ToString(),  "cyan"),
            StatCard("Failed Auths", summary["FailedValidations"].ToString(), "red")
        );
        AnsiConsole.Write(grid);

        // Daily activity
        AnsiConsole.MarkupLine("\n[bold]Daily Activity (Last 14 Days)[/]");
        var daily = _db.GetDailyActivity(14);
        var activityTable = new Table().Border(TableBorder.Simple)
            .AddColumn("Date")
            .AddColumn("Events")
            .AddColumn("Success")
            .AddColumn("Failures");

        foreach (var d in daily)
            activityTable.AddRow(
                $"[white]{d.Day}[/]",
                $"{d.Events}",
                $"[green]{d.Successes}[/]",
                $"[red]{d.Failures}[/]"
            );
        AnsiConsole.Write(activityTable);

        // Top HWIDs
        AnsiConsole.MarkupLine("\n[bold]Top HWIDs by Request Volume[/]");
        var hwids = _db.GetTopHwids(10);
        var hwidTable = new Table().Border(TableBorder.Simple)
            .AddColumn("HWID")
            .AddColumn("Requests")
            .AddColumn("Failures");

        foreach (var h in hwids)
            hwidTable.AddRow($"[white]{h.HWID}[/]", $"{h.Requests}", $"[red]{h.Failures}[/]");

        AnsiConsole.Write(hwidTable);
    }

    // ─── Recent Events ───────────────────────────────────────────────────────

    private void RecentEvents()
    {
        AnsiConsole.MarkupLine("[bold yellow]📋 Recent Events (Last 100)[/]\n");
        var events = _db.GetAnalytics(100);

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("Time")
            .AddColumn("Event")
            .AddColumn("Key")
            .AddColumn("HWID")
            .AddColumn("IP")
            .AddColumn("Note")
            .AddColumn("OK?");

        foreach (var e in events)
        {
            var color = e.Success ? "green" : "red";
            table.AddRow(
                $"[grey]{e.Timestamp:MM-dd HH:mm:ss}[/]",
                $"[cyan]{e.EventType}[/]",
                e.Key ?? "[grey]-[/]",
                e.HWID != null ? $"[white]{e.HWID[..Math.Min(12, e.HWID.Length)]}...[/]" : "[grey]-[/]",
                e.IpAddress ?? "[grey]-[/]",
                e.Note ?? "",
                $"[{color}]{(e.Success ? "✓" : "✗")}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void DrawHeader()
    {
        AnsiConsole.Write(new FigletText("ENGINE").Color(Color.DodgerBlue1));
        AnsiConsole.MarkupLine("[grey]License Key Management System[/]\n");
    }

    private static string StatCard(string label, string value, string color) =>
        $"[grey]{label}[/]\n[{color} bold]{value}[/]";
}
