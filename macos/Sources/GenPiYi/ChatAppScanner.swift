import AppKit

/// App chat đã biết trên macOS (nhận diện theo bundle ID).
struct KnownChatApp {
    let key: String
    let name: String
    /// Các bundle ID có thể có (bản App Store / bản tải web / bản quốc tế…).
    let bundleIds: [String]
    let color: String
}

/// Một dòng trong danh sách bật/tắt app chat.
struct DetectedChatApp: Identifiable {
    var id: String { key }
    let key: String
    let name: String
    let bundleIds: [String]
    var installed: Bool
    var running: Bool
    let isCustom: Bool
    var icon: NSImage?
    let color: String
}

/// App khác đang mở (để người dùng tự thêm).
struct RunningApp: Identifiable {
    var id: String { bundleId }
    let bundleId: String
    let name: String
    let icon: NSImage?
}

enum ChatAppScanner {
    static let defaultEnabled = [
        "com.vng.zalo", "com.tencent.xinWeChat", "com.tencent.WeChat",
    ]

    static let known: [KnownChatApp] = [
        KnownChatApp(key: "zalo", name: "Zalo", bundleIds: ["com.vng.zalo"], color: "#0068FF"),
        KnownChatApp(key: "wechat", name: "WeChat (微信)", bundleIds: ["com.tencent.xinWeChat", "com.tencent.WeChat"], color: "#07C160"),
        KnownChatApp(key: "wecom", name: "WeCom (企业微信)", bundleIds: ["com.tencent.WeWorkMac"], color: "#2B7BE5"),
        KnownChatApp(key: "line", name: "LINE", bundleIds: ["jp.naver.line.mac"], color: "#06C755"),
        KnownChatApp(key: "telegram", name: "Telegram", bundleIds: ["ru.keepcoder.Telegram", "org.telegram.desktop"], color: "#2AABEE"),
        KnownChatApp(key: "qq", name: "QQ", bundleIds: ["com.tencent.qq"], color: "#12B7F5"),
        KnownChatApp(key: "dingtalk", name: "DingTalk (钉钉)", bundleIds: ["com.alibaba.DingTalkMac"], color: "#3296FA"),
        KnownChatApp(key: "feishu", name: "Feishu / Lark", bundleIds: ["com.bytedance.macos.feishu", "com.electron.lark"], color: "#3370FF"),
        KnownChatApp(key: "whatsapp", name: "WhatsApp", bundleIds: ["net.whatsapp.WhatsApp", "desktop.WhatsApp"], color: "#25D366"),
        KnownChatApp(key: "messenger", name: "Messenger", bundleIds: ["com.facebook.archon", "com.facebook.archon.developerID"], color: "#0084FF"),
        KnownChatApp(key: "messages", name: "Messages (iMessage)", bundleIds: ["com.apple.MobileSMS"], color: "#34C759"),
        KnownChatApp(key: "discord", name: "Discord", bundleIds: ["com.hnc.Discord"], color: "#5865F2"),
        KnownChatApp(key: "slack", name: "Slack", bundleIds: ["com.tinyspeck.slackmacgap"], color: "#4A154B"),
        KnownChatApp(key: "teams", name: "Microsoft Teams", bundleIds: ["com.microsoft.teams2", "com.microsoft.teams"], color: "#5B5FC7"),
        KnownChatApp(key: "kakao", name: "KakaoTalk", bundleIds: ["com.kakao.KakaoTalkMac"], color: "#FEE500"),
        KnownChatApp(key: "skype", name: "Skype", bundleIds: ["com.skype.skype"], color: "#00AFF0"),
    ]

    static func appURL(_ bundleId: String) -> URL? {
        NSWorkspace.shared.urlForApplication(withBundleIdentifier: bundleId)
    }

    static func isRunning(_ bundleId: String) -> Bool {
        !NSRunningApplication.runningApplications(withBundleIdentifier: bundleId).isEmpty
    }

    static func icon(_ bundleId: String) -> NSImage? {
        guard let url = appURL(bundleId) else { return nil }
        return NSWorkspace.shared.icon(forFile: url.path)
    }

    static func displayName(_ bundleId: String) -> String {
        if let app = NSRunningApplication.runningApplications(withBundleIdentifier: bundleId).first,
           let n = app.localizedName { return n }
        if let url = appURL(bundleId) { return FileManager.default.displayName(atPath: url.path).replacingOccurrences(of: ".app", with: "") }
        return bundleId
    }

    /// Quét: app chat đã cài / đang chạy + app người dùng tự thêm + app đang bật dù không tìm thấy.
    static func scan(settings: SettingsData) -> [DetectedChatApp] {
        var list: [DetectedChatApp] = []
        var covered = Set<String>()

        for k in known {
            let installedIds = k.bundleIds.filter { appURL($0) != nil }
            let running = k.bundleIds.contains(where: isRunning)
            let enabled = k.bundleIds.contains { id in settings.chatApps.contains { $0.caseInsensitiveCompare(id) == .orderedSame } }
            k.bundleIds.forEach { covered.insert($0.lowercased()) }
            guard !installedIds.isEmpty || running || enabled else { continue }
            list.append(DetectedChatApp(key: k.key, name: k.name, bundleIds: k.bundleIds,
                                        installed: !installedIds.isEmpty, running: running, isCustom: false,
                                        icon: installedIds.first.flatMap { icon($0) }, color: k.color))
        }

        let extra = (settings.customApps + settings.chatApps)
        for id in extra where !covered.contains(id.lowercased()) {
            covered.insert(id.lowercased())
            list.append(DetectedChatApp(key: "custom:" + id, name: displayName(id), bundleIds: [id],
                                        installed: appURL(id) != nil, running: isRunning(id), isCustom: true,
                                        icon: icon(id), color: "#8A94A3"))
        }
        return list
    }

    /// App đang mở (có giao diện) chưa có trong danh sách.
    static func runningOthers(excluding: [DetectedChatApp]) -> [RunningApp] {
        let taken = Set(excluding.flatMap { $0.bundleIds.map { $0.lowercased() } })
        let me = Bundle.main.bundleIdentifier?.lowercased()
        var seen = Set<String>()
        return NSWorkspace.shared.runningApplications
            .filter { $0.activationPolicy == .regular }
            .compactMap { app -> RunningApp? in
                guard let id = app.bundleIdentifier, !taken.contains(id.lowercased()), id.lowercased() != me,
                      seen.insert(id.lowercased()).inserted else { return nil }
                return RunningApp(bundleId: id, name: app.localizedName ?? id, icon: app.icon)
            }
            .sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
    }
}
