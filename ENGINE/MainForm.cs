using System;
using System.Net.Http;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class MainForm : Form
    {
        private Button btnCheck;
        private Label lblStatus;

        public MainForm()
        {
            this.Text = "SmokeScreen Engine Client";
            this.Width = 600;
            this.Height = 400;

            btnCheck = new Button()
            {
                Text = "Check API Status",
                Top = 50,
                Left = 50,
                Width = 200
            };

            lblStatus = new Label()
            {
                Top = 100,
                Left = 50,
                Width = 400
            };

            btnCheck.Click += async (s, e) =>
            {
                using HttpClient client = new HttpClient();
                try
                {
                    var response = await client.GetStringAsync("http://localhost:4000/status");
                    lblStatus.Text = response;
                }
                catch
                {
                    lblStatus.Text = "API not reachable.";
                }
            };

            Controls.Add(btnCheck);
            Controls.Add(lblStatus);
        }
    }
}
