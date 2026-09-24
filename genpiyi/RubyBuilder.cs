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

            // Các ô cùng một từ (银行…) sáng lên cùng lúc khi rê chuột
            var wordCells = new Dictionary<int, List<Border>>();
            var wordInfo = new Dictionary<int, WordInfo>();
            bool dict = DictionaryService.Available;

            foreach (var line in lines)
            {
                if (dict)
                {
                    foreach (var g in line.Where(t => t.IsHan && t.WordId >= 0).GroupBy(t => t.WordId))
                    {
                        var toks = g.ToList();
                        wordInfo[g.Key] = DictionaryService.Describe(string.Concat(toks.Select(x => x.Text)), toks, s.ToneStyle);
                    }
                }

                var wrap = new WrapPanel { MaxWidth = maxWidth, Margin = new Thickness(0, 0, 0, 2) };
                if (line.Count == 0)
                {
                    wrap.Children.Add(new Border { Height = hz * 0.5 });
                }
                foreach (var t in line)
                {
                    wordInfo.TryGetValue(t.WordId, out var info);
                    wrap.Children.Add(MakeCell(t, s, hz, py, pal, theme, info, wordCells));
                }
                root.Children.Add(wrap);
            }
            return root;
        }

        private static FrameworkElement MakeCell(PyToken t, AppSettings s, double hzSize, double pySize, Palette pal,
                                                 PopupTheme theme, WordInfo? info, Dictionary<int, List<Border>> wordCells)
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
                if (!wordCells.TryGetValue(t.WordId, out var group)) wordCells[t.WordId] = group = new List<Border>();
                group.Add(cell);
                cell.MouseEnter += (_, _) => { foreach (var c in group) c.Background = pal.Hover; };
                cell.MouseLeave += (_, _) => { foreach (var c in group) c.Background = Brushes.Transparent; };

                if (info != null && (info.HasMeaning || info.HanViet != null))
                {
                    cell.ToolTip = BuildWordTip(t, info, s, theme);
                    if (t.Alternatives.Length > 1) pinyin.TextDecorations = DottedUnderline(pinyin.Foreground);
                }
                else if (t.Alternatives.Length > 1)
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
                ToolTipService.SetShowDuration(cell, 60000);
            }
            return cell;
        }

        private static TextDecorationCollection DottedUnderline(Brush brush) => new()
        {
            new TextDecoration(TextDecorationLocation.Underline,
                new Pen(brush, 1) { DashStyle = DashStyles.Dot }, 1,
                TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended)
        };

        /// <summary>Chú thích khi rê chuột: từ · pinyin · âm Hán Việt · nghĩa.</summary>
        private static object BuildWordTip(PyToken t, WordInfo info, AppSettings s, PopupTheme theme)
        {
            var panel = new StackPanel { MaxWidth = 340, Margin = new Thickness(2) };
            var head = new TextBlock { TextWrapping = TextWrapping.Wrap };
            head.Inlines.Add(new System.Windows.Documents.Run(info.Text) { FontFamily = HanziFont, FontSize = 18, FontWeight = FontWeights.SemiBold });
            head.Inlines.Add(new System.Windows.Documents.Run("  " + info.Pinyin)
                { FontFamily = PinyinFont, FontSize = 13, Foreground = ThemeCatalog.B(ThemeCatalog.Get("light").PinyinDefault) });
            panel.Children.Add(head);

            if (s.ShowHanViet && !string.IsNullOrEmpty(info.HanViet))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = Loc.T("dict.hv") + info.HanViet!.ToUpperInvariant(),
                    FontSize = 11.5, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0)
                });
            }
            var meaning = DictionaryService.MeaningText(info, s.ResolvedMeaningLang);
            if (meaning.Length > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = meaning, TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new Thickness(0, 5, 0, 0)
                });
            }
            if (t.Alternatives.Length > 1)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{t.Text}: {string.Join(" / ", t.Alternatives)}",
                    FontSize = 11.5, Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 0)
                });
            }
            return panel;
        }

        /// <summary>Danh sách từ vựng: chữ Hán + pinyin + âm Hán Việt | nghĩa.</summary>
        public static UIElement BuildVocab(List<WordInfo> words, AppSettings s, double maxWidth, PopupTheme theme)
        {
            var pal = PaletteOf(theme);
            var fg = ThemeCatalog.B(theme.Fg);
            var sub = ThemeCatalog.B(theme.Sub);
            var divider = ThemeCatalog.B(theme.Divider);
            var root = new StackPanel { MaxWidth = maxWidth };
            string lang = s.ResolvedMeaningLang;

            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto), MinWidth = 86 });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var left = new StackPanel { Margin = new Thickness(0, 0, 14, 0), MaxWidth = 170 };
                left.Children.Add(new TextBlock { Text = w.Text, FontFamily = HanziFont, FontSize = 17, Foreground = pal.Hanzi, TextWrapping = TextWrapping.Wrap });
                left.Children.Add(new TextBlock { Text = w.Pinyin, FontFamily = PinyinFont, FontSize = 12, Foreground = pal.PinyinDefault, TextWrapping = TextWrapping.Wrap });
                if (s.ShowHanViet && !string.IsNullOrEmpty(w.HanViet))
                    left.Children.Add(new TextBlock { Text = w.HanViet!.ToUpperInvariant(), FontSize = 10.5, Foreground = sub, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 0) });
                Grid.SetColumn(left, 0);
                grid.Children.Add(left);

                var meaning = DictionaryService.MeaningText(w, lang);
                var right = new TextBlock
                {
                    Text = meaning.Length > 0 ? meaning : "—",
                    FontSize = 12.5, Foreground = meaning.Length > 0 ? fg : sub,
                    TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0)
                };
                Grid.SetColumn(right, 1);
                grid.Children.Add(right);

                root.Children.Add(grid);
                if (i < words.Count - 1) root.Children.Add(new Border { Height = 1, Background = divider });
            }
            return root;
        }
    }
}
