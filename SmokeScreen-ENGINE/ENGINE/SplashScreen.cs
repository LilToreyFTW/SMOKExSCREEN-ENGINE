using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace SmokeScreenEngine
{
    internal class SplashScreen : Form
    {
        private System.Windows.Forms.Timer _fadeTimer;
        private System.Windows.Forms.Timer _videoTimer;
        private Process? _videoProcess;
        private float _opacity = 0f;
        private bool _fadingIn = true;
        private readonly string _videoPath;
        private readonly string _imagePath;
        private int _holdTime = 0;
        private const int HoldDuration = 50;

        public SplashScreen(string videoPath, string imagePath)
        {
            _videoPath = videoPath;
            _imagePath = imagePath;
            SetupForm();
            StartSplash();
        }

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(17, 17, 17);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            if (File.Exists(_videoPath))
            {
                this.ClientSize = new Size(800, 450);
            }
            else if (File.Exists(_imagePath))
            {
                using var bmp = Image.FromFile(_imagePath);
                this.ClientSize = new Size(bmp.Width, bmp.Height);
                AddImage();
            }
            else
            {
                this.ClientSize = new Size(600, 400);
                AddText();
            }
        }

        private void AddImage()
        {
            var pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = Image.FromFile(_imagePath)
            };
            this.Controls.Add(pic);
        }

        private void AddText()
        {
            var label = new Label
            {
                Text = "SmokeScreen ENGINE",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                AutoSize = true
            };
            label.Location = new Point((this.Width - label.Width) / 2, (this.Height - label.Height) / 2);
            this.Controls.Add(label);
        }

        private void StartSplash()
        {
            if (File.Exists(_videoPath))
            {
                try
                {
                    _videoProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = _videoPath,
                        UseShellExecute = true
                    });
                    _videoTimer = new System.Windows.Forms.Timer { Interval = 100 };
                    _videoTimer.Tick += VideoTimer_Tick;
                    _videoTimer.Start();
                }
                catch { }
            }

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        private void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (_videoProcess == null || _videoProcess.HasExited)
            {
                _videoTimer?.Stop();
                _holdTime = 100;
            }
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (_fadingIn)
            {
                _opacity += 0.04f;
                if (_opacity >= 1f)
                {
                    _opacity = 1f;
                    _fadingIn = false;
                }
            }
            else
            {
                _holdTime++;
                if (_holdTime >= HoldDuration)
                {
                    _opacity -= 0.04f;
                    if (_opacity <= 0f)
                    {
                        _opacity = 0f;
                        _fadeTimer.Stop();
                        CloseVideo();
                        this.Close();
                        return;
                    }
                }
            }
            this.Opacity = _opacity;
        }

        private void CloseVideo()
        {
            try
            {
                if (_videoProcess != null && !_videoProcess.HasExited)
                {
                    _videoProcess.Kill();
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            _fadeTimer?.Dispose();
            _videoTimer?.Dispose();
            CloseVideo();
            base.Dispose(disposing);
        }
    }
}
