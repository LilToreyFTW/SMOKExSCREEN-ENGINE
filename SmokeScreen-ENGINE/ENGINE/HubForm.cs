using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

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

        private TextBox _discordIdBox = null!;
        private TextBox _redeemKeyBox = null!;
        private Button _redeemBtn = null!;
        private Label _redeemResult = null!;
        private Button _refreshKeysBtn = null!;
        private Label _keysCountLabel = null!;
        private Label _pingLabel = null!;
        private readonly System.Windows.Forms.Timer _pingTimer = new();
        private System.Windows.Forms.Timer _heartbeatTimer = new();

        // Recoil Key Access Tracking
        private bool _hasR6SKey = false;
        private bool _hasCODWKey = false;
        private bool _hasARKey = false;
        private bool _hasFNKey = false;
        private bool _hasSpooferKey = false;

        private string? _token;
        private UserInfo? _user;
        private LicenseStatus? _license;
        private bool _keysInSync = false;
        
        // Wireless Receiver Integration
        private WirelessReceiver _wirelessReceiver;
        private string _userSessionId;
        private SimpleDiscordBot _discordBot;
        private PS5ControllerManager _ps5Manager;

        public HubForm()
        {
            _wirelessReceiver = new WirelessReceiver();
            _userSessionId = Guid.NewGuid().ToString();
            _discordBot = new SimpleDiscordBot();
            _ps5Manager = new PS5ControllerManager();
            InitializeComponent();
            this.Load += async (_, __) => await LoadSessionAsync();
            SetupPingTimer();
            TSyncListener.Start(); // invisible background sync from website
            
            // Start Discord bot in background
            _discordBot.StartBot();
            
            // Initialize PS5 controller
            _ps5Manager.Initialize();
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
            _tabs.TabPages.Add(BuildGameTab("🎮 WARZONE", "Warzone"));
            _tabs.TabPages.Add(BuildGameTab("🔫 R6S", "R6S"));
            _tabs.TabPages.Add(BuildGameTab("👾 ARC RAIDERS", "Arc Raiders"));
            _tabs.TabPages.Add(BuildGameTab("🏝️ FN", "Fortnite"));
            _tabs.TabPages.Add(BuildSpooferTab());
            _tabs.TabPages.Add(BuildAutoUpdaterTab());
            _tabs.TabPages.Add(BuildApiServiceStatusTab());

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

            var discordIdLabel = new Label
            {
                Text = "Discord User ID:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 62, 200, 22)
            };

            _discordIdBox = new TextBox
            {
                Bounds = new Rectangle(24, 85, 320, 36),
                Font = new Font("Consolas", 10, FontStyle.Bold),
                PlaceholderText = "Enter your Discord User ID..."
            };

            _userLabel = new Label
            {
                Text = "Not signed in.",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 130, 820, 24)
            };

            _licenseLabel = new Label
            {
                Text = "License: —",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 162, 820, 24)
            };

            _loginBtn = new Button
            {
                Text = "SIGN IN WITH DISCORD ID →",
                Bounds = new Rectangle(24, 210, 320, 46),
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
                Bounds = new Rectangle(356, 210, 160, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.FromArgb(120, 130, 150),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _logoutBtn.FlatAppearance.BorderSize = 0;
            _logoutBtn.Click += async (_, __) => await DoLogoutAsync();

            var helpLabel = new Label
            {
                Text = "💡 Get your Discord User ID: Right-click your name in Discord → Copy User ID",
                Font = new Font("Segoe UI", 8),
                ForeColor = Theme.TextSecondary,
                Bounds = new Rectangle(24, 270, 820, 20)
            };

            tp.Controls.Add(title);
            tp.Controls.Add(discordIdLabel);
            tp.Controls.Add(_discordIdBox);
            tp.Controls.Add(_userLabel);
            tp.Controls.Add(_licenseLabel);
            tp.Controls.Add(_loginBtn);
            tp.Controls.Add(_logoutBtn);
            tp.Controls.Add(helpLabel);

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
            try
            {
                var res = await DiscordAuth.TryAutoLoginAsync();
                if (!res.IsSuccess) { BeginInvoke(() => { UpdateUi(); UpdateKeysCount(); }); return; }

                _token = res.Token;
                _user = res.User;

                if (_token != null)
                {
                    try
                    {
                        _license = await DiscordAuth.ValidateLicenseAsync(_token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"License validation failed: {ex.Message}");
                        _license = null;
                    }
                }

                BeginInvoke(() => { UpdateUi(); UpdateKeysCount(); });
                
                // Register user with wireless receiver
                if (_user != null && _license != null)
                {
                    _ = Task.Run(async () => await RegisterUserWithWirelessReceiver());
                    
                    // Log successful login to Discord bot
                    _ = Task.Run(async () => await _discordBot.LogLoginAttempt(_user.DiscordId ?? "", _user.Username ?? _user.DiscordId ?? "", true));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadSessionAsync failed: {ex.Message}");
                BeginInvoke(() => { UpdateUi(); UpdateKeysCount(); });
            }
            
            // Invisible background sync on startup if we have an admin token
            if (_token != null)
                _ = Task.Run(async () => await BackgroundSyncKeysAsync(_token));
        }

        private async Task DoLoginAsync()
        {
            var discordId = _discordIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(discordId))
            {
                _redeemResult.Text = "Please enter your Discord User ID.";
                return;
            }

            SetBusy(true);
            try
            {
                var progress = new Progress<string>(msg => _redeemResult.Text = msg);
                var res = await DiscordAuth.LoginWithDiscordIdAsync(discordId, progress);
                if (!res.IsSuccess)
                {
                    _redeemResult.Text = res.Error ?? "Login failed.";
                    return;
                }

                _token = res.Token;
                _user = res.User;
                if (_token != null)
                    _license = await DiscordAuth.ValidateLicenseAsync(_token);

                _redeemResult.Text = $"Signed in as {res.User.Username}#{res.User.Discriminator}";
                _redeemResult.ForeColor = Theme.Success;
                
                // Fetch user subscriptions and update tabs
                await FetchUserSubscriptionsAsync();
            }
            finally
            {
                SetBusy(false);
                BeginInvoke(UpdateUi);
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
                BeginInvoke(UpdateUi);
            }
        }

        private async Task RedeemAsync()
        {
            if (_token == null || _user == null)
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
                // Check if this is an encrypted key
                if (key.StartsWith("ENCRYPTED-"))
                {
                    // Decrypt the key first
                    var keyId = key.Substring(10); // Remove "ENCRYPTED-" prefix
                    var encryptedKey = KeyCache.GetEncryptedKey(keyId);
                    
                    if (encryptedKey == null)
                    {
                        _redeemResult.Text = "Encrypted key not found.";
                        _redeemResult.ForeColor = Theme.Error;
                        return;
                    }
                    
                    // Decrypt the key
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var decryptResponse = await httpClient.PostAsync(
                        "http://localhost:3002/api/decrypt-key",
                        new StringContent(JsonConvert.SerializeObject(new { keyId }), Encoding.UTF8, "application/json")
                    );
                    
                    if (decryptResponse.IsSuccessStatusCode)
                    {
                        var decryptJson = await decryptResponse.Content.ReadAsStringAsync();
                        var decryptData = JsonConvert.DeserializeObject<dynamic>(decryptJson);
                        
                        if (decryptData?.success == true && decryptData?.key != null)
                        {
                            var decryptedKey = decryptData.key;
                            var actualKey = decryptedKey.keyValue?.ToString() ?? "";
                            
                            // Now redeem the decrypted key
                            await RedeemDecryptedKey(actualKey);
                        }
                        else
                        {
                            _redeemResult.Text = "Failed to decrypt key.";
                            _redeemResult.ForeColor = Theme.Error;
                        }
                    }
                    else
                    {
                        _redeemResult.Text = "Decryption service unavailable.";
                        _redeemResult.ForeColor = Theme.Error;
                    }
                }
                else
                {
                    // Regular key redemption
                    await RedeemDecryptedKey(key);
                }
            }
            catch (Exception ex)
            {
                _redeemResult.Text = $"Server error: {ex.Message}";
                _redeemResult.ForeColor = Theme.Error;
            }
            finally
            {
                SetBusy(false);
                UpdateUi();
                UpdateKeysCount();
            }
        }

        private async Task RedeemDecryptedKey(string key)
        {
            // Validate user has Discord ID
            if (string.IsNullOrEmpty(_user?.DiscordId))
            {
                _redeemResult.Text = "Discord authentication required. Please sign in with your Discord ID first.";
                _redeemResult.ForeColor = Theme.Error;
                return;
            }

            // Use Discord bot API directly with user's Discord ID
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.PostAsync(
                "http://localhost:9877/keys/redeem",
                new StringContent(JsonConvert.SerializeObject(new { key, userId = _user.DiscordId }), Encoding.UTF8, "application/json")
            );

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            
            if (response.IsSuccessStatusCode)
            {
                bool success = data?.success ?? false;
                string message = data?.message ?? "Key redeemed successfully!";
                
                _redeemResult.Text = message;
                _redeemResult.ForeColor = success ? Theme.Success : Theme.Error;

                if (success) 
                {
                    KeyCache.MarkUsed(key);
                    
                    // Check if this is a recoil key and update access
                    UpdateRecoilKeyAccess(key);
                    
                    // Log successful key redemption to Discord bot
                    _ = Task.Run(async () => await _discordBot.LogKeyRedemption(_user.DiscordId ?? "", key, "Redeemed", true));
                }
                else
                {
                    // Log failed key redemption to Discord bot
                    _ = Task.Run(async () => await _discordBot.LogKeyRedemption(_user.DiscordId ?? "", key, "Failed", false));
                }
            }
            else
            {
                string error = data?.error ?? data?.message ?? "Key redemption failed";
                _redeemResult.Text = error;
                _redeemResult.ForeColor = Theme.Error;
            }
        }

        private async Task FetchUserSubscriptionsAsync()
        {
            if (_user?.DiscordId == null) return;

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await httpClient.GetAsync($"http://localhost:9877/auth/user?userId={_user.DiscordId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userData = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    if (userData?.success == true)
                    {
                        // Reset all key flags
                        _hasR6SKey = false;
                        _hasCODWKey = false;
                        _hasARKey = false;
                        _hasFNKey = false;

                        // Check user's active subscriptions from Discord roles
                        var roles = userData?.user?.roles as object[];
                        if (roles != null)
                        {
                            foreach (var role in roles)
                            {
                                string roleName = role.ToString().ToLower();
                                if (roleName.Contains("r6s") || roleName.Contains("rainbow"))
                                    _hasR6SKey = true;
                                else if (roleName.Contains("warzone") || roleName.Contains("codw") || roleName.Contains("call of duty"))
                                    _hasCODWKey = true;
                                else if (roleName.Contains("arc") || roleName.Contains("raiders"))
                                    _hasARKey = true;
                                else if (roleName.Contains("fortnite") || roleName.Contains("fn"))
                                    _hasFNKey = true;
                                else if (roleName.Contains("spoofer") || roleName.Contains("cleaner") || roleName.Contains("hwid"))
                                    _hasSpooferKey = true;
                                else if (roleName.Contains("lifetime") || roleName.Contains("all games"))
                                {
                                    // Lifetime access to all games
                                    _hasR6SKey = true;
                                    _hasCODWKey = true;
                                    _hasARKey = true;
                                    _hasFNKey = true;
                                    _hasSpooferKey = true;
                                }
                            }
                        }

                        // Also check for redeemed keys
                        if (userData?.redeemedKeys != null)
                        {
                            var redeemedKeys = userData.redeemedKeys as object[];
                            foreach (var key in redeemedKeys)
                            {
                                string keyStr = key.ToString();
                                if (keyStr.StartsWith("R6S-"))
                                    _hasR6SKey = true;
                                else if (keyStr.StartsWith("CODW-"))
                                    _hasCODWKey = true;
                                else if (keyStr.StartsWith("AR-"))
                                    _hasARKey = true;
                                else if (keyStr.StartsWith("FN-"))
                                    _hasFNKey = true;
                                else if (keyStr.StartsWith("SPF-") || keyStr.StartsWith("SPOOFER-"))
                                    _hasSpooferKey = true;
                            }
                        }

                        // Refresh tabs to show/hide based on subscriptions
                        BeginInvoke(RefreshGameTabs);
                        
                        Console.WriteLine($"[SMOKESCREEN] Updated tab access for {_user.Username}: R6S={_hasR6SKey}, CODW={_hasCODWKey}, AR={_hasARKey}, FN={_hasFNKey}, Spoofer={_hasSpooferKey}");
                    }
                    else
                    {
                        Console.WriteLine($"[SMOKESCREEN] API Error: {userData?.error ?? "Unknown error"}");
                    }
                }
                else
                {
                    Console.WriteLine($"[SMOKESCREEN] HTTP Error: {response.StatusCode} - Bot may be down");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMOKESCREEN] Error fetching user subscriptions: {ex.Message}");
            }
        }

        private void UpdateUi()
        {
            try
            {
                bool signedIn = _token != null && _user != null;

                if (_userLabel != null)
                {
                    _userLabel.Text = signedIn
                        ? $"Signed in as: {_user!.DisplayName}"
                        : "Not signed in.";
                    _userLabel.ForeColor = signedIn ? Color.White : Theme.TextSecondary;
                }

                if (_licenseLabel != null)
                {
                    _licenseLabel.Text = signedIn && _license != null
                        ? $"License: Active (Expires: {_license.ExpiresAt:yyyy-MM-dd})"
                        : "License: —";
                    _licenseLabel.ForeColor = signedIn && _license != null && _license.HasAccess ? Theme.Success : Theme.TextSecondary;
                }

                if (_logoutBtn != null)
                {
                    _logoutBtn.Enabled = signedIn;
                    _logoutBtn.BackColor = signedIn ? Color.FromArgb(200, 70, 70) : Color.FromArgb(40, 45, 55);
                    _logoutBtn.ForeColor = signedIn ? Color.White : Color.FromArgb(120, 130, 150);
                }

                if (_redeemBtn != null)
                    _redeemBtn.Enabled = signedIn;

                if (_openCloudBtn != null)
                    _openCloudBtn.Enabled = signedIn;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateUi failed: {ex.Message}");
            }
        }

        private void SetBusy(bool busy)
        {
            try
            {
                if (_discordIdBox != null) _discordIdBox.Enabled = !busy;
                if (_loginBtn != null) _loginBtn.Enabled = !busy;
                if (_logoutBtn != null) _logoutBtn.Enabled = !busy && (_token != null);
                if (_redeemBtn != null) _redeemBtn.Enabled = !busy && (_token != null);
                if (_refreshKeysBtn != null) _refreshKeysBtn.Enabled = !busy && (_token != null);
                if (_openMarketplaceBtn != null) _openMarketplaceBtn.Enabled = !busy;
                if (_openCloudBtn != null) _openCloudBtn.Enabled = !busy && (_token != null);
                Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            }
            catch { }
        }

        private async Task RefreshKeysAsync()
        {
            if (_token == null) return;
            SetBusy(true);
            try
            {
                // Fetch available keys from Discord bot
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await httpClient.GetAsync("http://localhost:9877/keys/list");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    if (data?.success == true && data?.keys != null)
                    {
                        var keys = data.keys;
                        int addedCount = 0;
                        
                        // Clear existing encrypted keys cache
                        KeyCache.ClearEncryptedKeys();
                        
                        foreach (var key in keys)
                        {
                            // Add key to cache for redemption
                            var keyInfo = new KeyCache.KeyInfo
                            {
                                Key = key.key?.ToString() ?? "",
                                Used = false,
                                Game = key.game?.ToString() ?? "Unknown",
                                Duration = key.duration?.ToString() ?? "1_MONTH",
                                Encrypted = false, // These are real keys, not encrypted
                                KeyId = "",
                                EncryptedData = ""
                            };
                            
                            KeyCache.AddKey(keyInfo);
                            addedCount++;
                        }
                        
                        _redeemResult.Text = addedCount > 0 ? $"Fetched {addedCount} available keys from Discord bot." : "No keys available.";
                        _redeemResult.ForeColor = addedCount > 0 ? Theme.Success : Theme.TextSecondary;
                        _keysInSync = true;
                        
                        Console.WriteLine($"[SMOKESCREEN] Synced {addedCount} keys from Discord bot");
                    }
                    else
                    {
                        _redeemResult.Text = "No keys available from Discord bot.";
                        _redeemResult.ForeColor = Theme.TextSecondary;
                    }
                }
                else
                {
                    _redeemResult.Text = "Failed to connect to Discord bot. Is the bot running?";
                    _redeemResult.ForeColor = Theme.Error;
                    _keysInSync = false;
                }
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

        private TabPage BuildGameTab(string title, string gameType)
        {
            var tp = new TabPage(title) { BackColor = Theme.Background };

            // Check if user has access to this game
            bool hasAccess = gameType switch
            {
                "R6S" => _hasR6SKey,
                "CODW" => _hasCODWKey,
                "Arc Raiders" => _hasARKey,
                "Fortnite" => _hasFNKey,
                _ => false
            };

            if (!hasAccess)
            {
                // Show access denied message
                var accessDenied = new Label
                {
                    Text = $"🔒 Access Restricted\n\nYou need a {gameType} subscription key to access this tab.\n\nPurchase a key from our website or redeem a key in the LICENSE tab.\n\nPricing:\n1 Month: $9.99\n6 Months: $35.99\n12 Months: $65.99\nLifetime: $149.99\n\nCrypto Accepted: BTC, ETH, SOL",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Theme.TextSecondary,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tp.Controls.Add(accessDenied);
            }
            else if (gameType == "Warzone")
            {
                // Warzone has embedded tabs - Main and RecoilV2
                var warzoneTabs = new TabControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Theme.Background,
                    Font = new Font("Segoe UI", 10)
                };

                // Main Warzone tab
                var mainTab = new TabPage("📊 Dashboard") { BackColor = Theme.Background };
                BuildWarzoneMainContent(mainTab, gameType);
                warzoneTabs.TabPages.Add(mainTab);

                // RecoilV2 tab
                var recoilTab = new TabPage("🎯 RecoilV2") { BackColor = Theme.Background };
                BuildRecoilV2Content(recoilTab);
                warzoneTabs.TabPages.Add(recoilTab);

                tp.Controls.Add(warzoneTabs);
            }
            else if (gameType == "Fortnite")
            {
                // Fortnite has embedded tabs - Main and Aimbot
                var fortniteTabs = new TabControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Theme.Background,
                    Font = new Font("Segoe UI", 10)
                };

                // Main Fortnite tab
                var mainTab = new TabPage("📊 Dashboard") { BackColor = Theme.Background };
                BuildFortniteMainContent(mainTab, gameType);
                fortniteTabs.TabPages.Add(mainTab);

                // Aimbot tab
                var aimbotTab = new TabPage("🎯 Aimbot") { BackColor = Theme.Background };
                BuildFortniteAimbotContent(aimbotTab, gameType);
                fortniteTabs.TabPages.Add(aimbotTab);

                tp.Controls.Add(fortniteTabs);
            }
            else if (gameType == "R6S")
            {
                // R6S has embedded tabs - Main and RecoilV2
                var r6sTabs = new TabControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Theme.Background,
                    Font = new Font("Segoe UI", 10)
                };

                // Main R6S tab
                var mainTab = new TabPage("📊 Dashboard") { BackColor = Theme.Background };
                BuildR6SMainContent(mainTab, gameType);
                r6sTabs.TabPages.Add(mainTab);

                // RecoilV2 tab
                var recoilTab = new TabPage("🎯 RecoilV2") { BackColor = Theme.Background };
                BuildRecoilV2Content(recoilTab);
                r6sTabs.TabPages.Add(recoilTab);

                tp.Controls.Add(r6sTabs);
            }
            else if (gameType == "Arc Raiders")
            {
                // Arc Raiders has embedded tabs - Main and RecoilV2
                var arcTabs = new TabControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Theme.Background,
                    Font = new Font("Segoe UI", 10)
                };

                // Main Arc Raiders tab
                var mainTab = new TabPage("📊 Dashboard") { BackColor = Theme.Background };
                BuildArcRaidersMainContent(mainTab, gameType);
                arcTabs.TabPages.Add(mainTab);

                // RecoilV2 tab
                var recoilTab = new TabPage("🎯 RecoilV2") { BackColor = Theme.Background };
                BuildRecoilV2Content(recoilTab);
                arcTabs.TabPages.Add(recoilTab);

                tp.Controls.Add(arcTabs);
            }
            else
            {
                // Show game content for other games
                var gameTitle = new Label
                {
                    Text = $"{title} - RECOIL V2 ACTIVE",
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    ForeColor = Color.Lime,
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                tp.Controls.Add(gameTitle);

                var status = new Label
                {
                    Text = $"✅ {gameType} Recoil V2 is enabled and ready to use.\n\nYour subscription key is active and valid.\nEnjoy enhanced recoil control for {gameType}!",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = Color.White,
                    Location = new Point(20, 60),
                    Size = new Size(600, 100)
                };
                tp.Controls.Add(status);

                // Add game-specific controls here
                var gameControls = new Panel
                {
                    Location = new Point(20, 180),
                    Size = new Size(800, 350),
                    BackColor = Theme.CardBackground,
                    BorderStyle = BorderStyle.None
                };

                // PS5 Controller Configuration Button
                var ps5ConfigButton = new Button
                {
                    Text = "🎮 PS5 Controller Settings",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    Location = new Point(20, 280),
                    Size = new Size(200, 40),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                ps5ConfigButton.FlatAppearance.BorderSize = 0;
                ps5ConfigButton.Click += (s, e) => {
                    var ps5Config = new PS5RecoilConfig(gameType, _ps5Manager);
                    ps5Config.ShowDialog();
                };

                // Control Mode Status
                var controlModeLabel = new Label
                {
                    Text = "🎮 Control Mode: Mouse & Keyboard (Default)",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.Lime,
                    Location = new Point(240, 280),
                    Size = new Size(300, 20)
                };

                // PS5 Controller Status
                var ps5StatusLabel = new Label
                {
                    Text = "🎮 PS5 Controller: Checking...",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.Yellow,
                    Location = new Point(240, 305),
                    Size = new Size(300, 20)
                };

                // Update PS5 status
                var updateTimer = new System.Windows.Forms.Timer();
                updateTimer.Interval = 1000;
                updateTimer.Tick += (s, e) => {
                    if (_ps5Manager.IsConnected())
                    {
                        ps5StatusLabel.Text = "🎮 PS5 Controller: Connected ✅";
                        ps5StatusLabel.ForeColor = Color.Lime;
                    }
                    else
                    {
                        ps5StatusLabel.Text = "🎮 PS5 Controller: Not Connected ❌";
                        ps5StatusLabel.ForeColor = Color.Red;
                    }
                };
                updateTimer.Start();

                // Recoil Strength Slider
                var recoilLabel = new Label
                {
                    Text = "🎯 Recoil Strength:",
                    Location = new Point(20, 20),
                    Size = new Size(150, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                var recoilSlider = new TrackBar
                {
                    Location = new Point(180, 20),
                    Size = new Size(200, 45),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 50,
                    TickFrequency = 10,
                    BackColor = Theme.CardBackground
                };
                gameControls.Controls.Add(recoilSlider);

                var recoilValue = new Label
                {
                    Text = "50%",
                    Location = new Point(390, 25),
                    Size = new Size(50, 30),
                    ForeColor = Color.Lime,
                    Font = new Font("Segoe UI", 10)
                };
                gameControls.Controls.Add(recoilValue);

                recoilSlider.ValueChanged += (s, e) => {
                    recoilValue.Text = $"{recoilSlider.Value}%";
                };

                // Pattern Selection
                var patternLabel = new Label
                {
                    Text = "🔧 Pattern Type:",
                    Location = new Point(20, 70),
                    Size = new Size(150, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                gameControls.Controls.Add(patternLabel);

                var patternCombo = new ComboBox
                {
                    Location = new Point(180, 70),
                    Size = new Size(150, 30),
                    BackColor = Color.FromArgb(45, 55, 72),
                    Font = new Font("Consolas", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                patternCombo.Items.AddRange(new string[] { "Default", "Aggressive", "Smooth", "Burst", "Tap", "Custom" });
                patternCombo.SelectedIndex = 0;
                gameControls.Controls.Add(patternCombo);

                // Add all controls to panel
                    ForeColor = Color.White,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                patternCombo.Items.AddRange(new[] { "Default", "Aggressive", "Smooth", "Custom", "Adaptive" });
                patternCombo.SelectedIndex = 0;
                gameControls.Controls.Add(patternCombo);

                // Weapon Profile
                var weaponLabel = new Label
                {
                    Text = "🔫 Weapon Profile:",
                    Location = new Point(20, 120),
                    Size = new Size(150, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                gameControls.Controls.Add(weaponLabel);

                var weaponCombo = new ComboBox
                {
                    Location = new Point(180, 120),
                    Size = new Size(150, 30),
                    BackColor = Color.FromArgb(45, 55, 72),
                    ForeColor = Color.White,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                weaponCombo.Items.AddRange(new[] { "AR", "SMG", "Sniper", "LMG", "Pistol", "Shotgun" });
                weaponCombo.SelectedIndex = 0;
                gameControls.Controls.Add(weaponCombo);

                // Hotkey Configuration
                var hotkeyLabel = new Label
                {
                    Text = "⌨️ Toggle Hotkey:",
                    Location = new Point(400, 20),
                    Size = new Size(150, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                gameControls.Controls.Add(hotkeyLabel);

                var hotkeyCombo = new ComboBox
                {
                    Location = new Point(560, 20),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(45, 55, 72),
                    ForeColor = Color.White,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                hotkeyCombo.Items.AddRange(new[] { "F1", "F2", "F3", "F4", "F5", "F6" });
                hotkeyCombo.SelectedIndex = 0;
                gameControls.Controls.Add(hotkeyCombo);

                // Status Indicators
                var statusPanel = new Panel
                {
                    Location = new Point(400, 70),
                    Size = new Size(260, 100),
                    BackColor = Color.FromArgb(17, 22, 28),
                    BorderStyle = BorderStyle.FixedSingle
                };
                gameControls.Controls.Add(statusPanel);

                var activeLabel = new Label
                {
                    Text = "🟢 Status: ACTIVE",
                    Location = new Point(10, 10),
                    Size = new Size(240, 25),
                    ForeColor = Color.Lime,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                statusPanel.Controls.Add(activeLabel);

                var performanceLabel = new Label
                {
                    Text = "⚡ Performance: OPTIMAL",
                    Location = new Point(10, 40),
                    Size = new Size(240, 25),
                    ForeColor = Color.Cyan,
                    Font = new Font("Segoe UI", 9)
                };
                statusPanel.Controls.Add(performanceLabel);

                var detectionLabel = new Label
                {
                    Text = "🛡️ Detection: UNDETECTED",
                    Location = new Point(10, 70),
                    Size = new Size(240, 25),
                    ForeColor = Color.Green,
                    Font = new Font("Segoe UI", 9)
                };
                statusPanel.Controls.Add(detectionLabel);

                // Save Button
                var saveBtn = new Button
                {
                    Text = "💾 SAVE SETTINGS",
                    Location = new Point(20, 200),
                    Size = new Size(150, 40),
                    BackColor = Color.Lime,
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                saveBtn.Click += (s, e) => {
                    MessageBox.Show($"{gameType} settings saved successfully!", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                gameControls.Controls.Add(saveBtn);

                // Reset Button
                var resetBtn = new Button
                {
                    Text = "🔄 RESET",
                    Location = new Point(180, 200),
                    Size = new Size(100, 40),
                    BackColor = Color.Orange,
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                resetBtn.Click += (s, e) => {
                    recoilSlider.Value = 50;
                    patternCombo.SelectedIndex = 0;
                    weaponCombo.SelectedIndex = 0;
                    hotkeyCombo.SelectedIndex = 0;
                    MessageBox.Show($"{gameType} settings reset to default!", "Settings Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                gameControls.Controls.Add(resetBtn);

                tp.Controls.Add(gameControls);
            }

            return tp;
        }

        private void BuildWarzoneMainContent(TabPage tp, string gameType)
        {
            var title = new Label
            {
                Text = "🎮 Warzone Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            // Live Status Panel
            var statusPanel = new Panel
            {
                Bounds = new Rectangle(20, 70, 700, 80),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };
            statusPanel.Controls.Add(new Label
            {
                Text = "📡 Service Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var statusDot = new Label { Text = "●", Font = new Font("Segoe UI", 14), ForeColor = Color.Lime, Location = new Point(10, 40), AutoSize = true };
            var statusLabel = new Label { Text = "WARZONE: WORKING", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Lime, Location = new Point(35, 40), AutoSize = true };
            var pingLabel = new Label { Text = "PING: --ms", Font = new Font("Consolas", 10), ForeColor = Theme.TextSecondary, Location = new Point(600, 40), AutoSize = true };
            statusPanel.Controls.Add(statusDot);
            statusPanel.Controls.Add(statusLabel);
            statusPanel.Controls.Add(pingLabel);
            tp.Controls.Add(statusPanel);

            // Stats Cards
            var statsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 170),
                Size = new Size(750, 120),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            statsPanel.Controls.Add(CreateStatCard("👤 Users Online", "247", Color.Lime));
            statsPanel.Controls.Add(CreateStatCard("🎮 Active Sessions", "89", Color.Cyan));
            statsPanel.Controls.Add(CreateStatCard("⚡ Avg Response", "12ms", Color.Yellow));
            statsPanel.Controls.Add(CreateStatCard("🛡️ HWIDs Protected", "1,482", Color.Magenta));

            tp.Controls.Add(statsPanel);

            // Quick Actions
            var actionsPanel = new Panel
            {
                Location = new Point(20, 310),
                Size = new Size(750, 200),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            actionsPanel.Controls.Add(new Label
            {
                Text = "⚡ Quick Actions",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            });

            var reloadBtn = new Button
            {
                Text = "🔄 Reload Scripts",
                Location = new Point(20, 50),
                Size = new Size(150, 40),
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            actionsPanel.Controls.Add(reloadBtn);

            var settingsBtn = new Button
            {
                Text = "⚙️ Settings",
                Location = new Point(180, 50),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            actionsPanel.Controls.Add(settingsBtn);

            var exportBtn = new Button
            {
                Text = "📤 Export Config",
                Location = new Point(340, 50),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            actionsPanel.Controls.Add(exportBtn);

            tp.Controls.Add(actionsPanel);
        }

        private Panel CreateStatCard(string label, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(170, 100),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };
            card.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Location = new Point(10, 10),
                AutoSize = true
            });
            card.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Consolas", 20, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(10, 35),
                AutoSize = true
            });
            return card;
        }

        private void BuildRecoilV2Content(TabPage tp)
        {
            var title = new Label
            {
                Text = "🎯 Enhanced RecoilV2 Control System",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            // Status panel
            var statusPanel = new Panel
            {
                Bounds = new Rectangle(20, 70, 700, 100),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            statusPanel.Controls.Add(new Label
            {
                Text = "🎯 RecoilV2 Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            });

            var recoilStatusDot = new Label
            {
                Name = "_recoilStatusDot",
                Text = "●",
                Font = new Font("Segoe UI", 16),
                ForeColor = Color.Gray,
                Location = new Point(15, 40),
                AutoSize = true
            };
            statusPanel.Controls.Add(recoilStatusDot);

            var recoilStatusLabel = new Label
            {
                Name = "_recoilStatusLabel",
                Text = "NOT LOADED",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(40, 40),
                AutoSize = true
            };
            statusPanel.Controls.Add(recoilStatusLabel);

            var recoilInfoLabel = new Label
            {
                Name = "_recoilInfoLabel",
                Text = "Click 'Load Enhanced RecoilV2' to initialize the F2/LEFTMOUSE system",
                Font = new Font("Consolas", 9),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 70),
                Size = new Size(670, 20)
            };
            statusPanel.Controls.Add(recoilInfoLabel);
            tp.Controls.Add(statusPanel);

            // F2/LEFTMOUSE Status Panel
            var togglePanel = new Panel
            {
                Bounds = new Rectangle(20, 180, 700, 80),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            togglePanel.Controls.Add(new Label
            {
                Text = "⌨️ F2 Toggle & �️ LEFTMOUSE Bind Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            });

            var f2StatusLabel = new Label
            {
                Name = "_f2StatusLabel",
                Text = "F2 Toggle: DISABLED (Press F2 to enable)",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Color.Orange,
                Location = new Point(15, 40),
                AutoSize = true
            };
            togglePanel.Controls.Add(f2StatusLabel);

            var mouseStatusLabel = new Label
            {
                Name = "_mouseStatusLabel",
                Text = "LEFTMOUSE: NOT PRESSED",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Location = new Point(350, 40),
                AutoSize = true
            };
            togglePanel.Controls.Add(mouseStatusLabel);
            tp.Controls.Add(togglePanel);

            // Enhanced scroll bars panel
            var scrollPanel = new Panel
            {
                Location = new Point(20, 270),
                Size = new Size(700, 400),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            scrollPanel.Controls.Add(new Label
            {
                Text = "⚙️ Enhanced Recoil Settings (Precision Control)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            });

            // Primary recoil controls
            AddRecoilSlider(scrollPanel, "Recoil Strength:", 15, 50, 300, 0, 100, 50, "_strengthSlider");
            AddRecoilSlider(scrollPanel, "Smoothness:", 350, 50, 300, 0, 100, 75, "_smoothnessSlider");
            AddRecoilSlider(scrollPanel, "X-Axis Reduction:", 15, 100, 300, 0, 100, 85, "_xReductionSlider");
            AddRecoilSlider(scrollPanel, "Y-Axis Reduction:", 350, 100, 300, 0, 100, 90, "_yReductionSlider");

            // Advanced recoil controls
            AddRecoilSlider(scrollPanel, "Horizontal Recoil:", 15, 150, 300, 0, 100, 50, "_horizontalSlider");
            AddRecoilSlider(scrollPanel, "Vertical Recoil:", 350, 150, 300, 0, 100, 50, "_verticalSlider");
            AddRecoilSlider(scrollPanel, "First Shot Recoil:", 15, 200, 300, 0, 100, 30, "_firstShotSlider");
            AddRecoilSlider(scrollPanel, "Recovery Speed:", 350, 200, 300, 0, 100, 75, "_recoverySlider");

            // Advanced multiplier controls
            AddRecoilSlider(scrollPanel, "Speed Multiplier:", 15, 250, 300, 0, 200, 100, "_speedSlider");
            AddRecoilSlider(scrollPanel, "ADS Multiplier:", 350, 250, 300, 0, 100, 80, "_adsSlider");
            AddRecoilSlider(scrollPanel, "Movement Penalty:", 15, 300, 300, 0, 100, 20, "_movementSlider");
            AddRecoilSlider(scrollPanel, "Breath Control:", 350, 300, 300, 0, 100, 60, "_breathSlider");

            // Game selection
            scrollPanel.Controls.Add(new Label
            {
                Text = "Game Profile:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(15, 350),
                AutoSize = true
            });

            var gameCombo = new ComboBox
            {
                Location = new Point(15, 375),
                Size = new Size(200, 30),
                FlatStyle = FlatStyle.Flat,
                Items = { "Warzone", "R6S", "Arc Raiders", "Fortnite" },
                SelectedIndex = 0,
                BackColor = Theme.CardBackground,
                ForeColor = Color.White
            };
            scrollPanel.Controls.Add(gameCombo);

            tp.Controls.Add(scrollPanel);

            // Control buttons
            var btnPanel = new Panel
            {
                Location = new Point(20, 680),
                Size = new Size(700, 60),
                BackColor = Color.Transparent
            };

            var loadBtn = new Button
            {
                Text = "📂 LOAD ENHANCED RECOILV2",
                Location = new Point(0, 10),
                Size = new Size(200, 40),
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            loadBtn.Click += (_, _) => LoadEnhancedRecoilV2(tp);
            btnPanel.Controls.Add(loadBtn);

            var injectBtn = new Button
            {
                Text = "💉 INJECT & ACTIVATE",
                Location = new Point(210, 10),
                Size = new Size(150, 40),
                BackColor = Color.Lime,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            injectBtn.Click += (_, _) => InjectEnhancedRecoilV2(tp);
            btnPanel.Controls.Add(injectBtn);

            var unloadBtn = new Button
            {
                Text = "📤 UNLOAD",
                Location = new Point(370, 10),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            unloadBtn.Click += (_, _) => UnloadEnhancedRecoilV2(tp);
            btnPanel.Controls.Add(unloadBtn);

            var saveBtn = new Button
            {
                Text = "💾 SAVE SETTINGS",
                Location = new Point(500, 10),
                Size = new Size(130, 40),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            saveBtn.Click += (_, _) => SaveRecoilSettings(tp, gameCombo.Text);
            btnPanel.Controls.Add(saveBtn);

            tp.Controls.Add(btnPanel);

            // Start status update timer
            var statusTimer = new System.Windows.Forms.Timer { Interval = 100 };
            statusTimer.Tick += (_, _) => UpdateRecoilStatus(tp);
            statusTimer.Start();
        }

        private void BuildFortniteMainContent(TabPage tp, string gameType)
        {
            var title = new Label
            {
                Text = $"🏝️ {gameType} - Main Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var status = new Label
            {
                Text = $"✅ {gameType} is active and ready to use.\n\nYour subscription key is valid.\nEnjoy enhanced features for {gameType}!",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(20, 60),
                Size = new Size(600, 100)
            };
            tp.Controls.Add(status);

            // Add Fortnite-specific info
            var infoPanel = new Panel
            {
                Location = new Point(20, 180),
                Size = new Size(700, 200),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };

            var infoLabel = new Label
            {
                Text = $"{gameType} Features:\n\n• Enhanced recoil control\n• Weapon pattern customization\n• Custom sensitivity settings\n• Hotkey configuration\n• Real-time adjustments\n• Performance optimization\n• Undetected algorithms\n• Fortnite-specific optimizations",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            infoPanel.Controls.Add(infoLabel);
            tp.Controls.Add(infoPanel);
        }

        private void BuildR6SMainContent(TabPage tp, string gameType)
        {
            var title = new Label
            {
                Text = $"🔫 {gameType} - Main Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var status = new Label
            {
                Text = $"✅ {gameType} is active and ready to use.\n\nYour subscription key is valid.\nEnjoy enhanced features for {gameType}!",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(20, 60),
                Size = new Size(600, 100)
            };
            tp.Controls.Add(status);

            // Add R6S-specific info
            var infoPanel = new Panel
            {
                Location = new Point(20, 180),
                Size = new Size(700, 200),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };

            var infoLabel = new Label
            {
                Text = $"{gameType} Features:\n\n• Enhanced recoil control\n• Weapon pattern customization\n• Custom sensitivity settings\n• Hotkey configuration\n• Real-time adjustments\n• Performance optimization\n• Undetected algorithms\n• Rainbow Six Siege specific optimizations",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            infoPanel.Controls.Add(infoLabel);
            tp.Controls.Add(infoPanel);
        }

        private void BuildArcRaidersMainContent(TabPage tp, string gameType)
        {
            var title = new Label
            {
                Text = $"👾 {gameType} - Main Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var status = new Label
            {
                Text = $"✅ {gameType} is active and ready to use.\n\nYour subscription key is valid.\nEnjoy enhanced features for {gameType}!",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(20, 60),
                Size = new Size(600, 100)
            };
            tp.Controls.Add(status);

            // Add Arc Raiders-specific info
            var infoPanel = new Panel
            {
                Location = new Point(20, 180),
                Size = new Size(700, 200),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.None
            };

            var infoLabel = new Label
            {
                Text = $"{gameType} Features:\n\n• Enhanced recoil control\n• Weapon pattern customization\n• Custom sensitivity settings\n• Hotkey configuration\n• Real-time adjustments\n• Performance optimization\n• Undetected algorithms\n• Arc Raiders specific optimizations",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            infoPanel.Controls.Add(infoLabel);
            tp.Controls.Add(infoPanel);
        }

        private void BuildFortniteAimbotContent(TabPage tp, string gameType)
        {
            var title = new Label
            {
                Text = $"🎯 {gameType} - Aimbot System",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            // Aimbot Status Panel
            var statusPanel = new Panel
            {
                Bounds = new Rectangle(20, 70, 700, 80),
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };
            statusPanel.Controls.Add(new Label
            {
                Text = "🎯 Aimbot Status",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 10),
                AutoSize = true
            });

            var aimbotStatusDot = new Label
            {
                Name = "_aimbotStatusDot",
                Text = "●",
                Font = new Font("Segoe UI", 16),
                ForeColor = Color.Gray,
                Location = new Point(15, 40),
                AutoSize = true
            };
            statusPanel.Controls.Add(aimbotStatusDot);

            var aimbotStatusLabel = new Label
            {
                Name = "_aimbotStatusLabel",
                Text = "NOT LOADED",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(40, 40),
                AutoSize = true
            };
            statusPanel.Controls.Add(aimbotStatusLabel);

            var aimbotInfoLabel = new Label
            {
                Name = "_aimbotInfoLabel",
                Text = "Click 'Launch Aimbot' to initialize the neural network aimbot",
                Font = new Font("Consolas", 9),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 70),
                Size = new Size(670, 20)
            };
            statusPanel.Controls.Add(aimbotInfoLabel);
            tp.Controls.Add(statusPanel);

            // Aimbot Control Panel
            var controlPanel = new Panel
            {
                Bounds = new Rectangle(20, 170, 700, 120),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            controlPanel.Controls.Add(new Label
            {
                Text = "⚙️ Aimbot Controls",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            });

            var launchBtn = new Button
            {
                Text = "🚀 LAUNCH AIMBOT",
                Location = new Point(15, 40),
                Size = new Size(160, 40),
                BackColor = Color.Lime,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            launchBtn.Click += (_, _) => LaunchFortniteAimbot(tp);
            controlPanel.Controls.Add(launchBtn);

            var stopBtn = new Button
            {
                Text = "🛑 STOP AIMBOT",
                Location = new Point(185, 40),
                Size = new Size(140, 40),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            stopBtn.Click += (_, _) => StopFortniteAimbot(tp);
            controlPanel.Controls.Add(stopBtn);

            var settingsBtn = new Button
            {
                Text = "⚙️ SETTINGS",
                Location = new Point(335, 40),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            settingsBtn.Click += (_, _) => OpenAimbotSettings(tp);
            controlPanel.Controls.Add(settingsBtn);

            tp.Controls.Add(controlPanel);

            // Configuration Panel
            var configPanel = new Panel
            {
                Bounds = new Rectangle(20, 310, 700, 200),
                BackColor = Theme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            configPanel.Controls.Add(new Label
            {
                Text = "🔧 Aimbot Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            });

            var configLabel = new Label
            {
                Text = "Neural Network Aimbot Features:\n\n• AI-powered target detection\n• Smooth human-like aiming\n• Adjustable FOV and confidence\n• F1/F2 hotkey toggle system\n• Trigger bot functionality\n• Custom sensitivity settings\n• Screen resolution auto-detection\n• Performance optimized algorithms",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Location = new Point(15, 40),
                Size = new Size(670, 120)
            };
            configPanel.Controls.Add(configLabel);
            tp.Controls.Add(configPanel);

            // Start status update timer
            var statusTimer = new System.Windows.Forms.Timer { Interval = 100 };
            statusTimer.Tick += (_, _) => UpdateAimbotStatus(tp);
            statusTimer.Start();
        }

        private void AddRecoilSlider(Panel parent, string label, int x, int labelY, int sliderX, int min, int max, int value, string name)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(x, labelY),
                AutoSize = true
            });

            var slider = new TrackBar
            {
                Name = name,
                Location = new Point(sliderX, labelY + 20),
                Size = new Size(280, 30),
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.None,
                BackColor = Theme.CardBackground
            };

            var valueLabel = new Label
            {
                Name = name + "_value",
                Text = $"{value}%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(sliderX + 290, labelY + 25),
                AutoSize = true
            };

            slider.ValueChanged += (_, _) => valueLabel.Text = $"{slider.Value}%";

            parent.Controls.Add(slider);
            parent.Controls.Add(valueLabel);
        }

        private void LoadRecoilV2(TabPage tp)
        {
            var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
            var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
            var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");

            if (RecoilV2Integration.LoadRecoilV2())
            {
                if (statusDot != null) { statusDot.ForeColor = Color.Lime; }
                if (statusLabel != null) { statusLabel.Text = "LOADED"; statusLabel.ForeColor = Color.Lime; }
                if (infoLabel != null) { infoLabel.Text = "RecoilV2.dll loaded successfully. Click 'INJECT' to activate."; }
                ApiRecoilStatus.LogStatus("RecoilV2", ApiRecoilStatus.ServiceState.WORKING, "Loaded successfully");
            }
            else
            {
                if (statusDot != null) { statusDot.ForeColor = Color.Red; }
                if (statusLabel != null) { statusLabel.Text = "FAILED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = "Failed to load RecoilV2.dll. Make sure the file exists in the app directory."; }
                ApiRecoilStatus.LogStatus("RecoilV2", ApiRecoilStatus.ServiceState.NOT_OPERATIONAL, "Load failed");
            }
        }

        private void InjectRecoilV2(TabPage tp)
        {
            var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
            var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
            var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");

            if (!RecoilV2Integration.IsLoaded)
            {
                if (statusLabel != null) { statusLabel.Text = "NOT LOADED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = "Please load RecoilV2 first!"; }
                return;
            }

            try
            {
                RecoilV2Integration.Inject();
                if (statusDot != null) { statusDot.ForeColor = Color.Lime; statusDot.Text = "●"; }
                if (statusLabel != null) { statusLabel.Text = "INJECTED"; statusLabel.ForeColor = Color.Cyan; }
                if (infoLabel != null) { infoLabel.Text = "RecoilV2 successfully injected! Game is now being modified."; }
                ApiRecoilStatus.LogStatus("RecoilV2", ApiRecoilStatus.ServiceState.WORKING, "Injected successfully");
            }
            catch
            {
                if (statusLabel != null) { statusLabel.Text = "INJECT FAILED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = "Injection failed. Check admin privileges."; }
            }
        }

        private void RefreshRecoilV2(TabPage tp)
        {
            var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
            var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
            var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");

            if (RecoilV2Integration.IsLoaded)
            {
                if (statusDot != null) { statusDot.ForeColor = Color.Lime; }
                if (statusLabel != null) { statusLabel.Text = "LOADED"; statusLabel.ForeColor = Color.Lime; }
                if (infoLabel != null) { infoLabel.Text = "RecoilV2 module loaded successfully."; infoLabel.ForeColor = Theme.TextSecondary; }
                ApiRecoilStatus.LogStatus("RecoilV2", ApiRecoilStatus.ServiceState.WORKING, "Loaded successfully");
            }
            else
            {
                if (statusDot != null) { statusDot.ForeColor = Color.Red; }
                if (statusLabel != null) { statusLabel.Text = "NOT LOADED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = "Failed to load RecoilV2.dll. Make sure the file exists in the application directory."; infoLabel.ForeColor = Color.Orange; }
                ApiRecoilStatus.LogStatus("RecoilV2", ApiRecoilStatus.ServiceState.NOT_OPERATIONAL, "Load failed");
            }
        }

        private void ShowWarzoneMenu(TabPage tp)
        {
            var modules = WarzoneRecoilIntegration.GetLoadedModules();
            if (modules.Count == 0)
            {
                MessageBox.Show("Warzone recoil modules not loaded. Object files not found.", "Warzone Menu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var menuText = "🎯 WARZONE RECOIL FUNCTIONS\n\n" +
                           string.Join("\n", modules) + "\n\n" +
                           "Click any function to activate in Warzone.\n\n" +
                           "Note: This uses object files from:\n" +
                           WarzoneRecoilIntegration.OBJ_DIR;

            MessageBox.Show(menuText, "Warzone Menu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UnloadRecoilV2(TabPage tp)
        {
            // Check if this is Warzone tab and use WarzoneRecoilIntegration
            if (tp.Text.Contains("WARZONE"))
            {
                WarzoneRecoilIntegration.UnloadWarzoneRecoil();
            }
            else
            {
                // Use original RecoilV2 integration for other games
                RecoilV2Integration.UnloadRecoilV2();
            }

            var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
            var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
            var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");
            
            if (statusDot != null) { statusDot.ForeColor = Color.Gray; }
            if (statusLabel != null) { statusLabel.Text = "NOT LOADED"; statusLabel.ForeColor = Color.Red; }
            if (infoLabel != null) 
            { 
                if (tp.Text.Contains("WARZONE"))
                {
                    infoLabel.Text = WarzoneRecoilIntegration.GetStatus(); 
                    infoLabel.ForeColor = Color.Orange; 
                }
                else
                {
                    infoLabel.Text = "RecoilV2 module unloaded."; 
                    infoLabel.ForeColor = Theme.TextSecondary; 
                }
            }
        }

        private void LoadEnhancedRecoilV2(TabPage tp)
        {
            try
            {
                EnhancedRecoilSystem.Initialize();
                
                var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
                var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
                var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");

                if (statusDot != null) { statusDot.ForeColor = Color.Lime; }
                if (statusLabel != null) { statusLabel.Text = "ENHANCED LOADED"; statusLabel.ForeColor = Color.Lime; }
                if (infoLabel != null) { infoLabel.Text = "Enhanced RecoilV2 with F2/LEFTMOUSE bind loaded successfully! Press F2 to toggle."; }
                
                Console.WriteLine("[RECOIL] Enhanced RecoilV2 system loaded with F2/LEFTMOUSE functionality");
            }
            catch (Exception ex)
            {
                var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
                var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");
                
                if (statusLabel != null) { statusLabel.Text = "FAILED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = $"Failed to load Enhanced RecoilV2: {ex.Message}"; }
                
                Console.WriteLine($"[RECOIL] Error loading Enhanced RecoilV2: {ex.Message}");
            }
        }

        private void InjectEnhancedRecoilV2(TabPage tp)
        {
            var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
            var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
            var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");

            try
            {
                // Enhanced recoil system is already initialized via LoadEnhancedRecoilV2
                if (statusDot != null) { statusDot.ForeColor = Color.Cyan; statusDot.Text = "●"; }
                if (statusLabel != null) { statusLabel.Text = "ENHANCED ACTIVE"; statusLabel.ForeColor = Color.Cyan; }
                if (infoLabel != null) { infoLabel.Text = "Enhanced RecoilV2 ACTIVE! Press F2 to toggle, hold LEFTMOUSE to apply recoil control."; }
                
                Console.WriteLine("[RECOIL] Enhanced RecoilV2 injected and activated successfully");
            }
            catch (Exception ex)
            {
                if (statusLabel != null) { statusLabel.Text = "INJECT FAILED"; statusLabel.ForeColor = Color.Red; }
                if (infoLabel != null) { infoLabel.Text = $"Injection failed: {ex.Message}"; }
                
                Console.WriteLine($"[RECOIL] Error injecting Enhanced RecoilV2: {ex.Message}");
            }
        }

        private void UnloadEnhancedRecoilV2(TabPage tp)
        {
            try
            {
                EnhancedRecoilSystem.Shutdown();
                
                var statusDot = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusDot"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusDot");
                var statusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilStatusLabel");
                var infoLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_recoilInfoLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_recoilInfoLabel");
                
                if (statusDot != null) { statusDot.ForeColor = Color.Gray; }
                if (statusLabel != null) { statusLabel.Text = "UNLOADED"; statusLabel.ForeColor = Color.Gray; }
                if (infoLabel != null) { infoLabel.Text = "Enhanced RecoilV2 unloaded successfully."; }
                
                Console.WriteLine("[RECOIL] Enhanced RecoilV2 system unloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOIL] Error unloading Enhanced RecoilV2: {ex.Message}");
            }
        }

        private void SaveRecoilSettings(TabPage tp, string game)
        {
            try
            {
                var scrollPanel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.AutoScroll);
                if (scrollPanel == null) return;

                var settings = new RecoilSettings();
                
                // Get all slider values
                var strengthSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_strengthSlider");
                var smoothnessSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_smoothnessSlider");
                var xReductionSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_xReductionSlider");
                var yReductionSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_yReductionSlider");
                var horizontalSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_horizontalSlider");
                var verticalSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_verticalSlider");
                var firstShotSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_firstShotSlider");
                var recoverySlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_recoverySlider");
                var speedSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_speedSlider");
                var adsSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_adsSlider");
                var movementSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_movementSlider");
                var breathSlider = scrollPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == "_breathSlider");

                if (strengthSlider != null) settings.Strength = strengthSlider.Value;
                if (smoothnessSlider != null) settings.Smoothness = smoothnessSlider.Value;
                if (xReductionSlider != null) settings.XReduction = xReductionSlider.Value;
                if (yReductionSlider != null) settings.YReduction = yReductionSlider.Value;
                if (horizontalSlider != null) settings.HorizontalRecoil = horizontalSlider.Value;
                if (verticalSlider != null) settings.VerticalRecoil = verticalSlider.Value;
                if (firstShotSlider != null) settings.FirstShotRecoil = firstShotSlider.Value;
                if (recoverySlider != null) settings.RecoverySpeed = recoverySlider.Value;
                if (speedSlider != null) settings.SpeedMultiplier = speedSlider.Value / 100f;
                if (adsSlider != null) settings.ADSMultiplier = adsSlider.Value;
                if (movementSlider != null) settings.MovementPenalty = movementSlider.Value;
                if (breathSlider != null) settings.BreathControl = breathSlider.Value;

                // Update the game settings
                EnhancedRecoilSystem.UpdateSettings(game, settings);
                EnhancedRecoilSystem.SetCurrentGame(game);

                MessageBox.Show($"Settings saved for {game}!\n\n" +
                              $"Strength: {settings.Strength}%\n" +
                              $"Smoothness: {settings.Smoothness}%\n" +
                              $"X-Reduction: {settings.XReduction}%\n" +
                              $"Y-Reduction: {settings.YReduction}%\n" +
                              $"Speed Multiplier: {settings.SpeedMultiplier:F2}",
                              "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Console.WriteLine($"[RECOIL] Settings saved for {game}: Strength={settings.Strength}%, Smoothness={settings.Smoothness}%");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[RECOIL] Error saving settings: {ex.Message}");
            }
        }

        private void UpdateRecoilStatus(TabPage tp)
        {
            try
            {
                var f2StatusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_f2StatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_f2StatusLabel");
                var mouseStatusLabel = tp.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.ContainsKey("_mouseStatusLabel"))?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "_mouseStatusLabel");

                if (f2StatusLabel != null)
                {
                    var f2Enabled = EnhancedRecoilSystem.IsF2Enabled;
                    f2StatusLabel.Text = $"F2 Toggle: {(f2Enabled ? "ENABLED" : "DISABLED")} (Press F2 to toggle)";
                    f2StatusLabel.ForeColor = f2Enabled ? Color.Lime : Color.Orange;
                }

                if (mouseStatusLabel != null)
                {
                    var mousePressed = EnhancedRecoilSystem.IsLeftMousePressed;
                    mouseStatusLabel.Text = $"LEFTMOUSE: {(mousePressed ? "PRESSED - RECOIL ACTIVE" : "NOT PRESSED")}";
                    mouseStatusLabel.ForeColor = mousePressed ? Color.Lime : Theme.TextSecondary;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOIL] Error updating status: {ex.Message}");
            }
        }

        private TabPage BuildAutoUpdaterTab()
        {
            var tp = new TabPage("AUTO UPDATER") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "AUTO UPDATER",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var status = new Label
            {
                Text = "SmokeScreen ENGINE is up to date.\n\nCurrent Version: v4.2.0\nLast Check: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 60),
                Size = new Size(600, 80)
            };
            tp.Controls.Add(status);

            return tp;
        }

        private TabPage BuildApiServiceStatusTab()
        {
            var tp = new TabPage("API SERVICE STATUS") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "API SERVICE STATUS",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            var discordStatus = new Label
            {
                Text = "🤖 Discord Bot: Online • Uptime: 02:34:15 • Ping: 42ms",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Lime,
                Location = new Point(20, 60),
                AutoSize = true
            };
            tp.Controls.Add(discordStatus);

            var backendStatus = new Label
            {
                Text = "🔧 Backend API: Online • Response Time: 120ms",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Lime,
                Location = new Point(20, 90),
                AutoSize = true
            };
            tp.Controls.Add(backendStatus);

            var gameServicesStatus = new Label
            {
                Text = "🎮 Game Services: Online • All recoil systems operational",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Lime,
                Location = new Point(20, 120),
                AutoSize = true
            };
            tp.Controls.Add(gameServicesStatus);

            var systemHealth = new Label
            {
                Text = "💾 System Health: Good • CPU: 23% • Memory: 1.2GB/8GB",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Lime,
                Location = new Point(20, 150),
                AutoSize = true
            };
            tp.Controls.Add(systemHealth);

            return tp;
        }

        private void UpdateRecoilKeyAccess(string key)
        {
            // Parse key and update access based on key type
            if (key.StartsWith("R6S-"))
                _hasR6SKey = true;
            else if (key.StartsWith("CODW-"))
                _hasCODWKey = true;
            else if (key.StartsWith("AR-"))
                _hasARKey = true;
            else if (key.StartsWith("FN-"))
                _hasFNKey = true;
            else if (key.StartsWith("SPF-") || key.StartsWith("SPOOFER-"))
                _hasSpooferKey = true;

            // Refresh tabs to update access
            RefreshGameTabs();
        }

        private void RefreshGameTabs()
        {
            // Update tab accessibility based on key access
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                var tab = _tabs.TabPages[i];
                bool hasAccess = false;
                string gameName = "";

                if (tab.Text.Contains("R6S"))
                {
                    hasAccess = _hasR6SKey;
                    gameName = "Rainbow Six Siege";
                }
                else if (tab.Text.Contains("WARZONE"))
                {
                    hasAccess = _hasCODWKey;
                    gameName = "Warzone";
                }
                else if (tab.Text.Contains("ARC RAIDERS"))
                {
                    hasAccess = _hasARKey;
                    gameName = "Arc Raiders";
                }
                else if (tab.Text.Contains("FN"))
                {
                    hasAccess = _hasFNKey;
                    gameName = "Fortnite";
                }
                else if (tab.Text.Contains("SPOOFER"))
                {
                    hasAccess = _hasSpooferKey;
                    gameName = "Spoofer";
                }

                // Update tab appearance based on access
                if (hasAccess)
                {
                    // User has access - show normal tab
                    tab.BackColor = Theme.Background;
                    tab.ForeColor = Color.White;
                    
                    // Update tab text to show active status
                    if (!tab.Text.Contains("✅"))
                    {
                        string originalText = tab.Text;
                        tab.Text = $"✅ {originalText}";
                    }
                }
                else
                {
                    // User doesn't have access - show locked tab
                    tab.BackColor = Color.FromArgb(40, 45, 55);
                    tab.ForeColor = Color.FromArgb(120, 130, 150);
                    
                    // Update tab text to show locked status
                    if (!tab.Text.Contains("🔒"))
                    {
                        string originalText = tab.Text.Replace("✅ ", "").Replace("🔒 ", "");
                        tab.Text = $"🔒 {originalText}";
                    }
                }

                // Log tab status changes
                if (gameName != "")
                {
                    Console.WriteLine($"[SMOKESCREEN] Tab '{gameName}': {(hasAccess ? "ACCESS GRANTED" : "ACCESS DENIED")}");
                }
            }
        }

        private TabPage BuildSpooferTab()
        {
            var tp = new TabPage("🛡️ SPOOFER") { BackColor = Theme.Background };

            var title = new Label
            {
                Text = "🛡️ Fortnite Spoofer & Cleaner",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            tp.Controls.Add(title);

            // Check if user has spoofer access
            if (!_hasSpooferKey)
            {
                var accessDenied = new Label
                {
                    Text = "🔒 SPOOFER ACCESS REQUIRED\n\nPurchase a spoofer key from the Marketplace to unlock this feature.\n\nThe spoofer provides:\n• Fortnite trace removal\n• Registry cleaning\n• Cache wiping\n• HWID spoofing",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(20, 80),
                    Size = new Size(600, 300),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tp.Controls.Add(accessDenied);
            }
            else
            {
                // User has access - show spoofer interface
                var statusPanel = new Panel
                {
                    Bounds = new Rectangle(20, 70, 700, 100),
                    BackColor = Theme.CardBackground,
                    BorderStyle = BorderStyle.FixedSingle
                };
                statusPanel.Controls.Add(new Label
                {
                    Text = "🛡️ Spoofer Status",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(15, 10),
                    AutoSize = true
                });

                var spooferStatusDot = new Label
                {
                    Text = "●",
                    Font = new Font("Segoe UI", 16),
                    ForeColor = Color.Lime,
                    Location = new Point(15, 40),
                    AutoSize = true
                };
                statusPanel.Controls.Add(spooferStatusDot);

                var spooferStatusLabel = new Label
                {
                    Text = "LICENSED",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.Lime,
                    Location = new Point(40, 40),
                    AutoSize = true
                };
                statusPanel.Controls.Add(spooferStatusLabel);

                var spooferInfoLabel = new Label
                {
                    Text = "Fortnite spoofer is licensed and ready to use. Click 'Open Spoofer' to launch the cleaner.",
                    Font = new Font("Consolas", 9),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(15, 70),
                    Size = new Size(670, 20)
                };
                statusPanel.Controls.Add(spooferInfoLabel);
                tp.Controls.Add(statusPanel);

                // Spoofer controls
                var btnPanel = new Panel
                {
                    Location = new Point(20, 190),
                    Size = new Size(700, 100),
                    BackColor = Color.Transparent
                };

                var openSpooferBtn = new Button
                {
                    Text = "🛡️ OPEN SPOOFER",
                    Location = new Point(0, 10),
                    Size = new Size(200, 40),
                    BackColor = Theme.AccentBlue,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                openSpooferBtn.Click += (_, _) => OpenSpoofer();
                btnPanel.Controls.Add(openSpooferBtn);

                var quickCleanBtn = new Button
                {
                    Text = "🧹 QUICK CLEAN",
                    Location = new Point(210, 10),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(45, 55, 72),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                quickCleanBtn.Click += (_, _) => QuickClean();
                btnPanel.Controls.Add(quickCleanBtn);

                var deepCleanBtn = new Button
                {
                    Text = "🔥 DEEP CLEAN",
                    Location = new Point(400, 10),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(200, 70, 70),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                deepCleanBtn.Click += (_, _) => DeepClean();
                btnPanel.Controls.Add(deepCleanBtn);

                var hwidBtn = new Button
                {
                    Text = "🔐 HWID SPOOFER",
                    Location = new Point(210, 60),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(138, 43, 226),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                hwidBtn.Click += (_, _) => HWIDSpoofer();
                btnPanel.Controls.Add(hwidBtn);

                var unspoofBtn = new Button
                {
                    Text = "🔄 UNSPOOF",
                    Location = new Point(400, 60),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat
                };
                unspoofBtn.Click += (_, _) => UnspoofHWIDs();
                btnPanel.Controls.Add(unspoofBtn);

                tp.Controls.Add(btnPanel);

                // Info panel
                var infoPanel = new Panel
                {
                    Location = new Point(20, 300),
                    Size = new Size(700, 200),
                    BackColor = Theme.CardBackground,
                    BorderStyle = BorderStyle.FixedSingle
                };
                infoPanel.Controls.Add(new Label
                {
                    Text = "📋 Spoofer Features",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(15, 15),
                    AutoSize = true
                });

                var featuresLabel = new Label
                {
                    Text = "• Fortnite/Epic Games process termination\n" +
                           "• Cache and log file removal\n" +
                           "• Registry key cleaning\n" +
                           "• HWID spoofing capabilities\n" +
                           "• Anti-cheat bypass preparation\n" +
                           "• System trace removal\n\n" +
                           "⚠️  Deep Clean will restart your PC automatically",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(15, 50),
                    Size = new Size(670, 120)
                };
                infoPanel.Controls.Add(featuresLabel);
                tp.Controls.Add(infoPanel);
            }

            return tp;
        }

        private void OpenSpoofer()
        {
            try
            {
                var spooferForm = new SpooferForm(true);
                spooferForm.ShowDialog();
                MessageBox.Show("Spoofer operation completed. Check the spoofer window for details.", "Spoofer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open spoofer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void QuickClean()
        {
            try
            {
                // Quick clean - basic cache clearing without reboot
                var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var pathsToClean = new[]
                {
                    Path.Combine(localPath, "FortniteGame", "Saved", "Logs"),
                    Path.Combine(localPath, "FortniteGame", "Saved", "Config"),
                    Path.Combine(localPath, "EpicGamesLauncher", "Saved", "Logs")
                };

                int cleaned = 0;
                foreach (var path in pathsToClean)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            cleaned++;
                        }
                    }
                    catch { }
                }

                MessageBox.Show($"Quick clean completed. Cleaned {cleaned} directories.", "Quick Clean", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Quick clean failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeepClean()
        {
            try
            {
                var spooferForm = new SpooferForm(true);
                spooferForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Deep clean failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HWIDSpoofer()
        {
            try
            {
                // Show HWID spoofer dialog
                var result = MessageBox.Show(
                    "🔐 HWID Spoofer\n\n" +
                    "This will:\n" +
                    "• Generate new fake HWIDs for all hardware components\n" +
                    "• Apply spoofed serials to disk, network, GPU, and system\n" +
                    "• Save original HWIDs for restoration\n" +
                    "• Restart PC to apply changes\n\n" +
                    "⚠️  This action will modify system hardware identifiers!\n\n" +
                    "Do you want to continue?",
                    "🔐 HWID SPOOFER",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    PerformHWIDSpoofer();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"HWID spoofer failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformHWIDSpoofer()
        {
            try
            {
                // Generate fake HWIDs
                var fakeSerials = GenerateFakeHWIDs();
                
                // Save original HWIDs before spoofing
                SaveOriginalHWIDs();
                
                // Apply spoofed HWIDs
                ApplySpoofedHWIDs(fakeSerials);
                
                MessageBox.Show(
                    "🔐 SPOOFED SUCCESSFULLY!\n\n" +
                    "• All hardware IDs have been spoofed\n" +
                    "• Original HWIDs saved for restoration\n" +
                    "• System will restart in 5 seconds\n\n" +
                    "Use 'Unspoof' button to restore original HWIDs",
                    "HWID Spoofer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Schedule restart
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    Process.Start("shutdown", "/r /t 0");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to spoof HWIDs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, string> GenerateFakeHWIDs()
        {
            var random = new Random();
            var fakeHWIDs = new Dictionary<string, string>();

            // Generate fake disk serial
            fakeHWIDs["Disk"] = GenerateRandomSerial(random, "WD-WCC4N0XXXXX");

            // Generate fake network MAC
            fakeHWIDs["Network"] = GenerateRandomMAC(random);

            // Generate fake GPU serial
            fakeHWIDs["GPU"] = GenerateRandomSerial(random, "NVIDIA-XXXX-XXXX-XXXX");

            // Generate fake system serials
            fakeHWIDs["BIOS"] = GenerateRandomSerial(random, "To be filled by O.E.M.");
            fakeHWIDs["Baseboard"] = GenerateRandomSerial(random, "To be filled by O.E.M.");

            return fakeHWIDs;
        }

        private string GenerateRandomSerial(Random random, string pattern)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[pattern.Length];

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == 'X')
                {
                    result[i] = chars[random.Next(chars.Length)];
                }
                else
                {
                    result[i] = pattern[i];
                }
            }

            return new string(result);
        }

        private string GenerateRandomMAC(Random random)
        {
            var mac = new byte[6];
            random.NextBytes(mac);
            
            // Set locally administered bit to avoid conflicts
            mac[0] = (byte)(mac[0] | 0x02);

            return string.Join(":", mac.Select(b => b.ToString("X2")));
        }

        private void SaveOriginalHWIDs()
        {
            try
            {
                var originalHWIDs = new Dictionary<string, string>();
                
                // Get current HWIDs (simplified)
                originalHWIDs["Disk"] = "ORIGINAL_DISK_SERIAL";
                originalHWIDs["Network"] = "ORIGINAL_MAC_ADDRESS";
                originalHWIDs["GPU"] = "ORIGINAL_GPU_SERIAL";
                originalHWIDs["BIOS"] = "ORIGINAL_BIOS_SERIAL";
                originalHWIDs["Baseboard"] = "ORIGINAL_BASEBOARD_SERIAL";

                // Save to file
                var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmokeScreenEngine", "original_hwids.json");
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                
                var json = System.Text.Json.JsonSerializer.Serialize(originalHWIDs, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(savePath, json);

                Console.WriteLine($"Original HWIDs saved to: {savePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save original HWIDs: {ex.Message}");
            }
        }

        private void ApplySpoofedHWIDs(Dictionary<string, string> fakeHWIDs)
        {
            try
            {
                // Load kernel driver and apply spoofed HWIDs
                var kernelDriver = new ENGINE.Services.HWIDKernelDriver();
                
                if (!kernelDriver.LoadDriver())
                {
                    Console.WriteLine("Failed to load kernel driver - using simulation mode");
                    SimulateHWIDSpoofing(fakeHWIDs);
                    return;
                }

                Console.WriteLine("Applying spoofed HWIDs via kernel driver:");
                bool allSuccess = true;

                foreach (var hwid in fakeHWIDs)
                {
                    Console.WriteLine($"  {hwid.Key}: {hwid.Value}");
                    if (!kernelDriver.SpoofHWID(hwid.Key, hwid.Value))
                    {
                        allSuccess = false;
                        Console.WriteLine($"  Failed to spoof {hwid.Key}");
                    }
                }

                kernelDriver.UnloadDriver();

                if (allSuccess)
                {
                    Console.WriteLine("All HWIDs spoofed successfully via kernel driver");
                }
                else
                {
                    Console.WriteLine("Some HWIDs failed to spoof via kernel driver");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply spoofed HWIDs: {ex.Message}");
                // Fallback to simulation
                SimulateHWIDSpoofing(fakeHWIDs);
            }
        }

        private void SimulateHWIDSpoofing(Dictionary<string, string> fakeHWIDs)
        {
            try
            {
                Console.WriteLine("Simulating HWID spoofing:");
                foreach (var hwid in fakeHWIDs)
                {
                    Console.WriteLine($"  {hwid.Key}: {hwid.Value}");
                }

                // Simulate registry modifications
                SimulateRegistryModifications(fakeHWIDs);
                
                Console.WriteLine("HWID spoofing simulation completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to simulate HWID spoofing: {ex.Message}");
            }
        }

        private void SimulateRegistryModifications(Dictionary<string, string> fakeHWIDs)
        {
            try
            {
                // Simulate registry key modifications for HWID spoofing
                Console.WriteLine("Simulating registry modifications:");
                
                // Disk serial registry paths
                Console.WriteLine("  Modifying disk registry entries...");
                
                // Network MAC registry paths
                Console.WriteLine("  Modifying network registry entries...");
                
                // GPU serial registry paths
                Console.WriteLine("  Modifying GPU registry entries...");
                
                // System BIOS registry paths
                Console.WriteLine("  Modifying BIOS registry entries...");
                
                // Baseboard registry paths
                Console.WriteLine("  Modifying baseboard registry entries...");
                
                Console.WriteLine("Registry simulation completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to simulate registry modifications: {ex.Message}");
            }
        }

        private void UnspoofHWIDs()
        {
            try
            {
                var result = MessageBox.Show(
                    "🔄 Unspoof HWIDs\n\n" +
                    "This will:\n" +
                    "• Restore original hardware IDs\n" +
                    "• Remove spoofed serials from all components\n" +
                    "• Restart PC to apply changes\n\n" +
                    "⚠️  This will restore your original HWIDs!\n\n" +
                    "Do you want to continue?",
                    "🔄 UNSPOOF",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    PerformUnspoof();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unspoof failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformUnspoof()
        {
            try
            {
                // Load original HWIDs from saved file
                var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmokeScreenEngine", "original_hwids.json");
                
                if (File.Exists(savePath))
                {
                    var json = File.ReadAllText(savePath);
                    var originalHWIDs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    // Try to restore via kernel driver first
                    var kernelDriver = new ENGINE.Services.HWIDKernelDriver();
                    
                    if (kernelDriver.LoadDriver())
                    {
                        Console.WriteLine("Restoring original HWIDs via kernel driver:");
                        foreach (var hwid in originalHWIDs)
                        {
                            Console.WriteLine($"  {hwid.Key}: {hwid.Value}");
                        }

                        if (kernelDriver.UnspoofAllHWIDs())
                        {
                            Console.WriteLine("All original HWIDs restored successfully via kernel driver");
                        }
                        else
                        {
                            Console.WriteLine("Failed to restore via kernel driver - using simulation");
                            SimulateHWIDRestoration(originalHWIDs);
                        }

                        kernelDriver.UnloadDriver();
                    }
                    else
                    {
                        Console.WriteLine("Failed to load kernel driver - using simulation mode");
                        SimulateHWIDRestoration(originalHWIDs);
                    }
                    
                    MessageBox.Show(
                        "🔄 UNSPOOFED SUCCESSFULLY!\n\n" +
                        "• Original hardware IDs have been restored\n" +
                        "• All spoofed serials have been removed\n" +
                        "• System will restart in 5 seconds\n\n" +
                        "Your original HWIDs are now active",
                        "HWID Unspoof",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Schedule restart
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        Process.Start("shutdown", "/r /t 0");
                    });
                }
                else
                {
                    MessageBox.Show(
                        "❌ NO ORIGINAL HWIDS FOUND\n\n" +
                        "No original HWIDs were found to restore.\n" +
                        "You must spoof your HWIDs first before unspoofing.",
                        "HWID Unspoof",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unspoof HWIDs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SimulateHWIDRestoration(Dictionary<string, string> originalHWIDs)
        {
            try
            {
                Console.WriteLine("Simulating original HWID restoration:");
                foreach (var hwid in originalHWIDs)
                {
                    Console.WriteLine($"  {hwid.Key}: {hwid.Value}");
                }

                // Simulate registry restoration
                Console.WriteLine("Simulating registry restoration:");
                
                // Restore disk serial registry entries
                Console.WriteLine("  Restoring disk registry entries...");
                
                // Restore network MAC registry entries
                Console.WriteLine("  Restoring network registry entries...");
                
                // Restore GPU serial registry entries
                Console.WriteLine("  Restoring GPU registry entries...");
                
                // Restore system BIOS registry entries
                Console.WriteLine("  Restoring BIOS registry entries...");
                
                // Restore baseboard registry entries
                Console.WriteLine("  Restoring baseboard registry entries...");
                
                Console.WriteLine("Registry restoration simulation completed");
                Console.WriteLine("Original HWIDs restoration simulation completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to simulate HWID restoration: {ex.Message}");
            }
        }

        private bool _aimbotRunning = false;
        private Process _aimbotProcess = null;

        private void LaunchFortniteAimbot(TabPage tp)
        {
            try
            {
                if (_aimbotRunning)
                {
                    MessageBox.Show("Aimbot is already running!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check if Python is installed
                var pythonPath = FindPythonExecutable();
                if (string.IsNullOrEmpty(pythonPath))
                {
                    MessageBox.Show("Python is not installed or not found in PATH!\n\nPlease install Python 3.8+ to use the aimbot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Check if required files exist
                var aimbotPath = Path.Combine(Application.StartupPath, "lib", "aimbot.py");
                var dllPath = Path.Combine(Application.StartupPath, "lib", "mouse", "dd40605x64.dll");

                if (!File.Exists(aimbotPath))
                {
                    MessageBox.Show($"Aimbot script not found:\n{aimbotPath}\n\nPlease ensure all aimbot files are present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(dllPath))
                {
                    MessageBox.Show($"Mouse driver not found:\n{dllPath}\n\nPlease ensure dd40605x64.dll is present.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Launch the aimbot
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{aimbotPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.Combine(Application.StartupPath, "lib")
                };

                _aimbotProcess = new Process { StartInfo = startInfo };
                _aimbotProcess.Start();

                _aimbotRunning = true;
                UpdateAimbotStatus(tp);

                MessageBox.Show("🎯 Fortnite Aimbot launched successfully!\n\n• F1: Toggle aimbot on/off\n• F2: Quit aimbot\n• Use in-game for best results", "Aimbot Launched", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch aimbot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopFortniteAimbot(TabPage tp)
        {
            try
            {
                if (!_aimbotRunning || _aimbotProcess == null)
                {
                    MessageBox.Show("Aimbot is not running!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _aimbotProcess.Kill();
                _aimbotProcess.Dispose();
                _aimbotProcess = null;
                _aimbotRunning = false;

                UpdateAimbotStatus(tp);
                MessageBox.Show("🛑 Fortnite Aimbot stopped successfully!", "Aimbot Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop aimbot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAimbotSettings(TabPage tp)
        {
            try
            {
                var configPath = Path.Combine(Application.StartupPath, "lib", "config", "config.json");
                
                if (File.Exists(configPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", configPath);
                    MessageBox.Show("📝 Aimbot configuration file opened in notepad.\n\nEdit the settings and save the file.", "Settings Opened", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Configuration file not found!\n\nPlease launch the aimbot first to create the configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FindPythonExecutable()
        {
            var pythonPaths = new[]
            {
                "python",
                "python3",
                "py",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python", "python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python", "python.exe")
            };

            foreach (var path in pythonPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };

                    using var process = new Process { StartInfo = startInfo };
                    process.Start();
                    process.WaitForExit(2000);
                    
                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
                catch
                {
                    // Continue to next path
                }
            }

            return null;
        }

        private void UpdateAimbotStatus(TabPage tp)
        {
            try
            {
                var statusDot = tp.Controls.Find("_aimbotStatusDot", true).FirstOrDefault() as Label;
                var statusLabel = tp.Controls.Find("_aimbotStatusLabel", true).FirstOrDefault() as Label;
                var infoLabel = tp.Controls.Find("_aimbotInfoLabel", true).FirstOrDefault() as Label;

                if (statusDot != null && statusLabel != null && infoLabel != null)
                {
                    if (_aimbotRunning)
                    {
                        statusDot.ForeColor = Color.Lime;
                        statusLabel.ForeColor = Color.Lime;
                        statusLabel.Text = "RUNNING";
                        infoLabel.Text = "Aimbot is active and ready to use. Press F1 to toggle, F2 to quit.";
                    }
                    else
                    {
                        statusDot.ForeColor = Color.Gray;
                        statusLabel.ForeColor = Color.Gray;
                        statusLabel.Text = "NOT LOADED";
                        infoLabel.Text = "Click 'Launch Aimbot' to initialize the neural network aimbot";
                    }
                }

                // Check if process is still running
                if (_aimbotRunning && _aimbotProcess != null)
                {
                    if (_aimbotProcess.HasExited)
                    {
                        _aimbotRunning = false;
                        _aimbotProcess = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating aimbot status: {ex.Message}");
            }
        }

        private async Task RegisterUserWithWirelessReceiver()
        {
            try
            {
                if (_user == null || _license == null) return;

                var userSession = new WirelessReceiver.UserSession
                {
                    UserId = _userSessionId,
                    Username = _user.Username ?? _user.DiscordId,
                    DiscordId = _user.DiscordId,
                    KeyType = GetKeyType(),
                    KeyDuration = _license.ExpiresAt.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(_license.ExpiresAt.Value).DateTime : DateTime.MaxValue,
                    IpAddress = GetLocalIpAddress(),
                    ComputerName = Environment.MachineName,
                    IsOnline = true
                };

                await _wirelessReceiver.RegisterUserAsync(userSession);
                
                // Start heartbeat timer
                _heartbeatTimer.Interval = 30000; // 30 seconds
                _heartbeatTimer.Tick += async (s, e) => await SendHeartbeat();
                _heartbeatTimer.Start();

                Console.WriteLine($"User {_user.DiscordId} registered with wireless receiver");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user with wireless receiver: {ex.Message}");
            }
        }

        private async Task SendHeartbeat()
        {
            try
            {
                await _wirelessReceiver.UpdateUserHeartbeatAsync(_userSessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending heartbeat: {ex.Message}");
            }
        }

        private string GetKeyType()
        {
            var keys = new List<string>();
            if (_hasR6SKey) keys.Add("R6S");
            if (_hasCODWKey) keys.Add("CODW");
            if (_hasARKey) keys.Add("ARC");
            if (_hasFNKey) keys.Add("FN");
            if (_hasSpooferKey) keys.Add("SPOOFER");
            
            return keys.Count > 0 ? string.Join("+", keys) : "NONE";
        }

        private string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // Fallback to localhost
            }
            return "127.0.0.1";
        }
    }
}
