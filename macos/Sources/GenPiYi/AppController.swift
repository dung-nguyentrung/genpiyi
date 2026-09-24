import AppKit
import SwiftUI

/// Bộ não của app: theo dõi clipboard, phím tắt, biểu tượng thanh menu, cửa sổ chính và popup.
final class AppController: NSObject, ObservableObject, NSApplicationDelegate, NSWindowDelegate, NSMenuDelegate {
    static private(set) var shared: AppController!

    let settings = AppSettings.shared
    @Published private(set) var hotkeyError = ""

    private var statusItem: NSStatusItem?
    private var mainWindow: NSWindow?
    private var popup: PopupController?

    private var clipTimer: Timer?
    private var lastChangeCount = NSPasteboard.general.changeCount
    private var debounce: DispatchWorkItem?
    private var pendingApp: NSRunningApplication?

    private var forceUntil = Date.distantPast    // copy do phím tắt → luôn hiện
    private var ignoreUntil = Date.distantPast   // copy do chính app → bỏ qua
    private var lastText = ""
    private var lastShown = Date.distantPast
    private var hotkeySuspended = false

    private static let showNotification = Notification.Name("io.github.dung-nguyentrung.genpiyi.show")

    override init() {
        super.init()
        AppController.shared = self
    }

    // MARK: - Vòng đời

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Chỉ chạy một phiên: nếu đã có phiên khác → báo nó mở cửa sổ rồi thoát
        if let id = Bundle.main.bundleIdentifier {
            let others = NSRunningApplication.runningApplications(withBundleIdentifier: id)
                .filter { $0.processIdentifier != ProcessInfo.processInfo.processIdentifier }
            if !others.isEmpty {
                DistributedNotificationCenter.default().postNotificationName(Self.showNotification, object: nil,
                                                                             userInfo: nil, deliverImmediately: true)
                NSApp.terminate(nil)
                return
            }
        }
        DistributedNotificationCenter.default().addObserver(forName: Self.showNotification, object: nil, queue: .main) { [weak self] _ in
            self?.showMainWindow()
        }

        if settings.data.language.isEmpty {
            settings.data.language = Loc.detectDefault()
        }
        Loc.shared.apply(settings.data.language)

        settings.onChange = { [weak self] old in self?.settingsChanged(old: old) }

        NSApp.mainMenu = Self.buildMainMenu()
        setupStatusItem()

        HotKey.shared.onPressed = { [weak self] in self?.onHotkey() }
        applyHotkey()

        clipTimer = Timer.scheduledTimer(withTimeInterval: 0.25, repeats: true) { [weak self] _ in self?.pollClipboard() }
        RunLoop.main.add(clipTimer!, forMode: .common)

        DispatchQueue.global(qos: .utility).async { PinyinService.warmup() }

