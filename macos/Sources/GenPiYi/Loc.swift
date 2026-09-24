import Foundation
import Combine

/// Đa ngôn ngữ Việt / Anh. View SwiftUI quan sát `Loc.shared` nên đổi ngôn ngữ là cập nhật ngay.
final class Loc: ObservableObject {
    static let shared = Loc()

    @Published private(set) var lang: String = "vi"
    var isEn: Bool { lang == "en" }

    func apply(_ l: String?) { lang = (l == "en") ? "en" : "vi" }

    static func detectDefault() -> String {
        let code = Locale.preferredLanguages.first ?? "en"
        return code.lowercased().hasPrefix("vi") ? "vi" : "en"
    }

    static func t(_ key: String) -> String {
        guard let v = strings[key] else { return key }
        return shared.isEn ? v.1 : v.0
    }

    static func f(_ key: String, _ args: CVarArg...) -> String {
        String(format: t(key), arguments: args)
    }

    private static let strings: [String: (String, String)] = [
        // Chung
        "app.tagline": ("Pinyin cho tin nhắn tiếng Trung", "Pinyin for Chinese chat messages"),
        "common.copyPinyin": ("Copy pinyin", "Copy pinyin"),
        "common.copied": ("Đã copy", "Copied"),
        "common.add": ("Thêm", "Add"),
        "common.open": ("Mở", "Open"),

        // Điều hướng
        "nav.try": ("Tra pinyin", "Pinyin lookup"),
        "nav.theme": ("Giao diện", "Themes"),
        "nav.settings": ("Cài đặt", "Settings"),
        "nav.guide": ("Hướng dẫn", "Guide"),

        "engine.ok": ("Sẵn sàng · offline", "Ready · offline"),

        // Tra pinyin
        "try.subtitle": ("Gõ hoặc dán đoạn tiếng Trung để xem pinyin. Trong app chat chỉ cần chuột phải → Sao chép (hoặc ⌘C).",
                         "Type or paste Chinese text to see its pinyin. In chat apps, just right-click → Copy (or ⌘C)."),
        "try.paste": ("Dán", "Paste"),
        "try.clear": ("Xoá", "Clear"),
        "try.placeholder": ("Ví dụ: 你好！今天晚上我们去哪里吃饭？", "e.g. 你好！今天晚上我们去哪里吃饭？"),
        "try.preview": ("Xem thử popup", "Preview popup"),
        "try.result": ("Kết quả", "Result"),
        "try.empty": ("Kết quả sẽ hiện ở đây.", "The result will appear here."),

        // Cài đặt
        "set.subtitle": ("Mọi thay đổi được lưu ngay.", "Changes are saved instantly."),
        "set.groupActivity": ("Hoạt động", "Behavior"),
        "set.auto": ("Tự hiện pinyin khi copy", "Show pinyin on copy"),
        "set.autoDesc": ("Chuột phải vào tin nhắn → Sao chép (hoặc ⌘C), popup pinyin hiện ngay cạnh con trỏ.",
                         "Right-click a message → Copy (or ⌘C) and the pinyin popup appears next to the cursor."),
        "set.chatOnly": ("Chỉ với app chat", "Chat apps only"),
        "set.chatOnlyDesc": ("Không tự hiện khi copy trong Safari, Pages… (phím tắt vẫn dùng được ở mọi nơi).",
                             "Don't pop up when copying in Safari, Pages… (the hotkey still works everywhere)."),
        "set.rescan": ("Quét lại", "Rescan"),
        "set.addOthers": ("Thêm app khác đang mở", "Add another open app"),
        "set.othersDesc": ("Các ứng dụng đang mở trên máy — bấm Thêm để đưa vào danh sách:",
                           "Apps currently open on this Mac — click Add to include one:"),
        "set.hotkey": ("Phím tắt", "Hotkey"),
        "set.hotkeyDesc": ("Bôi đen chữ ở bất kỳ app nào rồi bấm. Bấm vào ô bên phải và nhấn tổ hợp phím mới để đổi.",
                           "Select text in any app, then press it. Click the box on the right and press a new combo to change it."),
        "set.axTitle": ("Cần quyền Trợ năng (Accessibility)", "Accessibility permission needed"),
        "set.axDesc": ("Để phím tắt tự copy đoạn đang bôi đen, hãy cho phép GenPiYi trong Cài đặt hệ thống → Quyền riêng tư & Bảo mật → Trợ năng. Không có quyền này, phím tắt chỉ đọc nội dung đã có trong clipboard.",
                       "To let the hotkey copy the selected text, allow GenPiYi in System Settings → Privacy & Security → Accessibility. Without it, the hotkey only reads what's already on the clipboard."),
        "set.axOpen": ("Mở cài đặt Trợ năng", "Open Accessibility settings"),
        "set.axOk": ("✓ Đã có quyền Trợ năng", "✓ Accessibility granted"),
        "set.groupDisplay": ("Hiển thị", "Display"),
        "set.toneStyle": ("Kiểu pinyin", "Pinyin style"),
        "set.toneStyleDesc": ("Dấu thanh hoặc số thanh điệu.", "Tone marks or tone numbers."),
        "set.fontSize": ("Cỡ chữ Hán", "Hanzi size"),
        "set.fontSizeDesc": ("Pinyin tự co theo tỉ lệ.", "Pinyin scales proportionally."),
        "set.toneColors": ("Tô màu theo thanh điệu", "Color by tone"),
        "set.popupTheme": ("Giao diện popup", "Popup theme"),
        "set.using": ("Đang dùng: ", "Current: "),
        "set.chooseTheme": ("Chọn mẫu", "Choose"),
        "set.language": ("Ngôn ngữ", "Language"),
        "set.languageDesc": ("Ngôn ngữ hiển thị của app.", "Display language of the app."),
        "set.groupSystem": ("Hệ thống", "System"),
        "set.startup": ("Mở cùng macOS", "Open at login"),
        "set.startupDesc": ("Chạy ẩn trên thanh menu khi đăng nhập.", "Run quietly in the menu bar when you log in."),
        "set.startupErr": ("Không bật được. Hãy chép GenPiYi.app vào thư mục Applications rồi thử lại.",
                           "Couldn't enable it. Move GenPiYi.app to the Applications folder and try again."),

        // App chat
        "scan.found": ("Tìm thấy %ld app chat trên máy · đang bật %ld", "Found %ld chat apps on this Mac · %ld enabled"),
        "scan.none": ("Không tìm thấy app chat nào trên máy.", "No chat apps found on this Mac."),
        "app.running": ("Đang chạy", "Running"),
        "app.installed": ("Đã cài", "Installed"),
        "app.notFound": ("Không tìm thấy trên máy", "Not found on this Mac"),
        "app.custom": (" · tự thêm", " · added by you"),
        "app.remove": ("Xoá khỏi danh sách", "Remove from list"),
        "others.none": ("Không có app nào khác đang mở.", "No other apps are open."),

        // Phím tắt
        "hotkey.press": ("Nhấn tổ hợp phím…", "Press a key combo…"),
        "hotkey.active": ("✓ Đang hoạt động", "✓ Active"),
        "hotkey.taken": ("Phím tắt đã được ứng dụng khác sử dụng.", "This hotkey is already used by another app."),
        "hotkey.needMod": ("Cần ít nhất một phím ⌘/⌃/⌥ (trừ F1–F20).", "Needs at least one of ⌘/⌃/⌥ (except F1–F20)."),
        "hotkey.unknown": ("Không nhận ra phím này.", "Unknown key."),
        "hotkey.escCancel": ("Esc để huỷ", "Esc to cancel"),

        "tone.neutral": ("nhẹ", "neutral"),

        // Giao diện
        "theme.subtitle": ("Chọn mẫu cho popup pinyin. Bấm vào ảnh để xem thử, bấm “Dùng” để áp dụng.",
                           "Pick a look for the pinyin popup. Click a preview to try it, click “Use” to apply."),
        "theme.featured": ("Mẫu nổi bật", "Featured"),
        "theme.basic": ("Cơ bản", "Basic"),
        "theme.previewSource": ("Xem thử", "Preview"),
        "theme.use": ("Dùng", "Use"),
        "theme.inUse": ("Đang dùng", "In use"),
        "badge.new": ("Mới", "New"),
        "badge.hot": ("Hot", "Hot"),

        // Hướng dẫn
        "guide.subtitle": ("Ba cách xem pinyin của tin nhắn tiếng Trung.", "Three ways to see the pinyin of Chinese messages."),
        "guide.s1": ("Chuột phải → Sao chép", "Right-click → Copy"),
        "guide.s1Desc": ("Trong Zalo, WeChat, LINE…, chuột phải vào tin nhắn và chọn Sao chép / Copy / 复制 (hoặc bôi đen rồi ⌘C). Popup pinyin hiện ngay cạnh con trỏ chuột.",
                         "In Zalo, WeChat, LINE…, right-click a message and choose Copy / Sao chép / 复制 (or select it and press ⌘C). The pinyin popup appears right next to the cursor."),
        "guide.s2": ("Bôi đen + phím tắt ", "Select + hotkey "),
        "guide.s2Desc": ("Dùng được ở mọi ứng dụng: Safari, Pages, PDF… (cần quyền Trợ năng). Nếu không bôi đen gì, app lấy nội dung đang có trong clipboard.",
                         "Works in any app: Safari, Pages, PDF… (needs Accessibility permission). If nothing is selected, the app uses what's already on the clipboard."),
        "guide.s3": ("Trong popup", "In the popup"),
        "guide.s3Desc": ("Rê chuột vào chữ để xem cách đọc (chữ có gạch chấm là chữ đa âm). Nút copy pinyin / copy cả chữ Hán + pinyin, nút ghim để giữ popup. Click ra ngoài hoặc Esc để đóng — popup không chiếm focus nên vẫn gõ chat bình thường.",
                         "Hover a character to see its readings (a dotted underline marks characters with several readings). Buttons copy the pinyin or hanzi + pinyin; the pin keeps the popup open. Click outside or press Esc to close — the popup never steals focus, so you can keep typing."),
        "guide.tray": ("Đóng cửa sổ này app vẫn chạy nền trên thanh menu (biểu tượng 拼 ở góc trên bên phải màn hình). Muốn tắt hẳn: bấm biểu tượng → Thoát.",
                       "Closing this window keeps the app running in the menu bar (the 拼 icon at the top-right of the screen). To quit: click the icon → Quit."),

        // Popup
        "popup.copyBoth": ("Copy chữ Hán + pinyin", "Copy hanzi + pinyin"),
        "popup.pin": ("Ghim popup", "Pin popup"),
        "popup.unpin": ("Bỏ ghim", "Unpin"),
        "popup.close": ("Đóng (Esc)", "Close (Esc)"),

        // Thanh menu
        "tray.open": ("Mở GenPiYi", "Open GenPiYi"),
        "tray.fromClipboard": ("Xem pinyin nội dung đã copy", "Show pinyin of copied text"),
        "tray.auto": ("Tự hiện khi copy chữ Trung", "Show when copying Chinese"),
        "tray.exit": ("Thoát GenPiYi", "Quit GenPiYi"),
        "tray.noHan": ("Không thấy chữ Hán trong nội dung vừa copy.", "No Chinese characters in the copied text."),
    ]
}
