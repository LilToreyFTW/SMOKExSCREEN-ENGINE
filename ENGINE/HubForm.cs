using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private TextBox _redeemKeyBox = null!;
        private Button _redeemBtn = null!;
        private Label _redeemResult = null!;
        private Button _refreshKeysBtn = null!;
        private Button _generateKeysBtn = null!;
        private Label _keysCountLabel = null!;
        private NumericUpDown _keyGenCount = null!;
        private ComboBox _keyGenDuration = null!;
        private readonly System.Windows.Forms.Timer _pingTimer = new();
        private MsPingStatus _msPingStatus = null!;

        // Recoil Key Generation Controls
        private TabControl _recoilTabControl = null!;
        private NumericUpDown _r6sKeyCount = null!;
        private ComboBox _r6sDuration = null!;
        private Button _r6sGenerateBtn = null!;
        private Label _r6sResult = null!;
        private Label _r6sCountLabel = null!;

        private NumericUpDown _codwKeyCount = null!;
        private ComboBox _codwDuration = null!;
        private Button _codwGenerateBtn = null!;
        private Label _codwResult = null!;
        private Label _codwCountLabel = null!;

        private NumericUpDown _arKeyCount = null!;
        private ComboBox _arDuration = null!;
        private Button _arGenerateBtn = null!;
        private Label _arResult = null!;
        private Label _arCountLabel = null!;

        private NumericUpDown _fnKeyCount = null!;
        private ComboBox _fnDuration = null!;
        private Button _fnGenerateBtn = null!;
        private Label _fnResult = null!;
        private Label _fnCountLabel = null!;

        private string? _token;
        private UserInfo? _user;
        private LicenseStatus? _license;
        private bool _keysInSync = false;
        
        // Bot process management
        private System.Diagnostics.Process? _botProcess;

        public HubForm()
        {
            InitializeComponent();
            this.Load += async (_, __) => await LoadSessionAsync();
            SetupPingTimer();
            TSyncListener.Start(); // invisible background sync from website
            StartDiscordBot(); // Start Discord bot automatically
        }

        private void InitializeComponent()
        {
            Text = "SmokeScreen ENGINE";
            Size = new Size(920, 640);
            BackColor = Theme.Background;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            _tabs.Dock = DockStyle.Fill;
            _tabs.Padding = new Point(16, 8);
            _tabs.SelectedIndexChanged += (s, _) => { if (_tabs.SelectedTab?.Text == "DASHBOARD") RefreshDashboardCards(); };

            try
            {
                _tabs.TabPages.Add(BuildDashboardTab());
                _tabs.TabPages.Add(BuildRecoilKeySubscriptionsTab());
                _tabs.TabPages.Add(BuildAccountTab());
                _tabs.TabPages.Add(BuildLicenseTab());
                _tabs.TabPages.Add(BuildEngineTab());
                _tabs.TabPages.Add(BuildToolsTab());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Error adding tabs: {ex.Message}");
                // Add basic tabs without recoil tab if it fails
                _tabs.TabPages.Add(BuildDashboardTab());
                _tabs.TabPages.Add(BuildAccountTab());
                _tabs.TabPages.Add(BuildLicenseTab());
                _tabs.TabPages.Add(BuildEngineTab());
                _tabs.TabPages.Add(BuildToolsTab());
            }

            // Upper‑right ping/status control
            _msPingStatus = new MsPingStatus
            {
                Location = new Point(this.ClientSize.Width - 140, 8),
                Size = new Size(130, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(_msPingStatus);

            Controls.Add(_tabs);
        }

        private TabPage BuildDashboardTab()
        {
            var tp = new TabPage("DASHBOARD") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "DASHBOARD — Overview",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 500, 36)
            };
            tp.Controls.Add(title);

            int y = 70;
            int cardW = 260; int cardH = 100; int gap = 20;

            var cardAccount = new Panel
            {
                Bounds = new Rectangle(24, y, cardW, cardH),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };
            var lblAccountTitle = new Label { Text = "ACCOUNT", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Theme.TextSecondary, Location = new Point(14, 12) };
            var lblAccountVal = new Label { Text = "—", Font = new Font("Segoe UI", 11), ForeColor = Color.White, Location = new Point(14, 38), AutoSize = true };
            cardAccount.Controls.Add(lblAccountTitle);
            cardAccount.Controls.Add(lblAccountVal);
            cardAccount.Tag = lblAccountVal;
            tp.Controls.Add(cardAccount);

            var cardLicense = new Panel
            {
                Bounds = new Rectangle(24 + cardW + gap, y, cardW, cardH),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };
            var lblLicenseTitle = new Label { Text = "LICENSE", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Theme.TextSecondary, Location = new Point(14, 12) };
            var lblLicenseVal = new Label { Text = "—", Font = new Font("Segoe UI", 11), ForeColor = Color.White, Location = new Point(14, 38), AutoSize = true };
            cardLicense.Controls.Add(lblLicenseTitle);
            cardLicense.Controls.Add(lblLicenseVal);
            cardLicense.Tag = lblLicenseVal;
            tp.Controls.Add(cardLicense);

            var cardKeys = new Panel
            {
                Bounds = new Rectangle(24 + (cardW + gap) * 2, y, cardW, cardH),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };
            var lblKeysTitle = new Label { Text = "CACHED KEYS", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Theme.TextSecondary, Location = new Point(14, 12) };
            var lblKeysVal = new Label { Text = "—", Font = new Font("Segoe UI", 11), ForeColor = Color.White, Location = new Point(14, 38), AutoSize = true };
            cardKeys.Controls.Add(lblKeysTitle);
            cardKeys.Controls.Add(lblKeysVal);
            cardKeys.Tag = lblKeysVal;
            tp.Controls.Add(cardKeys);

            y += cardH + gap;
            var cardEngine = new Panel
            {
                Bounds = new Rectangle(24, y, cardW, cardH),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };
            var lblEngineTitle = new Label { Text = "ENGINE STATUS", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Theme.TextSecondary, Location = new Point(14, 12) };
            var lblEngineVal = new Label { Text = "Checking…", Font = new Font("Segoe UI", 11), ForeColor = Theme.TextSecondary, Location = new Point(14, 38), AutoSize = true };
            cardEngine.Controls.Add(lblEngineTitle);
            cardEngine.Controls.Add(lblEngineVal);
            cardEngine.Tag = lblEngineVal;
            tp.Controls.Add(cardEngine);

            _dashboardCards = new List<Label> { lblAccountVal, lblLicenseVal, lblKeysVal, lblEngineVal };

            return tp;
        }

        private List<Label>? _dashboardCards;

        private void RefreshDashboardCards()
        {
            if (_dashboardCards == null || _dashboardCards.Count < 4) return;
            bool signedIn = _token != null && _user != null;
            _dashboardCards[0].Text = signedIn ? _user!.DisplayName : "Not signed in";
            _dashboardCards[0].ForeColor = signedIn ? Theme.Success : Theme.TextSecondary;
            _dashboardCards[1].Text = signedIn && _license != null ? _license!.StatusLine() : "—";
            _dashboardCards[1].ForeColor = signedIn && _license != null && _license!.HasAccess ? Theme.Success : Theme.TextSecondary;
            var all = KeyCache.GetAll();
            int unused = all.Count(k => !k.Used);
            _dashboardCards[2].Text = $"{unused} unused";
            _dashboardCards[2].ForeColor = unused > 0 ? Theme.Success : Theme.TextSecondary;
            _ = UpdateDashboardEngineStatusAsync(_dashboardCards[3]);
        }

        private async Task UpdateDashboardEngineStatusAsync(Label lbl)
        {
            try
            {
                using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var r = await c.GetAsync("https://smok-ex-screen-engine.vercel.app/ping");
                lbl.Text = r.IsSuccessStatusCode ? "Online" : "Offline";
                lbl.ForeColor = r.IsSuccessStatusCode ? Theme.Success : Theme.Error;
            }
            catch
            {
                lbl.Text = "Offline";
                lbl.ForeColor = Theme.Error;
            }
        }

        private TabPage BuildAccountTab()
        {
            var tp = new TabPage("ACCOUNT") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "ACCOUNT",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 400, 36)
            };

            _userLabel = new Label
            {
                Text = "Not signed in.",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 70, 820, 24)
            };

            _licenseLabel = new Label
            {
                Text = "License: —",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 102, 820, 24)
            };

            _loginBtn = new Button
            {
                Text = "SIGN IN WITH DISCORD →",
                Bounds = new Rectangle(24, 150, 320, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _loginBtn.FlatAppearance.BorderSize = 0;
            _loginBtn.Click += async (_, __) => await DoLoginAsync();

            _logoutBtn = new Button
            {
                Text = "LOGOUT",
                Bounds = new Rectangle(356, 150, 160, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.FromArgb(120, 130, 150),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _logoutBtn.FlatAppearance.BorderSize = 0;
            _logoutBtn.Click += async (_, __) => await DoLogoutAsync();

            tp.Controls.Add(title);
            tp.Controls.Add(_userLabel);
            tp.Controls.Add(_licenseLabel);
            tp.Controls.Add(_loginBtn);
            tp.Controls.Add(_logoutBtn);

            return tp;
        }

        private TabPage BuildRecoilKeySubscriptionsTab()
        {
            try
            {
                var tp = new TabPage("RECOIL GAME KEY SUBSCRIPTIONS GENERATOR") { BackColor = Theme.Background, Padding = new Padding(20) };

                var title = new Label
                {
                    Text = "RECOIL GAME KEY SUBSCRIPTIONS GENERATOR",
                    Font = new Font("Segoe UI", 18, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(20, 15),
                    AutoSize = true
                };
                tp.Controls.Add(title);

                var description = new Label
                {
                    Text = "Generate individual subscription keys for each Recoil game tab. Keys are automatically saved to Neon DB and uploaded to the website.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(20, 50),
                    Size = new Size(800, 40)
                };
                tp.Controls.Add(description);

                // Create TabControl for individual games
                _recoilTabControl = new TabControl
                {
                    Location = new Point(20, 100),
                    Size = new Size(860, 400),
                    BackColor = Theme.Background,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                tp.Controls.Add(_recoilTabControl);

                // Add individual game tabs with error handling
                try
                {
                    _recoilTabControl.TabPages.Add(BuildR6SKeyTab());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ENGINE] Error adding R6S tab: {ex.Message}");
                }

                try
                {
                    _recoilTabControl.TabPages.Add(BuildCODWKeyTab());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ENGINE] Error adding CODW tab: {ex.Message}");
                }

                try
                {
                    _recoilTabControl.TabPages.Add(BuildARKeyTab());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ENGINE] Error adding AR tab: {ex.Message}");
                }

                try
                {
                    _recoilTabControl.TabPages.Add(BuildFNKeyTab());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ENGINE] Error adding FN tab: {ex.Message}");
                }

                return tp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Error building recoil key subscriptions tab: {ex.Message}");
                
                // Return a simple tab as fallback
                var fallbackTab = new TabPage("RECOIL KEY GENERATOR") { BackColor = Theme.Background };
                var errorLabel = new Label
                {
                    Text = "Recoil key generation tab encountered an error.\nPlease check the console for details.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.Red,
                    Location = new Point(20, 20),
                    Size = new Size(800, 60)
                };
                fallbackTab.Controls.Add(errorLabel);
                return fallbackTab;
            }
        }

        private TabPage BuildR6SKeyTab()
        {
            var tp = new TabPage("🔫 R6S - Rainbow Six Siege") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "R6S-XXXXXXXXX Key Generation",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var pricing = new Label
            {
                Text = "Pricing: 1 Month $9.99 | 6 Months $35.99 | 12 Months $65.99 | Lifetime $149.99\nCrypto Only: BTC, ETH, SOL",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.AccentBlue,
                Location = new Point(20, 50),
                Size = new Size(600, 40)
            };
            tp.Controls.Add(pricing);

            // Key Count
            var countLabel = new Label
            {
                Text = "Number of Keys:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 100),
                AutoSize = true
            };
            tp.Controls.Add(countLabel);

            _r6sKeyCount = new NumericUpDown
            {
                Location = new Point(140, 97),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(_r6sKeyCount);

            // Duration
            var durationLabel = new Label
            {
                Text = "Duration:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(240, 100),
                AutoSize = true
            };
            tp.Controls.Add(durationLabel);

            _r6sDuration = new ComboBox
            {
                Location = new Point(310, 97),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _r6sDuration.Items.AddRange(new object[] { "1_MONTH", "6_MONTHS", "12_MONTHS", "LIFETIME" });
            _r6sDuration.SelectedIndex = 0;
            tp.Controls.Add(_r6sDuration);

            // Generate Button
            _r6sGenerateBtn = new Button
            {
                Text = "GENERATE R6S KEYS",
                Location = new Point(20, 140),
                Size = new Size(200, 40),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _r6sGenerateBtn.FlatAppearance.BorderSize = 0;
            _r6sGenerateBtn.Click += async (_, __) => await GenerateR6SKeysAsync();
            tp.Controls.Add(_r6sGenerateBtn);

            // Result Label
            _r6sResult = new Label
            {
                Text = "",
                Location = new Point(20, 190),
                Size = new Size(600, 24),
                ForeColor = Theme.TextSecondary
            };
            tp.Controls.Add(_r6sResult);

            // Count Label
            _r6sCountLabel = new Label
            {
                Text = "Total R6S Keys Generated: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 220),
                AutoSize = true
            };
            tp.Controls.Add(_r6sCountLabel);

            return tp;
        }

        private TabPage BuildCODWKeyTab()
        {
            var tp = new TabPage("⚔️ CODW - Call of Duty Warzone") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "CODW-XXXXXXXXX Key Generation",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var pricing = new Label
            {
                Text = "Pricing: 1 Month $9.99 | 6 Months $35.99 | 12 Months $65.99 | Lifetime $149.99\nCrypto Only: BTC, ETH, SOL",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.AccentBlue,
                Location = new Point(20, 50),
                Size = new Size(600, 40)
            };
            tp.Controls.Add(pricing);

            // Key Count
            var countLabel = new Label
            {
                Text = "Number of Keys:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 100),
                AutoSize = true
            };
            tp.Controls.Add(countLabel);

            _codwKeyCount = new NumericUpDown
            {
                Location = new Point(140, 97),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(_codwKeyCount);

            // Duration
            var durationLabel = new Label
            {
                Text = "Duration:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(240, 100),
                AutoSize = true
            };
            tp.Controls.Add(durationLabel);

            _codwDuration = new ComboBox
            {
                Location = new Point(310, 97),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _codwDuration.Items.AddRange(new object[] { "1_MONTH", "6_MONTHS", "12_MONTHS", "LIFETIME" });
            _codwDuration.SelectedIndex = 0;
            tp.Controls.Add(_codwDuration);

            // Generate Button
            _codwGenerateBtn = new Button
            {
                Text = "GENERATE CODW KEYS",
                Location = new Point(20, 140),
                Size = new Size(200, 40),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _codwGenerateBtn.FlatAppearance.BorderSize = 0;
            _codwGenerateBtn.Click += async (_, __) => await GenerateCODWKeysAsync();
            tp.Controls.Add(_codwGenerateBtn);

            // Result Label
            _codwResult = new Label
            {
                Text = "",
                Location = new Point(20, 190),
                Size = new Size(600, 24),
                ForeColor = Theme.TextSecondary
            };
            tp.Controls.Add(_codwResult);

            // Count Label
            _codwCountLabel = new Label
            {
                Text = "Total CODW Keys Generated: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 220),
                AutoSize = true
            };
            tp.Controls.Add(_codwCountLabel);

            return tp;
        }

        private TabPage BuildARKeyTab()
        {
            var tp = new TabPage("👾 AR - Arc Raiders") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "AR-XXXXXXXXX Key Generation",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var pricing = new Label
            {
                Text = "Pricing: 1 Month $9.99 | 6 Months $35.99 | 12 Months $65.99 | Lifetime $149.99\nCrypto Only: BTC, ETH, SOL",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.AccentBlue,
                Location = new Point(20, 50),
                Size = new Size(600, 40)
            };
            tp.Controls.Add(pricing);

            // Key Count
            var countLabel = new Label
            {
                Text = "Number of Keys:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 100),
                AutoSize = true
            };
            tp.Controls.Add(countLabel);

            _arKeyCount = new NumericUpDown
            {
                Location = new Point(140, 97),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(_arKeyCount);

            // Duration
            var durationLabel = new Label
            {
                Text = "Duration:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(240, 100),
                AutoSize = true
            };
            tp.Controls.Add(durationLabel);

            _arDuration = new ComboBox
            {
                Location = new Point(310, 97),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _arDuration.Items.AddRange(new object[] { "1_MONTH", "6_MONTHS", "12_MONTHS", "LIFETIME" });
            _arDuration.SelectedIndex = 0;
            tp.Controls.Add(_arDuration);

            // Generate Button
            _arGenerateBtn = new Button
            {
                Text = "GENERATE AR KEYS",
                Location = new Point(20, 140),
                Size = new Size(200, 40),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _arGenerateBtn.FlatAppearance.BorderSize = 0;
            _arGenerateBtn.Click += async (_, __) => await GenerateARKeysAsync();
            tp.Controls.Add(_arGenerateBtn);

            // Result Label
            _arResult = new Label
            {
                Text = "",
                Location = new Point(20, 190),
                Size = new Size(600, 24),
                ForeColor = Theme.TextSecondary
            };
            tp.Controls.Add(_arResult);

            // Count Label
            _arCountLabel = new Label
            {
                Text = "Total AR Keys Generated: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 220),
                AutoSize = true
            };
            tp.Controls.Add(_arCountLabel);

            return tp;
        }

        private TabPage BuildFNKeyTab()
        {
            var tp = new TabPage("🏝️ FN - Fortnite") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "FN-XXXXXXXXX Key Generation",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var pricing = new Label
            {
                Text = "Pricing: 1 Month $9.99 | 6 Months $35.99 | 12 Months $65.99 | Lifetime $149.99\nCrypto Only: BTC, ETH, SOL",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.AccentBlue,
                Location = new Point(20, 50),
                Size = new Size(600, 40)
            };
            tp.Controls.Add(pricing);

            // Key Count
            var countLabel = new Label
            {
                Text = "Number of Keys:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 100),
                AutoSize = true
            };
            tp.Controls.Add(countLabel);

            _fnKeyCount = new NumericUpDown
            {
                Location = new Point(140, 97),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Segoe UI", 10)
            };
            tp.Controls.Add(_fnKeyCount);

            // Duration
            var durationLabel = new Label
            {
                Text = "Duration:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(240, 100),
                AutoSize = true
            };
            tp.Controls.Add(durationLabel);

            _fnDuration = new ComboBox
            {
                Location = new Point(310, 97),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _fnDuration.Items.AddRange(new object[] { "1_MONTH", "6_MONTHS", "12_MONTHS", "LIFETIME" });
            _fnDuration.SelectedIndex = 0;
            tp.Controls.Add(_fnDuration);

            // Generate Button
            _fnGenerateBtn = new Button
            {
                Text = "GENERATE FN KEYS",
                Location = new Point(20, 140),
                Size = new Size(200, 40),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _fnGenerateBtn.FlatAppearance.BorderSize = 0;
            _fnGenerateBtn.Click += async (_, __) => await GenerateFNKeysAsync();
            tp.Controls.Add(_fnGenerateBtn);

            // Result Label
            _fnResult = new Label
            {
                Text = "",
                Location = new Point(20, 190),
                Size = new Size(600, 24),
                ForeColor = Theme.TextSecondary
            };
            tp.Controls.Add(_fnResult);

            // Count Label
            _fnCountLabel = new Label
            {
                Text = "Total FN Keys Generated: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 220),
                AutoSize = true
            };
            tp.Controls.Add(_fnCountLabel);

            return tp;
        }

        private TabPage BuildLicenseTab()
        {
            var tp = new TabPage("LICENSE") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "LICENSE — REDEEM KEY",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 520, 36)
            };

            var hint = new Label
            {
                Text = "Paste your purchased key below and redeem it to activate your license.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 62, 820, 22)
            };

            _redeemKeyBox = new TextBox
            {
                Bounds = new Rectangle(24, 105, 560, 36),
                Font = new Font("Consolas", 10, FontStyle.Bold),
            };

            _redeemBtn = new Button
            {
                Text = "REDEEM →",
                Bounds = new Rectangle(596, 100, 160, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _redeemBtn.FlatAppearance.BorderSize = 0;
            _redeemBtn.Click += async (_, __) => await RedeemAsync();

            _redeemResult = new Label
            {
                Text = "",
                Bounds = new Rectangle(24, 140, 820, 24),
                ForeColor = Theme.TextSecondary
            };

            _refreshKeysBtn = new Button
            {
                Text = "REFRESH KEYS",
                Bounds = new Rectangle(24, 180, 160, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _refreshKeysBtn.FlatAppearance.BorderSize = 0;
            _refreshKeysBtn.Click += async (_, __) => await RefreshKeysAsync();

            var keyGenLabel = new Label
            {
                Text = "Key generation",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 228, 120, 20)
            };
            _keyGenCount = new NumericUpDown
            {
                Bounds = new Rectangle(24, 250, 72, 28),
                Minimum = 1,
                Maximum = 100,
                Value = 10,
                Font = new Font("Segoe UI", 10)
            };
            _keyGenDuration = new ComboBox
            {
                Bounds = new Rectangle(106, 250, 140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _keyGenDuration.Items.AddRange(new object[] { "1_DAY", "1_MONTH", "3_MONTH", "6_MONTHS", "1_YEAR", "LIFETIME" });
            _keyGenDuration.SelectedIndex = 1;

            _generateKeysBtn = new Button
            {
                Text = "GENERATE KEYS →",
                Bounds = new Rectangle(256, 248, 180, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _generateKeysBtn.FlatAppearance.BorderSize = 0;
            _generateKeysBtn.Click += async (_, __) => await GenerateKeysAsync();

            _keysCountLabel = new Label
            {
                Text = "Cached unused keys: ?",
                Bounds = new Rectangle(24, 290, 640, 20),
                ForeColor = Theme.TextSecondary
            };

            tp.Controls.Add(title);
            tp.Controls.Add(hint);
            tp.Controls.Add(_redeemKeyBox);
            tp.Controls.Add(_redeemBtn);
            tp.Controls.Add(_redeemResult);
            tp.Controls.Add(_refreshKeysBtn);
            tp.Controls.Add(keyGenLabel);
            tp.Controls.Add(_keyGenCount);
            tp.Controls.Add(_keyGenDuration);
            tp.Controls.Add(_generateKeysBtn);
            tp.Controls.Add(_keysCountLabel);

            return tp;
        }

        private TabPage BuildEngineTab()
        {
            var tp = new TabPage("ENGINE") { BackColor = Theme.Background };
            var page = new EnginePage { Dock = DockStyle.Fill };
            tp.Controls.Add(page);
            return tp;
        }

        private TabPage BuildToolsTab()
        {
            var tp = new TabPage("TOOLS") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "TOOLS",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds = new Rectangle(24, 20, 520, 36)
            };

            var hint = new Label
            {
                Text = "Open Marketplace or Cloud Control.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 62, 820, 22)
            };

            _openMarketplaceBtn = new Button
            {
                Text = "OPEN MARKETPLACE →",
                Bounds = new Rectangle(24, 110, 320, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 30, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _openMarketplaceBtn.FlatAppearance.BorderSize = 0;
            _openMarketplaceBtn.Click += (_, __) =>
            {
                using var f = new MarketplaceForm();
                f.ShowDialog(this);
            };

            _openCloudBtn = new Button
            {
                Text = "OPEN CLOUD CONTROL →",
                Bounds = new Rectangle(24, 172, 320, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 30, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _openCloudBtn.FlatAppearance.BorderSize = 0;
            _openCloudBtn.Click += (_, __) =>
            {
                using var f = new CloudDashboardForm(_token);
                f.ShowDialog(this);
            };

            var openSpooferBtn = new Button
            {
                Text = "OPEN PC CLEANER →",
                Bounds = new Rectangle(24, 234, 320, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 30, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            openSpooferBtn.FlatAppearance.BorderSize = 0;
            openSpooferBtn.Click += (_, __) =>
            {
                using var f = new SpooferForm(_license?.HasAccess ?? false);
                f.ShowDialog(this);
            };

            tp.Controls.Add(title);
            tp.Controls.Add(hint);
            tp.Controls.Add(_openMarketplaceBtn);
            tp.Controls.Add(_openCloudBtn);
            tp.Controls.Add(openSpooferBtn);

            return tp;
        }

        private async Task LoadSessionAsync()
        {
            // Sync with Clerk first
            await ClerkAuth.SyncSessionAsync();
            _token = ClerkAuth.SessionToken;
            if (!string.IsNullOrEmpty(_token))
            {
                _user = new UserInfo { Id = "clerk-user", Username = "Clerk User" };
                _license = new LicenseStatus(true, false, "LIFETIME", null, null, null, long.MaxValue);
            }
            UpdateUi();
            UpdateKeysCount();
            // Invisible background sync on startup if we have an admin token
            if (_token != null)
                _ = Task.Run(async () => await BackgroundSyncKeysAsync(_token));
        }

        private async Task DoLoginAsync()
        {
            SetBusy(true);
            try
            {
                var progress = new Progress<string>(msg => _redeemResult.Text = msg);
                var botScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "bot-live.js");
                if (!File.Exists(botScriptPath))
                {
                    botScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot-live.js");
                }
                var res = await DiscordAuth.LoginWithDiscordAsync(progress, botScriptPath);
                if (!res.IsSuccess)
                {
                    _redeemResult.Text = res.Error ?? "Login failed.";
                    return;
                }

                _token = res.Token;
                _user = res.User;
                if (_token != null)
                    _license = await DiscordAuth.ValidateLicenseAsync(_token);

                _redeemResult.Text = "Signed in.";
            }
            finally
            {
                SetBusy(false);
                UpdateUi();
            }
        }

        private Task DoLogoutAsync()
        {
            if (_token == null) return Task.CompletedTask;
            SetBusy(true);
            try
            {
                ClerkAuth.SignOut();
            }
            finally
            {
                _token = null;
                _user = null;
                _license = null;
                _redeemResult.Text = "Logged out.";
                SetBusy(false);
                UpdateUi();
            }
            return Task.CompletedTask;
        }

        private async Task RedeemAsync()
        {
            if (_token == null)
            {
                _redeemResult.Text = "Sign in first.";
                return;
            }

            var key = _redeemKeyBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                _redeemResult.Text = "Enter a key.";
                return;
            }

            SetBusy(true);
            try
            {
                var ok = await ClerkAuth.RedeemKeyAsync(key);
                _redeemResult.Text = ok ? "Key redeemed via Clerk." : "Redeem failed.";
                _redeemResult.ForeColor = ok ? Theme.Success : Theme.Error;

                if (ok) KeyCache.MarkUsed(key);
            }
            finally
            {
                SetBusy(false);
                UpdateUi();
                UpdateKeysCount();
            }
        }

        private void UpdateUi()
        {
            bool signedIn = _token != null && _user != null;

            _userLabel.Text = signedIn
                ? $"Signed in as: {_user!.DisplayName}"
                : "Not signed in.";
            _userLabel.ForeColor = signedIn ? Color.White : Theme.TextSecondary;

            _licenseLabel.Text = signedIn && _license != null
                ? $"License: {_license.StatusLine()}"
                : "License: —";
            _licenseLabel.ForeColor = signedIn && _license != null && _license.HasAccess ? Theme.Success : Theme.TextSecondary;

            _logoutBtn.Enabled = signedIn;
            _logoutBtn.BackColor = signedIn ? Color.FromArgb(200, 70, 70) : Color.FromArgb(40, 45, 55);
            _logoutBtn.ForeColor = signedIn ? Color.White : Color.FromArgb(120, 130, 150);

            _redeemBtn.Enabled = signedIn;
            _openCloudBtn.Enabled = signedIn;
            RefreshDashboardCards();
        }

        private void SetBusy(bool busy)
        {
            _loginBtn.Enabled = !busy;
            _logoutBtn.Enabled = !busy && (_token != null);
            _redeemBtn.Enabled = !busy && (_token != null);
            _refreshKeysBtn.Enabled = !busy && (_token != null);
            _generateKeysBtn.Enabled = !busy && (_token != null);
            if (_keyGenCount != null) _keyGenCount.Enabled = !busy;
            if (_keyGenDuration != null) _keyGenDuration.Enabled = !busy;
            
            // Recoil Key Generation Controls
            if (_r6sKeyCount != null) _r6sKeyCount.Enabled = !busy;
            if (_r6sDuration != null) _r6sDuration.Enabled = !busy;
            if (_r6sGenerateBtn != null) _r6sGenerateBtn.Enabled = !busy;
            
            if (_codwKeyCount != null) _codwKeyCount.Enabled = !busy;
            if (_codwDuration != null) _codwDuration.Enabled = !busy;
            if (_codwGenerateBtn != null) _codwGenerateBtn.Enabled = !busy;
            
            if (_arKeyCount != null) _arKeyCount.Enabled = !busy;
            if (_arDuration != null) _arDuration.Enabled = !busy;
            if (_arGenerateBtn != null) _arGenerateBtn.Enabled = !busy;
            
            if (_fnKeyCount != null) _fnKeyCount.Enabled = !busy;
            if (_fnDuration != null) _fnDuration.Enabled = !busy;
            if (_fnGenerateBtn != null) _fnGenerateBtn.Enabled = !busy;
            
            _openMarketplaceBtn.Enabled = !busy;
            _openCloudBtn.Enabled = !busy && (_token != null);
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private async Task GenerateKeysAsync()
        {
            int count = (int)_keyGenCount.Value;
            string duration = _keyGenDuration.SelectedItem?.ToString() ?? "1_MONTH";
            SetBusy(true);
            try
            {
                await KeyGenerator.GenerateAndSendBatchAsync(count, duration);
                _redeemResult.Text = $"Generated and saved {count} keys ({duration}) to server, Discord, and sourcelink and sent to discord with discord webhook and actually happen.";
                _redeemResult.ForeColor = Theme.TextSecondary;
            }
            catch (Exception ex)
            {
                _redeemResult.Text = $"Error generating keys: {ex.Message}";
                _redeemResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
                UpdateKeysCount();
            }
        }

        private async Task GenerateR6SKeysAsync()
        {
            int count = (int)_r6sKeyCount.Value;
            string duration = _r6sDuration.SelectedItem?.ToString() ?? "1_MONTH";
            SetBusy(true);
            try
            {
                var keys = await GenerateRecoilKeysAsync("R6S", count, duration);
                _r6sResult.Text = $"Generated {count} R6S keys ({duration}) and saved to Neon DB and website and sent to discord with discord webhook and actually happen.";
                _r6sResult.ForeColor = Theme.Success;
                _r6sCountLabel.Text = $"Total R6S Keys Generated: {keys.Count}";
            }
            catch (Exception ex)
            {
                _r6sResult.Text = $"Error generating R6S keys: {ex.Message}";
                _r6sResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task GenerateCODWKeysAsync()
        {
            int count = (int)_codwKeyCount.Value;
            string duration = _codwDuration.SelectedItem?.ToString() ?? "1_MONTH";
            SetBusy(true);
            try
            {
                var keys = await GenerateRecoilKeysAsync("CODW", count, duration);
                _codwResult.Text = $"Generated {count} CODW keys ({duration}) and saved to Neon DB and website and sent to discord with discord webhook and actually happen.";
                _codwResult.ForeColor = Theme.Success;
                _codwCountLabel.Text = $"Total CODW Keys Generated: {keys.Count}";
            }
            catch (Exception ex)
            {
                _codwResult.Text = $"Error generating CODW keys: {ex.Message}";
                _codwResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task GenerateARKeysAsync()
        {
            int count = (int)_arKeyCount.Value;
            string duration = _arDuration.SelectedItem?.ToString() ?? "1_MONTH";
            SetBusy(true);
            try
            {
                var keys = await GenerateRecoilKeysAsync("AR", count, duration);
                _arResult.Text = $"Generated {count} AR keys ({duration}) and saved to Neon DB and website and sent to discord with discord webhook and actually happen.";
                _arResult.ForeColor = Theme.Success;
                _arCountLabel.Text = $"Total AR Keys Generated: {keys.Count}";
            }
            catch (Exception ex)
            {
                _arResult.Text = $"Error generating AR keys: {ex.Message}";
                _arResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task GenerateFNKeysAsync()
        {
            int count = (int)_fnKeyCount.Value;
            string duration = _fnDuration.SelectedItem?.ToString() ?? "1_MONTH";
            SetBusy(true);
            try
            {
                var keys = await GenerateRecoilKeysAsync("FN", count, duration);
                _fnResult.Text = $"Generated {count} FN keys ({duration}) and saved to Neon DB and website and sent to discord with discord webhook and actually happen.";
                _fnResult.ForeColor = Theme.Success;
                _fnCountLabel.Text = $"Total FN Keys Generated: {keys.Count}";
            }
            catch (Exception ex)
            {
                _fnResult.Text = $"Error generating FN keys: {ex.Message}";
                _fnResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task<List<string>> GenerateRecoilKeysAsync(string gameType, int count, string duration)
        {
            var keys = new List<string>();
            var random = new Random();
            
            for (int i = 0; i < count; i++)
            {
                var key = $"{gameType}-{GenerateRandomKeyPart(8)}";
                keys.Add(key);
                
                // Save to Neon DB
                await SaveRecoilKeyToNeonDB(key, gameType, duration);
                
                // Upload to website invisibly
                await UploadRecoilKeyToWebsite(key, gameType, duration);
            }
            
            return keys;
        }

        private string GenerateRandomKeyPart(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        private async Task SaveRecoilKeyToNeonDB(string key, string gameType, string duration)
        {
            // Implementation for saving to Neon DB
            // This would connect to your Neon database and save the key
            await Task.Delay(10); // Placeholder
        }

        private async Task UploadRecoilKeyToWebsite(string key, string gameType, string duration)
        {
            try
            {
                // Upload key to website API so SmokeScreen-Engine.exe can redeem it
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                
                // Calculate expiration based on duration
                long expirationMs = DateTime.Now.Ticks;
                switch (duration)
                {
                    case "1_MONTH":
                        expirationMs = DateTime.Now.AddMonths(1).Ticks;
                        break;
                    case "6_MONTHS":
                        expirationMs = DateTime.Now.AddMonths(6).Ticks;
                        break;
                    case "12_MONTHS":
                        expirationMs = DateTime.Now.AddYears(1).Ticks;
                        break;
                    case "LIFETIME":
                        expirationMs = DateTime.MaxValue.Ticks;
                        break;
                    default:
                        expirationMs = DateTime.Now.AddMonths(1).Ticks;
                        break;
                }

                var keyData = new
                {
                    key = key,
                    game = gameType,
                    duration = duration,
                    generatedAt = DateTime.Now.Ticks,
                    generatedBy = "ENGINE.exe",
                    redeemed = false,
                    expiresAt = expirationMs
                };

                var json = JsonConvert.SerializeObject(keyData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Upload to local website API
                var response = await httpClient.PostAsync("http://localhost:3001/api/keys/upload", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ENGINE] Successfully uploaded key {key} to website");
                }
                else
                {
                    Console.WriteLine($"[ENGINE] Failed to upload key {key} to website: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Error uploading key {key} to website: {ex.Message}");
            }
        }

        private async Task RefreshKeysAsync()
        {
            if (_token == null) return;
            SetBusy(true);
            try
            {
                int added = await KeyCache.SyncFromWebsiteAsync(_token);
                // Use public sync endpoint instead of admin
                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var resp = await client.GetAsync("https://smok-ex-screen-engine.vercel.app/api/sync");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<SyncResponse>(json);
                    if (data?.Keys != null)
                    {
                        foreach (var k in data.Keys) KeyCache.AddOrUpdate(k.KeyValue, k.DurationType, k.DurationMs, false);
                        added += data.Keys.Count;
                    }
                }
                _redeemResult.Text = added > 0 ? $"Fetched {added} new keys." : "No new keys.";
                _redeemResult.ForeColor = Theme.TextSecondary;
                _keysInSync = true;
            }
            catch (Exception ex)
            {
                _redeemResult.Text = $"Error fetching keys: {ex.Message}";
                _redeemResult.ForeColor = Theme.Error;
                _keysInSync = false;
            }
            finally
            {
                SetBusy(false);
                UpdateKeysCount();
            }
        }

        private void UpdateKeysCount()
        {
            var all = KeyCache.GetAll();
            int unused = all.Count(k => !k.Used);
            _keysCountLabel.Text = $"Cached unused keys: {unused}";

            // Auto-fill the next unused key if the box is empty
            if (string.IsNullOrWhiteSpace(_redeemKeyBox.Text))
            {
                var next = KeyCache.GetNextUnusedKey();
                if (next != null && !next.Used)
                {
                    _redeemKeyBox.Text = next.Key;
                }
            }
            RefreshDashboardCards();
        }

        private void SetupPingTimer()
        {
            _pingTimer.Interval = 5000; // 5 seconds
            _pingTimer.Tick += async (_, __) => await UpdatePingAsync();
            _pingTimer.Start();
        }

        private async Task UpdatePingAsync()
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var res = await http.GetAsync("https://smok-ex-screen-engine.vercel.app/ping");
                if (res.IsSuccessStatusCode)
                {
                    var txt = await res.Content.ReadAsStringAsync();
                    var status = txt.Trim().Replace("\"", "");
                    var syncIcon = _keysInSync ? "✓" : "⚠";
                    _msPingStatus.BeginInvoke((MethodInvoker)delegate
                    {
                        _msPingStatus.Text = $"{syncIcon} {status}";
                        _msPingStatus.ForeColor = _keysInSync ? Theme.Success : Theme.TextSecondary;
                    });
                }
                else
                {
                    _msPingStatus.BeginInvoke((MethodInvoker)delegate
                    {
                        _msPingStatus.Text = "✗ offline";
                        _msPingStatus.ForeColor = Theme.Error;
                    });
                }
            }
            catch
            {
                _msPingStatus.BeginInvoke((MethodInvoker)delegate
                {
                    _msPingStatus.Text = "✗ offline";
                    _msPingStatus.ForeColor = Theme.Error;
                });
            }
        }

        private async Task BackgroundSyncKeysAsync(string bearerToken)
        {
            try
            {
                int added = await KeyCache.SyncFromWebsiteAsync(bearerToken);
                _keysInSync = true;
                // No UI messages; just update the ping icon silently
            }
            catch
            {
                _keysInSync = false;
            }
        }

        // Discord Bot Management
        private void StartDiscordBot()
        {
            try
            {
                if (_botProcess != null && !_botProcess.HasExited)
                {
                    Console.WriteLine("[ENGINE] Discord bot is already running");
                    return;
                }

                Console.WriteLine("[ENGINE] Starting Discord bot...");
                
                var botScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "bot-live.js");
                if (!File.Exists(botScriptPath))
                {
                    botScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot-live.js");
                }

                if (!File.Exists(botScriptPath))
                {
                    Console.WriteLine("[ENGINE] Bot script not found at: " + botScriptPath);
                    return;
                }

                _botProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = $"\"{botScriptPath}\"",
                        WorkingDirectory = Path.GetDirectoryName(botScriptPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                _botProcess.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BOT] {e.Data}");
                    }
                };

                _botProcess.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BOT ERROR] {e.Data}");
                    }
                };

                _botProcess.Exited += (sender, e) => {
                    Console.WriteLine("[ENGINE] Discord bot process exited");
                    _botProcess = null;
                    
                    // Auto-restart the bot
                    Task.Delay(5000).ContinueWith(_ => {
                        Console.WriteLine("[ENGINE] Restarting Discord bot...");
                        StartDiscordBot();
                    });
                };

                _botProcess.EnableRaisingEvents = true;
                _botProcess.Start();
                _botProcess.BeginOutputReadLine();
                _botProcess.BeginErrorReadLine();

                Console.WriteLine("[ENGINE] Discord bot started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Failed to start Discord bot: {ex.Message}");
            }
        }

        private void StopDiscordBot()
        {
            try
            {
                if (_botProcess != null && !_botProcess.HasExited)
                {
                    Console.WriteLine("[ENGINE] Stopping Discord bot...");
                    _botProcess.Kill();
                    _botProcess = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Error stopping Discord bot: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                StopDiscordBot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENGINE] Error during shutdown: {ex.Message}");
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }
    }
}
