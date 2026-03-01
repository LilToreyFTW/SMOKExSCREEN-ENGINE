using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private string? _token;
        private UserInfo? _user;
        private LicenseStatus? _license;
        private bool _keysInSync = false;

        public HubForm()
        {
            InitializeComponent();
            this.Load += async (_, __) => await LoadSessionAsync();
            SetupPingTimer();
            TSyncListener.Start(); // invisible background sync from website
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

            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildAccountTab());
            _tabs.TabPages.Add(BuildLicenseTab());
            _tabs.TabPages.Add(BuildEngineTab());
            _tabs.TabPages.Add(BuildToolsTab());

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
                var res = await DiscordAuth.LoginWithDiscordAsync(progress);
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
                _redeemResult.Text = $"Generated and saved {count} keys ({duration}) to server, Discord, and sourcelink.";
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
    }
}
