using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace genpiyi
{
    internal static class ClipboardHelper
    {
        /// <summary>Đọc text clipboard, thử lại vài lần vì app khác có thể đang giữ clipboard.</summary>
        public static string? TryGetText()
        {
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    if (!Clipboard.ContainsText()) return null;
                    return Clipboard.GetText(TextDataFormat.UnicodeText);
                }
                catch (ExternalException) { Thread.Sleep(40); }
                catch (Exception) { return null; }
            }
            return null;
        }

        public static bool TrySetText(string text)
        {
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true);
                    return true;
                }
                catch (ExternalException) { Thread.Sleep(40); }
                catch (Exception) { return false; }
            }
            return false;
        }
    }

    internal static class IconFactory
    {
        private static Drawing.Icon? _icon;
        private static ImageSource? _image;

        /// <summary>Vẽ logo: ô vuông bo góc màu đỏ (gradient) có chữ 拼.</summary>
        private static Drawing.Bitmap Render(int size)
        {
            var bmp = new Drawing.Bitmap(size, size);
            using var g = Drawing.Graphics.FromImage(bmp);
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Drawing.Color.Transparent);

            float r = size * 0.24f;
            var rect = new Drawing.RectangleF(0.5f, 0.5f, size - 1f, size - 1f);
            using var path = new Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();

            using var bg = new Drawing.Drawing2D.LinearGradientBrush(rect,
                Drawing.Color.FromArgb(255, 107, 107), Drawing.Color.FromArgb(214, 54, 60), 45f);
            g.FillPath(bg, path);

            using var font = new Drawing.Font("Microsoft YaHei", size * 0.58f, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var sf = new Drawing.StringFormat
            {
                Alignment = Drawing.StringAlignment.Center,
                LineAlignment = Drawing.StringAlignment.Center
            };
            g.DrawString("拼", font, Drawing.Brushes.White, new Drawing.RectangleF(0, size * 0.03f, size, size), sf);
            return bmp;
        }

        /// <summary>Icon cho khay hệ thống.</summary>
        public static Drawing.Icon GetIcon()
        {
            if (_icon != null) return _icon;
            using var bmp = Render(32);
            _icon = Drawing.Icon.FromHandle(bmp.GetHicon());
            return _icon;
        }

        /// <summary>Icon cho cửa sổ (độ phân giải cao hơn).</summary>
        public static ImageSource GetImageSource()
        {
            if (_image != null) return _image;
            using var bmp = Render(64);
            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            _image = img;
            return _image;
        }

    }
}
