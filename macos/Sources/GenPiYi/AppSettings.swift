import Foundation
import ServiceManagement

/// Cài đặt của app, lưu dạng JSON trong ~/Library/Application Support/GenPiYi/settings.json.
struct SettingsData: Codable, Equatable {
    /// Tự hiện pinyin khi copy đoạn có chữ Hán.
    var autoOnCopy = true
    /// Chỉ tự hiện khi copy từ các app chat trong danh sách chatApps.
    var onlyChatApps = true
    /// Bundle ID của các app chat ĐANG BẬT.
    var chatApps: [String] = ChatAppScanner.defaultEnabled
    /// App người dùng tự thêm (vẫn hiện trong danh sách kể cả khi tắt).
    var customApps: [String] = []
    /// Phím tắt: copy đoạn đang bôi đen rồi hiện pinyin.
    var hotkey = "Ctrl+Option+P"
    /// "mark" = nǐ hǎo, "number" = ni3 hao3
    var toneStyle = "mark"
    var toneColors = true
    var popupTheme = "dark"
    var hanziFontSize: Double = 26
    /// "vi" hoặc "en". Trống = theo ngôn ngữ macOS.
    var language = ""

    init() {}

    // Giải mã chịu lỗi: thiếu khoá nào thì dùng mặc định
    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        let d = SettingsData()
        autoOnCopy = (try? c.decode(Bool.self, forKey: .autoOnCopy)) ?? d.autoOnCopy
        onlyChatApps = (try? c.decode(Bool.self, forKey: .onlyChatApps)) ?? d.onlyChatApps
        chatApps = (try? c.decode([String].self, forKey: .chatApps)) ?? d.chatApps
        customApps = (try? c.decode([String].self, forKey: .customApps)) ?? d.customApps
        hotkey = (try? c.decode(String.self, forKey: .hotkey)) ?? d.hotkey
        toneStyle = (try? c.decode(String.self, forKey: .toneStyle)) ?? d.toneStyle
        toneColors = (try? c.decode(Bool.self, forKey: .toneColors)) ?? d.toneColors
        popupTheme = (try? c.decode(String.self, forKey: .popupTheme)) ?? d.popupTheme
        hanziFontSize = (try? c.decode(Double.self, forKey: .hanziFontSize)) ?? d.hanziFontSize
        language = (try? c.decode(String.self, forKey: .language)) ?? d.language
        if hanziFontSize < 14 || hanziFontSize > 60 { hanziFontSize = 26 }
    }
}

final class AppSettings: ObservableObject {
    static let shared = AppSettings()

    @Published var data: SettingsData {
        didSet { if data != oldValue { save(); onChange?(oldValue) } }
    }

    /// Gọi sau mỗi lần đổi cài đặt (AppController dùng để đăng ký lại phím tắt…).
    var onChange: ((SettingsData) -> Void)?

    var theme: PopupTheme { ThemeCatalog.get(data.popupTheme) }

    static var folder: URL {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        return base.appendingPathComponent("GenPiYi", isDirectory: true)
    }

    private static var fileURL: URL { folder.appendingPathComponent("settings.json") }

    private init() {
        if let raw = try? Data(contentsOf: Self.fileURL),
           let d = try? JSONDecoder().decode(SettingsData.self, from: raw) {
            data = d
        } else {
            data = SettingsData()
        }
    }

    func isChatApp(_ bundleId: String?) -> Bool {
        guard let id = bundleId, !id.isEmpty else { return false }
        return data.chatApps.contains { $0.caseInsensitiveCompare(id) == .orderedSame }
    }

    func save() {
        do {
            try FileManager.default.createDirectory(at: Self.folder, withIntermediateDirectories: true)
            let enc = JSONEncoder()
            enc.outputFormatting = [.prettyPrinted, .sortedKeys]
            try enc.encode(data).write(to: Self.fileURL, options: .atomic)
        } catch {
            AppLog.write("save settings: \(error)")
        }
    }

    // MARK: - Mở cùng macOS (Login Items)

    static var startAtLogin: Bool {
        SMAppService.mainApp.status == .enabled
    }

    /// Trả về false nếu không bật/tắt được (vd: app chưa nằm trong /Applications).
    @discardableResult
    static func setStartAtLogin(_ on: Bool) -> Bool {
        do {
            if on { try SMAppService.mainApp.register() } else { try SMAppService.mainApp.unregister() }
            return true
        } catch {
            AppLog.write("login item: \(error)")
            return false
        }
    }
}

enum AppLog {
    static func write(_ msg: String) {
        let url = AppSettings.folder.appendingPathComponent("error.log")
        let line = "[\(Date())] \(msg)\n"
        try? FileManager.default.createDirectory(at: AppSettings.folder, withIntermediateDirectories: true)
        if let h = try? FileHandle(forWritingTo: url) {
            h.seekToEndOfFile()
            h.write(line.data(using: .utf8)!)
            try? h.close()
        } else {
            try? line.data(using: .utf8)?.write(to: url)
        }
    }
}
