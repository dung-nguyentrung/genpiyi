using System;
using System.Windows.Input;
using System.Windows.Interop;

namespace genpiyi
{
    /// <summary>
    /// Cửa sổ ẩn (message-only) nhận thông báo clipboard thay đổi và phím tắt toàn cục.
    /// </summary>
    internal sealed class MessageWindow : IDisposable
    {
        public const int HotkeyId = 0x5059; // "PY"

        private readonly HwndSource _source;
        private bool _hotkeyRegistered;

        public event Action? ClipboardChanged;
        public event Action? HotkeyPressed;

        public MessageWindow()
        {
            var p = new HwndSourceParameters("GenPiYiMessageWindow")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
            NativeMethods.AddClipboardFormatListener(_source.Handle);
        }

        public IntPtr Handle => _source.Handle;

        /// <summary>Đăng ký phím tắt dạng "Ctrl+Alt+P". Trả về false nếu sai cú pháp hoặc phím đã bị app khác chiếm.</summary>
        public bool RegisterHotkey(string text, out string error)
        {
            UnregisterHotkey();
            if (!TryParseHotkey(text, out uint mods, out uint vk, out error)) return false;
            _hotkeyRegistered = NativeMethods.RegisterHotKey(Handle, HotkeyId, mods | NativeMethods.MOD_NOREPEAT, vk);
            if (!_hotkeyRegistered) error = Loc.T("hotkey.taken");
            return _hotkeyRegistered;
        }

        public void UnregisterHotkey()
        {
            if (_hotkeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, HotkeyId);
                _hotkeyRegistered = false;
            }
        }

        public static bool TryParseHotkey(string text, out uint mods, out uint vk, out string error)
        {
            mods = 0; vk = 0; error = "";
            if (string.IsNullOrWhiteSpace(text)) { error = Loc.T("hotkey.empty"); return false; }

            string? keyPart = null;
            foreach (var raw in text.Split('+'))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                switch (part.ToLowerInvariant())
                {
                    case "ctrl":
                    case "control": mods |= NativeMethods.MOD_CONTROL; break;
                    case "alt": mods |= NativeMethods.MOD_ALT; break;
                    case "shift": mods |= NativeMethods.MOD_SHIFT; break;
                    case "win":
                    case "windows": mods |= NativeMethods.MOD_WIN; break;
                    default: keyPart = part; break;
                }
            }

            if (keyPart == null) { error = Loc.T("hotkey.noKey"); return false; }

            Key key;
            try
            {
                var converted = new KeyConverter().ConvertFromInvariantString(keyPart);
                if (converted is not Key k) { error = Loc.T("hotkey.unknown") + keyPart; return false; }
                key = k;
            }
            catch
            {
                error = Loc.T("hotkey.unknown") + keyPart;
                return false;
            }

            bool isFunctionKey = key >= Key.F1 && key <= Key.F24;
            if (mods == 0 && !isFunctionKey)
            {
                error = Loc.T("hotkey.needMod");
                return false;
            }

            vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            return vk != 0;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                ClipboardChanged?.Invoke();
                handled = true;
            }
            else if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            try
            {
                UnregisterHotkey();
                NativeMethods.RemoveClipboardFormatListener(Handle);
                _source.RemoveHook(WndProc);
                _source.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}
