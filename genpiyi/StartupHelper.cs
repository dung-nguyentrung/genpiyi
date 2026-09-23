using System.Threading.Tasks;

namespace genpiyi
{
    /// <summary>"Khởi động cùng Windows": ghi / xoá khoá registry HKCU\...\Run (xem AppSettings.ApplyStartup).</summary>
    public static class StartupHelper
    {
        /// <summary>Bật/tắt. Trả về (trạng thái sau khi đổi, thông báo lỗi nếu có).</summary>
        public static Task<(bool Enabled, string? Message)> SetEnabledAsync(bool enable)
        {
            AppSettings.ApplyStartup(enable);
            return Task.FromResult<(bool, string?)>((enable, null));
        }
    }
}
