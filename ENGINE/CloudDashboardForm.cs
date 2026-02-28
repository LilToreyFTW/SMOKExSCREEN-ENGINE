using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SmokeScreenEngine
{
    public partial class CloudDashboardForm : Form
    {
        private readonly string?    _sessionToken;
        private readonly HttpClient _http;
        private TabControl          tabControl = null!;
        private ListView            nodeList   = null!;
        private Label               statusLbl  = null!;

        // Accept null token gracefully — form just shows "not authenticated"
        public CloudDashboardForm(string? sessionToken = null)
        {
            _sessionToken = sessionToken;
            _http         = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            if (sessionToken != null)
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", sessionToken);
            _http.DefaultRequestHeaders.Add("User-Agent", "SmokeScreenENGINE/2.0");

            InitializeComponent();
            this.Load += async (s, e) => await LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Text            = "Cloud Control — SmokeScreen ENGINE";
            this.Size            = new Size(900, 620);
            this.BackColor       = Theme.Background;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.DoubleBuffered  = true;

            statusLbl = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 28,
                BackColor = Color.FromArgb(17, 22, 28),
                ForeColor = Theme.TextSecondary,
                Font      = new Font("Consolas", 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0),
                Text      = "● Loading...",
            };

            tabControl = new TabControl
            {
                Dock    = DockStyle.Fill,
                Padding = new Point(16, 8),
            };

            // ── Tab 1: Server Status ──────────────────────────────────────
            var t1 = new TabPage("SERVER STATUS") { BackColor = Theme.Background };
            nodeList = new ListView
            {
                Dock        = DockStyle.Fill,
                View        = View.Details,
                BackColor   = Theme.CardBackground,
                ForeColor   = Color.White,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                GridLines   = false,
            };
            nodeList.Columns.Add("ENDPOINT",  280);
            nodeList.Columns.Add("STATUS",    100);
            nodeList.Columns.Add("LATENCY",   80);
            nodeList.Columns.Add("DETAIL",    320);
            t1.Controls.Add(nodeList);

            // ── Tab 2: License Stats ──────────────────────────────────────
            var t2 = new TabPage("LICENSE STATS") { BackColor = Theme.Background };
            var statsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            // Populated in LoadDataAsync
            statsPanel.Tag = "statsPanel";
            t2.Controls.Add(statsPanel);

            // ── Tab 3: ChartControl ───────────────────────────────────────
            var t3 = new TabPage("NETWORK") { BackColor = Theme.Background };
            var chart = new ChartControl
            {
                Dock  = DockStyle.Fill,
                Title = "Latency History (ms)",
                XAxis = "Ping",
                YAxis = "ms",
            };
            chart.Tag = "latencyChart";
            t3.Controls.Add(chart);

            tabControl.TabPages.Add(t1);
            tabControl.TabPages.Add(t2);
            tabControl.TabPages.Add(t3);

            this.Controls.Add(tabControl);
            this.Controls.Add(statusLbl);
        }

        private async Task LoadDataAsync()
        {
            // ── Ping liveness endpoint (DB-independent) ───────────────────
            string pingUrl   = $"{DiscordAuth.API_BASE}/ping";
            string healthUrl = $"{DiscordAuth.API_BASE}/health";
            long   ms        = -1;
            bool   dbOk      = false;
            string dbDetail  = "";

            try
            {
                // 1) Fast liveness ping
                var sw    = System.Diagnostics.Stopwatch.StartNew();
                var ping  = await _http.GetAsync(pingUrl);
                sw.Stop();
                ms = ping.IsSuccessStatusCode ? sw.ElapsedMilliseconds : -1;

                // 2) Separate /health call for DB status (don't let DB failures
                //    make the whole server appear offline)
                var res = await _http.GetAsync(healthUrl);
                if (res.StatusCode == System.Net.HttpStatusCode.OK ||
                    res.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var h    = JsonConvert.DeserializeObject<dynamic>(json);
                    dbOk     = h?.db == true || (string?)h?.status == "ok";
                    dbDetail = dbOk ? "Neon PostgreSQL connected" : "DB error — check DATABASE_URL";
                }
                else
                {
                    dbDetail = $"HTTP {(int)res.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                dbDetail = ex.Message;
            }

            SafeInvoke(() =>
            {
                nodeList.Items.Clear();

                // Row 1: API server
                var row1 = new ListViewItem(new[]
                {
                    DiscordAuth.API_BASE,
                    ms >= 0 ? "ONLINE" : "OFFLINE",
                    ms >= 0 ? $"{ms}ms" : "—",
                    "Vercel Edge Function",
                })
                { ForeColor = ms >= 0 ? Theme.Success : Theme.Error };
                nodeList.Items.Add(row1);

                // Row 2: Database
                var row2 = new ListViewItem(new[]
                {
                    "Neon PostgreSQL",
                    dbOk ? "ONLINE" : "ERROR",
                    "—",
                    dbDetail,
                })
                { ForeColor = dbOk ? Theme.Success : Theme.Error };
                nodeList.Items.Add(row2);

                // Row 3: Discord OAuth
                nodeList.Items.Add(new ListViewItem(new[]
                {
                    "discord.com/api/oauth2",
                    "EXTERNAL",
                    "—",
                    "Managed by Discord",
                })
                { ForeColor = Color.FromArgb(88, 101, 242) });

                // Status bar
                if (ms >= 0)
                {
                    statusLbl.Text      = $"● API {ms}ms  |  DB: {(dbOk ? "OK" : "ERROR")}  |  {DateTime.Now:HH:mm:ss}";
                    statusLbl.ForeColor = ms < 300 ? Theme.Success : Color.Orange;
                }
                else
                {
                    statusLbl.Text      = "● OFFLINE — Cannot reach Vercel API";
                    statusLbl.ForeColor = Theme.Error;
                }

                // Update chart with latency point
                var chart = FindChartControl();
                if (chart != null && ms >= 0)
                    chart.AddPoint("Latency", (int)ms, Theme.AccentBlue);
            });

            // ── Load license stats (requires auth) ────────────────────────
            if (_sessionToken == null) return;
            try
            {
                var res  = await _http.GetAsync($"{DiscordAuth.API_BASE}/keys/validate");
                var json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return;

                var data   = JsonConvert.DeserializeObject<dynamic>(json);
                bool lic   = data?.licensed == true;
                int  count = (int)(data?.key_count ?? 0);

                SafeInvoke(() =>
                {
                    // Find stats panel and populate
                    var statsPanel = FindStatsPanel();
                    if (statsPanel == null) return;
                    statsPanel.Controls.Clear();

                    AddStat(statsPanel, 20, 20, "License Status",
                        lic ? "✓ LICENSED" : "⏱ TRIAL / UNLICENSED",
                        lic ? Theme.Success : Color.Orange);
                    AddStat(statsPanel, 20, 100, "Keys Redeemed",
                        count.ToString(), Theme.AccentBlue);
                    AddStat(statsPanel, 20, 180, "Auth Method",
                        "SESSION TOKEN (Bearer)", Theme.TextSecondary);
                });
            }
            catch { }
        }

        private void AddStat(Panel parent, int x, int y, string label, string value, Color valueColor)
        {
            var card = new Panel
            {
                Bounds    = new Rectangle(x, y, 320, 64),
                BackColor = Theme.CardBackground,
            };
            card.Controls.Add(new Label
            {
                Text = label, Font = new Font("Segoe UI", 8), ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(12, 8, 290, 16), TextAlign = ContentAlignment.MiddleLeft,
            });
            card.Controls.Add(new Label
            {
                Text = value, Font = new Font("Consolas", 10, FontStyle.Bold), ForeColor = valueColor,
                Bounds = new Rectangle(12, 28, 290, 24), TextAlign = ContentAlignment.MiddleLeft,
            });
            parent.Controls.Add(card);
        }

        private ChartControl? FindChartControl()
        {
            foreach (TabPage tp in tabControl.TabPages)
                foreach (Control c in tp.Controls)
                    if (c is ChartControl ch) return ch;
            return null;
        }

        private Panel? FindStatsPanel()
        {
            foreach (TabPage tp in tabControl.TabPages)
                foreach (Control c in tp.Controls)
                    if (c is Panel p && (string?)p.Tag == "statsPanel") return p;
            return null;
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) Invoke(action);
            else action();
        }
    }
}
