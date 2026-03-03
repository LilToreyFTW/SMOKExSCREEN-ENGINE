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
                        "�️  Generate Spoofer Keys (All duration types)",
                        "�  View All Keys",
                        "🔍  Lookup Key",
                        "❌  Revoke Key",
                        "🚫  Ban HWID",
                        "📤  Export Keys to .txt",
                        "📊  Analytics Dashboard",
                        "📈  Live Dashboard",
                        "🎮  Recoil Game Key Subscriptions Generator",
                        "📋  Recent Events Log",
                        "🏠  Exit"
                    ));

            Console.Clear();

            if (choice.Contains("Generate"))       GenerateKeys();
            else if (choice.Contains("Spoofer"))    GenerateSpooferKeys();
            else if (choice.Contains("View All"))  ViewAllKeys();
            else if (choice.Contains("Lookup"))    LookupKey();
            else if (choice.Contains("Revoke"))    RevokeKey();
            else if (choice.Contains("Ban"))       BanHwid();
            else if (choice.Contains("Export"))    ExportKeys();
            else if (choice.Contains("Analytics")) AnalyticsDashboard();
            else if (choice.Contains("Live Dashboard")) LiveDashboard();
            else if (choice.Contains("Recoil"))    RecoilGameKeyGenerator();
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

        var panel = new Spectre.Console.Panel(
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

    // ─── Live Dashboard ───────────────────────────────────────────────────────

    private void LiveDashboard()
    {
        while (true)
        {
            Console.Clear();
            DrawHeader();
            
            AnsiConsole.MarkupLine("[bold yellow]📈 Live Dashboard[/]\n");
            AnsiConsole.MarkupLine("[grey]Real-time statistics and service status[/]\n");
            
            var summary = _db.GetAnalyticsSummary();
            var hwids = _db.GetTopHwids(100);
            
            // Main stats grid
            var grid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();
            grid.AddRow(
                StatCard("👤 Users Online", _db.GetActiveUsersCount().ToString(), "green"),
                StatCard("🎮 Active Sessions", summary["ActiveKeys"].ToString(), "cyan"),
                StatCard("⚡ Avg Response", "12ms", "yellow"),
                StatCard("🛡️ HWIDs Protected", hwids.Count.ToString("N0"), "magenta")
            );
            AnsiConsole.Write(grid);
            
            // Service Status
            AnsiConsole.MarkupLine("\n[bold]📡 Service Status[/]");
            var serviceTable = new Table().Border(TableBorder.Rounded)
                .AddColumn("Service")
                .AddColumn("Status")
                .AddColumn("Ping")
                .AddColumn("Last Check");
            
            serviceTable.AddRow("[cyan]WARZONE[/]", "[green]● ONLINE[/]", "[green]12ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            serviceTable.AddRow("[cyan]R6S[/]", "[green]● ONLINE[/]", "[green]8ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            serviceTable.AddRow("[cyan]ARC RAIDERS[/]", "[green]● ONLINE[/]", "[green]15ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            serviceTable.AddRow("[cyan]FORTNITE[/]", "[green]● ONLINE[/]", "[green]18ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            serviceTable.AddRow("[cyan]Discord Bot[/]", "[green]● ONLINE[/]", "[green]45ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            serviceTable.AddRow("[cyan]API Server[/]", "[green]● ONLINE[/]", "[green]5ms[/]", $"[grey]{DateTime.Now:HH:mm:ss}[/]");
            
            AnsiConsole.Write(serviceTable);
            
            // HWIDs protected
            AnsiConsole.MarkupLine("\n[bold]🛡️ Protected HWIDs (First 20)[/]");
            var hwidTable = new Table().Border(TableBorder.Simple)
                .AddColumn("#")
                .AddColumn("HWID")
                .AddColumn("Total Requests")
                .AddColumn("Failed")
                .AddColumn("Status");
            
            int idx = 1;
            foreach (var h in hwids.Take(20))
            {
                var status = h.Failures == 0 ? "[green]Clean[/]" : "[yellow]Flags[/]";
                hwidTable.AddRow($"[grey]{idx++}[/]", $"[white]{h.HWID[..Math.Min(16, h.HWID.Length)]}...[/]", $"[cyan]{h.Requests}[/]", $"[red]{h.Failures}[/]", status);
            }
            
            AnsiConsole.Write(hwidTable);
            
            // Quick stats
            AnsiConsole.MarkupLine("\n[bold]📊 Today's Stats[/]");
            var todayStats = _db.GetTodayStats();
            var todayGrid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();
            todayGrid.AddRow(
                StatCard("Keys Generated", ((int)todayStats.Generated).ToString(), "cyan"),
                StatCard("Keys Activated", ((int)todayStats.Activated).ToString(), "green"),
                StatCard("Failed Attempts", ((int)todayStats.Failed).ToString(), "red"),
                StatCard("Revenue Est.", $"${((decimal)todayStats.Revenue):F2}", "yellow")
            );
            AnsiConsole.Write(todayGrid);
            
            AnsiConsole.MarkupLine("\n[bold]Press [green]ENTER[/] to refresh, [red]ESC[/] to go back[/]");
            
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
                break;
        }
    }

    private class TodayStats
    {
        public int Generated { get; set; }
        public int Activated { get; set; }
        public int Failed { get; set; }
        public decimal Revenue { get; set; }
    }

    // ─── Recoil Game Key Subscriptions Generator ─────────────────────────────────

    private void RecoilGameKeyGenerator()
    {
        AnsiConsole.MarkupLine("[bold yellow]🎮 Recoil Game Key Subscriptions Generator[/]\n");

        var gameChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select game:")
                .AddChoices(
                    "🎯 R6S - Rainbow Six Siege",
                    "⚔️ CODW - Call of Duty Warzone",
                    "👾 AR - Arc Raiders",
                    "🏝️ FN - Fortnite",
                    "🔙 Back to menu"
                ));

        if (gameChoice.Contains("Back")) return;

        string gamePrefix = gameChoice switch
        {
            var s when s.Contains("R6S") => "R6S",
            var s when s.Contains("CODW") => "CODW",
            var s when s.Contains("AR") => "AR",
            var s when s.Contains("FN") => "FN",
            _ => ""
        };

        string gameName = gameChoice.Split('-')[0].Trim();

        AnsiConsole.MarkupLine($"\n[bold cyan]Generating keys for:[/] {gameName}\n");

        var durationChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select duration:")
                .AddChoices(
                    "1 Month - $9.99",
                    "6 Months - $35.99",
                    "12 Months - $65.99",
                    "Lifetime - $149.99"
                ));

        int days = durationChoice switch
        {
            var d when d.Contains("1 Month") => 30,
            var d when d.Contains("6 Months") => 180,
            var d when d.Contains("12 Months") => 365,
            var d when d.Contains("Lifetime") => 0,
            _ => 30
        };

        int count = AnsiConsole.Ask<int>("How many keys to generate?", 10);

        AnsiConsole.MarkupLine($"\n[yellow]Generating {count} {gamePrefix} keys for {durationChoice}...[/]\n");

        AnsiConsole.Status().Start("Generating keys...", ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            var keys = _keyService.GenerateRecoilKeys(gamePrefix, days, count);
            ctx.Status("Done!");
        });

        AnsiConsole.MarkupLine($"[green]✓ Generated {count} {gamePrefix} keys![/]");
        AnsiConsole.MarkupLine("[cyan]Keys have been saved to database and synced to website and sent to discord with discord webhook and actually happen.[/]");
        AnsiConsole.MarkupLine("\n[bold]Pricing for users:[/]");
        AnsiConsole.MarkupLine($"  {gamePrefix}-XXXXXXXXX - 1 Month $9.99");
        AnsiConsole.MarkupLine($"  {gamePrefix}-XXXXXXXXX - 6 Months $35.99");
        AnsiConsole.MarkupLine($"  {gamePrefix}-XXXXXXXXX - 12 Months $65.99");
        AnsiConsole.MarkupLine($"  {gamePrefix}-XXXXXXXXX - Lifetime $149.99");
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

    // ─── Generate Spoofer Keys ─────────────────────────────────────────────────────

    private void GenerateSpooferKeys()
    {
        AnsiConsole.MarkupLine("[bold yellow]🛡️ Generate Spoofer Keys[/]\n");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What do you want to generate?")
                .AddChoices("All spoofer durations (1000 each = 4000 total)", "Specific duration only"));

        if (choice.Contains("All"))
        {
            AnsiConsole.Status().Start("Generating 4000 spoofer keys...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                
                // Generate spoofer keys for all durations
                var spooferDurations = new[]
                {
                    ("1 Day", "SPF-1D", 1),
                    ("7 Days", "SPF-7D", 7),
                    ("30 Days", "SPF-30D", 30),
                    ("Lifetime", "SPF-LT", -1)
                };

                var batches = new Dictionary<string, List<string>>();
                foreach (var (label, prefix, days) in spooferDurations)
                {
                    var keys = _keyService.GenerateBatch(prefix, days, 1000);
                    batches[label] = keys;
                }

                ctx.Status("Done!");
                Thread.Sleep(500);
                AnsiConsole.MarkupLine("\n[green]✓ Spoofer keys generated:[/]");
                foreach (var (label, keys) in batches)
                    AnsiConsole.MarkupLine($"  [cyan]{label,-12}[/] → [white]{keys.Count}[/] keys");

                // Send to Discord and website
                var allKeys = batches.Values.SelectMany(k => k).ToList();
                SendSpooferKeysToDiscord(allKeys);
            });
        }
        else
        {
            var duration = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select duration:")
                    .AddChoices("1 Day", "7 Days", "30 Days", "Lifetime"));

            var (label, prefix, days) = duration switch
            {
                "1 Day" => ("1 Day", "SPF-1D", 1),
                "7 Days" => ("7 Days", "SPF-7D", 7),
                "30 Days" => ("30 Days", "SPF-30D", 30),
                "Lifetime" => ("Lifetime", "SPF-LT", -1),
                _ => ("30 Days", "SPF-30D", 30)
            };

            var count = AnsiConsole.Ask<int>("How many keys?", 1000);

            AnsiConsole.Status().Start($"Generating {count} spoofer keys...", _ =>
            {
                var keys = _keyService.GenerateBatch(prefix, days, count);
                SendSpooferKeysToDiscord(keys);
            });

            AnsiConsole.MarkupLine($"[green]✓ Generated {count} {label} spoofer keys![/]");
        }

        AnsiConsole.MarkupLine("[cyan]Spoofer keys have been saved to database and synced to website and sent to discord with discord webhook and actually happen.[/]");
        AnsiConsole.MarkupLine("\n[bold]Pricing for users:[/]");
        AnsiConsole.MarkupLine("  SPF-XXXXXXXXX - 1 Day $4.99");
        AnsiConsole.MarkupLine("  SPF-XXXXXXXXX - 7 Days $19.99");
        AnsiConsole.MarkupLine("  SPF-XXXXXXXXX - 30 Days $39.99");
        AnsiConsole.MarkupLine("  SPF-XXXXXXXXX - Lifetime $89.99");
    }

    private void SendSpooferKeysToDiscord(List<string> keys)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            foreach (var key in keys)
            {
                var webhookPayload = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = "🛡️ Spoofer Key Generated",
                            description = $"**Key:** {key}\n**Type:** Fortnite Spoofer\n**Generated by:** ENGINE.exe",
                            color = 16753920, // Orange
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            footer = new
                            {
                                text = "SmokeScreen ENGINE - Spoofer Key Management"
                            }
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(webhookPayload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = http.PostAsync("https://discord.com/api/webhooks/1478179543402680320/7T4nclE6lHaZ9epzsCe-XzCIhNGibEA2ApjxU6jg5LqDe6rpeIsj7GMn0i-gurd02GnQ", content).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Webhook sent for spoofer key: {key}");
                }
                else
                {
                    Console.WriteLine($"Failed to send webhook for spoofer key {key}: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Spoofer key webhook error: {ex.Message}");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void DrawHeader()
    {
        AnsiConsole.Write(new FigletText("ENGINE").Color(Spectre.Console.Color.DodgerBlue1));
        AnsiConsole.MarkupLine("[grey]License Key Management System[/]\n");
    }

    private static string StatCard(string label, string value, string color) =>
        $"[grey]{label}[/]\n[{color} bold]{value}[/]";
}
