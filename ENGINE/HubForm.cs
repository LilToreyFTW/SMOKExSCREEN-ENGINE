using System;
using System.Drawing;
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
        private Label _keysCountLabel = null!;
        private Label _pingLabel = null!;
        private readonly System.Windows.Forms.Timer _pingTimer = new();

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

            _tabs.TabPages.Add(BuildAccountTab());
            _tabs.TabPages.Add(BuildLicenseTab());
            _tabs.TabPages.Add(BuildToolsTab());

            // Upper‑left ping/status label
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

            _keysCountLabel = new Label
            {
                Text = "Cached unused keys: ?",
                Bounds = new Rectangle(200, 190, 640, 20),
                ForeColor = Theme.TextSecondary
            };

            tp.Controls.Add(title);
            tp.Controls.Add(hint);
            tp.Controls.Add(_redeemKeyBox);
            tp.Controls.Add(_redeemBtn);
            tp.Controls.Add(_redeemResult);
            tp.Controls.Add(_refreshKeysBtn);
            tp.Controls.Add(_keysCountLabel);

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

            tp.Controls.Add(title);
            tp.Controls.Add(hint);
            tp.Controls.Add(_openMarketplaceBtn);
            tp.Controls.Add(_openCloudBtn);

            return tp;
        }

        private async Task LoadSessionAsync()
        {
            var res = await DiscordAuth.TryAutoLoginAsync();
            if (!res.IsSuccess) { UpdateUi(); UpdateKeysCount(); return; }

            _token = res.Token;
            _user = res.User;

            if (_token != null)
                _license = await DiscordAuth.ValidateLicenseAsync(_token);

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

        private async Task DoLogoutAsync()
        {
            if (_token == null) return;
            SetBusy(true);
            try
            {
                await DiscordAuth.LogoutAsync(_token);
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
                var (ok, message) = await DiscordAuth.RedeemKeyAsync(_token, key);
                _redeemResult.Text = message;
                _redeemResult.ForeColor = ok ? Theme.Success : Theme.Error;

                if (ok) KeyCache.MarkUsed(key);

                _license = await DiscordAuth.ValidateLicenseAsync(_token);
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
        }

        private void SetBusy(bool busy)
        {
            _loginBtn.Enabled = !busy;
            _logoutBtn.Enabled = !busy && (_token != null);
            _redeemBtn.Enabled = !busy && (_token != null);
            _refreshKeysBtn.Enabled = !busy && (_token != null);
            _openMarketplaceBtn.Enabled = !busy;
            _openCloudBtn.Enabled = !busy && (_token != null);
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private async Task RefreshKeysAsync()
        {
            if (_token == null) return;
            SetBusy(true);
            try
            {
                int added = await KeyCache.SyncFromWebsiteAsync(_token);
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
                    _pingLabel.Invoke((MethodInvoker)delegate
                    {
                        _pingLabel.Text = $"{syncIcon} {status}";
                        _pingLabel.ForeColor = _keysInSync ? Theme.Success : Theme.TextSecondary;
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
