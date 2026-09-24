import SwiftUI
import AppKit
import Combine

struct SettingsPage: View {
    @ObservedObject private var settings = AppSettings.shared
    @ObservedObject private var controller = AppController.shared
    @ObservedObject private var nav = NavModel.shared

    @State private var apps: [DetectedChatApp] = []
    @State private var others: [RunningApp] = []
    @State private var showOthers = false
    @State private var trusted = KeySender.isTrusted
    @State private var startAtLogin = AppSettings.startAtLogin
    @State private var loginError = false

    private let axTimer = Timer.publish(every: 1.5, on: .main, in: .common).autoconnect()

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            PageHeader(title: Loc.t("nav.settings"), subtitle: Loc.t("set.subtitle"))

            // ---------- Hoạt động ----------
            groupTitle(Loc.t("set.groupActivity"))
            Card {
                row(Loc.t("set.auto"), Loc.t("set.autoDesc")) {
                    Toggle("", isOn: $settings.data.autoOnCopy).toggleStyle(.switch).labelsHidden()
                }
                Divider().padding(.vertical, 12)
                row(Loc.t("set.chatOnly"), Loc.t("set.chatOnlyDesc")) {
                    Toggle("", isOn: $settings.data.onlyChatApps).toggleStyle(.switch).labelsHidden()
                }
                .disabled(!settings.data.autoOnCopy)

                if settings.data.autoOnCopy && settings.data.onlyChatApps {
                    chatAppList.padding(.top, 12)
                }
            }

            Card {
                row(Loc.t("set.hotkey"), Loc.t("set.hotkeyDesc")) {
                    VStack(alignment: .trailing, spacing: 4) {
                        HotkeyRecorder()
                        if controller.hotkeyError.isEmpty {
                            Text(Loc.t("hotkey.active")).font(.system(size: 11)).foregroundColor(Color(hex: "#2EB86E"))
                        } else {
                            Text(controller.hotkeyError).font(.system(size: 11)).foregroundColor(Brand.accent)
                        }
                    }
                }
                Divider().padding(.vertical, 12)
                accessibilityRow
            }

            // ---------- Hiển thị ----------
            groupTitle(Loc.t("set.groupDisplay")).padding(.top, 6)
            Card {
                row(Loc.t("set.toneStyle"), Loc.t("set.toneStyleDesc")) {
                    Picker("", selection: $settings.data.toneStyle) {
                        Text("nǐ hǎo").tag("mark")
                        Text("ni3 hao3").tag("number")
                    }
                    .pickerStyle(.segmented)
                    .labelsHidden()
                    .frame(width: 180)
                }
                Divider().padding(.vertical, 12)
                row(Loc.t("set.fontSize"), Loc.t("set.fontSizeDesc")) {
                    HStack(spacing: 10) {
                        Slider(value: $settings.data.hanziFontSize, in: 16...44, step: 1).frame(width: 150)
                        Text("\(Int(settings.data.hanziFontSize))").font(.system(size: 12, design: .monospaced))
                            .frame(width: 24, alignment: .trailing)
                    }
                }
                Divider().padding(.vertical, 12)
                VStack(alignment: .leading, spacing: 8) {
                    row(Loc.t("set.toneColors"), nil) {
                        Toggle("", isOn: $settings.data.toneColors).toggleStyle(.switch).labelsHidden()
                    }
                    toneLegend
                }
                Divider().padding(.vertical, 12)
                row(Loc.t("set.popupTheme"), Loc.t("set.using") + settings.theme.locName) {
                    HStack(spacing: 10) {
                        EmblemView(theme: settings.theme, size: 22)
                        PillButton(title: Loc.t("set.chooseTheme"), symbol: "paintpalette") { nav.page = .themes }
                    }
                }
                Divider().padding(.vertical, 12)
                row(Loc.t("set.language"), Loc.t("set.languageDesc")) {
                    Picker("", selection: $settings.data.language) {
                        Text("Tiếng Việt").tag("vi")
                        Text("English").tag("en")
                    }
                    .pickerStyle(.segmented)
                    .labelsHidden()
                    .frame(width: 180)
                }
            }

