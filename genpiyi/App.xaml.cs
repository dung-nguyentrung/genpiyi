using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace genpiyi
{
    public partial class App : Application
    {
        private const string MutexName = "GenPiYi_SingleInstance_7C2E";
        private const string ShowEventName = "GenPiYi_ShowMainWindow_7C2E";

        public static new App Current => (App)Application.Current;

        public AppSettings Settings { get; private set; } = new();

        private Mutex? _mutex;
        private EventWaitHandle? _showEvent;
        private MessageWindow? _msg;
        private TrayIcon? _tray;
        private global::genpiyi.MainWindow? _main;
        private PinyinPopup? _popup;
        private DispatcherTimer? _debounce;

        private string _pendingProcess = "";
        private DateTime _forceUntil = DateTime.MinValue;   // copy do phím tắt → luôn hiện
        private DateTime _ignoreUntil = DateTime.MinValue;  // copy do chính app → bỏ qua
        private string _lastText = "";
        private DateTime _lastShown = DateTime.MinValue;
        private readonly string _selfProcess = Process.GetCurrentProcess().ProcessName;

        public string HotkeyError { get; private set; } = "";

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Đã có một phiên chạy → báo nó mở cửa sổ rồi thoát
                try { EventWaitHandle.OpenExisting(ShowEventName).Set(); } catch { /* ignore */ }
                Shutdown();
                return;
            }

            base.OnStartup(e);
            DispatcherUnhandledException += (s, a) => { Log(a.Exception); a.Handled = true; };
            System.Windows.Forms.Application.EnableVisualStyles();

            Settings = AppSettings.Load();
            if (string.IsNullOrEmpty(Settings.Language))
            {
                Settings.Language = Loc.DetectDefault();
                Settings.Save();
            }
            Loc.Apply(Settings.Language);

            // Đồng bộ công tắc "Khởi động cùng Windows" với registry (bộ cài đặt có thể đã bật sẵn)
            bool registered = AppSettings.IsStartupRegistered();
            if (registered != Settings.StartWithWindows)
            {
                Settings.StartWithWindows = registered;
                Settings.Save();
            }

            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _debounce.Tick += (s, a) => { _debounce.Stop(); ProcessClipboard(DateTime.Now < _forceUntil, _pendingProcess); };

            _msg = new MessageWindow();
            _msg.ClipboardChanged += OnClipboardChanged;
            _msg.HotkeyPressed += OnHotkey;
            ApplyHotkey();

            _tray = new TrayIcon(this);

            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            var listener = new Thread(() =>
            {
                while (true)
                {
                    try { _showEvent.WaitOne(); } catch { return; }
                    Dispatcher.BeginInvoke(new Action(ShowMainWindow));
                }
            }) { IsBackground = true };
            listener.Start();

            Task.Run(() => { DictionaryService.Warmup(); PinyinService.Warmup(); });

            bool startedHidden = e.Args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
            if (startedHidden)
            {
                _tray.ShowBalloon(Loc.T("tray.running"), Loc.T("tray.runningMsg"));
            }
            else
            {
                ShowMainWindow();
            }
        }

        // ------------------------------------------------------------------

        public bool ApplyHotkey()
        {
            if (_msg == null) return false;
            bool ok = _msg.RegisterHotkey(Settings.Hotkey, out var err);
            HotkeyError = ok ? "" : err;
            return ok;
        }

        /// <summary>Tạm gỡ phím tắt (khi đang ghi phím tắt mới trong cài đặt).</summary>
        public void SuspendHotkey() => _msg?.UnregisterHotkey();

        /// <summary>Đổi ngôn ngữ giao diện (vi / en).</summary>
        public void SetLanguage(string lang)
        {
            if (Settings.Language == lang) return;
            Settings.Language = lang;
            Loc.Apply(lang);
            SettingsChanged();
        }

        public void SettingsChanged()
        {
            Settings.Save();
            _tray?.Refresh();
            _main?.ReloadSettings();
        }

        public void ShowMainWindow()
        {
            if (_main == null)
            {
                _main = new global::genpiyi.MainWindow();
            }
            _main.ReloadSettings();
            _main.Show();
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.Activate();
        }

        public void ShowFromClipboard() => ProcessClipboard(force: true, processName: "");

        public void CopyToClipboard(string text)
        {
            _ignoreUntil = DateTime.Now.AddMilliseconds(1000);
            ClipboardHelper.TrySetText(text);
        }

        public void ExitApp()
        {
            try { _popup?.Close(); } catch { /* ignore */ }
            _tray?.Dispose(); _tray = null;
            _msg?.Dispose(); _msg = null;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            _msg?.Dispose();
            try { _mutex?.ReleaseMutex(); } catch { /* ignore */ }
            base.OnExit(e);
        }

        // ------------------------------------------------------------------

        private void OnClipboardChanged()
        {
            // Ghi nhận app đang focus ngay lúc copy (vd: Zalo/WeChat), rồi đợi clipboard ổn định
            _pendingProcess = NativeMethods.GetForegroundProcessName();
            _debounce?.Stop();
            _debounce?.Start();
        }

        private async void OnHotkey()
        {
            try
            {
                string proc = NativeMethods.GetForegroundProcessName();
                uint seq = NativeMethods.GetClipboardSequenceNumber();

                // Đợi người dùng nhả Ctrl/Alt/Shift để Ctrl+C giả lập không bị lẫn phím
                var start = DateTime.Now;
                while (NativeMethods.AnyModifierDown() && (DateTime.Now - start).TotalMilliseconds < 1500)
                    await Task.Delay(20);

                _forceUntil = DateTime.Now.AddMilliseconds(1200);
                NativeMethods.SendCtrlC();
                await Task.Delay(450);

                if (NativeMethods.GetClipboardSequenceNumber() == seq)
                {
                    // Không có gì được bôi đen → dùng nội dung clipboard hiện có
                    _forceUntil = DateTime.MinValue;
                    ProcessClipboard(force: true, processName: proc);
                }
            }
            catch (Exception ex) { Log(ex); }
        }

        private void ProcessClipboard(bool force, string processName)
        {
            if (!force)
            {
                if (!Settings.AutoOnCopy) return;
                if (DateTime.Now < _ignoreUntil) return;
                if (string.Equals(processName, _selfProcess, StringComparison.OrdinalIgnoreCase)) return;
                if (Settings.OnlyChatApps && !Settings.IsChatApp(processName)) return;
            }

            var text = ClipboardHelper.TryGetText();
            if (string.IsNullOrWhiteSpace(text) || !PinyinService.ContainsHan(text))
            {
                if (force) _tray?.ShowBalloon("GenPiYi", Loc.T("tray.noHan"));
                return;
            }

            text = text.Trim();
            if (!force && text == _lastText && (DateTime.Now - _lastShown).TotalSeconds < 1.5) return;
            if (text.Length > 3000) text = text.Substring(0, 3000) + "…";

            _lastText = text;
            _lastShown = DateTime.Now;
            _forceUntil = DateTime.MinValue;
            ShowPopup(text, processName);
        }

        public void ShowPopup(string text, string source, PopupTheme? theme = null)
        {
            if (_popup != null && !_popup.IsPinned)
            {
                try { _popup.Close(); } catch { /* ignore */ }
            }
            var popup = new PinyinPopup(text, source, theme);
            popup.Closed += (s, e) => { if (ReferenceEquals(_popup, s)) _popup = null; };
            _popup = popup;
            popup.Show();
        }

        public static void Log(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppSettings.Folder);
                File.AppendAllText(Path.Combine(AppSettings.Folder, "error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
            }
            catch { /* ignore */ }
        }
    }
}
