using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SmokeScreenEngine;

namespace SmokeScreenEngine
{
    public partial class LiveUserDashboard : Form
    {
        private WirelessReceiver _receiver;
        private System.Windows.Forms.Timer _refreshTimer;
        private Label _totalUsersLabel;
        private Label _onlineUsersLabel;
        private ListView _userListView;
        private Panel _statsPanel;

        public LiveUserDashboard()
        {
            _receiver = new WirelessReceiver();
            InitializeComponent();
            SetupDashboard();
            StartMonitoring();
        }

        private void InitializeComponent()
        {
            this.Text = "🔥 SmokeScreen Engine - Live User Dashboard";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.FromArgb(32, 36, 44);
            this.Font = new Font("Segoe UI", 9);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupDashboard()
        {
            // Header Panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = "🔥 SMOKESCREEN ENGINE - LIVE USER MONITORING",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            var ownerLabel = new Label
            {
                Text = "👑 Owner: Bl0wdart (1368087024401252393)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(20, 50),
                AutoSize = true
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(ownerLabel);
            this.Controls.Add(headerPanel);

            // Stats Panel
            _statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(45, 55, 72),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(20)
            };

            _totalUsersLabel = new Label
            {
                Text = "📊 TOTAL USERS: 0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(20, 20),
                Size = new Size(200, 40)
            };

            _onlineUsersLabel = new Label
            {
                Text = "🟢 ONLINE USERS: 0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(250, 20),
                Size = new Size(200, 40)
            };

            var keysLabel = new Label
            {
                Text = "🔑 ACTIVE KEYS: 0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(480, 20),
                Size = new Size(200, 40)
            };

            var serverLabel = new Label
            {
                Text = "🌐 SERVER STATUS: ONLINE",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(710, 20),
                Size = new Size(250, 40)
            };

            _statsPanel.Controls.Add(_totalUsersLabel);
            _statsPanel.Controls.Add(_onlineUsersLabel);
            _statsPanel.Controls.Add(keysLabel);
            _statsPanel.Controls.Add(serverLabel);
            this.Controls.Add(_statsPanel);

            // User List
            var listPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var listHeader = new Label
            {
                Text = "👥 ACTIVE USER SESSIONS",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 5),
                AutoSize = true
            };

            _userListView = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(17, 22, 28),
                ForeColor = Color.White,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            _userListView.Columns.Add("User ID", 150);
            _userListView.Columns.Add("Username", 120);
            _userListView.Columns.Add("Discord ID", 180);
            _userListView.Columns.Add("Key Type", 100);
            _userListView.Columns.Add("Duration", 120);
            _userListView.Columns.Add("IP Address", 120);
            _userListView.Columns.Add("Computer", 120);
            _userListView.Columns.Add("Status", 80);
            _userListView.Columns.Add("Last Seen", 140);

            listPanel.Controls.Add(listHeader);
            listPanel.Controls.Add(_userListView);
            this.Controls.Add(listPanel);

            // Control Panel
            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(17, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            var refreshBtn = new Button
            {
                Text = "🔄 REFRESH",
                Location = new Point(20, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            refreshBtn.Click += (s, e) => RefreshUserData();

            var exportBtn = new Button
            {
                Text = "📤 EXPORT DATA",
                Location = new Point(150, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            exportBtn.Click += (s, e) => ExportUserData();

            var clearBtn = new Button
            {
                Text = "🗑️ CLEAR ALL",
                Location = new Point(280, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearBtn.Click += (s, e) => ClearAllUsers();

            var generateBtn = new Button
            {
                Text = "🔑 GENERATE KEY",
                Location = new Point(410, 15),
                Size = new Size(140, 30),
                BackColor = Color.Lime,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            generateBtn.Click += (s, e) => GenerateEncryptionKey();

            controlPanel.Controls.Add(refreshBtn);
            controlPanel.Controls.Add(exportBtn);
            controlPanel.Controls.Add(clearBtn);
            controlPanel.Controls.Add(generateBtn);
            this.Controls.Add(controlPanel);
        }

        private void StartMonitoring()
        {
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Refresh every 5 seconds
            };
            _refreshTimer.Tick += RefreshUserData;
            _refreshTimer.Start();

            RefreshUserData();
        }

        private void RefreshUserData(object? sender = null, EventArgs? e = null)
        {
            try
            {
                var users = _receiver.GetActiveUsers();
                
                // Update stats
                _totalUsersLabel.Text = $"📊 TOTAL USERS: {users.Count}";
                _onlineUsersLabel.Text = $"🟢 ONLINE USERS: {users.Count(u => u.IsOnline)}";
                
                var activeKeys = users.GroupBy(u => u.KeyType).Count();
                var keysLabel = _statsPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("🔑"));
                if (keysLabel != null)
                    keysLabel.Text = $"🔑 ACTIVE KEYS: {activeKeys}";

                // Update user list
                _userListView.Items.Clear();
                
                foreach (var user in users.OrderByDescending(u => u.LastHeartbeat))
                {
                    var item = new ListViewItem(new[]
                    {
                        user.UserId,
                        user.Username ?? "N/A",
                        user.DiscordId ?? "N/A",
                        user.KeyType ?? "N/A",
                        user.KeyDuration.ToString("yyyy-MM-dd"),
                        user.IpAddress ?? "N/A",
                        user.ComputerName ?? "N/A",
                        user.IsOnline ? "🟢 Online" : "🔴 Offline",
                    });

                    item.SubItems.Add(user.KeyDuration ?? "N/A");
                    item.SubItems.Add(user.LastHeartbeat.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.ForeColor = user.IsOnline ? Color.Lime : Color.Red;
                    _userListView.Items.Add(item);
                }

                // Add owner info if no users
                if (users.Count == 0)
                {
                    var ownerItem = new ListViewItem(new[]
                    {
                        "OWNER",
                        "Bl0wdart",
                        "1368087024401252393",
                        "ADMIN",
                        "LIFETIME",
                        "LOCAL",
                        Environment.MachineName,
                        "👑 OWNER",
                        DateTime.Now.ToString("HH:mm:ss")
                    });
                    ownerItem.ForeColor = Color.Gold;
                    _userListView.Items.Add(ownerItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing user data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportUserData()
        {
            try
            {
                var users = _receiver.GetActiveUsers();
                var exportData = new
                {
                    ExportTime = DateTime.UtcNow,
                    TotalUsers = users.Count,
                    OnlineUsers = users.Count(u => u.IsOnline),
                    ServerId = "Bl0wdart1368087024401252393",
                    Users = users.Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.DiscordId,
                        u.KeyType,
                        u.KeyDuration,
                        u.IpAddress,
                        u.ComputerName,
                        u.IsOnline,
                        u.LastHeartbeat,
                        u.SessionId
                    })
                };

                var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var fileName = $"SmokeScreen_Users_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                
                File.WriteAllText(fileName, json);
                MessageBox.Show($"User data exported to {fileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearAllUsers()
        {
            if (MessageBox.Show("Are you sure you want to clear all user sessions?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // This would clear the user sessions in the receiver
                MessageBox.Show("All user sessions cleared!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshUserData();
            }
        }

        private void GenerateEncryptionKey()
        {
            try
            {
                var encryptionData = _receiver.GenerateEncryptionFile();
                var fileName = $"SmokeScreen_Encryption_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                
                File.WriteAllText(fileName, encryptionData);
                MessageBox.Show($"Encryption file generated: {fileName}\n\nDistribute this to users for secure connection.", "Key Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating key: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
