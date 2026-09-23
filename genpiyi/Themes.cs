using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace genpiyi
{
    /// <summary>Một mẫu giao diện (skin) cho popup pinyin.</summary>
    public sealed class PopupTheme
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string NameEn { get; init; } = "";
        public string DescriptionEn { get; init; } = "";
        public string Badge { get; init; } = "";          // "new", "hot" (trống = không có)

        public string LocName => Loc.IsEn && NameEn.Length > 0 ? NameEn : Name;
        public string LocDescription => Loc.IsEn && DescriptionEn.Length > 0 ? DescriptionEn : Description;
        public string LocBadge => Badge.Length == 0 ? "" : Loc.T("badge." + Badge);
        public bool Dark { get; init; }

        // Nền thẻ (gradient chéo từ trên-trái xuống dưới-phải)
        public string[] Background { get; init; } = { "#FFFFFF", "#FFFFFF" };
        public string Border { get; init; } = "#DDE1E6";
        public double Radius { get; init; } = 12;

        // Chữ & điều khiển
        public string Fg { get; init; } = "#1B1F27";
        public string Sub { get; init; } = "#5B6573";
        public string Hover { get; init; } = "#0F000000";
        public string Chip { get; init; } = "#F0F2F5";
        public string Footer { get; init; } = "#F7F8FA";
        public string Divider { get; init; } = "#EEF0F3";
        public string Accent { get; init; } = "#E5484D";

        // Chữ Hán / pinyin
        public string Hanzi { get; init; } = "#1B1F27";
        public string PinyinDefault { get; init; } = "#2F6FEB";
        public string Other { get; init; } = "#5B6573";
        public string Missing { get; init; } = "#A3ACB8";
        public string CellHover { get; init; } = "#0D000000";
        /// <summary>Màu thanh điệu: [0]=không dùng, [1..4], [5]=thanh nhẹ.</summary>
        public string[] Tones { get; init; } = { "#5B6573", "#E5484D", "#D97A00", "#1E9E5A", "#2F6FEB", "#8A94A3" };

        // Trang trí
        public string Emblem { get; init; } = "拼";
        public string[] EmblemColors { get; init; } = { "#FF6B6B", "#D93A40" };
        public string Deco { get; init; } = "none";      // hearts, stars, paws, petals, leaves, bubbles, sparkles
        public string DecoColor { get; init; } = "#FF6B6B";
        public string DecoColor2 { get; init; } = "";    // màu phụ (tuỳ chọn)
        public bool CatEars { get; init; }
    }

    /// <summary>Danh sách các mẫu có sẵn + hàm dựng hình trang trí dùng chung cho popup và thẻ xem trước.</summary>
    public static class ThemeCatalog
    {
        public static readonly IReadOnlyList<PopupTheme> All = new List<PopupTheme>
        {
            new()
            {
                Id = "dark", Name = "Đêm", Description = "Tối giản, dễ nhìn", NameEn = "Night", DescriptionEn = "Minimal and easy on the eyes", Dark = true,
                Background = new[] { "#FA1C2029", "#FA1C2029" }, Border = "#33FFFFFF",
                Fg = "#F3F4F6", Sub = "#A0A8B5", Hover = "#1FFFFFFF", Chip = "#26FFFFFF", Footer = "#10FFFFFF", Divider = "#1AFFFFFF",
                Hanzi = "#F5F7FA", PinyinDefault = "#9EC5FF", Other = "#AEB6C2", Missing = "#6B7480", CellHover = "#1AFFFFFF",
                Tones = new[] { "#AEB6C2", "#FF6B6B", "#FFB454", "#5FD38D", "#5AA9FF", "#98A2AF" }
            },
            new()
            {
                Id = "light", Name = "Trắng tinh", Description = "Sáng sủa, gọn gàng", NameEn = "Pure White", DescriptionEn = "Bright and tidy",
                Background = new[] { "#FFFFFF", "#FFFFFF" }
            },
            new()
            {
                Id = "strawberry", Name = "Sữa dâu", Description = "Hồng ngọt ngào", NameEn = "Strawberry Milk", DescriptionEn = "Sweet and pink", Badge = "new",
                Background = new[] { "#FFF6F8", "#FFE1EA" }, Border = "#FFC9D8", Radius = 16,
                Fg = "#6B2C3E", Sub = "#A0667A", Hover = "#14FF5C8A", Chip = "#FFE3EC", Footer = "#80FFFFFF", Divider = "#FFD3E0",
                Accent = "#FF5C8A", Hanzi = "#4A1F2C", PinyinDefault = "#E0457B", Other = "#A0667A", CellHover = "#14FF5C8A",
                Tones = new[] { "#A0667A", "#E8365D", "#E27A12", "#2E9A62", "#3C6FE0", "#B08A98" },
                Emblem = "莓", EmblemColors = new[] { "#FF8FB1", "#FF4F7E" },
                Deco = "hearts", DecoColor = "#FF7AA2", DecoColor2 = "#FFB3C9"
            },
            new()
            {
                Id = "tabby", Name = "Mèo mướp", Description = "Có tai mèo và dấu chân", NameEn = "Tabby Cat", DescriptionEn = "Cat ears and paw prints", Badge = "hot",
                Background = new[] { "#FFFBF2", "#FFEBCB" }, Border = "#F6D7A6", Radius = 16,
                Fg = "#5A3A17", Sub = "#9A7447", Hover = "#18F08A24", Chip = "#FDE7C4", Footer = "#80FFFFFF", Divider = "#F6DDB5",
                Accent = "#F08A24", Hanzi = "#4A2F12", PinyinDefault = "#D46F0E", Other = "#9A7447", CellHover = "#18F08A24",
                Tones = new[] { "#9A7447", "#E0453A", "#D97A00", "#2F9A56", "#2F6FEB", "#A68B6A" },
                Emblem = "喵", EmblemColors = new[] { "#FFB25B", "#F08A24" },
                Deco = "paws", DecoColor = "#E9A45E", DecoColor2 = "#F6C997", CatEars = true
            },
            new()
            {
                Id = "matcha", Name = "Trà xanh", Description = "Xanh dịu mắt", NameEn = "Matcha", DescriptionEn = "Soft, calming green", Badge = "new",
                Background = new[] { "#F6FBF0", "#E1F0D2" }, Border = "#CDE3B8", Radius = 14,
                Fg = "#2F4A22", Sub = "#6A8757", Hover = "#185A9E4B", Chip = "#E3F1D6", Footer = "#80FFFFFF", Divider = "#D6E8C5",
                Accent = "#5A9E4B", Hanzi = "#233A18", PinyinDefault = "#3F8A34", Other = "#6A8757", CellHover = "#185A9E4B",
                Tones = new[] { "#6A8757", "#D9463E", "#C97A10", "#23904F", "#2E6BD6", "#8C9C80" },
                Emblem = "茶", EmblemColors = new[] { "#8CC77A", "#4E9442" },
                Deco = "leaves", DecoColor = "#7DB46A", DecoColor2 = "#A9D196"
            },
            new()
            {
                Id = "sakura", Name = "Hoa anh đào", Description = "Cánh hoa bay nhẹ", NameEn = "Cherry Blossom", DescriptionEn = "Gently drifting petals",
                Background = new[] { "#FFFFFF", "#FCE6EF" }, Border = "#F7D2E0", Radius = 16,
                Fg = "#5C3446", Sub = "#9C7385", Hover = "#14E86A9A", Chip = "#FBE3EC", Footer = "#80FFFFFF", Divider = "#F6DCE6",
                Accent = "#E86A9A", Hanzi = "#43222F", PinyinDefault = "#D0578A", Other = "#9C7385", CellHover = "#14E86A9A",
                Tones = new[] { "#9C7385", "#E23D5E", "#DB7A10", "#2C955C", "#3A6BE0", "#B294A2" },
                Emblem = "樱", EmblemColors = new[] { "#F8A5C2", "#E86A9A" },
                Deco = "petals", DecoColor = "#F4A3C0", DecoColor2 = "#FAD0DF"
            },
            new()
            {
                Id = "ocean", Name = "Biển xanh", Description = "Mát lạnh như sóng biển", NameEn = "Ocean Blue", DescriptionEn = "Cool as the sea breeze",
                Background = new[] { "#F2FAFF", "#D5ECFF" }, Border = "#BFDDF7", Radius = 14,
                Fg = "#173A5C", Sub = "#5C7E9E", Hover = "#182F8FE0", Chip = "#DCEEFF", Footer = "#80FFFFFF", Divider = "#CDE4F7",
                Accent = "#2F8FE0", Hanzi = "#10304F", PinyinDefault = "#1F7BD0", Other = "#5C7E9E", CellHover = "#182F8FE0",
                Tones = new[] { "#5C7E9E", "#E0454F", "#D27A0E", "#1E9460", "#2C5FD8", "#8499AD" },
                Emblem = "海", EmblemColors = new[] { "#6EC1FF", "#2F8FE0" },
                Deco = "bubbles", DecoColor = "#6FB7F0", DecoColor2 = "#A8D6FA"
            },
            new()
            {
                Id = "galaxy", Name = "Ngân hà", Description = "Bầu trời đêm lấp lánh", NameEn = "Galaxy", DescriptionEn = "A sparkling night sky", Badge = "new", Dark = true,
                Background = new[] { "#FA221A4A", "#FA3A2470" }, Border = "#40B69CFF", Radius = 14,
                Fg = "#F2EEFF", Sub = "#B7AEDB", Hover = "#22FFFFFF", Chip = "#2EFFFFFF", Footer = "#14FFFFFF", Divider = "#22FFFFFF",
                Accent = "#B69CFF", Hanzi = "#FFFFFF", PinyinDefault = "#C9B8FF", Other = "#B7AEDB", Missing = "#7C73A3", CellHover = "#1FFFFFFF",
                Tones = new[] { "#B7AEDB", "#FF7A93", "#FFC46B", "#6FE3A5", "#7DB8FF", "#A59DC6" },
                Emblem = "星", EmblemColors = new[] { "#C9B8FF", "#8A6CF0" },
                Deco = "sparkles", DecoColor = "#FFE9A8", DecoColor2 = "#C9B8FF"
            },
        };

        public static PopupTheme Get(string? id) =>
            All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];

        // ------------------------------------------------------------------
        // Hàm tiện ích màu

        public static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        public static SolidColorBrush B(string hex)
        {
            var b = new SolidColorBrush(C(hex));
            b.Freeze();
            return b;
        }

        public static Brush BackgroundBrush(PopupTheme t)
        {
            if (t.Background.Length < 2 || t.Background[0] == t.Background[1]) return B(t.Background[0]);
            var g = new LinearGradientBrush(C(t.Background[0]), C(t.Background[1]), new Point(0, 0), new Point(1, 1));
            g.Freeze();
            return g;
        }

        public static Brush EmblemBrush(PopupTheme t)
        {
            var g = new LinearGradientBrush(C(t.EmblemColors[0]), C(t.EmblemColors[^1]), new Point(0, 0), new Point(1, 1));
            g.Freeze();
            return g;
        }

        // ------------------------------------------------------------------
        // Hình trang trí (vẽ bằng vector, khung 24x24)

        private static readonly Geometry Heart = Geometry.Parse(
            "M12,21 C12,21 3,14.5 3,8.5 C3,5.4 5.4,3 8.3,3 C10,3 11.3,3.9 12,5 C12.7,3.9 14,3 15.7,3 C18.6,3 21,5.4 21,8.5 C21,14.5 12,21 12,21 Z");

        private static readonly Geometry Sparkle = Geometry.Parse(
            "M12,2 C12.8,8 16,11.2 22,12 C16,12.8 12.8,16 12,22 C11.2,16 8,12.8 2,12 C8,11.2 11.2,8 12,2 Z");

        private static readonly Geometry Leaf = Geometry.Parse(
            "M4,20 C4,10 10,4 20,4 C20,14 14,20 4,20 Z");

        private static Geometry Paw()
        {
            var g = new GeometryGroup { FillRule = FillRule.Nonzero };
            g.Children.Add(new EllipseGeometry(new Point(12, 15.5), 5.2, 4.4));
            g.Children.Add(new EllipseGeometry(new Point(5.6, 10), 2.2, 2.6));
            g.Children.Add(new EllipseGeometry(new Point(9.6, 5.8), 2.2, 2.7));
            g.Children.Add(new EllipseGeometry(new Point(14.4, 5.8), 2.2, 2.7));
            g.Children.Add(new EllipseGeometry(new Point(18.4, 10), 2.2, 2.6));
            g.Freeze();
            return g;
        }

        private static Geometry Blossom()
        {
            var g = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (int i = 0; i < 5; i++)
            {
                var petal = new EllipseGeometry(new Point(12, 6.5), 3.6, 5.2)
                {
                    Transform = new RotateTransform(i * 72, 12, 12)
                };
                g.Children.Add(petal);
            }
            g.Freeze();
            return g;
        }

        private static readonly Geometry PawGeo = Paw();
        private static readonly Geometry BlossomGeo = Blossom();

        private static FrameworkElement Shape(PopupTheme t, double size, double angle, double opacity, bool alt)
        {
            var color = B(alt && !string.IsNullOrEmpty(t.DecoColor2) ? t.DecoColor2 : t.DecoColor);
            FrameworkElement el;
            switch (t.Deco)
            {
                case "bubbles":
                    el = new Grid
                    {
                        Children =
                        {
                            new Ellipse { Stroke = color, StrokeThickness = Math.Max(1.2, size / 14), Fill = B("#33FFFFFF") },
                            new Ellipse
                            {
                                Fill = B("#CCFFFFFF"), Width = size * 0.22, Height = size * 0.16,
                                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(size * 0.2, size * 0.2, 0, 0)
                            }
                        }
                    };
                    break;
                default:
                    var geo = t.Deco switch
                    {
                        "hearts" => Heart,
                        "paws" => PawGeo,
                        "petals" => BlossomGeo,
                        "leaves" => Leaf,
                        "sparkles" => Sparkle,
                        _ => Heart
                    };
                    el = new Path { Data = geo, Fill = color, Stretch = Stretch.Uniform };
                    break;
            }
            el.Width = size;
            el.Height = size;
            el.Opacity = opacity;
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            el.RenderTransform = new RotateTransform(angle);
            el.IsHitTestVisible = false;
            return el;
        }

        /// <summary>Lớp hình trang trí rải ở các góc (không che nút ở góc trên phải).</summary>
        public static UIElement BuildDecorations(PopupTheme t, double scale = 1.0)
        {
            var grid = new Grid { IsHitTestVisible = false, ClipToBounds = true };
            if (t.Deco == "none") return grid;

            // (căn ngang, căn dọc, lề, cỡ, góc xoay, độ mờ, dùng màu phụ)
            var spots = new (HorizontalAlignment h, VerticalAlignment v, Thickness m, double size, double angle, double op, bool alt)[]
            {
                (HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, 10, 42), 36, 14, 0.45, false),
                (HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, 50, 66), 16, -18, 0.40, true),
                (HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, 18, 88), 11, 25, 0.35, true),
                (HorizontalAlignment.Left, VerticalAlignment.Bottom, new Thickness(8, 0, 0, 44), 18, -12, 0.30, true),
                (HorizontalAlignment.Right, VerticalAlignment.Center, new Thickness(0, 0, 6, 10), 12, 8, 0.28, false),
                (HorizontalAlignment.Left, VerticalAlignment.Top, new Thickness(140, 8, 0, 0), 10, -20, 0.35, true),
            };

            foreach (var s in spots)
            {
                var el = Shape(t, s.size * scale, s.angle, s.op, s.alt);
                el.HorizontalAlignment = s.h;
                el.VerticalAlignment = s.v;
                el.Margin = new Thickness(s.m.Left * scale, s.m.Top * scale, s.m.Right * scale, s.m.Bottom * scale);
                grid.Children.Add(el);
            }
            return grid;
        }

        /// <summary>Hai tai mèo nhô lên trên mép thẻ (chỉ mẫu có CatEars).</summary>
        public static UIElement BuildCatEars(PopupTheme t, double scale = 1.0)
        {
            var panel = new Canvas
            {
                IsHitTestVisible = false,
                Width = 76 * scale,
                Height = 20 * scale,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var outer = B(t.Background[0]);
            var stroke = B(t.Border);
            var inner = B("#FFC9B0");

            void Ear(double x, bool mirror)
            {
                var ear = new Path
                {
                    Data = Geometry.Parse("M0,20 L9,2 Q11,-1 14,2 L24,20 Z"),
                    Fill = outer,
                    Stroke = stroke,
                    StrokeThickness = 1,
                    Width = 24 * scale,
                    Height = 20 * scale,
                    Stretch = Stretch.Fill
                };
                var innerEar = new Path
                {
                    Data = Geometry.Parse("M6,20 L11,8 Q12,6.5 13,8 L18,20 Z"),
                    Fill = inner,
                    Width = 12 * scale,
                    Height = 12 * scale,
                    Stretch = Stretch.Fill
                };
                if (mirror)
                {
                    ear.RenderTransformOrigin = new Point(0.5, 1);
                    ear.RenderTransform = new RotateTransform(8);
                    innerEar.RenderTransformOrigin = new Point(0.5, 1);
                    innerEar.RenderTransform = new RotateTransform(8);
                }
                else
                {
                    ear.RenderTransformOrigin = new Point(0.5, 1);
                    ear.RenderTransform = new RotateTransform(-8);
                    innerEar.RenderTransformOrigin = new Point(0.5, 1);
                    innerEar.RenderTransform = new RotateTransform(-8);
                }
                Canvas.SetLeft(ear, x * scale);
                Canvas.SetTop(ear, 1 * scale);
                Canvas.SetLeft(innerEar, (x + 6) * scale);
                Canvas.SetTop(innerEar, 9 * scale);
                panel.Children.Add(ear);
                panel.Children.Add(innerEar);
            }

            Ear(4, false);
            Ear(46, true);
            return panel;
        }

        /// <summary>Huy hiệu vuông bo góc có 1 chữ Hán (thay cho logo 拼 ở đầu popup).</summary>
        public static FrameworkElement BuildEmblem(PopupTheme t, double size)
        {
            return new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size * 0.28),
                Background = EmblemBrush(t),
                Child = new TextBlock
                {
                    Text = t.Emblem,
                    FontFamily = new FontFamily("Microsoft YaHei UI, Microsoft YaHei"),
                    FontWeight = FontWeights.Bold,
                    FontSize = size * 0.56,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        /// <summary>Ảnh xem trước thu nhỏ của popup (dùng trong thẻ chọn mẫu).</summary>
        public static FrameworkElement BuildPreview(PopupTheme t)
        {
            var root = new Grid();
            root.Children.Add(new Border { Background = BackgroundBrush(t) });
            root.Children.Add(BuildDecorations(t, 0.85));

            var content = new StackPanel { Margin = new Thickness(14, 12, 14, 10) };

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(BuildEmblem(t, 18));
            header.Children.Add(new TextBlock
            {
                Text = "Pinyin",
                Foreground = B(t.Fg),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(7, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(header);

            var sample = new[] { ("你", "nǐ", 3), ("好", "hǎo", 3), ("朋", "péng", 2), ("友", "you", 5) };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            foreach (var (hz, py, tone) in sample)
            {
                var cell = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
                cell.Children.Add(new TextBlock
                {
                    Text = py,
                    FontSize = 11,
                    Foreground = B(t.Tones[tone]),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                cell.Children.Add(new TextBlock
                {
                    Text = hz,
                    FontSize = 22,
                    FontFamily = new FontFamily("Microsoft YaHei UI, Microsoft YaHei"),
                    Foreground = B(t.Hanzi),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                row.Children.Add(cell);
            }
            content.Children.Add(row);

            // dải chân (giống dòng pinyin thuần ở popup)
            content.Children.Add(new Border
            {
                Background = B(t.Footer),
                BorderBrush = B(t.Divider),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(-14, 8, -14, -10),
                Padding = new Thickness(14, 5, 14, 6),
                Child = new TextBlock { Text = "nǐ hǎo péngyou", FontSize = 10.5, Foreground = B(t.Sub) }
            });

            root.Children.Add(content);
            return root;
        }
    }
}
