using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace genpiyi
{
    /// <summary>Dựng giao diện "pinyin nằm trên chữ Hán" (kiểu ruby) cho danh sách token.</summary>
    internal static class RubyBuilder
    {
        private sealed class Palette
        {
            public Brush[] Tones = Array.Empty<Brush>(); // index 1..5
            public Brush PinyinDefault = Brushes.Gray;
            public Brush Hanzi = Brushes.Black;
            public Brush Other = Brushes.Gray;
            public Brush Missing = Brushes.Gray;
            public Brush Hover = Brushes.Transparent;
        }

        private static readonly Dictionary<string, Palette> Cache = new();

        private static Palette PaletteOf(PopupTheme t)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(t.Id, out var p)) return p;
                p = new Palette
                {
                    Tones = t.Tones.Select(Make).ToArray(),
                    PinyinDefault = Make(t.PinyinDefault),
                    Hanzi = Make(t.Hanzi),
                    Other = Make(t.Other),
                    Missing = Make(t.Missing),
                    Hover = Make(t.CellHover)
                };
                Cache[t.Id] = p;
                return p;
            }
        }

        private static readonly FontFamily PinyinFont = new("Segoe UI Variable Text, Segoe UI");
        private static readonly FontFamily HanziFont = new("Microsoft YaHei UI, Microsoft YaHei, SimSun, Segoe UI");

        private static Brush Make(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        /// <summary>Màu của một thanh điệu (dùng cho chú thích màu trong cài đặt).</summary>
        public static Brush ToneBrush(int tone, PopupTheme theme) => PaletteOf(theme).Tones[Math.Clamp(tone, 0, 5)];

        public static UIElement Build(List<List<PyToken>> lines, AppSettings s, double maxWidth, PopupTheme theme)
        {
            var pal = PaletteOf(theme);
            var root = new StackPanel();
            double hz = s.HanziFontSize;
            double py = Math.Max(11, Math.Round(hz * 0.5));

            foreach (var line in lines)
            {
                var wrap = new WrapPanel { MaxWidth = maxWidth, Margin = new Thickness(0, 0, 0, 2) };
                if (line.Count == 0)
                {
                    wrap.Children.Add(new Border { Height = hz * 0.5 });
                }
                foreach (var t in line)
                {
                    wrap.Children.Add(MakeCell(t, s, hz, py, pal));
                }
                root.Children.Add(wrap);
            }
            return root;
        }

        private static FrameworkElement MakeCell(PyToken t, AppSettings s, double hzSize, double pySize, Palette pal)
        {
            var stack = new StackPanel();

            var pinyin = new TextBlock
            {
                Text = t.IsHan ? (t.Pinyin ?? "?") : " ",
                FontFamily = PinyinFont,
                FontSize = pySize,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = !t.IsHan ? pal.Other
                           : t.Pinyin == null ? pal.Missing
                           : s.ToneColors && t.Tone >= 1 && t.Tone <= 5 ? pal.Tones[t.Tone]
                           : pal.PinyinDefault
            };

            var hanzi = new TextBlock
            {
                Text = t.Text,
                FontFamily = HanziFont,
                FontSize = t.IsHan ? hzSize : hzSize * 0.72,
                Foreground = t.IsHan ? pal.Hanzi : pal.Other,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            // Giữ cùng chiều cao hàng chữ để chữ Hán và chữ thường thẳng hàng đáy
            stack.Children.Add(pinyin);
            stack.Children.Add(new Border { Height = hzSize * 1.35, Child = hanzi });

            var cell = new Border
            {
                Child = stack,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(t.IsHan ? 3 : 1, 2, t.IsHan ? 3 : 1, 0),
                Margin = new Thickness(0, 0, 0, 4),
                Background = Brushes.Transparent
            };

            if (t.IsHan)
            {
                cell.MouseEnter += (_, _) => cell.Background = pal.Hover;
                cell.MouseLeave += (_, _) => cell.Background = Brushes.Transparent;

                if (t.Alternatives.Length > 1)
                {
                    cell.ToolTip = $"{t.Text}  ·  {string.Join(" / ", t.Alternatives)}";
                    // chấm nhỏ báo chữ đa âm
                    pinyin.TextDecorations = new TextDecorationCollection
                    {
                        new TextDecoration(TextDecorationLocation.Underline,
                            new Pen(pinyin.Foreground, 1) { DashStyle = DashStyles.Dot }, 1,
                            TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended)
                    };
                }
                else if (t.Pinyin != null)
                {
                    cell.ToolTip = $"{t.Text}  ·  {t.Pinyin}";
                }
                ToolTipService.SetInitialShowDelay(cell, 250);
            }
            return cell;
        }
    }
}
