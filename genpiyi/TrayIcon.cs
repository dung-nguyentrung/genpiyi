using System;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace genpiyi
{
    /// <summary>Biểu tượng ở khay hệ thống (góc phải thanh taskbar) + menu chuột phải.</summary>
    internal sealed class TrayIcon : IDisposable
    {
        private readonly WinForms.NotifyIcon _ni;
        private readonly WinForms.ToolStripMenuItem _auto;
        private readonly WinForms.ToolStripMenuItem _chatOnly;
        private readonly WinForms.ToolStripMenuItem _open;
        private readonly WinForms.ToolStripMenuItem _fromClipboard;
        private readonly WinForms.ToolStripMenuItem _exit;
        private readonly App _app;

        public TrayIcon(App app)
        {
            _app = app;
            _ni = new WinForms.NotifyIcon
            {
                Icon = IconFactory.GetIcon(),
                Text = Loc.T("tray.tooltip"),
                Visible = true
            };

            var menu = new WinForms.ContextMenuStrip
            {
                Renderer = new ModernMenuRenderer(),
                Font = new Drawing.Font("Segoe UI", 9.5f),
                Padding = new WinForms.Padding(4, 6, 4, 6),
                ShowImageMargin = true,
                DropShadowEnabled = true
            };

            _open = new WinForms.ToolStripMenuItem("", null, (s, e) => _app.ShowMainWindow())
            {
                Font = new Drawing.Font("Segoe UI", 9.5f, Drawing.FontStyle.Bold)
            };
            menu.Items.Add(_open);
            _fromClipboard = new WinForms.ToolStripMenuItem("", null, (s, e) => _app.ShowFromClipboard());
            menu.Items.Add(_fromClipboard);
            menu.Items.Add(new WinForms.ToolStripSeparator());

            _auto = new WinForms.ToolStripMenuItem("") { CheckOnClick = true };
            _auto.Click += (s, e) => { _app.Settings.AutoOnCopy = _auto.Checked; _app.SettingsChanged(); };
            menu.Items.Add(_auto);

            _chatOnly = new WinForms.ToolStripMenuItem("") { CheckOnClick = true };
            _chatOnly.Click += (s, e) => { _app.Settings.OnlyChatApps = _chatOnly.Checked; _app.SettingsChanged(); };
            menu.Items.Add(_chatOnly);

            menu.Items.Add(new WinForms.ToolStripSeparator());
            _exit = new WinForms.ToolStripMenuItem("", null, (s, e) => _app.ExitApp());
            menu.Items.Add(_exit);

            foreach (WinForms.ToolStripItem item in menu.Items)
            {
                if (item is WinForms.ToolStripMenuItem mi) mi.Padding = new WinForms.Padding(2, 5, 8, 5);
            }

            menu.Opening += (s, e) => Refresh();
            _ni.ContextMenuStrip = menu;
            _ni.MouseClick += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left) _app.ShowMainWindow();
            };
            Refresh();
        }

        public void Refresh()
        {
            // Chữ theo ngôn ngữ hiện tại
            _ni.Text = Loc.T("tray.tooltip");
            _open.Text = Loc.T("tray.open");
            _fromClipboard.Text = Loc.T("tray.fromClipboard");
            _auto.Text = Loc.T("tray.auto");
            _chatOnly.Text = Loc.T("set.chatOnly");
            _exit.Text = Loc.T("tray.exit");

            _auto.Checked = _app.Settings.AutoOnCopy;
            _chatOnly.Checked = _app.Settings.OnlyChatApps;
            _chatOnly.Enabled = _app.Settings.AutoOnCopy;
        }

        public void ShowBalloon(string title, string text)
        {
            try { _ni.ShowBalloonTip(3000, title, text, WinForms.ToolTipIcon.Info); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            _ni.Visible = false;
            _ni.Dispose();
        }
    }

    /// <summary>Vẽ menu khay kiểu phẳng, hiện đại (nền trắng, mục chọn bo góc, dấu tích màu đỏ).</summary>
    internal sealed class ModernMenuRenderer : WinForms.ToolStripProfessionalRenderer
    {
        private static readonly Drawing.Color Accent = Drawing.Color.FromArgb(229, 72, 77);
        private static readonly Drawing.Color Hover = Drawing.Color.FromArgb(240, 242, 245);
        private static readonly Drawing.Color Border = Drawing.Color.FromArgb(213, 217, 224);
        private static readonly Drawing.Color Separator = Drawing.Color.FromArgb(232, 234, 238);
        private static readonly Drawing.Color TextColor = Drawing.Color.FromArgb(27, 31, 39);
        private static readonly Drawing.Color TextDisabled = Drawing.Color.FromArgb(160, 168, 180);

        public ModernMenuRenderer() : base(new FlatColors())
        {
            RoundedEdges = false;
        }

        private sealed class FlatColors : WinForms.ProfessionalColorTable
        {
            public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.White;
            public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.White;
            public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.White;
            public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.White;
            public override Drawing.Color MenuBorder => Border;
            public override Drawing.Color MenuItemBorder => Drawing.Color.Transparent;
            public override Drawing.Color MenuItemSelected => Hover;
            public override Drawing.Color SeparatorDark => Separator;
            public override Drawing.Color SeparatorLight => Drawing.Color.White;
        }

        protected override void OnRenderToolStripBackground(WinForms.ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Drawing.Color.White);
        }

        protected override void OnRenderImageMargin(WinForms.ToolStripRenderEventArgs e)
        {
            // không vẽ dải xám bên trái
        }

        protected override void OnRenderToolStripBorder(WinForms.ToolStripRenderEventArgs e)
        {
            using var pen = new Drawing.Pen(Border);
            var r = e.AffectedBounds;
            e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(WinForms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            var g = e.Graphics;
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Drawing.Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            using var path = RoundRect(rect, 5);
            using var brush = new Drawing.SolidBrush(Hover);
            g.FillPath(brush, path);
        }

        protected override void OnRenderItemText(WinForms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(WinForms.ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var r = e.ImageRectangle;
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using var pen = new Drawing.Pen(e.Item.Enabled ? Accent : TextDisabled, 2f)
            {
                StartCap = Drawing.Drawing2D.LineCap.Round,
                EndCap = Drawing.Drawing2D.LineCap.Round,
                LineJoin = Drawing.Drawing2D.LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new Drawing.PointF(cx - 5, cy),
                new Drawing.PointF(cx - 1.5f, cy + 3.5f),
                new Drawing.PointF(cx + 5, cy - 4)
            });
        }

        protected override void OnRenderSeparator(WinForms.ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Drawing.Pen(Separator);
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
        }

        private static Drawing.Drawing2D.GraphicsPath RoundRect(Drawing.Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
