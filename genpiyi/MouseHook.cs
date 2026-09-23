using System;
using System.Runtime.InteropServices;

namespace genpiyi
{
    /// <summary>
    /// Hook chuột toàn cục (chỉ bật khi popup đang mở) để đóng popup khi người dùng click ra ngoài,
    /// mà không cần lấy focus khỏi ứng dụng chat.
    /// </summary>
    internal sealed class MouseHook : IDisposable
    {
        private readonly NativeMethods.LowLevelMouseProc _proc; // giữ tham chiếu để không bị GC
        private readonly Action<int, int> _onButtonDown;
        private IntPtr _hook;

        public MouseHook(Action<int, int> onButtonDown)
        {
            _onButtonDown = onButtonDown;
            _proc = Callback;
            _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, NativeMethods.GetModuleHandle(null), 0);
        }

        private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int m = wParam.ToInt32();
                if (m == NativeMethods.WM_LBUTTONDOWN || m == NativeMethods.WM_RBUTTONDOWN || m == NativeMethods.WM_MBUTTONDOWN)
                {
                    try
                    {
                        var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                        _onButtonDown(info.pt.X, info.pt.Y);
                    }
                    catch { /* không để lỗi làm treo chuột */ }
                }
            }
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
