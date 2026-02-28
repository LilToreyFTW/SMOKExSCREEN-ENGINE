using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public partial class EnginePage : UserControl
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
        private Label _statusLabel = null!;

        public EnginePage()
        {
            InitializeComponent();
            this.Load += async (_, __) => await RefreshAsync();
        }

        private void InitializeComponent()
        {
            BackColor = Theme.Background;
            AutoScroll = true;

            var title = new Label
            {
                Text = "ENGINE — Real-Time Distributed System",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 20),
                AutoSize = true
            };
            Controls.Add(title);

            var features = new[]
            {
                new { Icon = "⚡", Title = "Real-Time Engine", Desc = "Sub‑millisecond event processing with distributed state sync across all nodes." },
                new { Icon = "🔐", Title = "Zero-Trust Auth", Desc = "End-to-end encryption with dynamic token rotation, SSO, MFA, and role‑based permissions." },
                new { Icon = "📡", Title = "Live Data Pipelines", Desc = "Push millions of events per second through customizable pipelines." },
                new { Icon = "🧠", Title = "AI Inference Layer", Desc = "Deploy custom ML models directly into the ENGINE runtime at the edge." },
                new { Icon = "🗂️", Title = "Modular Workflow Builder", Desc = "Drag, wire, and deploy complex automation workflows. Versioned and auditable." },
                new { Icon = "📊", Title = "Observability Suite", Desc = "Full‑stack tracing, custom dashboards, and anomaly detection." },
                new { Icon = "🌐", Title = "Global CDN Mesh", Desc = "40+ edge nodes. Automatic geo‑routing, failover, and intelligent caching." },
                new { Icon = "🔌", Title = "Unified API Gateway", Desc = "One control plane for REST, GraphQL, gRPC — managed, secured, rate‑limited." },
                new { Icon = "🚀", Title = "Instant Deploy", Desc = "From commit to live in under 30 seconds. Blue/green, canary, smoke tests." }
            };

            int y = 70;
            foreach (var f in features)
            {
                var card = new Panel
                {
                    Bounds = new Rectangle(24, y, 820, 80),
                    BackColor = Color.FromArgb(21, 24, 38),
                    BorderStyle = BorderStyle.None
                };

                var icon = new Label
                {
                    Text = f.Icon,
                    Font = new Font("Segoe UI Emoji", 24),
                    ForeColor = Theme.AccentBlue,
                    Location = new Point(16, 20),
                    AutoSize = true
                };

                var titleLbl = new Label
                {
                    Text = f.Title,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(70, 16),
                    AutoSize = true
                };

                var descLbl = new Label
                {
                    Text = f.Desc,
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(70, 40),
                    Width = 720,
                    AutoSize = false
                };

                card.Controls.AddRange(new Control[] { icon, titleLbl, descLbl });
                Controls.Add(card);
                y += 100;
            }

            // Status line
            _statusLabel = new Label
            {
                Text = "Checking ENGINE status…",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, y + 20),
                AutoSize = true
            };
            Controls.Add(_statusLabel);
        }

        private async Task RefreshAsync()
        {
            try
            {
                var resp = await _http.GetAsync("https://smok-ex-screen-engine.vercel.app/ping");
                if (resp.IsSuccessStatusCode)
                {
                    _statusLabel.Text = "✓ ENGINE Online — All systems operational";
                    _statusLabel.ForeColor = Theme.Success;
                }
                else
                {
                    _statusLabel.Text = "✗ ENGINE unreachable";
                    _statusLabel.ForeColor = Theme.Error;
                }
            }
            catch
            {
                _statusLabel.Text = "✗ ENGINE unreachable";
                _statusLabel.ForeColor = Theme.Error;
            }
        }
    }
}