            // ---------- Hệ thống ----------
            groupTitle(Loc.t("set.groupSystem")).padding(.top, 6)
            Card {
                row(Loc.t("set.startup"), loginError ? Loc.t("set.startupErr") : Loc.t("set.startupDesc")) {
                    Toggle("", isOn: $startAtLogin).toggleStyle(.switch).labelsHidden()
                        .onChange(of: startAtLogin) { on in
                            guard on != AppSettings.startAtLogin else { return }
                            if AppSettings.setStartAtLogin(on) {
                                loginError = false
                            } else {
                                loginError = true
                                startAtLogin = AppSettings.startAtLogin
                            }
                        }
                }
            }
        }
        .onAppear { rescan() }
        .onReceive(axTimer) { _ in trusted = KeySender.isTrusted }
    }

    // MARK: - Thành phần

    private func groupTitle(_ s: String) -> some View {
        Text(s.uppercased()).font(.system(size: 11, weight: .semibold)).foregroundColor(.secondary).kerning(0.6)
    }

    private func row<Trailing: View>(_ title: String, _ desc: String?, @ViewBuilder trailing: () -> Trailing) -> some View {
        HStack(alignment: .center, spacing: 16) {
            VStack(alignment: .leading, spacing: 3) {
                Text(title).font(.system(size: 13.5, weight: .medium))
                if let d = desc, !d.isEmpty {
                    Text(d).font(.system(size: 12)).foregroundColor(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            Spacer(minLength: 12)
            trailing()
        }
    }

    private var toneLegend: some View {
        let t = settings.theme
        return HStack(spacing: 14) {
            ForEach(1...5, id: \.self) { tone in
                HStack(spacing: 5) {
                    Circle().fill(t.toneColor(tone)).frame(width: 9, height: 9)
                    Text(tone == 5 ? Loc.t("tone.neutral") : "\(tone)")
                        .font(.system(size: 11.5)).foregroundColor(.secondary)
                }
            }
        }
        .opacity(settings.data.toneColors ? 1 : 0.4)
    }

    private var accessibilityRow: some View {
        HStack(alignment: .top, spacing: 12) {
            Image(systemName: trusted ? "checkmark.shield.fill" : "exclamationmark.shield.fill")
                .font(.system(size: 18))
                .foregroundColor(trusted ? Color(hex: "#2EB86E") : Color(hex: "#F08A24"))
            VStack(alignment: .leading, spacing: 4) {
                Text(trusted ? Loc.t("set.axOk") : Loc.t("set.axTitle")).font(.system(size: 13, weight: .medium))
                if !trusted {
                    Text(Loc.t("set.axDesc")).font(.system(size: 12)).foregroundColor(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            Spacer()
            if !trusted {
                PillButton(title: Loc.t("set.axOpen"), symbol: "lock.open") {
                    KeySender.requestTrust()
                    KeySender.openAccessibilitySettings()
                }
            }
        }
    }

    // MARK: - Danh sách app chat

    private var enabledCount: Int { apps.filter { isOn($0) }.count }

    private func isOn(_ a: DetectedChatApp) -> Bool {
        a.bundleIds.contains { id in settings.data.chatApps.contains { $0.caseInsensitiveCompare(id) == .orderedSame } }
    }

    private func setOn(_ a: DetectedChatApp, _ on: Bool) {
        var list = settings.data.chatApps.filter { id in !a.bundleIds.contains { $0.caseInsensitiveCompare(id) == .orderedSame } }
        if on { list.append(contentsOf: a.bundleIds) }
        settings.data.chatApps = list
    }

    private func rescan() {
        apps = ChatAppScanner.scan(settings: settings.data)
        others = ChatAppScanner.runningOthers(excluding: apps)
    }

    private var chatAppList: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(apps.isEmpty ? Loc.t("scan.none") : Loc.f("scan.found", apps.filter { !$0.isCustom }.count, enabledCount))
                    .font(.system(size: 12)).foregroundColor(.secondary)
                Spacer()
                Button(action: rescan) {
                    Label(Loc.t("set.rescan"), systemImage: "arrow.clockwise").font(.system(size: 12))
                }
                .buttonStyle(.borderless)
            }

            VStack(spacing: 0) {
                ForEach(apps) { a in
                    appRow(a)
                    if a.id != apps.last?.id { Divider().padding(.leading, 44) }
                }
            }
            .background(RoundedRectangle(cornerRadius: 10).fill(Color.primary.opacity(0.035)))

            DisclosureGroup(isExpanded: $showOthers) {
                VStack(alignment: .leading, spacing: 6) {
                    Text(Loc.t("set.othersDesc")).font(.system(size: 11.5)).foregroundColor(.secondary)
                    if others.isEmpty {
                        Text(Loc.t("others.none")).font(.system(size: 12)).foregroundColor(.secondary)
                    }
                    ForEach(others) { o in
                        HStack(spacing: 10) {
                            appIcon(o.icon, fallback: "#8A94A3", name: o.name)
                            VStack(alignment: .leading, spacing: 1) {
                                Text(o.name).font(.system(size: 12.5))
                                Text(o.bundleId).font(.system(size: 10.5)).foregroundColor(.secondary)
                            }
                            Spacer()
                            PillButton(title: Loc.t("common.add"), symbol: "plus") {
                                if !settings.data.customApps.contains(o.bundleId) { settings.data.customApps.append(o.bundleId) }
                                if !settings.data.chatApps.contains(o.bundleId) { settings.data.chatApps.append(o.bundleId) }
                                rescan()
                            }
                        }
                    }
                }
                .padding(.top, 6)
            } label: {
                Text(Loc.t("set.addOthers")).font(.system(size: 12.5, weight: .medium))
            }
            .padding(.top, 4)
            .onChange(of: showOthers) { open in if open { others = ChatAppScanner.runningOthers(excluding: apps) } }
        }
    }

    private func appIcon(_ img: NSImage?, fallback: String, name: String) -> some View {
        Group {
            if let img {
                Image(nsImage: img).resizable().interpolation(.high)
            } else {
                ZStack {
                    RoundedRectangle(cornerRadius: 6).fill(Color(hex: fallback))
                    Text(String(name.prefix(1))).font(.system(size: 13, weight: .bold)).foregroundColor(.white)
                }
            }
        }
        .frame(width: 26, height: 26)
    }

    private func appRow(_ a: DetectedChatApp) -> some View {
        HStack(spacing: 10) {
            appIcon(a.icon, fallback: a.color, name: a.name)
            VStack(alignment: .leading, spacing: 1) {
                Text(a.name).font(.system(size: 13, weight: .medium))
                HStack(spacing: 4) {
                    if a.running {
                        Circle().fill(Color(hex: "#2EB86E")).frame(width: 6, height: 6)
                        Text(Loc.t("app.running"))
                    } else {
                        Text(a.installed ? Loc.t("app.installed") : Loc.t("app.notFound"))
                    }
                    if a.isCustom { Text(Loc.t("app.custom")) }
                }
                .font(.system(size: 11)).foregroundColor(.secondary)
            }
            Spacer()
            if a.isCustom {
                Button {
                    settings.data.customApps.removeAll { a.bundleIds.contains($0) }
                    settings.data.chatApps.removeAll { a.bundleIds.contains($0) }
                    rescan()
                } label: {
                    Image(systemName: "trash").font(.system(size: 11))
                }
                .buttonStyle(.borderless)
                .help(Loc.t("app.remove"))
            }
            Toggle("", isOn: Binding(get: { isOn(a) }, set: { setOn(a, $0) }))
                .toggleStyle(.switch).labelsHidden().controlSize(.small)
        }
        .padding(.horizontal, 10).padding(.vertical, 8)
    }
}

/// Ô ghi phím tắt: bấm vào rồi nhấn tổ hợp phím mới.
struct HotkeyRecorder: View {
    @ObservedObject private var settings = AppSettings.shared
    @State private var recording = false
    @State private var monitor: Any?
    @State private var error = ""

    var body: some View {
        Button(action: toggle) {
            Text(recording ? Loc.t("hotkey.press") : KeyCombo.display(settings.data.hotkey))
                .font(.system(size: 13, weight: .semibold, design: recording ? .default : .rounded))
                .foregroundColor(recording ? Brand.accent : .primary)
                .padding(.horizontal, 14).padding(.vertical, 6)
                .frame(minWidth: 120)
                .background(
                    RoundedRectangle(cornerRadius: 8)
                        .fill(recording ? Brand.accent.opacity(0.1) : Color.primary.opacity(0.06))
                )
                .overlay(
                    RoundedRectangle(cornerRadius: 8)
                        .stroke(recording ? Brand.accent : Color.primary.opacity(0.12), lineWidth: 1)
                )
        }
        .buttonStyle(.plain)
        .help(recording ? Loc.t("hotkey.escCancel") : Loc.t("set.hotkey"))
        .onDisappear { stop() }
        .overlay(alignment: .bottomTrailing) {
            if !error.isEmpty {
                Text(error).font(.system(size: 10.5)).foregroundColor(Brand.accent)
                    .fixedSize().offset(y: 16)
            }
        }
    }

    private func toggle() { recording ? stop() : start() }

    private func start() {
        error = ""
        recording = true
        AppController.shared.suspendHotkey()
        monitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { e in
            if e.keyCode == 53 { stop(); return nil } // Esc
            guard let combo = KeyCombo.from(event: e) else { error = Loc.t("hotkey.unknown"); return nil }
            if let err = combo.validationError { error = err; return nil }
            settings.data.hotkey = combo.text
            stop()
            return nil
        }
    }

    private func stop() {
        if let m = monitor { NSEvent.removeMonitor(m) }
        monitor = nil
        if recording {
            recording = false
            AppController.shared.resumeHotkey()
        }
    }
}
