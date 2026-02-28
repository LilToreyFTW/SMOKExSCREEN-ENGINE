using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class ChartControl : Control
    {
        private readonly List<(string Name, List<int> Data, Color Color)> _series = new();
        private const int MAX_POINTS = 60;

        public string Title { get; set; } = "";
        public string XAxis { get; set; } = "";
        public string YAxis { get; set; } = "";

        public ChartControl() { DoubleBuffered = true; }

        /// <summary>Add a single data point to a named series (creates series if new).</summary>
        public void AddPoint(string name, int value, Color color)
        {
            var existing = _series.FindIndex(s => s.Name == name);
            if (existing < 0)
            {
                _series.Add((name, new List<int> { value }, color));
            }
            else
            {
                var (n, data, c) = _series[existing];
                data.Add(value);
                if (data.Count > MAX_POINTS) data.RemoveAt(0);
                _series[existing] = (n, data, c);
            }
            Invalidate();
        }

        /// <summary>Legacy: add a full data array.</summary>
        public void AddLine(string name, dynamic[] data, Color color, int index = 0)
        {
            var ints = new List<int>();
            foreach (var v in data) ints.Add((int)v);
            _series.Add((name, ints, color));
            Invalidate();
        }

        public void Clear() { _series.Clear(); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bg = Color.FromArgb(17, 22, 28);
            g.Clear(bg);

            // Grid
            using var gridPen = new Pen(Color.FromArgb(30, 40, 55));
            for (int x = 0; x < Width; x += 60)  g.DrawLine(gridPen, x, 0, x, Height);
            for (int y = 0; y < Height; y += 40)  g.DrawLine(gridPen, 0, y, Width, y);

            // Title
            if (!string.IsNullOrEmpty(Title))
            {
                using var tf = new Font("Segoe UI", 9, FontStyle.Bold);
                g.DrawString(Title, tf, Brushes.White, 10, 8);
            }

            if (_series.Count == 0)
            {
                using var ef = new Font("Segoe UI", 9);
                using var eb = new SolidBrush(Color.FromArgb(80, 90, 110));
                g.DrawString("No data yet", ef, eb,
                    (Width - 70) / 2f, (Height - 18) / 2f);
                return;
            }

            const int padL = 48, padR = 16, padT = 28, padB = 28;
            int chartW = Width  - padL - padR;
            int chartH = Height - padT - padB;
            if (chartW <= 0 || chartH <= 0) return;

            // Find global Y range
            int globalMin = int.MaxValue, globalMax = int.MinValue;
            foreach (var (_, data, _) in _series)
                foreach (var v in data) { if (v < globalMin) globalMin = v; if (v > globalMax) globalMax = v; }
            if (globalMin == int.MaxValue) return;
            if (globalMin == globalMax) { globalMin -= 10; globalMax += 10; }

            float yRange = globalMax - globalMin;

            // Y-axis labels
            using var axFont  = new Font("Consolas", 7);
            using var axBrush = new SolidBrush(Color.FromArgb(80, 90, 110));
            for (int i = 0; i <= 4; i++)
            {
                int   val = globalMin + (int)(yRange * i / 4);
                float yPx = padT + chartH - (chartH * i / 4);
                g.DrawString(val.ToString(), axFont, axBrush, 2, yPx - 7);
                using var gl = new Pen(Color.FromArgb(25, 35, 50));
                g.DrawLine(gl, padL, yPx, padL + chartW, yPx);
            }

            // Draw each series
            foreach (var (name, data, color) in _series)
            {
                if (data.Count < 2) continue;

                var points = new PointF[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    float px = padL + chartW * i / (data.Count - 1);
                    float py = padT + chartH - chartH * (data[i] - globalMin) / yRange;
                    points[i] = new PointF(px, py);
                }

                // Glow: wide transparent line
                using (var glowPen = new Pen(Color.FromArgb(40, color), 6))
                    g.DrawLines(glowPen, points);

                // Main line
                using (var linePen = new Pen(color, 1.8f))
                {
                    linePen.LineJoin = LineJoin.Round;
                    g.DrawLines(linePen, points);
                }

                // Latest value dot
                var last = points[^1];
                using (var dotBrush = new SolidBrush(color))
                    g.FillEllipse(dotBrush, last.X - 4, last.Y - 4, 8, 8);

                // Value label
                g.DrawString(data[^1].ToString(), axFont, axBrush, last.X + 6, last.Y - 8);
            }

            // Legend
            int lx = padL, ly = Height - padB + 6;
            foreach (var (name, _, color) in _series)
            {
                using var lb = new SolidBrush(color);
                g.FillRectangle(lb, lx, ly, 10, 10);
                g.DrawString(name, axFont, axBrush, lx + 14, ly);
                lx += 80;
            }
        }
    }
}
