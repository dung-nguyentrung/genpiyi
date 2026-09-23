using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace genpiyi
{
    /// <summary>App chat đã biết: tên tiến trình, cách nhận diện khi đã cài.</summary>
    public sealed class KnownChatApp
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>Tên tiến trình (không .exe). Bật app = thêm tất cả tên này vào danh sách.</summary>
        public string[] Processes { get; init; } = Array.Empty<string>();
        /// <summary>Regex so với DisplayName trong mục "Apps &amp; features" (registry Uninstall).</summary>
        public string[] RegistryPatterns { get; init; } = Array.Empty<string>();
        /// <summary>Đường dẫn cài đặt thường gặp (có biến môi trường).</summary>
        public string[] KnownPaths { get; init; } = Array.Empty<string>();
        /// <summary>Tên thư mục gói Microsoft Store trong %LocalAppData%\Packages.</summary>
        public string[] StorePackages { get; init; } = Array.Empty<string>();
        public string Color { get; init; } = "#8A94A3";
    }

    /// <summary>Kết quả quét: một app hiển thị trong danh sách bật/tắt.</summary>
    public sealed class DetectedChatApp
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public string[] Processes { get; init; } = Array.Empty<string>();
        public bool Installed { get; set; }
        public bool Running { get; set; }
        public bool IsCustom { get; init; }
        public string? ExePath { get; set; }
        public ImageSource? Icon { get; set; }
        public string Color { get; init; } = "#8A94A3";
    }

    /// <summary>App khác đang mở cửa sổ (để người dùng tự thêm).</summary>
    public sealed class RunningWindowApp
    {
        public string Process { get; init; } = "";
        public string Title { get; init; } = "";
        public string? ExePath { get; init; }
        public ImageSource? Icon { get; set; }
    }

    public static class ChatAppScanner
    {
        public static readonly string[] DefaultEnabled = { "Zalo", "WeChat", "Weixin", "WeChatAppEx" };

        public static readonly IReadOnlyList<KnownChatApp> Known = new List<KnownChatApp>
        {
            new() { Key = "zalo", Name = "Zalo", Processes = new[] { "Zalo" }, Color = "#0068FF",
                    RegistryPatterns = new[] { @"^Zalo\b" },
                    KnownPaths = new[] { @"%LocalAppData%\Programs\Zalo\Zalo.exe" } },
            new() { Key = "wechat", Name = "WeChat (微信)", Processes = new[] { "Weixin", "WeChat", "WeChatAppEx" }, Color = "#07C160",
                    RegistryPatterns = new[] { @"^WeChat\b", @"^Weixin\b", @"^微信" },
                    KnownPaths = new[] { @"%ProgramFiles%\Tencent\Weixin\Weixin.exe", @"%ProgramFiles(x86)%\Tencent\WeChat\WeChat.exe",
                                         @"%ProgramFiles%\Tencent\WeChat\WeChat.exe" } },
            new() { Key = "wxwork", Name = "WeCom (企业微信)", Processes = new[] { "WXWork" }, Color = "#2B7BE5",
                    RegistryPatterns = new[] { @"企业微信", @"^WXWork", @"^WeCom\b" },
                    KnownPaths = new[] { @"%ProgramFiles(x86)%\WXWork\WXWork.exe", @"%ProgramFiles%\WXWork\WXWork.exe" } },
            new() { Key = "line", Name = "LINE", Processes = new[] { "LINE", "LineLauncher" }, Color = "#06C755",
                    RegistryPatterns = new[] { @"(?-i)^LINE(\s|$)" },
                    KnownPaths = new[] { @"%LocalAppData%\LINE\bin\LineLauncher.exe", @"%ProgramFiles(x86)%\LINE\bin\LineLauncher.exe" },
                    StorePackages = new[] { "NAVER.LINEwin8" } },
            new() { Key = "telegram", Name = "Telegram", Processes = new[] { "Telegram" }, Color = "#2AABEE",
                    RegistryPatterns = new[] { @"^Telegram( Desktop)?\b" },
                    KnownPaths = new[] { @"%AppData%\Telegram Desktop\Telegram.exe" },
                    StorePackages = new[] { "TelegramMessengerLLP.TelegramDesktop" } },
            new() { Key = "qq", Name = "QQ", Processes = new[] { "QQ" }, Color = "#12B7F5",
                    RegistryPatterns = new[] { @"^QQ(NT)?$", @"^腾讯QQ", @"^QQ\s" },
                    KnownPaths = new[] { @"%ProgramFiles%\Tencent\QQNT\QQ.exe", @"%ProgramFiles(x86)%\Tencent\QQNT\QQ.exe" } },
            new() { Key = "dingtalk", Name = "DingTalk (钉钉)", Processes = new[] { "DingTalk" }, Color = "#1677FF",
                    RegistryPatterns = new[] { @"钉钉", @"^DingTalk\b" },
                    KnownPaths = new[] { @"%ProgramFiles(x86)%\DingDing\main\current\DingTalk.exe" } },
            new() { Key = "feishu", Name = "Feishu / Lark (飞书)", Processes = new[] { "Feishu", "Lark" }, Color = "#3370FF",
                    RegistryPatterns = new[] { @"飞书", @"^Feishu\b", @"^Lark\b" },
                    KnownPaths = new[] { @"%LocalAppData%\Feishu\Feishu.exe", @"%LocalAppData%\Lark\Lark.exe" } },
            new() { Key = "viber", Name = "Viber", Processes = new[] { "Viber" }, Color = "#7360F2",
                    RegistryPatterns = new[] { @"^Viber\b" },
                    KnownPaths = new[] { @"%LocalAppData%\Viber\Viber.exe" } },
            new() { Key = "whatsapp", Name = "WhatsApp", Processes = new[] { "WhatsApp" }, Color = "#25D366",
                    RegistryPatterns = new[] { @"^WhatsApp\b" },
                    StorePackages = new[] { "WhatsAppDesktop" } },
            new() { Key = "messenger", Name = "Messenger", Processes = new[] { "Messenger" }, Color = "#0084FF",
                    RegistryPatterns = new[] { @"^Messenger$" },
                    StorePackages = new[] { "FACEBOOK.317180B0BB486", "Facebook.Messenger" } },
            new() { Key = "discord", Name = "Discord", Processes = new[] { "Discord" }, Color = "#5865F2",
                    RegistryPatterns = new[] { @"^Discord$" },
                    KnownPaths = new[] { @"%LocalAppData%\Discord\Update.exe" } },
            new() { Key = "skype", Name = "Skype", Processes = new[] { "Skype" }, Color = "#00AFF0",
                    RegistryPatterns = new[] { @"^Skype\b" },
                    StorePackages = new[] { "Microsoft.SkypeApp" } },
            new() { Key = "teams", Name = "Microsoft Teams", Processes = new[] { "ms-teams", "Teams" }, Color = "#5B5FC7",
                    RegistryPatterns = new[] { @"^Microsoft Teams\b" },
                    StorePackages = new[] { "MSTeams" } },
            new() { Key = "slack", Name = "Slack", Processes = new[] { "slack" }, Color = "#4A154B",
                    RegistryPatterns = new[] { @"^Slack\b" },
                    KnownPaths = new[] { @"%LocalAppData%\slack\slack.exe" } },
            new() { Key = "kakaotalk", Name = "KakaoTalk", Processes = new[] { "KakaoTalk" }, Color = "#F7C600",
                    RegistryPatterns = new[] { @"^KakaoTalk\b", @"^카카오톡" },
                    KnownPaths = new[] { @"%ProgramFiles(x86)%\Kakao\KakaoTalk\KakaoTalk.exe" } },
        };

        /// <summary>Tên tiến trình thuộc app đã biết → key (để phân biệt app tự thêm).</summary>
        public static bool IsKnownProcess(string process) =>
            Known.Any(k => k.Processes.Any(p => p.Equals(process, StringComparison.OrdinalIgnoreCase)));

        // ------------------------------------------------------------------

        private sealed class UninstallEntry
        {
            public string DisplayName = "";
            public string? DisplayIcon;
            public string? InstallLocation;
        }

        /// <summary>Quét máy (chạy ở luồng nền). Trả về app chat đã cài / đang chạy / đang bật.</summary>
        public static List<DetectedChatApp> Scan(IEnumerable<string> enabled, IEnumerable<string> custom)
        {
            var enabledSet = new HashSet<string>(enabled, StringComparer.OrdinalIgnoreCase);
            var customList = custom.ToList();
            var interesting = new HashSet<string>(customList.Concat(enabledSet).Concat(Known.SelectMany(k => k.Processes)),
                                                  StringComparer.OrdinalIgnoreCase);
            var running = GetRunningProcesses(interesting);
            var uninstall = ReadUninstallEntries();
            var packages = GetStorePackageNames();
            var result = new List<DetectedChatApp>();

            foreach (var k in Known)
            {
                var app = new DetectedChatApp { Key = k.Key, Name = k.Name, Processes = k.Processes, Color = k.Color };

                // 1) đang chạy
                foreach (var p in k.Processes)
                {
                    if (running.TryGetValue(p, out var path))
                    {
                        app.Running = true;
                        app.Installed = true;
                        app.ExePath ??= path;
                    }
                }

                // 2) đường dẫn cài đặt phổ biến
                foreach (var kp in k.KnownPaths)
                {
                    var full = Environment.ExpandEnvironmentVariables(kp);
                    if (File.Exists(full)) { app.Installed = true; app.ExePath ??= full; }
                }

                // 3) registry "Apps & features"
                foreach (var e in uninstall)
                {
                    if (!k.RegistryPatterns.Any(rp => SafeMatch(e.DisplayName, rp))) continue;
                    app.Installed = true;
                    app.ExePath ??= ExeFromEntry(e, k);
                }

                // 4) app Microsoft Store
                if (k.StorePackages.Any(sp => packages.Any(pk => pk.StartsWith(sp, StringComparison.OrdinalIgnoreCase))))
                    app.Installed = true;

                bool isEnabled = k.Processes.Any(enabledSet.Contains);
                if (app.Installed || app.Running || isEnabled) result.Add(app);
            }

            // App người dùng tự thêm (hoặc có trong danh sách bật nhưng không thuộc app đã biết)
            var customNames = customList.Concat(enabledSet)
                .Where(n => !IsKnownProcess(n))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var n in customNames)
            {
                var app = new DetectedChatApp { Key = "custom:" + n, Name = n, Processes = new[] { n }, IsCustom = true };
                if (running.TryGetValue(n, out var path)) { app.Running = true; app.Installed = true; app.ExePath = path; }
                result.Add(app);
            }

            foreach (var app in result)
                app.Icon = LoadIcon(app.ExePath);

            // Sắp xếp: đang bật → đang chạy → đã cài; giữ thứ tự danh mục
            return result
                .Select((a, i) => (a, i))
                .OrderByDescending(x => x.a.Processes.Any(enabledSet.Contains))
                .ThenByDescending(x => x.a.Running)
                .ThenBy(x => x.i)
                .Select(x => x.a)
                .ToList();
        }

        /// <summary>Các app đang mở cửa sổ (không thuộc danh sách hiện có) để người dùng tự thêm.</summary>
        public static List<RunningWindowApp> ScanRunningWindows(IEnumerable<string> exclude)
        {
            var ex = new HashSet<string>(exclude, StringComparer.OrdinalIgnoreCase)
            {
                Process.GetCurrentProcess().ProcessName, "explorer", "ApplicationFrameHost", "TextInputHost",
                "SystemSettings", "ShellExperienceHost", "SearchHost", "StartMenuExperienceHost", "LockApp", "devenv"
            };
            var list = new List<RunningWindowApp>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.MainWindowHandle == IntPtr.Zero) continue;
                    var title = p.MainWindowTitle;
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    if (ex.Contains(p.ProcessName) || list.Any(x => x.Process.Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase))) continue;
                    var path = GetProcessPath(p.Id);
                    list.Add(new RunningWindowApp { Process = p.ProcessName, Title = title, ExePath = path, Icon = LoadIcon(path) });
                }
                catch { /* bỏ qua tiến trình không truy cập được */ }
                finally { p.Dispose(); }
            }
            return list.OrderBy(x => x.Process, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ------------------------------------------------------------------

        private static bool SafeMatch(string input, string pattern)
        {
            try { return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase); }
            catch { return false; }
        }

        /// <summary>Tên tiến trình đang chạy → đường dẫn exe (chỉ tra đường dẫn cho các tên cần quan tâm).</summary>
        private static Dictionary<string, string?> GetRunningProcesses(HashSet<string> interesting)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    var name = p.ProcessName;
                    dict.TryGetValue(name, out var existing);
                    if (existing == null && interesting.Contains(name))
                        dict[name] = GetProcessPath(p.Id);
                    else if (!dict.ContainsKey(name))
                        dict[name] = null;
                }
                catch { /* ignore */ }
                finally { p.Dispose(); }
            }
            return dict;
        }

        private static List<UninstallEntry> ReadUninstallEntries()
        {
            var list = new List<UninstallEntry>();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            var roots = new (RegistryHive hive, RegistryView view)[]
            {
                (RegistryHive.LocalMachine, RegistryView.Registry64),
                (RegistryHive.LocalMachine, RegistryView.Registry32),
                (RegistryHive.CurrentUser, RegistryView.Default),
            };
            foreach (var (hive, view) in roots)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var key = baseKey.OpenSubKey(path);
                    if (key == null) continue;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var k = key.OpenSubKey(sub);
                            var name = k?.GetValue("DisplayName") as string;
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            list.Add(new UninstallEntry
                            {
                                DisplayName = name.Trim(),
                                DisplayIcon = k!.GetValue("DisplayIcon") as string,
                                InstallLocation = k.GetValue("InstallLocation") as string
                            });
                        }
                        catch { /* ignore */ }
                    }
                }
                catch { /* ignore */ }
            }
            return list;
        }

        private static List<string> GetStorePackageNames()
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
                return Directory.Exists(dir)
                    ? Directory.GetDirectories(dir).Select(d => Path.GetFileName(d) ?? "").ToList()
                    : new List<string>();
            }
            catch { return new List<string>(); }
        }

        private static string? ExeFromEntry(UninstallEntry e, KnownChatApp k)
        {
            if (!string.IsNullOrWhiteSpace(e.DisplayIcon))
            {
                var p = e.DisplayIcon.Trim().Trim('"');
                int comma = p.LastIndexOf(',');
                if (comma > 2) p = p[..comma].Trim().Trim('"');
                if (File.Exists(p) && (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)))
                    return p;
            }
            if (!string.IsNullOrWhiteSpace(e.InstallLocation))
            {
                foreach (var proc in k.Processes)
                {
                    var p = Path.Combine(e.InstallLocation.Trim('"'), proc + ".exe");
                    if (File.Exists(p)) return p;
                }
            }
            return null;
        }

        // ---------------- Đường dẫn exe của tiến trình ----------------

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder exeName, ref int size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr h);

        public static string? GetProcessPath(int pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                var sb = new StringBuilder(1024);
                int size = sb.Capacity;
                return QueryFullProcessImageName(h, 0, sb, ref size) ? sb.ToString() : null;
            }
            finally { CloseHandle(h); }
        }

        // ---------------- Icon ----------------

        private static readonly Dictionary<string, ImageSource?> IconCache = new(StringComparer.OrdinalIgnoreCase);

        public static ImageSource? LoadIcon(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            lock (IconCache)
            {
                if (IconCache.TryGetValue(path, out var cached)) return cached;
            }
            ImageSource? img = null;
            try
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    var src = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze(); // cho phép dùng ở luồng UI
                    img = src;
                }
            }
            catch { /* ignore */ }
            lock (IconCache) { IconCache[path] = img; }
            return img;
        }
    }
}
