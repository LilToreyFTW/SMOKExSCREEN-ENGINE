using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class MsPingStatus : UserControl
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
        private Label _statusLabel = null!;
        private System.Windows.Forms.Timer _timer = null!;

        public MsPingStatus()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _statusLabel = new Label
            {
                Text = "Checking...",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(6, 6)
            };
            Controls.Add(_statusLabel);

            _timer = new System.Windows.Forms.Timer { Interval = 5000 };
            _timer.Tick += async (_, __) => await RefreshAsync();
            _timer.Start();
        }

        private async Task RefreshAsync()
        {
            try
            {
                var resp = await _http.GetAsync("https://smok-ex-screen-engine.vercel.app/ping");
                var ok = resp.IsSuccessStatusCode;
                _statusLabel.Text = ok ? "Vercel: Online" : "Vercel: Offline";
                _statusLabel.ForeColor = ok ? Color.Lime : Color.Red;
            }
            catch
            {
                _statusLabel.Text = "Vercel: Offline";
                _statusLabel.ForeColor = Color.Red;
            }
        }
    }
}
