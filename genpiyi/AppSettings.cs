using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

namespace genpiyi
{
    public sealed class AppSettings
    {
        /// <summary>Tự hiện pinyin khi copy (chuột phải → Sao chép) đoạn có chữ Hán.</summary>
        public bool AutoOnCopy { get; set; } = true;

        /// <summary>Chỉ tự hiện khi copy từ các app chat trong danh sách ChatApps.</summary>
        public bool OnlyChatApps { get; set; } = true;

        /// <summary>Tên tiến trình (không có .exe) của các app chat ĐANG BẬT. Mặc định chỉ Zalo + WeChat.</summary>
        public List<string> ChatApps { get; set; } = new(ChatAppScanner.DefaultEnabled);

        /// <summary>App người dùng tự thêm (vẫn hiện trong danh sách kể cả khi tắt).</summary>
        public List<string> CustomApps { get; set; } = new();

        /// <summary>Phiên bản định dạng file cài đặt (để chuyển đổi từ bản cũ).</summary>
        public int SettingsVersion { get; set; }

        // Danh sách mặc định của bản 1.0/1.1 (bật tất cả) – dùng khi chuyển đổi cài đặt cũ
        private static readonly string[] OldDefaultApps =
        {
            "Zalo", "WeChat", "Weixin", "WeChatAppEx", "WXWork", "LINE", "LineLauncher",
            "Telegram", "Messenger", "Skype", "QQ", "DingTalk", "WhatsApp", "Discord",
            "ms-teams", "Teams", "Slack", "KakaoTalk", "Feishu", "Lark"
        };

        /// <summary>Phím tắt: copy đoạn đang bôi đen rồi hiện pinyin (hoạt động ở mọi app).</summary>
        public string Hotkey { get; set; } = "Ctrl+Alt+P";

        /// <summary>"mark" = nǐ hǎo, "number" = ni3 hao3</summary>
        public string ToneStyle { get; set; } = "mark";

        public bool ToneColors { get; set; } = true;

        /// <summary>Id mẫu giao diện popup (xem ThemeCatalog): dark, light, strawberry, tabby, matcha, sakura, ocean, galaxy.</summary>
        public string PopupTheme { get; set; } = "dark";

        public global::genpiyi.PopupTheme GetTheme() => ThemeCatalog.Get(PopupTheme);

        public double HanziFontSize { get; set; } = 26;

        public bool StartWithWindows { get; set; } = false;

        /// <summary>"vi" hoặc "en". Trống = lấy theo ngôn ngữ Windows.</summary>
        public string Language { get; set; } = "";

        // ---------------------------------------------------------------

        public static string Folder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenPiYi");

        private static string FilePath => Path.Combine(Folder, "settings.json");

        public bool IsChatApp(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return false;
            return ChatApps.Any(a => string.Equals(a.Trim(), processName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                    if (s != null)
                    {
                        s.ChatApps ??= new List<string>();
                        s.CustomApps ??= new List<string>();
                        if (s.HanziFontSize < 14 || s.HanziFontSize > 60) s.HanziFontSize = 26;
                        if (s.SettingsVersion < 2) s.MigrateToV2();
                        return s;
                    }
                }
            }
            catch { /* file hỏng → dùng mặc định */ }
            return new AppSettings { SettingsVersion = 2 };
        }

        /// <summary>
        /// Bản cũ bật sẵn ~20 app. Chuyển sang: chỉ bật Zalo + WeChat, giữ lại các app người dùng tự thêm.
        /// </summary>
        private void MigrateToV2()
        {
            var custom = ChatApps
                .Where(a => !OldDefaultApps.Contains(a, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            CustomApps = CustomApps.Union(custom, StringComparer.OrdinalIgnoreCase).ToList();
            ChatApps = ChatAppScanner.DefaultEnabled.Union(custom, StringComparer.OrdinalIgnoreCase).ToList();
            SettingsVersion = 2;
            Save();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* ignore */ }
        }

        // ---------------- Khởi động cùng Windows ----------------

        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunName = "GenPiYi";

        public static void ApplyStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                                ?? Registry.CurrentUser.CreateSubKey(RunKey);
                if (enable)
                {
                    var exe = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exe))
                        key.SetValue(RunName, $"\"{exe}\" --tray");
                }
                else
                {
                    key.DeleteValue(RunName, throwOnMissingValue: false);
                }
            }
            catch { /* ignore */ }
        }
    }
}
