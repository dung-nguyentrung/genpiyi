import AppKit
import Carbon.HIToolbox
import ApplicationServices

/// Phím tắt toàn cục bằng Carbon RegisterEventHotKey (không cần quyền Trợ năng).
final class HotKey {
    static let shared = HotKey()

    var onPressed: (() -> Void)?
    private var ref: EventHotKeyRef?
    private var handlerInstalled = false
    private let hotKeyID = EventHotKeyID(signature: OSType(0x5059_5949), id: 1) // "PYYI"

    private func installHandler() {
        guard !handlerInstalled else { return }
        var spec = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
        InstallEventHandler(GetApplicationEventTarget(), { _, _, _ -> OSStatus in
            DispatchQueue.main.async { HotKey.shared.onPressed?() }
            return noErr
        }, 1, &spec, nil, nil)
        handlerInstalled = true
    }

    /// Đăng ký phím tắt dạng "Ctrl+Option+P". Trả về thông báo lỗi (rỗng nếu thành công).
    @discardableResult
    func register(_ text: String) -> String {
        unregister()
        installHandler()
        guard let combo = KeyCombo.parse(text) else { return Loc.t("hotkey.unknown") }
        if let err = combo.validationError { return err }
        var newRef: EventHotKeyRef?
        let status = RegisterEventHotKey(combo.keyCode, combo.carbonModifiers, hotKeyID,
                                         GetApplicationEventTarget(), 0, &newRef)
        if status != noErr || newRef == nil { return Loc.t("hotkey.taken") }
        ref = newRef
        return ""
    }

    func unregister() {
        if let r = ref { UnregisterEventHotKey(r) }
        ref = nil
    }
}

/// Tổ hợp phím: mã phím (kVK_*) + phím bổ trợ.
struct KeyCombo {
    var keyCode: UInt32
    var command = false, control = false, option = false, shift = false

    var carbonModifiers: UInt32 {
        var m: UInt32 = 0
        if command { m |= UInt32(cmdKey) }
        if control { m |= UInt32(controlKey) }
        if option { m |= UInt32(optionKey) }
        if shift { m |= UInt32(shiftKey) }
        return m
    }

    var isFunctionKey: Bool { KeyCombo.functionKeys.contains(keyCode) }

    var validationError: String? {
        if !command && !control && !option && !isFunctionKey { return Loc.t("hotkey.needMod") }
        return nil
    }

    /// Chuỗi lưu cài đặt, vd "Ctrl+Option+P".
    var text: String {
        var parts: [String] = []
        if control { parts.append("Ctrl") }
        if option { parts.append("Option") }
        if shift { parts.append("Shift") }
        if command { parts.append("Cmd") }
        parts.append(KeyCombo.name(for: keyCode) ?? "?")
        return parts.joined(separator: "+")
    }

    /// Hiển thị kiểu Mac, vd "⌃⌥P".
    var symbols: String {
        var s = ""
        if control { s += "⌃" }
        if option { s += "⌥" }
        if shift { s += "⇧" }
        if command { s += "⌘" }
        return s + (KeyCombo.name(for: keyCode) ?? "?")
    }

    static func parse(_ text: String) -> KeyCombo? {
        var combo = KeyCombo(keyCode: UInt32.max)
        for raw in text.split(separator: "+") {
            let p = raw.trimmingCharacters(in: .whitespaces)
            switch p.lowercased() {
            case "cmd", "command", "⌘", "win": combo.command = true
            case "ctrl", "control", "⌃": combo.control = true
            case "alt", "option", "opt", "⌥": combo.option = true
            case "shift", "⇧": combo.shift = true
            case "": continue
            default:
                guard let code = keyCodes[p.uppercased()] else { return nil }
                combo.keyCode = code
            }
        }
        return combo.keyCode == UInt32.max ? nil : combo
    }

    /// Tạo từ sự kiện bàn phím (khi người dùng ghi phím tắt mới).
    static func from(event: NSEvent) -> KeyCombo? {
        let code = UInt32(event.keyCode)
        guard name(for: code) != nil else { return nil }
        let f = event.modifierFlags
        return KeyCombo(keyCode: code, command: f.contains(.command), control: f.contains(.control),
                        option: f.contains(.option), shift: f.contains(.shift))
    }

    static func display(_ text: String) -> String { parse(text)?.symbols ?? text }

    static func name(for code: UInt32) -> String? {
        keyCodes.first { $0.value == code }?.key
    }

    static let functionKeys: Set<UInt32> = [
        UInt32(kVK_F1), UInt32(kVK_F2), UInt32(kVK_F3), UInt32(kVK_F4), UInt32(kVK_F5), UInt32(kVK_F6),
        UInt32(kVK_F7), UInt32(kVK_F8), UInt32(kVK_F9), UInt32(kVK_F10), UInt32(kVK_F11), UInt32(kVK_F12),
        UInt32(kVK_F13), UInt32(kVK_F14), UInt32(kVK_F15), UInt32(kVK_F16), UInt32(kVK_F17), UInt32(kVK_F18),
        UInt32(kVK_F19), UInt32(kVK_F20),
    ]