        // Mở cùng macOS → chạy ẩn trên thanh menu; mở bằng tay → hiện cửa sổ
        let launchedAtLogin = AppSettings.startAtLogin && ProcessInfo.processInfo.systemUptime < 180
        if !launchedAtLogin { showMainWindow() }
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        showMainWindow()
        return true
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }

    // MARK: - Cài đặt

    private func settingsChanged(old: SettingsData) {
        let d = settings.data
        if d.language != old.language { Loc.shared.apply(d.language); statusItem?.menu = buildStatusMenu() }
        if d.hotkey != old.hotkey && !hotkeySuspended { applyHotkey() }
    }

    @discardableResult
    func applyHotkey() -> Bool {
        hotkeyError = HotKey.shared.register(settings.data.hotkey)
        return hotkeyError.isEmpty
    }

    /// Tạm gỡ phím tắt khi đang ghi phím tắt mới trong cài đặt.
    func suspendHotkey() { hotkeySuspended = true; HotKey.shared.unregister() }
    func resumeHotkey() { hotkeySuspended = false; applyHotkey() }

    // MARK: - Clipboard

    private func pollClipboard() {
        let cc = NSPasteboard.general.changeCount
        guard cc != lastChangeCount else { return }
        lastChangeCount = cc
        // Ghi nhận app đang dùng ngay lúc copy (vd: Zalo/WeChat), rồi đợi clipboard ổn định
        pendingApp = NSWorkspace.shared.frontmostApplication
        debounce?.cancel()
        let work = DispatchWorkItem { [weak self] in
            guard let self else { return }
            self.processClipboard(force: Date() < self.forceUntil, app: self.pendingApp)
        }
        debounce = work
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.15, execute: work)
    }

    func copyToClipboard(_ text: String) {
        ignoreUntil = Date().addingTimeInterval(1)
        let pb = NSPasteboard.general
        pb.clearContents()
        pb.setString(text, forType: .string)
    }

    func showFromClipboard() { processClipboard(force: true, app: nil) }

    private func processClipboard(force: Bool, app: NSRunningApplication?) {
        let bundleId = app?.bundleIdentifier
        if !force {
            guard settings.data.autoOnCopy, Date() >= ignoreUntil else { return }
            if bundleId == Bundle.main.bundleIdentifier { return }
            if settings.data.onlyChatApps && !settings.isChatApp(bundleId) { return }
        }

        guard var text = NSPasteboard.general.string(forType: .string), PinyinService.containsHan(text) else {
            if force { showNotice(Loc.t("tray.noHan")) }
            return
        }
        text = text.trimmingCharacters(in: .whitespacesAndNewlines)
        if !force && text == lastText && Date().timeIntervalSince(lastShown) < 1.5 { return }
        if text.count > 3000 { text = String(text.prefix(3000)) + "…" }

        lastText = text
        lastShown = Date()
        forceUntil = .distantPast
        showPopup(text: text, source: app?.localizedName ?? "")
    }

    func showPopup(text: String, source: String, theme: PopupTheme? = nil) {
        if let p = popup, !p.isPinned { p.close() }
        let model = PopupModel(text: text, source: source, theme: theme ?? settings.theme, settings: settings.data)
        model.onCopy = { [weak self] s in self?.copyToClipboard(s) }
        let pc = PopupController(model: model)
        pc.onClosed = { [weak self, weak pc] in
            if let self, let pc, self.popup === pc { self.popup = nil }
        }
        popup = pc
        pc.show()
    }

    // MARK: - Phím tắt

    private func onHotkey() {
        let app = NSWorkspace.shared.frontmostApplication
        let seq = NSPasteboard.general.changeCount

        guard KeySender.isTrusted else {
            // Chưa có quyền Trợ năng → không tự copy được, dùng nội dung clipboard hiện có
            KeySender.requestTrust()
            processClipboard(force: true, app: app)
            return
        }

        Task { @MainActor in
            // Đợi người dùng nhả ⌃⌥⇧⌘ để ⌘C giả lập không bị lẫn phím
            let start = Date()
            while KeySender.anyModifierDown && Date().timeIntervalSince(start) < 1.5 {
                try? await Task.sleep(nanoseconds: 20_000_000)
            }
            self.forceUntil = Date().addingTimeInterval(1.2)
            KeySender.sendCommandC()
            try? await Task.sleep(nanoseconds: 450_000_000)
            if NSPasteboard.general.changeCount == seq {
                // Không có gì được bôi đen → dùng nội dung clipboard hiện có
                self.forceUntil = .distantPast
                self.processClipboard(force: true, app: app)
            }
        }
    }

    // MARK: - Thông báo nhỏ

    private func showNotice(_ msg: String) {
        guard let button = statusItem?.button else { return }
        let pop = NSPopover()
        pop.behavior = .transient
        let label = NSHostingController(rootView: Text(msg).font(.system(size: 12)).padding(12).frame(maxWidth: 280))
        pop.contentViewController = label
        pop.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
        DispatchQueue.main.asyncAfter(deadline: .now() + 2.5) { pop.performClose(nil) }
    }

    // MARK: - Thanh menu

    private func setupStatusItem() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let b = item.button {
            b.title = "拼"
            b.font = NSFont.systemFont(ofSize: 14, weight: .semibold)
            b.toolTip = "GenPiYi"
        }
        item.menu = buildStatusMenu()
        statusItem = item
    }

    private func buildStatusMenu() -> NSMenu {
        let menu = NSMenu()
        menu.delegate = self
        menu.addItem(withTitle: Loc.t("tray.open"), action: #selector(menuOpen), keyEquivalent: "").target = self
        menu.addItem(withTitle: Loc.t("tray.fromClipboard"), action: #selector(menuFromClipboard), keyEquivalent: "").target = self
        let auto = menu.addItem(withTitle: Loc.t("tray.auto"), action: #selector(menuToggleAuto), keyEquivalent: "")
        auto.target = self
        auto.tag = 1
        menu.addItem(.separator())
        menu.addItem(withTitle: Loc.t("tray.exit"), action: #selector(menuQuit), keyEquivalent: "q").target = self
        return menu
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.item(withTag: 1)?.state = settings.data.autoOnCopy ? .on : .off
    }

    @objc private func menuOpen() { showMainWindow() }
    @objc private func menuFromClipboard() { showFromClipboard() }
    @objc private func menuToggleAuto() { settings.data.autoOnCopy.toggle() }
    @objc private func menuQuit() { NSApp.terminate(nil) }

    /// App dạng thanh menu không có menu chính → tự tạo để ⌘C / ⌘V / ⌘A / ⌘W hoạt động trong cửa sổ.
    private static func buildMainMenu() -> NSMenu {
        let main = NSMenu()

        let appItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "Quit GenPiYi", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        main.addItem(appItem)

        let editItem = NSMenuItem()
        let edit = NSMenu(title: "Edit")
        edit.addItem(withTitle: "Undo", action: Selector(("undo:")), keyEquivalent: "z")
        edit.addItem(withTitle: "Redo", action: Selector(("redo:")), keyEquivalent: "Z")
        edit.addItem(.separator())
        edit.addItem(withTitle: "Cut", action: #selector(NSText.cut(_:)), keyEquivalent: "x")
        edit.addItem(withTitle: "Copy", action: #selector(NSText.copy(_:)), keyEquivalent: "c")
        edit.addItem(withTitle: "Paste", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        edit.addItem(withTitle: "Select All", action: #selector(NSText.selectAll(_:)), keyEquivalent: "a")
        editItem.submenu = edit
        main.addItem(editItem)

        let winItem = NSMenuItem()
        let win = NSMenu(title: "Window")
        win.addItem(withTitle: "Close", action: #selector(NSWindow.performClose(_:)), keyEquivalent: "w")
        win.addItem(withTitle: "Minimize", action: #selector(NSWindow.performMiniaturize(_:)), keyEquivalent: "m")
        winItem.submenu = win
        main.addItem(winItem)
        return main
    }

    // MARK: - Cửa sổ chính

    func showMainWindow() {
        if mainWindow == nil {
            let host = NSHostingController(rootView: MainView())
            let w = NSWindow(contentViewController: host)
            w.title = "GenPiYi"
            w.styleMask = [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView]
            w.titlebarAppearsTransparent = true
            w.titleVisibility = .hidden
            w.setContentSize(NSSize(width: 900, height: 640))
            w.minSize = NSSize(width: 760, height: 520)
            w.isReleasedWhenClosed = false
            w.delegate = self
            w.center()
            w.setFrameAutosaveName("GenPiYiMain")
            mainWindow = w
        }
        // Hiện biểu tượng ở Dock khi cửa sổ đang mở
        NSApp.setActivationPolicy(.regular)
        mainWindow?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    func windowWillClose(_ notification: Notification) {
        if (notification.object as? NSWindow) === mainWindow {
            // Đóng cửa sổ → app vẫn chạy nền trên thanh menu
            NSApp.setActivationPolicy(.accessory)
        }
    }
}

