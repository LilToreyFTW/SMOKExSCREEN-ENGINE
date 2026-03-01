using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public partial class MarketplaceForm : Form
    {
        private record Product(string Name, string Price, string DurationCode, string Desc, Color Accent);

        private static readonly List<Product> _catalog = new()
        {
            new("1-Month Access",  "$4.99",  "1_MONTH",  "30 days of full SmokeScreen ENGINE access.",     Color.FromArgb(52,  199, 89)),
            new("6-Month Access",  "$19.99", "6_MONTHS", "6 months — save 33% vs monthly.",                Color.FromArgb(31,  111, 235)),
            new("1-Year Access",   "$34.99", "1_YEAR",   "12 months — our best value subscription.",       Color.FromArgb(175, 82,  222)),
            new("Lifetime Access", "$74.99", "LIFETIME", "One-time purchase. Never pay again. Unlimited.", Color.FromArgb(255, 214, 0)),
        };

        private Product? _selected;
        private Button   _buyBtn = null!;
        private Label    _infoLbl = null!;

        public MarketplaceForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text          = "Marketplace — SmokeScreen ENGINE";
            this.Size          = new Size(860, 600);
            this.BackColor     = Theme.Background;
            this.StartPosition = FormStartPosition.CenterParent;
            this.DoubleBuffered = true;

            // Header
            var header = new Label
            {
                Text      = "PURCHASE LICENSE",
                Font      = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds    = new Rectangle(40, 28, 500, 36),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var sub = new Label
            {
                Text      = "Select a tier below and click Purchase to checkout on the website.",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Bounds    = new Rectangle(40, 68, 700, 22),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            this.Controls.Add(header);
            this.Controls.Add(sub);

            // Product cards grid
            int cardW = 180, cardH = 200, startX = 40, startY = 108, gap = 16;
            for (int i = 0; i < _catalog.Count; i++)
            {
                var p    = _catalog[i];
                int x    = startX + i * (cardW + gap);
                var card = BuildProductCard(p, x, startY, cardW, cardH);
                this.Controls.Add(card);
            }

            // Details / selection area
            _infoLbl = new Label
            {
                Text      = "Select a plan above to see details.",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Theme.TextSecondary,
                Bounds    = new Rectangle(40, 336, 760, 28),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            this.Controls.Add(_infoLbl);

            // Buy button
            _buyBtn = new Button
            {
                Text      = "SELECT A PLAN",
                Bounds    = new Rectangle(40, 376, 300, 52),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 60, 80),
                ForeColor = Color.FromArgb(120, 130, 150),
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Enabled   = false,
            };
            _buyBtn.FlatAppearance.BorderSize = 0;
            _buyBtn.Click += OnBuyClicked;
            this.Controls.Add(_buyBtn);

            // Footer note
            this.Controls.Add(new Label
            {
                Text      = "🔒  Secure checkout on our website. After purchase you will receive a key to redeem in Settings.",
                Font      = new Font("Segoe UI", 8),
                ForeColor = Theme.TextSecondary,
                Bounds    = new Rectangle(40, 452, 760, 20),
                TextAlign = ContentAlignment.MiddleLeft,
            });

            this.Controls.Add(new Label
            {
                Text      = "Already have a key?  →  Go to Settings → License → Redeem Key",
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(80, 100, 130),
                Bounds    = new Rectangle(40, 476, 760, 20),
                TextAlign = ContentAlignment.MiddleLeft,
            });
        }

        private Panel BuildProductCard(Product p, int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Bounds    = new Rectangle(x, y, w, h),
                BackColor = Theme.CardBackground,
                Cursor    = Cursors.Hand,
                Tag       = p,
            };

            // Accent top bar
            var bar = new Panel
            {
                Bounds    = new Rectangle(0, 0, w, 4),
                BackColor = p.Accent,
            };
            card.Controls.Add(bar);

            card.Controls.Add(new Label
            {
                Text      = p.Name.ToUpper(),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Bounds    = new Rectangle(12, 18, w - 24, 20),
                TextAlign = ContentAlignment.MiddleLeft,
            });

            card.Controls.Add(new Label
            {
                Text      = p.Price,
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = p.Accent,
                Bounds    = new Rectangle(12, 44, w - 24, 40),
                TextAlign = ContentAlignment.MiddleLeft,
            });

            card.Controls.Add(new Label
            {
                Text      = p.Desc,
                Font      = new Font("Segoe UI", 8),
                ForeColor = Theme.TextSecondary,
                Bounds    = new Rectangle(12, 92, w - 24, 64),
                TextAlign = ContentAlignment.TopLeft,
            });

            // Hover + click
            card.Click      += (s, e) => SelectProduct(p, card);
            bar.Click       += (s, e) => SelectProduct(p, card);
            foreach (Control c in card.Controls) c.Click += (s, e) => SelectProduct(p, card);

            return card;
        }

        private void SelectProduct(Product p, Panel card)
        {
            _selected = p;

            // Reset all card borders
            foreach (Control c in this.Controls)
            {
                if (c is Panel cp && cp.Tag is Product)
                    cp.BackColor = Theme.CardBackground;
            }
            card.BackColor = Color.FromArgb(24, 30, 42);

            _infoLbl.Text      = $"{p.Name}  —  {p.Price}  |  {p.Desc}";
            _infoLbl.ForeColor = p.Accent;

            _buyBtn.Enabled   = true;
            _buyBtn.Text      = $"PURCHASE {p.Name.ToUpper()}  →";
            _buyBtn.BackColor = p.Accent;
            _buyBtn.ForeColor = p.DurationCode == "LIFETIME" ? Color.FromArgb(30, 20, 0) : Color.White;
        }

        private void OnBuyClicked(object? sender, EventArgs e)
        {
            if (_selected == null) return;
            // Navigate to the Vercel purchase page with the tier pre-selected
            var url = $"{DiscordAuth.API_BASE}/?buy={_selected.DurationCode}";
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }

            MessageBox.Show(
                $"Opening checkout for: {_selected.Name} ({_selected.Price})\n\n" +
                "After purchase, you'll receive a license key.\n" +
                "Redeem it in:  Settings → License → Redeem Key",
                "Redirecting to Checkout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
