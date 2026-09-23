using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace genpiyi
{
    public partial class PinyinPopup : Window
    {
        private const string GlyphCopy = "\uE8C8";
        private const string GlyphBoth = "\uE8C1";
        private const string GlyphCheck = "\uE73E";
        private const string GlyphPin = "\uE718";
        private const string GlyphPinned = "\uE840";

        private readonly string _text;
        private readonly string _plain;
        private readonly PopupTheme _theme;
        private MouseHook? _hook;
        private bool _closing;

        public bool IsPinned { get; private set; }

        public PinyinPopup(string text, string source, PopupTheme? themeOverride = null)
        {
            InitializeComponent();
            _text = text;

            var s = App.Current.Settings;
            _theme = themeOverride ?? s.GetTheme();
            ApplyTheme(_theme);

            List<List<PyToken>> lines = PinyinService.Convert(text, s.ToneStyle);
            RubyHost.Content = RubyBuilder.Build(lines, s, 530, _theme);
            _plain = PinyinService.ToPlainPinyin(lines);
            PlainPinyin.Text = _plain;

            if (!string.IsNullOrEmpty(source))
            {
                SourceLabel.Text = source;
                SourceChip.Visibility = Visibility.Visible;
            }

            if (!PinyinService.Available)
            {
                ErrorLabel.Text = PinyinService.LoadError ?? Loc.T("engine.failPopup");
                ErrorLabel.Visibility = Visibility.Visible;
            }

            Loaded += OnLoaded;
            Closing += (_, _) => _closing = true;
            Closed += (_, _) => { _hook?.Dispose(); _hook = null; };
            PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        }

        private void ApplyTheme(PopupTheme t)
        {
            Resources["P.Bg"] = ThemeCatalog.BackgroundBrush(t);
            Resources["P.Border"] = ThemeCatalog.B(t.Border);
            Resources["P.Fg"] = ThemeCatalog.B(t.Fg);
            Resources["P.Sub"] = ThemeCatalog.B(t.Sub);
            Resources["P.Hover"] = ThemeCatalog.B(t.Hover);
            Resources["P.Chip"] = ThemeCatalog.B(t.Chip);
            Resources["P.Footer"] = ThemeCatalog.B(t.Footer);
            Resources["P.Divider"] = ThemeCatalog.B(t.Divider);

            Card.CornerRadius = new CornerRadius(t.Radius);
            FooterBorder.CornerRadius = new CornerRadius(0, 0, t.Radius, t.Radius);
            PlainPinyin.SelectionBrush = ThemeCatalog.B(t.Accent);

            EmblemHost.Content = ThemeCatalog.BuildEmblem(t, 20);
            DecoHost.Content = ThemeCatalog.BuildDecorations(t);
            if (t.CatEars) EarsHost.Content = ThemeCatalog.BuildCatEars(t);

            // Popup nền tối đổ bóng đậm hơn, nền sáng nhạt hơn
            if (Card.Effect is System.Windows.Media.Effects.DropShadowEffect fx)
                fx.Opacity = t.Dark ? 0.35 : 0.18;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PositionNearCursor();
            _hook = new MouseHook(OnGlobalMouseDown);

            // Hiệu ứng xuất hiện: mờ dần + trượt nhẹ lên
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
            CardShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        }

        /// <summary>Đặt popup cạnh con trỏ chuột, không tràn khỏi màn hình (tính theo pixel vật lý).</summary>
        private void PositionNearCursor()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            NativeMethods.GetCursorPos(out var cur);
            NativeMethods.GetWindowRect(hwnd, out var r);
            int w = r.Right - r.Left;
            int h = r.Bottom - r.Top;

            var wa = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(cur.X, cur.Y)).WorkingArea;

            // Cửa sổ có viền trong suốt 16 DIP (để đổ bóng) → bù lại cho thẻ sát con trỏ hơn
            double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            int m = (int)(16 * scale);

            int x = cur.X - m + 6;
            int y = cur.Y - m + 14;
            if (x + w > wa.Right) x = wa.Right - w;
            if (y + h > wa.Bottom) y = cur.Y - h + m - 6;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;

            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }

        /// <summary>Gọi từ hook chuột toàn cục: click ra ngoài popup thì đóng (trừ khi đã ghim).</summary>
        private void OnGlobalMouseDown(int x, int y)
        {
            if (IsPinned || _closing) return;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            NativeMethods.GetWindowRect(hwnd, out var r);
            double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            int m = (int)(16 * scale);
            bool inside = x >= r.Left + m && x < r.Right - m && y >= r.Top + m && y < r.Bottom - m;
            if (!inside)
            {
                Dispatcher.BeginInvoke(new Action(() => { if (!_closing && !IsPinned) Close(); }));
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { /* ignore */ }
            }
        }

        private void BtnCopyPinyin_Click(object sender, RoutedEventArgs e)
        {
            App.Current.CopyToClipboard(_plain);
            Flash(BtnCopyPinyin, GlyphCopy);
        }

        private void BtnCopyBoth_Click(object sender, RoutedEventArgs e)
        {
            App.Current.CopyToClipboard(_text + Environment.NewLine + _plain);
            Flash(BtnCopyBoth, GlyphBoth);
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            IsPinned = !IsPinned;
            BtnPin.Content = IsPinned ? GlyphPinned : GlyphPin;
            if (IsPinned)
            {
                BtnPin.Foreground = ThemeCatalog.B(_theme.Accent);
                BtnPin.ToolTip = Loc.T("popup.unpin");
            }
            else
            {
                BtnPin.ClearValue(ForegroundProperty);
                BtnPin.ToolTip = Loc.T("popup.pin");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>Đổi icon thành dấu ✓ xanh trong giây lát để báo đã copy.</summary>
        private static void Flash(Button btn, string originalGlyph)
        {
            btn.Content = GlyphCheck;
            btn.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xB8, 0x6E));
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                btn.Content = originalGlyph;
                btn.ClearValue(ForegroundProperty);
            };
            t.Start();
        }
    }
}