    static let keyCodes: [String: UInt32] = [
        "A": UInt32(kVK_ANSI_A), "B": UInt32(kVK_ANSI_B), "C": UInt32(kVK_ANSI_C), "D": UInt32(kVK_ANSI_D),
        "E": UInt32(kVK_ANSI_E), "F": UInt32(kVK_ANSI_F), "G": UInt32(kVK_ANSI_G), "H": UInt32(kVK_ANSI_H),
        "I": UInt32(kVK_ANSI_I), "J": UInt32(kVK_ANSI_J), "K": UInt32(kVK_ANSI_K), "L": UInt32(kVK_ANSI_L),
        "M": UInt32(kVK_ANSI_M), "N": UInt32(kVK_ANSI_N), "O": UInt32(kVK_ANSI_O), "P": UInt32(kVK_ANSI_P),
        "Q": UInt32(kVK_ANSI_Q), "R": UInt32(kVK_ANSI_R), "S": UInt32(kVK_ANSI_S), "T": UInt32(kVK_ANSI_T),
        "U": UInt32(kVK_ANSI_U), "V": UInt32(kVK_ANSI_V), "W": UInt32(kVK_ANSI_W), "X": UInt32(kVK_ANSI_X),
        "Y": UInt32(kVK_ANSI_Y), "Z": UInt32(kVK_ANSI_Z),
        "0": UInt32(kVK_ANSI_0), "1": UInt32(kVK_ANSI_1), "2": UInt32(kVK_ANSI_2), "3": UInt32(kVK_ANSI_3),
        "4": UInt32(kVK_ANSI_4), "5": UInt32(kVK_ANSI_5), "6": UInt32(kVK_ANSI_6), "7": UInt32(kVK_ANSI_7),
        "8": UInt32(kVK_ANSI_8), "9": UInt32(kVK_ANSI_9),
        "SPACE": UInt32(kVK_Space), "RETURN": UInt32(kVK_Return), "TAB": UInt32(kVK_Tab),
        "F1": UInt32(kVK_F1), "F2": UInt32(kVK_F2), "F3": UInt32(kVK_F3), "F4": UInt32(kVK_F4),
        "F5": UInt32(kVK_F5), "F6": UInt32(kVK_F6), "F7": UInt32(kVK_F7), "F8": UInt32(kVK_F8),
        "F9": UInt32(kVK_F9), "F10": UInt32(kVK_F10), "F11": UInt32(kVK_F11), "F12": UInt32(kVK_F12),
        "F13": UInt32(kVK_F13), "F14": UInt32(kVK_F14), "F15": UInt32(kVK_F15), "F16": UInt32(kVK_F16),
        "F17": UInt32(kVK_F17), "F18": UInt32(kVK_F18), "F19": UInt32(kVK_F19), "F20": UInt32(kVK_F20),
        "=": UInt32(kVK_ANSI_Equal), "-": UInt32(kVK_ANSI_Minus), "[": UInt32(kVK_ANSI_LeftBracket),
        "]": UInt32(kVK_ANSI_RightBracket), ";": UInt32(kVK_ANSI_Semicolon), "'": UInt32(kVK_ANSI_Quote),
        ",": UInt32(kVK_ANSI_Comma), ".": UInt32(kVK_ANSI_Period), "/": UInt32(kVK_ANSI_Slash),
        "\\": UInt32(kVK_ANSI_Backslash), "`": UInt32(kVK_ANSI_Grave),
    ]
}

/// Giả lập ⌘C để copy đoạn đang bôi đen (cần quyền Trợ năng).
enum KeySender {
    static var isTrusted: Bool { AXIsProcessTrusted() }

    /// Hiện hộp thoại hệ thống xin quyền Trợ năng (chỉ hiện nếu chưa có quyền).
    static func requestTrust() {
        let key = kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String
        _ = AXIsProcessTrustedWithOptions([key: true] as CFDictionary)
    }

    static func openAccessibilitySettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility") {
            NSWorkspace.shared.open(url)
        }
    }

    static func sendCommandC() {
        let src = CGEventSource(stateID: .combinedSessionState)
        let code = CGKeyCode(kVK_ANSI_C)
        let down = CGEvent(keyboardEventSource: src, virtualKey: code, keyDown: true)
        let up = CGEvent(keyboardEventSource: src, virtualKey: code, keyDown: false)
        down?.flags = .maskCommand
        up?.flags = .maskCommand
        down?.post(tap: .cghidEventTap)
        up?.post(tap: .cghidEventTap)
    }

    /// Có phím bổ trợ nào đang giữ không (đợi người dùng nhả ⌃⌥ trước khi gửi ⌘C).
    static var anyModifierDown: Bool {
        let f = CGEventSource.flagsState(.combinedSessionState)
        return f.contains(.maskCommand) || f.contains(.maskControl) || f.contains(.maskAlternate) || f.contains(.maskShift)
    }
}
