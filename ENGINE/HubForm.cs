using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace SmokeScreenEngine
{
    public class HubForm : Form
    {
        private readonly TabControl _tabs = new();

        private Label _userLabel = null!;
        private Label _licenseLabel = null!;
        private Button _loginBtn = null!;
        private Button _logoutBtn = null!;
        private Button _openMarketplaceBtn = null!;
        private Button _openCloudBtn = null!;

        private TextBox _tokenInput = null!;
        private TextBox _redeemKeyBox = null!;
        private Button _redeemBtn = null!;
        private Label _redeemResult = null!;
        private Button _refreshKeysBtn = null!;
        private Label _keysCountLabel = null!;
        private Label _pingLabel = null!;
        private readonly System.Windows.Forms.Timer _pingTimer = new();
        private readonly System.Windows.Forms.Timer _statsTimer = new();

        private string? _token;
        private UserInfo? _user;
        private LicenseStatus? _license;
        private bool _keysInSync = false;

        public HubForm()
        {
            InitializeComponent();
            
            // Initialize KeyUserGrid config
            KeyUserGridManager.Initialize(AppContext.BaseDirectory);
            
            this.Load += async (_, __) => await LoadSessionAsync();
            SetupPingTimer();
            SetupStatsTimer();
            TSyncListener.Start();
            
            // Initialize EngineAI
            _ = EngineAI.Instance;
            _ = EngineAI.Instance.NotifyAsync("🚀 SmokeScreenEngine Started", "Desktop application is now online", "exe", "success");
        }

        private void InitializeComponent()
        {
            Text = "SmokeScreen ENGINE v4.2";
            Size = new Size(1200, 800);
            BackColor = Theme.Background;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            WindowState = FormWindowState.Maximized;

            _tabs.Dock = DockStyle.Fill;
            _tabs.Padding = new Point(16, 8);
            _tabs.Font = new Font("Segoe UI", 10);

            _tabs.TabPages.Add(BuildAccountTab());
            _tabs.TabPages.Add(BuildLicenseTab());
            _tabs.TabPages.Add(BuildGameTab("🎮 WARZONE", "Warzone"));
            _tabs.TabPages.Add(BuildGameTab("🔫 R6S", "R6S"));
            _tabs.TabPages.Add(BuildGameTab("👾 ARC RAIDERS", "Arc Raiders"));
            _tabs.TabPages.Add(BuildGameTab("🏝️ FN", "Fortnite"));
            _tabs.TabPages.Add(BuildAutoUpdaterTab());

            _pingLabel = new Label
            {
                Text = "…",
                Font = new Font("Consolas", 8),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.FromArgb(32, 36, 44),
                AutoSize = false,
                Size = new Size(120, 24),
                Location = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_pingLabel);

            Controls.Add(_tabs);
        }

        private void SetupStatsTimer()
        {
            _statsTimer.Interval = 5000;
            _statsTimer.Tick += (_, _) => UpdateStats();
            _statsTimer.Start();
        }

        private void UpdateStats()
        {
            try
            {
                AnalyticsService.Instance.TrackEvent("app_heartbeat");
            }
            catch { }
        }

        private TabPage BuildDashboardTab()
        {
            var tp = new TabPage("🏠 DASHBOARD") { BackColor = Theme.Background, Padding = new Padding(20) };

            var header = new Label
            {
                Text = "🎯 Dashboard Overview",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(header);

            var statsGrid = new FlowLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(1100, 200),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true
            };

            var cards = new[]
            {
                ("Keys Available", $"{KeyCache.GetAll().Count(x => !x.Used)}", "🎫", Theme.AccentBlue),
                ("Events Today", $"{AnalyticsService.Instance.GetMonthlyEventCount():N0}", "⚡", Color.FromArgb(255, 193, 7)),
                ("Active Projects", $"{ProjectService.Instance.GetActiveProjectCount()}", "📁", Color.FromArgb(156, 39, 176)),
                ("Team Members", $"{TeamService.Instance.GetActiveMemberCount()}", "👥", Color.FromArgb(0, 188, 212)),
                ("AI Requests", "0", "🧠", Color.FromArgb(255, 87, 34)),
                ("API Calls", "0", "🔌", Color.FromArgb(76, 175, 80)),
                ("Deployments", "0", "🚀", Color.FromArgb(233, 30, 99)),
                ("Uptime", "99.99%", "⏱️", Color.FromArgb(121, 85, 72))
            };

            foreach (var (title, value, icon, color) in cards)
            {
                var card = CreateStatCard(title, value, icon, color);
                statsGrid.Controls.Add(card);
            }
            tp.Controls.Add(statsGrid);

            var quickActions = new Label
            {
                Text = "⚡ Quick Actions",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 300),
                AutoSize = true
            };
            tp.Controls.Add(quickActions);

            var actionsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 340),
                Size = new Size(800, 60),
                FlowDirection = FlowDirection.LeftToRight
            };

            var generateBtn = CreateActionButton("🎫 Generate Keys", () => _tabs.SelectedIndex = 3);
            var redeemBtn = CreateActionButton("🎁 Redeem Key", () => _tabs.SelectedIndex = 2);
            var aiBtn = CreateActionButton("🧠 AI Assistant", () => _tabs.SelectedIndex = 7);
            var syncAiBtn = CreateActionButton("🔄 Sync AI", async () => {
                try {
                    await EngineAI.Instance.NotifyAsync("🔄 AI Sync", "Syncing with website...", "exe", "info");
                    MessageBox.Show("AI Sync Complete!", "SmokeScreen ENGINE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } catch (Exception ex) {
                    MessageBox.Show($"Sync failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var deployBtn = CreateActionButton("🚀 Deploy", () => _tabs.SelectedIndex = 12);

            actionsPanel.Controls.Add(generateBtn);
            actionsPanel.Controls.Add(redeemBtn);
            actionsPanel.Controls.Add(aiBtn);
            actionsPanel.Controls.Add(syncAiBtn);
            actionsPanel.Controls.Add(deployBtn);
            tp.Controls.Add(actionsPanel);

            var recentActivity = new Label
            {
                Text = "📊 Recent Activity",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 420),
                AutoSize = true
            };
            tp.Controls.Add(recentActivity);

            var activityList = new ListBox
            {
                Location = new Point(20, 460),
                Size = new Size(600, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            activityList.Items.AddRange(new[]
            {
                $"[{DateTime.Now:HH:mm:ss}] System initialized",
                $"[{DateTime.Now:HH:mm:ss}] Connecting to license server...",
                $"[{DateTime.Now:HH:mm:ss}] Loading user session...",
                $"[{DateTime.Now:HH:mm:ss}] Engine ready"
            });
            tp.Controls.Add(activityList);

            return tp;
        }

        private Panel CreateStatCard(string title, string value, string icon, Color accent)
        {
            var card = new Panel
            {
                Size = new Size(180, 100),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };

            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 24),
                Location = new Point(10, 10),
                AutoSize = true
            };
            card.Controls.Add(iconLabel);

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Location = new Point(10, 45),
                AutoSize = true
            };
            card.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accent,
                Location = new Point(10, 60),
                AutoSize = true
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private Button CreateActionButton(string text, Action onClick)
        {
            return new Button
            {
                Text = text,
                Size = new Size(140, 40),
                BackColor = Color.FromArgb(31, 111, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5),
                Cursor = Cursors.Hand
            };
        }

        private TabPage BuildAccountTab()
        {
            var tp = new TabPage("👤 ACCOUNT") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "👤 Account Management",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 400, 36)
            };
            tp.Controls.Add(title);

            _userLabel = new Label
            {
                Text = "Not signed in.",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 70, 820, 24)
            };
            tp.Controls.Add(_userLabel);

            _licenseLabel = new Label
            {
                Text = "License: —",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 102, 820, 24)
            };
            tp.Controls.Add(_licenseLabel);

            _loginBtn = new Button
            {
                Text = "🔐 SIGN IN WITH DISCORD →",
                Bounds = new Rectangle(24, 150, 320, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            tp.Controls.Add(_loginBtn);

            _tokenInput = new TextBox
            {
                PlaceholderText = "Or paste Discord token here...",
                Bounds = new Rectangle(24, 200, 320, 30),
                BackColor = Color.FromArgb(30, 40, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            tp.Controls.Add(_tokenInput);

            _logoutBtn = new Button
            {
                Text = "🚪 LOGOUT",
                Bounds = new Rectangle(360, 150, 150, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Visible = false
            };
            tp.Controls.Add(_logoutBtn);

            _loginBtn.Click += async (_, _) => await SignInAsync();
            _logoutBtn.Click += async (_, _) => await SignOutAsync();

            var profilePanel = new Panel
            {
                Bounds = new Rectangle(24, 220, 500, 300),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var profileTitle = new Label
            {
                Text = "👤 Profile Information",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 15, 300, 20)
            };
            profilePanel.Controls.Add(profileTitle);

            var plan = new Label
            {
                Text = "Current Plan: STARTER",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 50, 470, 20)
            };
            profilePanel.Controls.Add(plan);

            var memberSince = new Label
            {
                Text = $"Member Since: {DateTime.Now:MMMM yyyy}",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 75, 470, 20)
            };
            profilePanel.Controls.Add(memberSince);

            var discord = new Label
            {
                Text = "Discord: Not connected",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 100, 470, 20)
            };
            profilePanel.Controls.Add(discord);

            var hwid = new Label
            {
                Text = $"HWID: {HardwareId.GetId()}",
                Font = new Font("Consolas", 8),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(15, 260, 470, 20)
            };
            profilePanel.Controls.Add(hwid);

            tp.Controls.Add(profilePanel);

            return tp;
        }

        private TabPage BuildLicenseTab()
        {
            var tp = new TabPage("🎫 LICENSE") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "🎫 License & Keys",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 400, 36)
            };
            tp.Controls.Add(title);

            var licensePanel = new Panel
            {
                Bounds = new Rectangle(24, 60, 700, 120),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var licTitle = new Label
            {
                Text = "📋 License Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 15, 200, 20)
            };
            licensePanel.Controls.Add(licTitle);

            var licStatus = new Label
            {
                Name = "_licStatusLabel",
                Text = "Status: Not checked",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(15, 45, 300, 20)
            };
            licensePanel.Controls.Add(licStatus);

            var licType = new Label
            {
                Name = "_licTypeLabel",
                Text = "Type: —",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(15, 70, 300, 20)
            };
            licensePanel.Controls.Add(licType);

            var licExpiry = new Label
            {
                Name = "_licExpiryLabel",
                Text = "Expires: —",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(350, 45, 300, 20)
            };
            licensePanel.Controls.Add(licExpiry);

            var licDays = new Label
            {
                Name = "_licDaysLabel",
                Text = "Days Remaining: —",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                Bounds = new Rectangle(350, 70, 300, 20)
            };
            licensePanel.Controls.Add(licDays);

            var licHWID = new Label
            {
                Name = "_licHWIDLabel",
                Text = $"HWID: {HardwareId.GetId()}",
                Font = new Font("Consolas", 8),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(15, 95, 670, 20)
            };
            licensePanel.Controls.Add(licHWID);

            tp.Controls.Add(licensePanel);

            var checkLicBtn = new Button
            {
                Text = "🔄 CHECK LICENSE",
                Bounds = new Rectangle(24, 195, 150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            tp.Controls.Add(checkLicBtn);

            var redeemLabel = new Label
            {
                Text = "Redeem License Key",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 245, 200, 20)
            };
            tp.Controls.Add(redeemLabel);

            _redeemKeyBox = new TextBox
            {
                Bounds = new Rectangle(24, 270, 400, 35),
                Font = new Font("Consolas", 11),
                BackColor = Color.FromArgb(17, 22, 28),
                ForeColor = Color.White,
                PlaceholderText = "Enter license key (e.g., SS-XXXXXXXXXXXX)"
            };
            tp.Controls.Add(_redeemKeyBox);

            _redeemBtn = new Button
            {
                Text = "🎁 REDEEM KEY",
                Bounds = new Rectangle(440, 270, 150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            tp.Controls.Add(_redeemBtn);

            _redeemResult = new Label
            {
                Text = "",
                Font = new Font("Consolas", 10),
                Bounds = new Rectangle(24, 315, 600, 30)
            };
            tp.Controls.Add(_redeemResult);

            _redeemBtn.Click += async (_, _) =>
            {
                await RedeemAsync();
                checkLicBtn.PerformClick();
            };

            checkLicBtn.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_token))
                {
                    licStatus.Text = "Status: Not signed in";
                    licStatus.ForeColor = Theme.Error;
                    return;
                }
                var lic = await DiscordAuth.ValidateLicenseAsync(_token);
                licStatus.Text = lic.HasAccess ? "Status: Active" : "Status: Inactive";
                licStatus.ForeColor = lic.HasAccess ? Theme.Success : Theme.Error;
                licType.Text = $"Type: {lic.DurationLabel ?? "None"}";
                if (lic.ExpiresAt.HasValue)
                {
                    var expDate = DateTimeOffset.FromUnixTimeSeconds(lic.ExpiresAt.Value).DateTime;
                    licExpiry.Text = $"Expires: {expDate:MMM dd, yyyy}";
                }
                else
                {
                    licExpiry.Text = "Expires: Never";
                }
                licDays.Text = lic.DaysRemaining.HasValue ? $"Days Remaining: {lic.DaysRemaining}" : "Days Remaining: Unlimited";
            };

            var keysLabel = new Label
            {
                Text = "Available Keys",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 360, 200, 20)
            };
            tp.Controls.Add(keysLabel);

            _refreshKeysBtn = new Button
            {
                Text = "🔄 SYNC KEYS",
                Bounds = new Rectangle(600, 355, 120, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            tp.Controls.Add(_refreshKeysBtn);

            _keysCountLabel = new Label
            {
                Text = "Loading...",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 390, 300, 20)
            };
            tp.Controls.Add(_keysCountLabel);

            var keysList = new ListBox
            {
                Bounds = new Rectangle(24, 420, 700, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            tp.Controls.Add(keysList);

            _refreshKeysBtn.Click += async (_, _) =>
            {
                _keysCountLabel.Text = $"Keys: {KeyCache.GetAll().Count(x => !x.Used)}";
                keysList.Items.Clear();
                var keys = KeyCache.GetAll().Take(50);
                foreach (var k in keys)
                {
                    keysList.Items.Add($"{k.Key} | {k.DurationType} | {(k.Used ? "Used" : "Available")}");
                }
            };

            return tp;
        }

        private TabPage BuildGameTab(string tabTitle, string gameName)
        {
            var tp = new TabPage(tabTitle) { BackColor = Theme.Background, Padding = new Padding(20) };

            var manager = new PythonProcessManager(gameName);
            bool isRunning = false;
            bool hasLicense = _license?.HasAccess ?? false;

            if (!hasLicense)
            {
                var lockIcon = new Label
                {
                    Text = "🔒",
                    Font = new Font("Segoe UI", 48),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(300, 80),
                    AutoSize = true
                };
                tp.Controls.Add(lockIcon);

                var lockTitle = new Label
                {
                    Text = "LICENSE REQUIRED",
                    Font = new Font("Segoe UI", 24, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(220, 150),
                    AutoSize = true
                };
                tp.Controls.Add(lockTitle);

                var lockDesc = new Label
                {
                    Text = $"Redeem a license key to unlock the {gameName} module",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(180, 200),
                    AutoSize = true
                };
                tp.Controls.Add(lockDesc);

                var redeemBtn = new Button
                {
                    Text = "🎫 REDEEM KEY",
                    Bounds = new Rectangle(260, 260, 180, 45),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.AccentBlue,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                redeemBtn.Click += (_, _) => _tabs.SelectedIndex = 1;
                tp.Controls.Add(redeemBtn);

                return tp;
            }

            var title = new Label
            {
                Text = $"{gameName} Module",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var statusPanel = new Panel
            {
                Bounds = new Rectangle(24, 60, 700, 100),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var statusTitle = new Label
            {
                Text = "📡 Connection Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            statusPanel.Controls.Add(statusTitle);

            var connStatus = new Label
            {
                Name = "_connStatus",
                Text = "Status: Idle",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 45),
                AutoSize = true
            };
            statusPanel.Controls.Add(connStatus);

            var hwidStatus = new Label
            {
                Name = "_hwidStatus",
                Text = $"HWID: {HardwareId.GetId()}",
                Font = new Font("Consolas", 8),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 70),
                AutoSize = true
            };
            statusPanel.Controls.Add(hwidStatus);

            tp.Controls.Add(statusPanel);

            var injectPanel = new Panel
            {
                Bounds = new Rectangle(24, 180, 700, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var injectTitle = new Label
            {
                Text = "🎯 Injection",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            injectPanel.Controls.Add(injectTitle);

            var injectBtn = new Button
            {
                Text = "🚀 INJECT",
                Bounds = new Rectangle(15, 50, 150, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            injectPanel.Controls.Add(injectBtn);

            var injectResult = new Label
            {
                Text = "Ready to inject...",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 105),
                AutoSize = true
            };
            injectPanel.Controls.Add(injectResult);

            manager.OnStarted += () =>
            {
                isRunning = true;
                connStatus.Text = "Status: Running";
                connStatus.ForeColor = Theme.Success;
                injectBtn.Text = "⏹ STOP";
                injectBtn.BackColor = Color.FromArgb(220, 53, 69);
            };

            manager.OnStopped += () =>
            {
                isRunning = false;
                connStatus.Text = "Status: Stopped";
                connStatus.ForeColor = Theme.TextSecondary;
                injectBtn.Text = "🚀 INJECT";
                injectBtn.BackColor = Theme.AccentBlue;
            };

            manager.OnOutput += (msg) =>
            {
                injectResult.Text = msg;
            };

            injectBtn.Click += async (_, _) =>
            {
                if (isRunning)
                {
                    manager.Stop();
                }
                else
                {
                    injectResult.Text = "Starting...";
                    injectResult.ForeColor = Color.Orange;
                    await manager.StartAsync();
                }
            };

            var settingsBtn = new Button
            {
                Text = "⚙️ SETTINGS",
                Bounds = new Rectangle(180, 50, 150, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            injectPanel.Controls.Add(settingsBtn);

            var settingsPanel = new Panel
            {
                Bounds = new Rectangle(24, 400, 700, 280),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var settingsTitle = new Label
            {
                Text = "🎛️ Recoil Settings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            settingsPanel.Controls.Add(settingsTitle);

            var recoilYLabel = new Label
            {
                Text = "Recoil Y: 35",
                Font = new Font("Consolas", 9),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                AutoSize = true
            };
            settingsPanel.Controls.Add(recoilYLabel);

            var recoilYSlider = new TrackBar
            {
                Location = new Point(15, 70),
                Size = new Size(200, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 35,
                TickStyle = TickStyle.None
            };
            recoilYSlider.ValueChanged += (s, e) =>
            {
                recoilYLabel.Text = $"Recoil Y: {recoilYSlider.Value}";
                manager.SendCommandAsync($"set_recoil_y:{recoilYSlider.Value}");
            };
            settingsPanel.Controls.Add(recoilYSlider);

            var recoilXLabel = new Label
            {
                Text = "Recoil X: 8",
                Font = new Font("Consolas", 9),
                ForeColor = Color.White,
                Location = new Point(250, 50),
                AutoSize = true
            };
            settingsPanel.Controls.Add(recoilXLabel);

            var recoilXSlider = new TrackBar
            {
                Location = new Point(250, 70),
                Size = new Size(200, 30),
                Minimum = 0,
                Maximum = 50,
                Value = 8,
                TickStyle = TickStyle.None
            };
            recoilXSlider.ValueChanged += (s, e) =>
            {
                recoilXLabel.Text = $"Recoil X: {recoilXSlider.Value}";
                manager.SendCommandAsync($"set_recoil_x:{recoilXSlider.Value}");
            };
            settingsPanel.Controls.Add(recoilXSlider);

            var sensLabel = new Label
            {
                Text = "Sensitivity: 1.0",
                Font = new Font("Consolas", 9),
                ForeColor = Color.White,
                Location = new Point(15, 110),
                AutoSize = true
            };
            settingsPanel.Controls.Add(sensLabel);

            var sensSlider = new TrackBar
            {
                Location = new Point(15, 130),
                Size = new Size(200, 30),
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                TickStyle = TickStyle.None
            };
            sensSlider.ValueChanged += (s, e) =>
            {
                double val = sensSlider.Value / 10.0;
                sensLabel.Text = $"Sensitivity: {val:F1}";
                manager.SendCommandAsync($"set_sensitivity:{val}");
            };
            settingsPanel.Controls.Add(sensSlider);

            var smoothLabel = new Label
            {
                Text = "Smoothing: 0.5",
                Font = new Font("Consolas", 9),
                ForeColor = Color.White,
                Location = new Point(250, 110),
                AutoSize = true
            };
            settingsPanel.Controls.Add(smoothLabel);

            var smoothSlider = new TrackBar
            {
                Location = new Point(250, 130),
                Size = new Size(200, 30),
                Minimum = 1,
                Maximum = 20,
                Value = 5,
                TickStyle = TickStyle.None
            };
            smoothSlider.ValueChanged += (s, e) =>
            {
                double val = smoothSlider.Value / 10.0;
                smoothLabel.Text = $"Smoothing: {val:F1}";
                manager.SendCommandAsync($"set_smoothing:{val}");
            };
            settingsPanel.Controls.Add(smoothSlider);

            var saveSettingsBtn = new Button
            {
                Text = "💾 SAVE",
                Bounds = new Rectangle(15, 180, 100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            saveSettingsBtn.Click += (s, e) =>
            {
                injectResult.Text = "Settings saved!";
                injectResult.ForeColor = Theme.Success;
            };
            settingsPanel.Controls.Add(saveSettingsBtn);

            settingsBtn.Click += (s, e) =>
            {
                settingsPanel.Visible = !settingsPanel.Visible;
            };
            tp.Controls.Add(settingsPanel);

            tp.Controls.Add(injectPanel);

            tp.Disposed += (_, _) => manager.Dispose();

            return tp;
        }

        private TabPage BuildAutoUpdaterTab()
        {
            var tp = new TabPage("🔄 AUTO-UPDATER") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🔄 Auto-Updater",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var currentVer = new Label
            {
                Text = $"Current Version: 4.2.0",
                Font = new Font("Consolas", 11),
                ForeColor = Color.White,
                Location = new Point(20, 55),
                AutoSize = true
            };
            tp.Controls.Add(currentVer);

            var checkBtn = new Button
            {
                Text = "🔍 CHECK FOR UPDATES",
                Bounds = new Rectangle(20, 90, 200, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            tp.Controls.Add(checkBtn);

            var statusLabel = new Label
            {
                Text = "Click to check for updates...",
                Font = new Font("Consolas", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 145),
                AutoSize = true
            };
            tp.Controls.Add(statusLabel);

            var updatePanel = new Panel
            {
                Bounds = new Rectangle(24, 180, 700, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var updateTitle = new Label
            {
                Text = "🎮 Game Version Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            updatePanel.Controls.Add(updateTitle);

            var warzoneVer = new Label { Text = "Warzone: Checking...", Font = new Font("Consolas", 9), ForeColor = Theme.TextSecondary, Location = new Point(15, 45), AutoSize = true };
            var r6sVer = new Label { Text = "R6S: Checking...", Font = new Font("Consolas", 9), ForeColor = Theme.TextSecondary, Location = new Point(15, 70), AutoSize = true };
            var arcVer = new Label { Text = "Arc Raiders: Checking...", Font = new Font("Consolas", 9), ForeColor = Theme.TextSecondary, Location = new Point(15, 95), AutoSize = true };
            var fnVer = new Label { Text = "Fortnite: Checking...", Font = new Font("Consolas", 9), ForeColor = Theme.TextSecondary, Location = new Point(15, 120), AutoSize = true };

            updatePanel.Controls.Add(warzoneVer);
            updatePanel.Controls.Add(r6sVer);
            updatePanel.Controls.Add(arcVer);
            updatePanel.Controls.Add(fnVer);

            var refreshBtn = new Button
            {
                Text = "🔄 REFRESH",
                Bounds = new Rectangle(15, 155, 120, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            updatePanel.Controls.Add(refreshBtn);

            tp.Controls.Add(updatePanel);

            async Task CheckUpdatesAsync()
            {
                statusLabel.Text = "Checking for updates...";
                statusLabel.ForeColor = Color.Orange;
                checkBtn.Enabled = false;

                try
                {
                    var warzoneTask = VersionChecker.GetWarzoneVersionAsync();
                    var r6sTask = VersionChecker.GetR6SVersionAsync();
                    var arcTask = VersionChecker.GetArcRaidersVersionAsync();
                    var fnTask = VersionChecker.GetFortniteVersionAsync();

                    await Task.WhenAll(warzoneTask, r6sTask, arcTask, fnTask);

                    warzoneVer.Text = $"Warzone: v{warzoneTask.Result}";
                    warzoneVer.ForeColor = Theme.Success;
                    r6sVer.Text = $"R6S: v{r6sTask.Result}";
                    r6sVer.ForeColor = Theme.Success;
                    arcVer.Text = $"Arc Raiders: v{arcTask.Result}";
                    arcVer.ForeColor = Theme.Success;
                    fnVer.Text = $"Fortnite: v{fnTask.Result}";
                    fnVer.ForeColor = Theme.Success;

                    updatePanel.Visible = true;
                    statusLabel.Text = "Version check complete!";
                    statusLabel.ForeColor = Theme.Success;
                }
                catch
                {
                    statusLabel.Text = "Error checking versions";
                    statusLabel.ForeColor = Theme.Error;
                }
                finally
                {
                    checkBtn.Enabled = true;
                }
            }

            checkBtn.Click += (_, _) => _ = CheckUpdatesAsync();
            refreshBtn.Click += (_, _) => _ = CheckUpdatesAsync();

            return tp;
        }

        private TabPage BuildRealtimeTab()
        {
            var tp = new TabPage("⚡ ENGINE") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "⚡ Real-Time Engine Control Center",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var desc = new Label
            {
                Text = "Sub-millisecond event processing with distributed state sync across all nodes. No lag. No compromise.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 50),
                AutoSize = true
            };
            tp.Controls.Add(desc);

            var metricsPanel = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(350, 280),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            metricsPanel.Controls.Add(new Label
            {
                Text = "📊 Performance Metrics",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var eventRate = new Label { Text = "Event Rate: 0/sec", Location = new Point(10, 45), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var totalEvents = new Label { Text = $"Total Events: {AnalyticsService.Instance.GetMonthlyEventCount():N0}", Location = new Point(10, 70), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var avgLatency = new Label { Text = "Avg Latency: <1ms", Location = new Point(10, 95), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var p99Latency = new Label { Text = "P99 Latency: 2ms", Location = new Point(10, 120), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var throughput = new Label { Text = "Throughput: 10K/sec", Location = new Point(10, 145), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var queueDepth = new Label { Text = "Queue Depth: 0", Location = new Point(10, 170), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var uptime = new Label { Text = "Uptime: 99.99%", Location = new Point(10, 195), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };
            var nodes = new Label { Text = "Active Nodes: 1", Location = new Point(10, 220), AutoSize = true, ForeColor = Color.White, Font = new Font("Consolas", 10) };

            metricsPanel.Controls.AddRange(new Control[] { eventRate, totalEvents, avgLatency, p99Latency, throughput, queueDepth, uptime, nodes });
            tp.Controls.Add(metricsPanel);

            var nodePanel = new Panel
            {
                Location = new Point(390, 90),
                Size = new Size(350, 280),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            nodePanel.Controls.Add(new Label
            {
                Text = "🖥️ Node Management",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var nodeList = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 150),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            nodeList.Items.AddRange(new[] { "🟢 node-1 (local) | CPU: 12% | RAM: 2.1GB", "⚪ node-2 (remote) | Offline", "⚪ node-3 (remote) | Offline" });

            var addNodeBtn = new Button { Text = "+ Add Node", Location = new Point(10, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var removeNodeBtn = new Button { Text = "- Remove", Location = new Point(120, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var restartBtn = new Button { Text = "🔄 Restart", Location = new Point(230, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };

            nodePanel.Controls.Add(nodeList);
            nodePanel.Controls.Add(addNodeBtn);
            nodePanel.Controls.Add(removeNodeBtn);
            nodePanel.Controls.Add(restartBtn);
            tp.Controls.Add(nodePanel);

            var configPanel = new Panel
            {
                Location = new Point(20, 390),
                Size = new Size(720, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            configPanel.Controls.Add(new Label
            {
                Text = "⚙️ Engine Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var workerCount = new NumericUpDown { Location = new Point(10, 45), Maximum = 64, Minimum = 1, Value = 4, Size = new Size(100, 25) };
            var batchSize = new NumericUpDown { Location = new Point(10, 80), Maximum = 10000, Minimum = 1, Value = 100, Size = new Size(100, 25) };
            var timeout = new NumericUpDown { Location = new Point(10, 115), Maximum = 300000, Minimum = 1000, Value = 30000, Size = new Size(100, 25) };
            var enableCompression = new CheckBox { Text = "Enable Compression", Location = new Point(150, 45), ForeColor = Color.White, Checked = true };
            var enableEncryption = new CheckBox { Text = "Enable TLS 1.3", Location = new Point(150, 70), ForeColor = Color.White, Checked = true };
            var enableClustering = new CheckBox { Text = "Enable Clustering", Location = new Point(150, 95), ForeColor = Color.White, Checked = false };

            var saveConfigBtn = new Button { Text = "💾 Save Configuration", Location = new Point(10, 150), Size = new Size(180, 35), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            configPanel.Controls.AddRange(new Control[] { 
                new Label { Text = "Worker Threads:", Location = new Point(10, 45), ForeColor = Color.White, AutoSize = true },
                workerCount,
                new Label { Text = "Batch Size:", Location = new Point(10, 80), ForeColor = Color.White, AutoSize = true },
                batchSize,
                new Label { Text = "Timeout (ms):", Location = new Point(10, 115), ForeColor = Color.White, AutoSize = true },
                timeout,
                enableCompression, enableEncryption, enableClustering, saveConfigBtn 
            });
            tp.Controls.Add(configPanel);

            return tp;
        }

        private TabPage BuildSecurityTab()
        {
            var tp = new TabPage("🔐 SECURITY") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🔐 Zero-Trust Security Center",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var statusGrid = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 120),
                FlowDirection = FlowDirection.LeftToRight
            };

            var secCards = new[]
            {
                ("🔒 Encryption", "AES-256-GCM", "Active", Color.FromArgb(76, 175, 80)),
                ("🔑 Token Rotation", "Every 15min", "Active", Color.FromArgb(76, 175, 80)),
                ("🛡️ MFA", "Available", "Ready", Color.FromArgb(255, 193, 7)),
                ("👥 RBAC", "4 Roles", "Active", Color.FromArgb(76, 175, 80)),
                ("🔗 SSO", "Available", "Ready", Color.FromArgb(255, 193, 7)),
                ("📝 Audit Log", "30 Days", "Active", Color.FromArgb(76, 175, 80))
            };

            foreach (var (name, value, status, color) in secCards)
            {
                var card = new Panel { Size = new Size(170, 100), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                card.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                card.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true });
                card.Controls.Add(new Label { Text = status, Font = new Font("Segoe UI", 9), ForeColor = color, Location = new Point(10, 55), AutoSize = true });
                statusGrid.Controls.Add(card);
            }
            tp.Controls.Add(statusGrid);

            var authPanel = new Panel
            {
                Location = new Point(20, 210),
                Size = new Size(350, 300),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            authPanel.Controls.Add(new Label
            {
                Text = "🔐 Authentication Settings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var mfaToggle = new CheckBox { Text = "Enable Multi-Factor Authentication", Location = new Point(10, 50), ForeColor = Color.White, Checked = false };
            var ssoToggle = new CheckBox { Text = "Enable SSO (SAML/OAuth)", Location = new Point(10, 80), ForeColor = Color.White, Checked = false };
            var ipWhitelist = new TextBox { Location = new Point(10, 115), Size = new Size(320, 25), BackColor = Color.FromArgb(11, 15, 20), ForeColor = Color.White, PlaceholderText = "IP Whitelist (comma separated)" };
            var sessionTimeout = new NumericUpDown { Location = new Point(10, 155), Maximum = 480, Minimum = 5, Value = 60, Size = new Size(100, 25) };
            var saveAuthBtn = new Button { Text = "💾 Save Auth Settings", Location = new Point(10, 195), Size = new Size(180, 35), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            authPanel.Controls.AddRange(new Control[] { mfaToggle, ssoToggle, ipWhitelist, new Label { Text = "Session Timeout (min):", Location = new Point(10, 155), ForeColor = Color.White, AutoSize = true }, sessionTimeout, saveAuthBtn });
            tp.Controls.Add(authPanel);

            var rolesPanel = new Panel
            {
                Location = new Point(390, 210),
                Size = new Size(350, 300),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            rolesPanel.Controls.Add(new Label
            {
                Text = "👥 Role-Based Access Control",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var rolesList = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 150),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            rolesList.Items.AddRange(new[] { "👑 Owner - Full Access", "🔧 Admin - Manage Users", "👤 User - Standard Access", "👁️ Viewer - Read Only" });

            var addRoleBtn = new Button { Text = "+ Add Role", Location = new Point(10, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var editRoleBtn = new Button { Text = "✏️ Edit", Location = new Point(115, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var deleteRoleBtn = new Button { Text = "🗑️ Delete", Location = new Point(220, 200), Size = new Size(100, 30), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            rolesPanel.Controls.Add(rolesList);
            rolesPanel.Controls.Add(addRoleBtn);
            rolesPanel.Controls.Add(editRoleBtn);
            rolesPanel.Controls.Add(deleteRoleBtn);
            tp.Controls.Add(rolesPanel);

            var auditPanel = new Panel
            {
                Location = new Point(20, 530),
                Size = new Size(720, 150),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            auditPanel.Controls.Add(new Label
            {
                Text = "📝 Security Audit Log",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var auditLog = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(700, 100),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 8)
            };
            auditLog.Items.AddRange(new[] {
                $"[{DateTime.Now:HH:mm:ss}] User login successful",
                $"[{DateTime.Now:HH:mm:ss}] MFA verification bypassed",
                $"[{DateTime.Now:HH:mm:ss}] Session token rotated"
            });
            auditPanel.Controls.Add(auditLog);
            tp.Controls.Add(auditPanel);

            return tp;
        }

        private TabPage BuildDataPipelinesTab()
        {
            var tp = new TabPage("📡 DATA") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "📡 Live Data Pipelines",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var desc = new Label
            {
                Text = "Push millions of events per second through customizable pipelines.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 50),
                AutoSize = true
            };
            tp.Controls.Add(desc);

            var statsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 90),
                Size = new Size(720, 100),
                FlowDirection = FlowDirection.LeftToRight
            };

            var pipeStats = new[]
            {
                ("📥 Throughput", "0 events/sec"),
                ("📤 Outgoing", "0 events/sec"),
                ("🔄 Active Pipelines", "0"),
                ("⏳ Queue Size", "0"),
                ("❌ Failed", "0"),
                ("✅ Success Rate", "100%")
            };

            foreach (var (label, value) in pipeStats)
            {
                var p = new Panel { Size = new Size(150, 80), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true });
                statsPanel.Controls.Add(p);
            }
            tp.Controls.Add(statsPanel);

            var pipelinePanel = new Panel
            {
                Location = new Point(20, 210),
                Size = new Size(720, 350),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            pipelinePanel.Controls.Add(new Label
            {
                Text = "🔧 Pipeline Builder",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var pipelineList = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(700, 200),
                BackgroundColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            pipelineList.Columns.Add("Name", "Pipeline Name");
            pipelineList.Columns.Add("Status", "Status");
            pipelineList.Columns.Add("Events", "Events/hr");
            pipelineList.Columns.Add("LastRun", "Last Run");
            pipelineList.Rows.Add("Default Pipeline", "Active", "0", "Never");
            pipelinePanel.Controls.Add(pipelineList);

            var newPipelineBtn = new Button { Text = "+ New Pipeline", Location = new Point(10, 255), Size = new Size(130, 35), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var editPipelineBtn = new Button { Text = "✏️ Edit", Location = new Point(150, 255), Size = new Size(100, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var deletePipelineBtn = new Button { Text = "🗑️ Delete", Location = new Point(260, 255), Size = new Size(100, 35), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var testPipelineBtn = new Button { Text = "🧪 Test Pipeline", Location = new Point(370, 255), Size = new Size(130, 35), BackColor = Color.FromArgb(33, 150, 243), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            pipelinePanel.Controls.AddRange(new Control[] { newPipelineBtn, editPipelineBtn, deletePipelineBtn, testPipelineBtn });
            tp.Controls.Add(pipelinePanel);

            return tp;
        }

        private TabPage BuildAIMLTab()
        {
            var tp = new TabPage("🧠 AI/ML") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🧠 AI Inference Layer",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var modelsGrid = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 180),
                FlowDirection = FlowDirection.LeftToRight
            };

            var models = new[]
            {
                ("GPT-4", "OpenAI", "8192", "Available", Color.FromArgb(76, 175, 80)),
                ("GPT-3.5 Turbo", "OpenAI", "4096", "Available", Color.FromArgb(76, 175, 80)),
                ("Claude 3", "Anthropic", "100K", "Available", Color.FromArgb(76, 175, 80)),
                ("DALL-E 3", "OpenAI", "4K", "Available", Color.FromArgb(76, 175, 80)),
                ("SDXL", "Stability AI", "2K", "Available", Color.FromArgb(76, 175, 80)),
                ("Codex", "OpenAI", "4K", "Available", Color.FromArgb(76, 175, 80))
            };

            foreach (var (name, provider, tokens, status, color) in models)
            {
                var card = new Panel { Size = new Size(170, 150), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Padding = new Padding(10) };
                card.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(5, 5), AutoSize = true });
                card.Controls.Add(new Label { Text = $"Provider: {provider}", Font = new Font("Consolas", 8), ForeColor = Theme.TextSecondary, Location = new Point(5, 30), AutoSize = true });
                card.Controls.Add(new Label { Text = $"Max Tokens: {tokens}", Font = new Font("Consolas", 8), ForeColor = Theme.TextSecondary, Location = new Point(5, 50), AutoSize = true });
                card.Controls.Add(new Label { Text = status, Font = new Font("Segoe UI", 9), ForeColor = color, Location = new Point(5, 80), AutoSize = true });
                var useBtn = new Button { Text = "Use Model", Location = new Point(5, 110), Size = new Size(150, 25), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                card.Controls.Add(useBtn);
                modelsGrid.Controls.Add(card);
            }
            tp.Controls.Add(modelsGrid);

            var chatPanel = new Panel
            {
                Location = new Point(20, 270),
                Size = new Size(720, 350),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            chatPanel.Controls.Add(new Label
            {
                Text = "💬 AI Chat Interface",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var chatHistory = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(700, 200),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            chatHistory.Items.Add("🧠 AI: Hello! Select a model above to start. I can help with code, images, and more.");
            chatPanel.Controls.Add(chatHistory);

            var messageBox = new TextBox
            {
                Location = new Point(10, 255),
                Size = new Size(550, 30),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                PlaceholderText = "Ask AI anything..."
            };
            chatPanel.Controls.Add(messageBox);

            var sendBtn = new Button { Text = "Send ➤", Location = new Point(570, 255), Size = new Size(140, 30), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            chatPanel.Controls.Add(sendBtn);

            var clearBtn = new Button { Text = "Clear", Location = new Point(10, 295), Size = new Size(100, 30), BackColor = Color.FromArgb(45, 55, 72), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            chatPanel.Controls.Add(clearBtn);

            tp.Controls.Add(chatPanel);

            return tp;
        }

        private TabPage BuildAutomationTab()
        {
            var tp = new TabPage("🗂️ AUTOMATION") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🗂️ Modular Workflow Builder",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var statsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 80),
                FlowDirection = FlowDirection.LeftToRight
            };

            var wfStats = new[]
            {
                ("Active Workflows", "0"),
                ("Total Runs Today", "0"),
                ("Success Rate", "100%"),
                ("Avg Duration", "0ms")
            };

            foreach (var (label, value) in wfStats)
            {
                var p = new Panel { Size = new Size(150, 60), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true });
                statsPanel.Controls.Add(p);
            }
            tp.Controls.Add(statsPanel);

            var workflowPanel = new Panel
            {
                Location = new Point(20, 170),
                Size = new Size(720, 450),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            workflowPanel.Controls.Add(new Label
            {
                Text = "🔧 Workflow Management",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var workflowList = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(700, 280),
                BackgroundColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            workflowList.Columns.Add("Name", "Workflow Name");
            workflowList.Columns.Add("Trigger", "Trigger");
            workflowList.Columns.Add("Status", "Status");
            workflowList.Columns.Add("Runs", "Runs Today");
            workflowList.Columns.Add("LastRun", "Last Run");
            workflowPanel.Controls.Add(workflowList);

            var btnPanel = new FlowLayoutPanel { Location = new Point(10, 330), Size = new Size(700, 50), FlowDirection = FlowDirection.LeftToRight };

            var newWfBtn = new Button { Text = "+ New Workflow", Size = new Size(130, 35), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var editWfBtn = new Button { Text = "✏️ Edit", Size = new Size(100, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var deleteWfBtn = new Button { Text = "🗑️ Delete", Size = new Size(100, 35), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var runNowBtn = new Button { Text = "▶️ Run Now", Size = new Size(120, 35), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var historyBtn = new Button { Text = "📜 History", Size = new Size(100, 35), BackColor = Color.FromArgb(45, 55, 72), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnPanel.Controls.Add(newWfBtn);
            btnPanel.Controls.Add(editWfBtn);
            btnPanel.Controls.Add(deleteWfBtn);
            btnPanel.Controls.Add(runNowBtn);
            btnPanel.Controls.Add(historyBtn);
            workflowPanel.Controls.Add(btnPanel);

            tp.Controls.Add(workflowPanel);

            return tp;
        }

        private TabPage BuildMonitoringTab()
        {
            var tp = new TabPage("📊 MONITORING") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "📊 Observability Suite",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var metricsGrid = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 100),
                FlowDirection = FlowDirection.LeftToRight
            };

            var monStats = new[]
            {
                ("📈 Requests/sec", "0"),
                ("📉 Error Rate", "0%"),
                ("⏱️ Avg Response", "0ms"),
                ("💾 Memory", "0 MB"),
                ("💻 CPU", "0%"),
                ("🟢 Services", "1/1")
            };

            foreach (var (label, value) in monStats)
            {
                var p = new Panel { Size = new Size(150, 80), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true });
                metricsGrid.Controls.Add(p);
            }
            tp.Controls.Add(metricsGrid);

            var dashboardPanel = new Panel
            {
                Location = new Point(20, 190),
                Size = new Size(350, 430),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            dashboardPanel.Controls.Add(new Label
            {
                Text = "📊 Custom Dashboards",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var dashList = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 250),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            dashList.Items.AddRange(new[] { "📊 Overview Dashboard", "📈 Performance Metrics", "🔒 Security Events", "🌐 Network Traffic", "🗄️ Database Stats" });
            dashboardPanel.Controls.Add(dashList);

            var dashBtnPanel = new FlowLayoutPanel { Location = new Point(10, 300), Size = new Size(320, 40), FlowDirection = FlowDirection.LeftToRight };
            dashBtnPanel.Controls.Add(new Button { Text = "+ New Dashboard", Size = new Size(130, 30), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            dashBtnPanel.Controls.Add(new Button { Text = "Edit", Size = new Size(80, 30), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat });
            dashBtnPanel.Controls.Add(new Button { Text = "Delete", Size = new Size(80, 30), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            dashboardPanel.Controls.Add(dashBtnPanel);

            var alertsPanel = new Panel
            {
                Location = new Point(390, 190),
                Size = new Size(350, 430),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            alertsPanel.Controls.Add(new Label
            {
                Text = "🚨 Alert Rules",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var alertList = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 250),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            alertList.Items.AddRange(new[] { "⚠️ High CPU Usage (>90%)", "⚠️ Memory Critical (>95%)", "⚠️ Error Rate Spike (>5%)", "⚠️ Service Down", "⚠️ Latency High (>500ms)" });
            alertsPanel.Controls.Add(alertList);

            var alertBtnPanel = new FlowLayoutPanel { Location = new Point(10, 300), Size = new Size(320, 40), FlowDirection = FlowDirection.LeftToRight };
            alertBtnPanel.Controls.Add(new Button { Text = "+ New Alert", Size = new Size(100, 30), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            alertBtnPanel.Controls.Add(new Button { Text = "Edit", Size = new Size(80, 30), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat });
            alertBtnPanel.Controls.Add(new Button { Text = "Delete", Size = new Size(80, 30), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            alertBtnPanel.Controls.Add(new Button { Text = "Test", Size = new Size(60, 30), BackColor = Color.FromArgb(33, 150, 243), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            alertsPanel.Controls.Add(alertBtnPanel);

            tp.Controls.Add(dashboardPanel);
            tp.Controls.Add(alertsPanel);

            return tp;
        }

        private TabPage BuildCDNTab()
        {
            var tp = new TabPage("🌐 CDN") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🌐 Global CDN Mesh",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var cdnStats = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 80),
                FlowDirection = FlowDirection.LeftToRight
            };

            var stats = new[]
            {
                ("🌍 Edge Nodes", "42"),
                ("📶 Requests Today", "0"),
                ("💾 Cache Hit Rate", "0%"),
                ("⚡ Avg Response", "0ms")
            };

            foreach (var (label, value) in stats)
            {
                var p = new Panel { Size = new Size(150, 60), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true });
                cdnStats.Controls.Add(p);
            }
            tp.Controls.Add(cdnStats);

            var mapPanel = new Panel
            {
                Location = new Point(20, 170),
                Size = new Size(720, 200),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            mapPanel.Controls.Add(new Label
            {
                Text = "🗺️ Global Edge Network",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var regionList = new FlowLayoutPanel { Location = new Point(10, 40), Size = new Size(700, 150), FlowDirection = FlowDirection.LeftToRight };
            var regions = new[] { ("🇺🇸 US East", "Online", Color.FromArgb(76, 175, 80)), ("🇺🇸 US West", "Online", Color.FromArgb(76, 175, 80)), ("🇪🇺 EU West", "Online", Color.FromArgb(76, 175, 80)), ("🇪🇺 EU Central", "Online", Color.FromArgb(76, 175, 80)), ("🇩🇪 Frankfurt", "Online", Color.FromArgb(76, 175, 80)), ("🇬🇧 London", "Online", Color.FromArgb(76, 175, 80)), ("🇯🇵 Tokyo", "Online", Color.FromArgb(76, 175, 80)), ("🇸🇬 Singapore", "Online", Color.FromArgb(76, 175, 80)), ("🇦🇺 Sydney", "Online", Color.FromArgb(76, 175, 80)), ("🇧🇷 São Paulo", "Online", Color.FromArgb(76, 175, 80)), ("🇮🇳 Mumbai", "Online", Color.FromArgb(76, 175, 80)), ("🇨🇦 Toronto", "Online", Color.FromArgb(76, 175, 80)) };
            foreach (var (name, status, color) in regions)
            {
                var r = new Panel { Size = new Size(100, 40), BackColor = Color.FromArgb(11, 15, 20), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(3), Padding = new Padding(5) };
                r.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 8), ForeColor = Color.White, Location = new Point(5, 5), AutoSize = true });
                r.Controls.Add(new Label { Text = status, Font = new Font("Segoe UI", 7), ForeColor = color, Location = new Point(5, 22), AutoSize = true });
                regionList.Controls.Add(r);
            }
            mapPanel.Controls.Add(regionList);
            tp.Controls.Add(mapPanel);

            var configPanel = new Panel
            {
                Location = new Point(20, 390),
                Size = new Size(720, 250),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            configPanel.Controls.Add(new Label
            {
                Text = "⚙️ CDN Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var cachingToggle = new CheckBox { Text = "Enable Smart Caching", Location = new Point(10, 45), ForeColor = Color.White, Checked = true };
            var compressionToggle = new CheckBox { Text = "Enable Brotli Compression", Location = new Point(10, 70), ForeColor = Color.White, Checked = true };
            var geoToggle = new CheckBox { Text = "Enable Geo-Routing", Location = new Point(10, 95), ForeColor = Color.White, Checked = true };
            var failoverToggle = new CheckBox { Text = "Enable Auto-Failover", Location = new Point(10, 120), ForeColor = Color.White, Checked = true };
            var ttlBox = new NumericUpDown { Location = new Point(10, 155), Maximum = 86400, Minimum = 60, Value = 3600, Size = new Size(100, 25) };
            var purgeBtn = new Button { Text = "🗑️ Purge All Cache", Location = new Point(10, 195), Size = new Size(150, 35), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var saveCDNBtn = new Button { Text = "💾 Save Settings", Location = new Point(170, 195), Size = new Size(150, 35), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            configPanel.Controls.AddRange(new Control[] { cachingToggle, compressionToggle, geoToggle, failoverToggle, new Label { Text = "Cache TTL (seconds):", Location = new Point(10, 155), ForeColor = Color.White, AutoSize = true }, ttlBox, purgeBtn, saveCDNBtn });
            tp.Controls.Add(configPanel);

            return tp;
        }

        private TabPage BuildAPITab()
        {
            var tp = new TabPage("🔌 API") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🔌 Unified API Gateway",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var apiStats = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 80),
                FlowDirection = FlowDirection.LeftToRight
            };

            var apis = new[]
            {
                ("🌐 REST", "Active", Color.FromArgb(76, 175, 80)),
                ("📊 GraphQL", "Available", Color.FromArgb(255, 193, 7)),
                ("🔮 gRPC", "Available", Color.FromArgb(255, 193, 7)),
                ("⚡ WebSocket", "Available", Color.FromArgb(255, 193, 7))
            };

            foreach (var (name, status, color) in apis)
            {
                var p = new Panel { Size = new Size(150, 60), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = status, Font = new Font("Segoe UI", 9), ForeColor = color, Location = new Point(10, 35), AutoSize = true });
                apiStats.Controls.Add(p);
            }
            tp.Controls.Add(apiStats);

            var endpointsPanel = new Panel
            {
                Location = new Point(20, 170),
                Size = new Size(350, 470),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            endpointsPanel.Controls.Add(new Label
            {
                Text = "📡 API Endpoints",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var endpointList = new ListBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 350),
                BackColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 8)
            };
            endpointList.Items.AddRange(new[] {
                "GET  /api/v1/keys",
                "POST /api/v1/keys/redeem",
                "GET  /api/v1/users",
                "POST /api/v1/auth/login",
                "GET  /api/v1/stats",
                "POST /api/v1/ai/inference",
                "WS   /api/v1/realtime"
            });
            endpointsPanel.Controls.Add(endpointList);

            var ratePanel = new Panel
            {
                Location = new Point(390, 170),
                Size = new Size(350, 470),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            ratePanel.Controls.Add(new Label
            {
                Text = "⚡ Rate Limiting",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var enableRate = new CheckBox { Text = "Enable Rate Limiting", Location = new Point(10, 45), ForeColor = Color.White, Checked = true };
            var limitBox = new NumericUpDown { Location = new Point(10, 80), Maximum = 100000, Minimum = 10, Value = 1000, Size = new Size(100, 25) };
            var windowBox = new NumericUpDown { Location = new Point(10, 115), Maximum = 3600, Minimum = 1, Value = 60, Size = new Size(100, 25) };
            var apiKeyBox = new TextBox { Location = new Point(10, 155), Size = new Size(320, 25), BackColor = Color.FromArgb(11, 15, 20), ForeColor = Color.White, PlaceholderText = "API Key: sk-..." };
            var regenerateBtn = new Button { Text = "🔄 Regenerate Key", Location = new Point(10, 195), Size = new Size(150, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var copyBtn = new Button { Text = "📋 Copy", Location = new Point(170, 195), Size = new Size(80, 35), BackColor = Color.FromArgb(45, 55, 72), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var testApiBtn = new Button { Text = "🧪 Test API", Location = new Point(10, 300), Size = new Size(320, 35), BackColor = Theme.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            ratePanel.Controls.AddRange(new Control[] { enableRate, new Label { Text = "Requests per window:", Location = new Point(10, 80), ForeColor = Color.White, AutoSize = true }, limitBox, new Label { Text = "Window (seconds):", Location = new Point(10, 115), ForeColor = Color.White, AutoSize = true }, windowBox, apiKeyBox, regenerateBtn, copyBtn, testApiBtn });
            tp.Controls.Add(endpointsPanel);
            tp.Controls.Add(ratePanel);

            return tp;
        }

        private TabPage BuildDevOpsTab()
        {
            var tp = new TabPage("🚀 DEVOPS") { BackColor = Theme.Background, Padding = new Padding(20) };

            var title = new Label
            {
                Text = "🚀 Instant Deploy & CI/CD",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var deployStats = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(720, 80),
                FlowDirection = FlowDirection.LeftToRight
            };

            var dStats = new[]
            {
                ("🚀 Total Deploys", "0"),
                ("✅ Success", "0%"),
                ("⏱️ Avg Duration", "0s"),
                ("🔄 Active Jobs", "0")
            };

            foreach (var (label, value) in dStats)
            {
                var p = new Panel { Size = new Size(150, 60), BackColor = Color.FromArgb(17, 22, 28), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5) };
                p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9), ForeColor = Theme.TextSecondary, Location = new Point(10, 10), AutoSize = true });
                p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true });
                deployStats.Controls.Add(p);
            }
            tp.Controls.Add(deployStats);

            var deployPanel = new Panel
            {
                Location = new Point(20, 170),
                Size = new Size(720, 470),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };
            deployPanel.Controls.Add(new Label
            {
                Text = "📦 Deployment Management",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var deployList = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(700, 280),
                BackgroundColor = Color.FromArgb(11, 15, 20),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            deployList.Columns.Add("ID", "ID");
            deployList.Columns.Add("Status", "Status");
            deployList.Columns.Add("Branch", "Branch");
            deployList.Columns.Add("Duration", "Duration");
            deployList.Columns.Add("Time", "Time");
            deployPanel.Controls.Add(deployList);

            var deployBtns = new FlowLayoutPanel { Location = new Point(10, 335), Size = new Size(700, 40), FlowDirection = FlowDirection.LeftToRight };
            deployBtns.Controls.Add(new Button { Text = "🚀 New Deploy", Size = new Size(120, 35), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            deployBtns.Controls.Add(new Button { Text = "📜 View Logs", Size = new Size(120, 35), BackColor = Color.FromArgb(33, 150, 243), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            deployBtns.Controls.Add(new Button { Text = "↩️ Rollback", Size = new Size(100, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat });
            deployBtns.Controls.Add(new Button { Text = "🗑️ Cancel", Size = new Size(100, 35), BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            deployBtns.Controls.Add(new Button { Text = "⚙️ Settings", Size = new Size(100, 35), BackColor = Color.FromArgb(45, 55, 72), ForeColor = Color.White, FlatStyle = FlatStyle.Flat });
            deployPanel.Controls.Add(deployBtns);

            var strategyPanel = new FlowLayoutPanel { Location = new Point(10, 380), Size = new Size(700, 60), FlowDirection = FlowDirection.LeftToRight };
            strategyPanel.Controls.Add(new Label { Text = "Strategy:", ForeColor = Color.White, AutoSize = true, Margin = new Padding(5) });
            strategyPanel.Controls.Add(new ComboBox { Size = new Size(150, 30), Items = { "Rolling", "Blue-Green", "Canary", "Custom" }, SelectedIndex = 0 });
            strategyPanel.Controls.Add(new CheckBox { Text = "Auto Rollback on Failure", ForeColor = Color.White, Checked = true, Margin = new Padding(10, 0, 0, 0) });
            strategyPanel.Controls.Add(new CheckBox { Text = "Smoke Tests", ForeColor = Color.White, Checked = true, Margin = new Padding(10, 0, 0, 0) });
            deployPanel.Controls.Add(strategyPanel);

            tp.Controls.Add(deployPanel);

            return tp;
        }

        private TabPage BuildToolsTab()
        {
            var tp = new TabPage("🔧 TOOLS") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "🔧 Tools & Utilities",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 400, 36)
            };
            tp.Controls.Add(title);

            var clearCacheBtn = new Button
            {
                Text = "🗑️ Clear Cache",
                Bounds = new Rectangle(24, 80, 200, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(clearCacheBtn);

            var exportBtn = new Button
            {
                Text = "📤 Export Data",
                Bounds = new Rectangle(240, 80, 200, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(exportBtn);

            var importBtn = new Button
            {
                Text = "📥 Import Data",
                Bounds = new Rectangle(456, 80, 200, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(importBtn);

            var aboutLabel = new Label
            {
                Text = "SmokeScreen ENGINE v4.2.0\n© 2024 SmokeScreen Technologies",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 150, 400, 50)
            };
            tp.Controls.Add(aboutLabel);

            return tp;
        }

        private void SetupPingTimer()
        {
            _pingTimer.Interval = 10000;
            _pingTimer.Tick += async (_, _) => await CheckPingAsync();
            _pingTimer.Start();
        }

        private async Task CheckPingAsync()
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var res = await http.GetAsync("https://smokescreen-engine.vercel.app/ping");
                if (res.IsSuccessStatusCode)
                {
                    var txt = await res.Content.ReadAsStringAsync();
                    _pingLabel.Invoke((MethodInvoker)delegate
                    {
                        _pingLabel.Text = "✓ online";
                        _pingLabel.ForeColor = Theme.Success;
                    });
                }
                else
                {
                    _pingLabel.Invoke((MethodInvoker)delegate
                    {
                        _pingLabel.Text = "✗ offline";
                        _pingLabel.ForeColor = Theme.Error;
                    });
                }
            }
            catch
            {
                _pingLabel.Invoke((MethodInvoker)delegate
                {
                    _pingLabel.Text = "✗ offline";
                    _pingLabel.ForeColor = Theme.Error;
                });
            }
        }

        private async Task LoadSessionAsync()
        {
            await Task.CompletedTask;
            _keysCountLabel.Text = $"Keys: {KeyCache.GetAll().Count(x => !x.Used)}";
        }

        private async Task SignInAsync()
        {
            try
            {
                string? token = _tokenInput.Text.Trim();
                
                if (!string.IsNullOrEmpty(token) && token.Length > 50)
                {
                    _redeemResult.Text = "Validating token...";
                    _redeemResult.ForeColor = Theme.TextSecondary;
                    
                    var user = await DiscordAuth.GetUserFromTokenAsync(token);
                    if (user != null)
                    {
                        _token = token;
                        _user = user;
                        _redeemResult.Text = $"Signed in as {user.Username}!";
                        _redeemResult.ForeColor = Theme.Success;
                        _loginBtn.Visible = false;
                        _logoutBtn.Visible = true;
                        _userLabel.Text = $"Signed in: {user.Username}";
                        _tokenInput.Clear();

                        var license = await DiscordAuth.ValidateLicenseAsync(_token!);
                        _license = license;
                        _licenseLabel.Text = license.HasAccess ? $"License: {license.DurationLabel}" : "License: Not found";
                        return;
                    }
                    else
                    {
                        _redeemResult.Text = "Invalid token";
                        _redeemResult.ForeColor = Theme.Error;
                        return;
                    }
                }

                _redeemResult.Text = "Opening Discord OAuth...";
                _redeemResult.ForeColor = Theme.TextSecondary;
                
                var progress = new Progress<string>(msg => _redeemResult.Text = msg);
                var result = await DiscordAuth.LoginWithDiscordAsync(progress);
                
                if (result.IsSuccess)
                {
                    _token = result.Token;
                    _user = result.User;
                    _redeemResult.Text = $"Signed in as {result.User?.Username}!";
                    _redeemResult.ForeColor = Theme.Success;
                    _loginBtn.Visible = false;
                    _logoutBtn.Visible = true;
                    _userLabel.Text = $"Signed in: {result.User?.Username}";
                    
                    // Check license
                    var license = await DiscordAuth.ValidateLicenseAsync(_token!);
                    _license = license;
                    _licenseLabel.Text = license.HasAccess ? $"License: {license.DurationLabel}" : "License: Not found";
                }
                else
                {
                    _redeemResult.Text = result.Error ?? "Login failed";
                    _redeemResult.ForeColor = Theme.Error;
                }
            }
            catch (Exception ex)
            {
                _redeemResult.Text = $"Error: {ex.Message}";
                _redeemResult.ForeColor = Theme.Error;
            }
        }

        private async Task SignOutAsync()
        {
            _token = null;
            _user = null;
            _license = null;
            _loginBtn.Visible = true;
            _logoutBtn.Visible = false;
            _userLabel.Text = "Not signed in.";
            _licenseLabel.Text = "License: —";
            await Task.CompletedTask;
        }

        private async Task RedeemAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                _redeemResult.Text = "Sign in first.";
                _redeemResult.ForeColor = Theme.Error;
                return;
            }

            var key = _redeemKeyBox.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                _redeemResult.Text = "Enter a key.";
                _redeemResult.ForeColor = Theme.Error;
                return;
            }

            var (ok, message) = await DiscordAuth.RedeemKeyAsync(_token, key);
            _redeemResult.Text = ok ? "Key redeemed!" : $"Error: {message}";
            _redeemResult.ForeColor = ok ? Theme.Success : Theme.Error;
            if (ok)
            {
                _redeemKeyBox.Clear();
                _license = await DiscordAuth.ValidateLicenseAsync(_token!);
                _licenseLabel.Text = _license.HasAccess ? $"License: {_license.DurationLabel}" : "License: Not found";
            }
        }
    }
}
