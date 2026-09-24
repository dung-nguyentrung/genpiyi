using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace genpiyi
{
    /// <summary>
    /// Đa ngôn ngữ Việt / Anh.
    /// - XAML dùng {DynamicResource S.&lt;key&gt;} → đổi ngôn ngữ là cập nhật ngay.
    /// - Code C# dùng Loc.T("key") / Loc.F("key", args).
    /// </summary>
    public static class Loc
    {
        public static string Lang { get; private set; } = "vi";
        public static bool IsEn => Lang == "en";

        public static event Action? LanguageChanged;

        private static readonly Dictionary<string, (string Vi, string En)> S = new()
        {
            // ---------- Chung ----------
            ["app.tagline"] = ("Pinyin cho tin nhắn tiếng Trung", "Pinyin for Chinese chat messages"),
            ["cap.min"] = ("Thu nhỏ", "Minimize"),
            ["cap.close"] = ("Ẩn xuống khay hệ thống", "Hide to system tray"),
            ["cap.lang"] = ("Đổi ngôn ngữ (Tiếng Việt / English)", "Change language (English / Tiếng Việt)"),
            ["common.copyPinyin"] = ("Copy pinyin", "Copy pinyin"),
            ["common.copied"] = ("Đã copy", "Copied"),
            ["common.add"] = ("Thêm", "Add"),

            // ---------- Điều hướng ----------
            ["nav.try"] = ("Tra pinyin", "Pinyin lookup"),
            ["nav.theme"] = ("Giao diện", "Themes"),
            ["nav.settings"] = ("Cài đặt", "Settings"),
            ["nav.guide"] = ("Hướng dẫn", "Guide"),

            // ---------- Trạng thái bộ pinyin ----------
            ["engine.ok"] = ("Sẵn sàng · offline", "Ready · offline"),
            ["engine.okTip"] = ("Bộ chuyển pinyin ToolGood.Words.Pinyin đã nạp", "ToolGood.Words.Pinyin engine loaded"),
            ["engine.fail"] = ("Chưa nạp được bộ pinyin", "Pinyin engine not loaded"),
            ["engine.failTip"] = ("Hãy Restore NuGet packages rồi build lại.", "Restore NuGet packages and rebuild."),
            ["engine.failPopup"] = ("Chưa nạp được thư viện pinyin.", "Pinyin library not loaded."),
            ["engine.errNoClass"] = ("Không tìm thấy lớp WordsHelper trong ToolGood.Words.Pinyin.", "WordsHelper class not found in ToolGood.Words.Pinyin."),
            ["engine.errNoMethod"] = ("Phiên bản ToolGood.Words.Pinyin không có hàm GetPinyinList/GetPinyin.", "This ToolGood.Words.Pinyin version has no GetPinyinList/GetPinyin."),
            ["engine.errLoad"] = ("Không nạp được thư viện pinyin: ", "Could not load the pinyin library: "),

            // ---------- Trang Tra pinyin ----------
            ["try.subtitle"] = ("Gõ hoặc dán đoạn tiếng Trung để xem pinyin. Trong app chat chỉ cần chuột phải → Sao chép.",
                                "Type or paste Chinese text to see its pinyin. In chat apps, just right-click → Copy."),
            ["try.pasteTip"] = ("Dán từ clipboard", "Paste from clipboard"),
            ["try.paste"] = ("Dán", "Paste"),
            ["try.clearTip"] = ("Xoá nội dung", "Clear text"),
            ["try.clear"] = ("Xoá", "Clear"),
            ["try.input"] = ("Văn bản tiếng Trung", "Chinese text"),
            ["try.placeholder"] = ("Ví dụ: 你好！今天晚上我们去哪里吃饭？", "e.g. 你好！今天晚上我们去哪里吃饭？"),
            ["try.previewTip"] = ("Hiện popup giống như khi copy tin nhắn", "Show the popup as if you copied a message"),
            ["try.preview"] = ("Xem thử popup", "Preview popup"),
            ["try.result"] = ("Kết quả", "Result"),

            // ---------- Trang Cài đặt ----------
            ["set.subtitle"] = ("Mọi thay đổi được lưu ngay.", "Changes are saved instantly."),
            ["set.groupActivity"] = ("Hoạt động", "Behavior"),
            ["set.auto"] = ("Tự hiện pinyin khi copy", "Show pinyin on copy"),
            ["set.autoDesc"] = ("Chuột phải vào tin nhắn → Sao chép, popup pinyin hiện ngay cạnh con trỏ.",
                                "Right-click a message → Copy and the pinyin popup appears next to the cursor."),
            ["set.chatOnly"] = ("Chỉ với app chat", "Chat apps only"),
            ["set.chatOnlyDesc"] = ("Không tự hiện khi copy trong trình duyệt, Word… (phím tắt vẫn dùng được ở mọi nơi).",
                                    "Don't pop up when copying in browsers, Word… (the hotkey still works everywhere)."),
            ["set.scanTip"] = ("Quét lại các app trên máy", "Rescan apps on this PC"),
            ["set.rescan"] = ("Quét lại", "Rescan"),
            ["set.scanning"] = ("Đang quét app trên máy…", "Scanning apps on this PC…"),
            ["set.addOthers"] = ("Thêm app khác đang mở", "Add another open app"),
            ["set.othersDesc"] = ("Các ứng dụng đang mở cửa sổ trên máy — bấm Thêm để đưa vào danh sách:",
                                  "Apps with an open window on this PC — click Add to include one:"),
            ["set.processPlaceholder"] = ("Hoặc gõ tên tiến trình, vd: Zalo, Weixin", "Or type a process name, e.g. Zalo, Weixin"),
            ["set.processHint"] = ("Tên tiến trình xem trong Task Manager → Details (bỏ đuôi .exe).",
                                   "Find process names in Task Manager → Details (without .exe)."),
            ["set.hotkey"] = ("Phím tắt", "Hotkey"),
            ["set.hotkeyDesc"] = ("Bôi đen chữ ở bất kỳ app nào rồi bấm. Click vào ô bên phải và nhấn tổ hợp phím mới để đổi.",
                                  "Select text in any app, then press it. Click the box on the right and press a new combo to change it."),
            ["set.groupDisplay"] = ("Hiển thị", "Display"),
            ["set.toneStyle"] = ("Kiểu pinyin", "Pinyin style"),
            ["set.toneStyleDesc"] = ("Dấu thanh hoặc số thanh điệu.", "Tone marks or tone numbers."),
            ["set.fontSize"] = ("Cỡ chữ Hán", "Hanzi size"),
            ["set.fontSizeDesc"] = ("Pinyin tự co theo tỉ lệ.", "Pinyin scales proportionally."),
            ["set.toneColors"] = ("Tô màu theo thanh điệu", "Color by tone"),
            ["set.popupTheme"] = ("Giao diện popup", "Popup theme"),
            ["set.using"] = ("Đang dùng: ", "Current: "),
            ["set.chooseTheme"] = ("Chọn mẫu", "Choose"),
            ["set.language"] = ("Ngôn ngữ", "Language"),
            ["set.languageDesc"] = ("Ngôn ngữ hiển thị của app.", "Display language of the app."),
            ["set.groupSystem"] = ("Hệ thống", "System"),
            ["set.startup"] = ("Khởi động cùng Windows", "Start with Windows"),
            ["set.startupDesc"] = ("Chạy ẩn ở khay hệ thống khi bật máy.", "Run hidden in the system tray when you sign in."),

            // ---------- Danh sách app chat ----------
            ["scan.none"] = ("Không tìm thấy app chat nào trên máy.", "No chat apps found on this PC."),
            ["scan.found"] = ("Tìm thấy {0} app chat trên máy · đang bật {1}", "Found {0} chat apps on this PC · {1} enabled"),
            ["scan.empty"] = ("Chưa có app nào. Bấm “Thêm app khác đang mở” để tự thêm.", "No apps yet. Click “Add another open app” to add one."),
            ["app.running"] = ("Đang chạy", "Running"),
            ["app.installed"] = ("Đã cài", "Installed"),
            ["app.notFound"] = ("Không tìm thấy trên máy", "Not found on this PC"),
            ["app.custom"] = (" · tự thêm", " · added by you"),
            ["app.processes"] = ("Tiến trình: ", "Process: "),
            ["app.remove"] = ("Xoá khỏi danh sách", "Remove from list"),
            ["app.onTip"] = ("Đang bật – copy trong app này sẽ hiện pinyin", "On – copying in this app shows pinyin"),
            ["app.offTip"] = ("Đang tắt", "Off"),
            ["others.loading"] = ("Đang tìm các app đang mở…", "Looking for open apps…"),
            ["others.none"] = ("Không có app nào khác đang mở.", "No other apps are open."),

            // ---------- Phím tắt ----------
            ["hotkey.press"] = ("Nhấn tổ hợp phím…", "Press a key combo…"),
            ["hotkey.escCancel"] = ("Esc để huỷ", "Esc to cancel"),
            ["hotkey.active"] = ("✓ Đang hoạt động", "✓ Active"),
            ["hotkey.taken"] = ("Phím tắt đã được ứng dụng khác sử dụng.", "This hotkey is already used by another app."),
            ["hotkey.empty"] = ("Chưa nhập phím tắt.", "No hotkey entered."),
            ["hotkey.noKey"] = ("Thiếu phím chính (vd: P).", "Missing the main key (e.g. P)."),
            ["hotkey.unknown"] = ("Không nhận ra phím: ", "Unknown key: "),
            ["hotkey.needMod"] = ("Cần ít nhất một phím Ctrl/Alt/Shift/Win (trừ F1–F24).", "Needs at least one of Ctrl/Alt/Shift/Win (except F1–F24)."),

            // ---------- Màu thanh điệu ----------
            ["tone.neutral"] = ("nhẹ", "neutral"),

            // ---------- Trang Giao diện ----------
            ["theme.subtitle"] = ("Chọn mẫu cho popup pinyin. Bấm vào ảnh để xem thử, bấm “Dùng” để áp dụng.",
                                  "Pick a look for the pinyin popup. Click a preview to try it, click “Use” to apply."),
            ["theme.featured"] = ("Mẫu nổi bật", "Featured"),
            ["theme.basic"] = ("Cơ bản", "Basic"),
            ["theme.previewTip"] = ("Bấm để xem thử popup", "Click to preview the popup"),
            ["theme.previewSource"] = ("Xem thử", "Preview"),
            ["theme.use"] = ("Dùng", "Use"),
            ["theme.inUse"] = ("Đang dùng", "In use"),
            ["badge.new"] = ("Mới", "New"),
            ["badge.hot"] = ("Hot", "Hot"),

            // ---------- Trang Hướng dẫn ----------
            ["guide.subtitle"] = ("Ba cách xem pinyin của tin nhắn tiếng Trung.", "Three ways to see the pinyin of Chinese messages."),
            ["guide.s1"] = ("Chuột phải → Sao chép", "Right-click → Copy"),
            ["guide.s1Desc"] = ("Trong Zalo, WeChat, LINE…, chuột phải vào tin nhắn và chọn Sao chép / Copy / 复制. Popup pinyin hiện ngay cạnh con trỏ chuột.",
                                "In Zalo, WeChat, LINE…, right-click a message and choose Copy / Sao chép / 复制. The pinyin popup appears right next to the cursor."),
            ["guide.s2"] = ("Bôi đen + phím tắt ", "Select + hotkey "),
            ["guide.s2Desc"] = ("Dùng được ở mọi ứng dụng: trình duyệt, Word, PDF… Nếu không bôi đen gì, app lấy nội dung đang có trong clipboard.",
                                "Works in any app: browsers, Word, PDF… If nothing is selected, the app uses what's already on the clipboard."),
            ["guide.s3"] = ("Trong popup", "In the popup"),
            ["guide.s3Desc"] = ("Rê chuột vào chữ để xem cách đọc (chữ có gạch chấm là chữ đa âm). Nút sách mở danh sách từ kèm nghĩa; nút copy pinyin / copy cả chữ Hán + pinyin; nút ghim để giữ popup. Click ra ngoài hoặc Esc để đóng — popup không chiếm focus nên vẫn gõ chat bình thường.",
                                "Hover a character to see its readings (a dotted underline marks characters with several readings). The book button lists each word with its meaning; other buttons copy the pinyin or hanzi + pinyin; the pin keeps the popup open. Click outside or press Esc to close — the popup never steals focus, so you can keep typing."),
            ["guide.tray"] = ("Đóng cửa sổ này app vẫn chạy nền ở khay hệ thống (biểu tượng 拼 góc phải thanh taskbar). Muốn tắt hẳn: chuột phải biểu tượng → Thoát.",
                              "Closing this window keeps the app running in the system tray (the 拼 icon at the right of the taskbar). To quit: right-click the icon → Exit."),

            // ---------- Popup ----------
            ["popup.copyBoth"] = ("Copy chữ Hán + pinyin", "Copy hanzi + pinyin"),
            ["popup.pin"] = ("Ghim popup", "Pin popup"),
            ["popup.unpin"] = ("Bỏ ghim", "Unpin"),
            ["popup.close"] = ("Đóng (Esc)", "Close (Esc)"),
            ["popup.vocab"] = ("Xem từ vựng & nghĩa", "Show vocabulary & meanings"),
            ["popup.vocabHide"] = ("Ẩn từ vựng", "Hide vocabulary"),

            // ---------- Từ điển ----------
            ["dict.hv"] = ("Hán Việt: ", "Sino-Vietnamese: "),
            ["set.vocab"] = ("Hiện nghĩa từ vựng", "Show word meanings"),
            ["set.vocabDesc"] = ("Popup có thêm danh sách từ kèm nghĩa (bật/tắt nhanh bằng nút sách). Rê chuột vào chữ để xem nghĩa của cả từ.",
                                 "The popup lists each word with its meaning (toggle with the book button). Hover a character to see the meaning of the whole word."),
            ["set.meaningLang"] = ("Ngôn ngữ của nghĩa", "Meaning language"),
            ["set.meaningLangDesc"] = ("Nghĩa tiếng Việt từ CVDICT, tiếng Anh từ CC-CEDICT.", "Vietnamese meanings from CVDICT, English from CC-CEDICT."),
            ["set.meanVi"] = ("Tiếng Việt", "Vietnamese"),
            ["set.meanEn"] = ("English", "English"),
            ["set.meanBoth"] = ("Cả hai", "Both"),
            ["set.hanviet"] = ("Hiện âm Hán Việt", "Show Sino-Vietnamese readings"),
            ["set.hanvietDesc"] = ("Ví dụ 银行 → NGÂN HÀNG.", "e.g. 银行 → NGÂN HÀNG (the Vietnamese reading of the characters)."),
            ["set.dictMissing"] = ("Bản build này chưa có dữ liệu từ điển. Chạy python tools/build_dict.py rồi build lại.",
                                   "This build has no dictionary data. Run python tools/build_dict.py and rebuild."),
            ["set.dictCredit"] = ("Dữ liệu: CVDICT, CC-CEDICT (CC BY-SA 4.0) · Unihan (Unicode). {0:N0} mục từ.",
                                  "Data: CVDICT, CC-CEDICT (CC BY-SA 4.0) · Unihan (Unicode). {0:N0} entries."),
            ["try.vocab"] = ("Từ vựng", "Vocabulary"),

            // ---------- Khay hệ thống ----------
            ["tray.tooltip"] = ("GenPiYi – xem pinyin tin nhắn tiếng Trung", "GenPiYi – pinyin for Chinese messages"),
            ["tray.open"] = ("Mở GenPiYi", "Open GenPiYi"),
            ["tray.fromClipboard"] = ("Xem pinyin nội dung đã copy", "Show pinyin of copied text"),
            ["tray.auto"] = ("Tự hiện khi copy chữ Trung", "Show when copying Chinese"),
            ["tray.exit"] = ("Thoát", "Exit"),
            ["tray.running"] = ("GenPiYi đang chạy", "GenPiYi is running"),
            ["tray.runningMsg"] = ("Copy tin nhắn tiếng Trung (chuột phải → Sao chép) để xem pinyin.", "Copy a Chinese message (right-click → Copy) to see its pinyin."),
            ["tray.noHan"] = ("Không thấy chữ Hán trong nội dung vừa copy.", "No Chinese characters in the copied text."),
        };

        public static string T(string key) =>
            S.TryGetValue(key, out var v) ? (IsEn ? v.En : v.Vi) : key;

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Ngôn ngữ mặc định theo Windows: máy tiếng Việt → vi, còn lại → en.</summary>
        public static string DetectDefault() =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

        /// <summary>Đổi ngôn ngữ: nạp toàn bộ chuỗi vào tài nguyên App (key "S.xxx") để XAML tự cập nhật.</summary>
        public static void Apply(string? lang)
        {
            Lang = lang == "en" ? "en" : "vi";
            var res = Application.Current?.Resources;
            if (res != null)
            {
                foreach (var kv in S)
                    res["S." + kv.Key] = IsEn ? kv.Value.En : kv.Value.Vi;
            }
            LanguageChanged?.Invoke();
        }
    }
}
