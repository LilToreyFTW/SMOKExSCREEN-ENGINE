using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SmokeScreenEngine
{
    public partial class SpooferForm : Form
    {
        private RichTextBox _logBox     = null!;
        private Button      _btnStart   = null!;
        private ProgressBar _progress   = null!;
        private readonly bool _isLicensed;

        public SpooferForm(bool isLicensed = false)
        {
            _isLicensed = isLicensed;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text           = "FORTNITE ZERO-TRACE PURGE";
            this.Size           = new Size(680, 560);
            this.BackColor      = Theme.Background;
            this.StartPosition  = FormStartPosition.CenterParent;
            this.DoubleBuffered = true;

            var title = new Label
            {
                Text      = "DEEP PC CLEANER & LOG WIPER",
                Font      = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Theme.Error,
                Dock      = DockStyle.Top,
                Height    = 60,
                TextAlign = ContentAlignment.MiddleCenter,
            };

            var warnLabel = new Label
            {
                Text      = "⚠  This will kill Fortnite/Epic processes, wipe cache & registry, then REBOOT your PC.",
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(200, 140, 0),
                Bounds    = new Rectangle(20, 65, 620, 20),
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _logBox = new RichTextBox
            {
                Bounds      = new Rectangle(20, 90, 620, 280),
                BackColor   = Color.Black,
                ForeColor   = Color.Lime,
                Font        = new Font("Consolas", 9),
                ReadOnly    = true,
                BorderStyle = BorderStyle.FixedSingle,
            };

            _progress = new ProgressBar
            {
                Bounds  = new Rectangle(20, 382, 620, 14),
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous,
            };

            _btnStart = new Button
            {
                Bounds    = new Rectangle(20, 408, 620, 60),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand,
            };
            _btnStart.FlatAppearance.BorderSize = 0;

            if (_isLicensed)
            {
                _btnStart.Text      = "PURGE ALL TRACES & RESTART PC";
                _btnStart.BackColor = Theme.AccentBlue;
                _btnStart.ForeColor = Color.White;
                _btnStart.Click    += async (s, e) => await ConfirmAndPurge();
            }
            else
            {
                _btnStart.Text      = "🔒  LOCKED — LICENSE REQUIRED";
                _btnStart.BackColor = Color.FromArgb(40, 45, 55);
                _btnStart.ForeColor = Color.FromArgb(100, 110, 130);
                _btnStart.Enabled   = false;

                this.Controls.Add(new Label
                {
                    Text      = "Purchase a license from the Marketplace to unlock the Spoofer.",
                    Font      = new Font("Segoe UI", 9),
                    ForeColor = Theme.TextSecondary,
                    Bounds    = new Rectangle(20, 478, 620, 22),
                    TextAlign = ContentAlignment.MiddleCenter,
                });
            }

            this.Controls.Add(title);
            this.Controls.Add(warnLabel);
            this.Controls.Add(_logBox);
            this.Controls.Add(_progress);
            this.Controls.Add(_btnStart);

            Log("Ready. Click the button above to begin the purge sequence.");
            Log("All Fortnite / Epic Games files, cache and registry entries will be removed.");
        }

        private async Task ConfirmAndPurge()
        {
            var confirm = MessageBox.Show(
                "This will:\n" +
                "  • Kill all Epic Games / Fortnite processes\n" +
                "  • Delete Fortnite cache, logs and EAC data\n" +
                "  • Remove Epic Games registry keys\n" +
                "  • REBOOT your PC immediately after\n\n" +
                "Are you absolutely sure?",
                "⚠ CONFIRM FULL PURGE",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;
            await StartDeepPurge();
        }

        private async Task StartDeepPurge()
        {
            _btnStart.Enabled = false;
            _btnStart.Text    = "PURGING...";
            SetProgress(0);

            // ── Step 1: Kill processes ──────────────────────────────────────
            Log("\n[STEP 1/4] KILLING ACTIVE PROCESSES...");
            string[] procs = { "EpicGamesLauncher", "FortniteClient-Win64-Shipping",
                                "FortniteLauncher", "EpicWebHelper", "UnrealCEFSubProcess" };
            foreach (var name in procs) KillProcess(name);
            SetProgress(10);
            await Task.Delay(500);

            // ── Step 2: Delete folders ──────────────────────────────────────
            Log("\n[STEP 2/4] WIPING CACHE AND GAME FILES...");
            string local    = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] paths  =
            {
                Path.Combine(local,   "FortniteGame"),
                Path.Combine(local,   "EpicGamesLauncher"),
                Path.Combine(local,   "UnrealEngine"),
                Path.Combine(local,   "BattlEye"),
                Path.Combine(local,   "EasyAntiCheat"),
                Path.Combine(appData, "Epic"),
                Path.Combine(appData, "EpicGamesLauncher"),
            };

            int total = paths.Length, done = 0;
            foreach (var path in paths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        Log($"  > Purged: {Path.GetFileName(path)}");
                    }
                    else
                    {
                        Log($"  > Skipped (not found): {Path.GetFileName(path)}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"  ! Failed: {Path.GetFileName(path)} — {ex.Message}", Color.Orange);
                }
                done++;
                SetProgress(10 + (int)(done * 50.0 / total));
                await Task.Delay(100);
            }

            // ── Step 3: Registry ────────────────────────────────────────────
            Log("\n[STEP 3/4] CLEANING REGISTRY...");
            string[] regKeys =
            {
                @"Software\Epic Games",
                @"Software\EpicGames",
                @"Software\BattlEye",
                @"Software\EasyAntiCheat",
            };
            foreach (var key in regKeys)
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(key, false);
                    Log($"  > Removed: HKCU\\{key}");
                }
                catch (Exception ex)
                {
                    Log($"  ! Registry: {key} — {ex.Message}", Color.Orange);
                }
            }
            SetProgress(80);
            await Task.Delay(300);

            // ── Step 4: Reboot ──────────────────────────────────────────────
            Log("\n[STEP 4/4] PURGE COMPLETE ✓", Color.Lime);
            Log("Rebooting in 5 seconds to flush kernel drivers...", Color.Yellow);
            SetProgress(100);

            for (int i = 5; i >= 1; i--)
            {
                Log($"  Rebooting in {i}...", Color.Yellow);
                await Task.Delay(1000);
            }

            Process.Start("shutdown", "/r /t 0");
        }

        private void KillProcess(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    p.Kill();
                    Log($"  > Killed: {name}.exe");
                }
            }
            catch (Exception ex)
            {
                Log($"  ! Could not kill {name}: {ex.Message}", Color.Orange);
            }
        }

        private void SetProgress(int value)
        {
            SafeInvoke(() => _progress.Value = Math.Clamp(value, 0, 100));
        }

        private void Log(string msg, Color? color = null)
        {
            SafeInvoke(() =>
            {
                _logBox.SelectionStart  = _logBox.TextLength;
                _logBox.SelectionLength = 0;
                _logBox.SelectionColor  = color ?? Color.Lime;
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
                _logBox.ScrollToCaret();
            });
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) Invoke(action);
            else action();
        }
    }
}
