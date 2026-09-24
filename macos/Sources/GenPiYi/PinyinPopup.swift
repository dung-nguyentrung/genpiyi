import AppKit
import SwiftUI

/// Trạng thái của một popup pinyin.
final class PopupModel: ObservableObject {
    let text: String
    let source: String
    let theme: PopupTheme
    let lines: [[PyToken]]
    let plain: String
    let hanziSize: CGFloat
    let toneColors: Bool
    let contentWidth: CGFloat
    let scrolls: Bool

    @Published var pinned = false
    @Published var flashed: String? = nil      // nút vừa copy ("py" / "both")
    @Published var focused: PyToken? = nil

    var onClose: (() -> Void)?
    var onCopy: ((String) -> Void)?

    static let maxContent: CGFloat = 530

    init(text: String, source: String, theme: PopupTheme, settings: SettingsData) {
        let lines = PinyinService.convert(text, style: settings.toneStyle)
        let hz = CGFloat(settings.hanziFontSize)
        self.text = text
        self.source = source
        self.theme = theme
        self.lines = lines
        self.plain = PinyinService.toPlainPinyin(lines)
        self.hanziSize = hz
        self.toneColors = settings.toneColors

        // Đo trước bề rộng thật của phần chữ để popup co gọn theo nội dung
        let size = PopupModel.measure(lines: lines, theme: theme, hanziSize: hz, toneColors: settings.toneColors)
        self.contentWidth = min(PopupModel.maxContent, max(240, ceil(size.width) + 2))
        self.scrolls = size.height > 400
    }

    static func measure(lines: [[PyToken]], theme: PopupTheme, hanziSize: CGFloat, toneColors: Bool) -> CGSize {
        let host = NSHostingController(rootView: RubyView(lines: lines, theme: theme, hanziSize: hanziSize,
                                                          toneColors: toneColors, maxWidth: maxContent))
        return host.sizeThatFits(in: CGSize(width: maxContent, height: 100_000))
    }

    func copy(_ which: String) {
        onCopy?(which == "both" ? text + "\n" + plain : plain)
        flashed = which
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.2) { [weak self] in
            if self?.flashed == which { self?.flashed = nil }
        }
    }
}

struct PinyinPopupView: View {
    @ObservedObject var model: PopupModel
    @ObservedObject private var loc = Loc.shared

    private var t: PopupTheme { model.theme }

    var body: some View {
        ZStack(alignment: .topLeading) {
            card
                .padding(.top, t.catEars ? 12 : 0)
            if t.catEars {
                CatEarsView(theme: t).padding(.leading, 30)
            }
        }
        .padding(16) // chừa chỗ cho bóng đổ
    }

    private var card: some View {
        VStack(alignment: .leading, spacing: 0) {
            header
            content
            if let f = model.focused, f.isHan {
                detail(f)
            }
            footer
        }
        .background(DecorationsView(theme: t))
        .background(t.backgroundStyle)
        .clipShape(RoundedRectangle(cornerRadius: t.radius, style: .continuous))
        .overlay(RoundedRectangle(cornerRadius: t.radius, style: .continuous).stroke(Color(hex: t.border), lineWidth: 1))
        .shadow(color: .black.opacity(t.dark ? 0.35 : 0.18), radius: 12, x: 0, y: 5)
    }

    private var header: some View {
        HStack(spacing: 8) {
            EmblemView(theme: t, size: 20)
            Text("Pinyin")
                .font(.system(size: 12.5, weight: .semibold))
                .foregroundColor(Color(hex: t.fg))
            if !model.source.isEmpty {
                Text(model.source)
                    .font(.system(size: 11))
                    .foregroundColor(Color(hex: t.sub))
                    .lineLimit(1)
                    .padding(.horizontal, 8).padding(.vertical, 2)
                    .background(Capsule().fill(Color(hex: t.chip)))
            }
            Spacer(minLength: 8)
            iconButton(model.flashed == "py" ? "checkmark" : "doc.on.doc", tip: Loc.t("common.copyPinyin"),
                       tint: model.flashed == "py" ? Color(hex: "#2EB86E") : nil) { model.copy("py") }
            iconButton(model.flashed == "both" ? "checkmark" : "doc.on.clipboard", tip: Loc.t("popup.copyBoth"),
                       tint: model.flashed == "both" ? Color(hex: "#2EB86E") : nil) { model.copy("both") }
            iconButton(model.pinned ? "pin.fill" : "pin", tip: Loc.t(model.pinned ? "popup.unpin" : "popup.pin"),
                       tint: model.pinned ? Color(hex: t.accent) : nil) { model.pinned.toggle() }
            iconButton("xmark", tip: Loc.t("popup.close"), tint: nil) { model.onClose?() }
        }
        .padding(.leading, 14).padding(.trailing, 8).padding(.top, 8).padding(.bottom, 2)
    }

    private func iconButton(_ symbol: String, tip: String, tint: Color?, action: @escaping () -> Void) -> some View {
        PopupIconButton(symbol: symbol, tip: tip, tint: tint, theme: t, action: action)
    }

    @ViewBuilder
    private var content: some View {
        let ruby = RubyView(lines: model.lines, theme: t, hanziSize: model.hanziSize, toneColors: model.toneColors,
                            maxWidth: model.contentWidth, onFocus: { tok in
                                if let tok { model.focused = tok }
                            })
            .frame(width: model.contentWidth, alignment: .leading)
        if model.scrolls {
            ScrollView(.vertical) { ruby }
                .frame(width: model.contentWidth + 12, height: 400)
                .padding(.leading, 12).padding(.vertical, 6)
        } else {
            ruby
                .padding(.leading, 12).padding(.trailing, 10).padding(.top, 6).padding(.bottom, 8)
        }
    }

    private func detail(_ tok: PyToken) -> some View {
        HStack(spacing: 6) {
            Text(tok.text).font(.custom("PingFang SC", size: 13)).foregroundColor(Color(hex: t.hanzi))
            Text("·").foregroundColor(Color(hex: t.sub))
            Text(tok.alternatives.count > 1 ? tok.alternatives.joined(separator: " / ") : (tok.pinyin ?? "?"))
                .font(.system(size: 12))
                .foregroundColor(Color(hex: t.sub))
        }
        .padding(.horizontal, 16).padding(.bottom, 6)
        .frame(width: model.contentWidth + 22, alignment: .leading)
    }

    private var footer: some View {
        Text(model.plain)
            .font(.system(size: 12.5))
            .foregroundColor(Color(hex: t.sub))
            .textSelection(.enabled)
            .fixedSize(horizontal: false, vertical: true)
            .frame(width: model.contentWidth, alignment: .leading)
            .padding(.horizontal, 12).padding(.top, 7).padding(.bottom, 9)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(hex: t.footer))
            .overlay(Rectangle().fill(Color(hex: t.divider)).frame(height: 1), alignment: .top)
    }
}

struct PopupIconButton: View {
    let symbol: String
    let tip: String
    let tint: Color?
    let theme: PopupTheme
    let action: () -> Void
    @State private var hover = false

    var body: some View {
        Button(action: action) {
            Image(systemName: symbol)
                .font(.system(size: 12, weight: .medium))
                .foregroundColor(tint ?? Color(hex: hover ? theme.fg : theme.sub))
                .frame(width: 28, height: 26)
                .background(RoundedRectangle(cornerRadius: 6).fill(hover ? Color(hex: theme.hover) : Color.clear))
                .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .help(tip)
        .onHover { hover = $0 }
    }
}

// MARK: - Cửa sổ popup

/// Panel nổi không chiếm focus: đang gõ chat vẫn gõ tiếp được.
final class PopupPanel: NSPanel {
    var onEscape: (() -> Void)?

    static func make() -> PopupPanel {
        let p = PopupPanel(contentRect: .zero, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        p.isOpaque = false
        p.backgroundColor = .clear
        p.hasShadow = false               // bóng đổ vẽ bằng SwiftUI (khớp góc bo)
        p.level = .floating
        p.isFloatingPanel = true
        p.hidesOnDeactivate = false
        p.becomesKeyOnlyIfNeeded = true
        p.isMovableByWindowBackground = true
        p.isReleasedWhenClosed = false
        p.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .transient, .ignoresCycle]
        return p
    }

    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { false }

    override func keyDown(with event: NSEvent) {
        if event.keyCode == 53 { onEscape?() } else { super.keyDown(with: event) }
    }
}

/// Quản lý hiển thị popup: đặt cạnh con trỏ, đóng khi click ra ngoài / Esc.
final class PopupController {
    let model: PopupModel
    private let panel = PopupPanel.make()
    private var monitors: [Any] = []
    private(set) var isClosed = false
    var onClosed: (() -> Void)?

    var isPinned: Bool { model.pinned }

    init(model: PopupModel) {
        self.model = model
        model.onClose = { [weak self] in self?.close() }
        panel.onEscape = { [weak self] in self?.close() }

        let host = NSHostingView(rootView: PinyinPopupView(model: model))
        host.layoutSubtreeIfNeeded()
        let size = host.fittingSize
        host.frame = NSRect(origin: .zero, size: size)
        panel.contentView = host
        panel.setContentSize(size)
    }

    func show() {
        position()
        panel.alphaValue = 0
        panel.orderFrontRegardless()
        NSAnimationContext.runAnimationGroup { ctx in
            ctx.duration = 0.16
            panel.animator().alphaValue = 1
        }

        // Click ra ngoài (ở app khác) → đóng, trừ khi đã ghim
        if let m = NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .rightMouseDown], handler: { [weak self] _ in
            guard let self, !self.model.pinned else { return }
            self.close()
        }) { monitors.append(m) }
        // Esc ở app khác (chỉ hoạt động khi có quyền Trợ năng)
        if let m = NSEvent.addGlobalMonitorForEvents(matching: .keyDown, handler: { [weak self] e in
            if e.keyCode == 53 { self?.close() }
        }) { monitors.append(m) }
        // Click vào cửa sổ khác của chính GenPiYi
        if let m = NSEvent.addLocalMonitorForEvents(matching: [.leftMouseDown, .rightMouseDown], handler: { [weak self] e in
            if let self, !self.model.pinned, e.window !== self.panel { self.close() }
            return e
        }) { monitors.append(m) }
    }

    /// Đặt popup ngay dưới-phải con trỏ, không tràn khỏi màn hình.
    private func position() {
        let mouse = NSEvent.mouseLocation
        let screen = NSScreen.screens.first { NSMouseInRect(mouse, $0.frame, false) } ?? NSScreen.main
        let vf = screen?.visibleFrame ?? NSRect(x: 0, y: 0, width: 1440, height: 900)
        let size = panel.frame.size
        let m: CGFloat = 16 // lề trong suốt quanh thẻ (chỗ cho bóng)

        var x = mouse.x - m + 6
        var y = mouse.y + m - 14 - size.height   // cạnh trên của thẻ nằm dưới con trỏ
        if x + size.width > vf.maxX { x = vf.maxX - size.width }
        if y < vf.minY { y = mouse.y - m + 6 }      // không đủ chỗ phía dưới → hiện phía trên con trỏ
        if y + size.height > vf.maxY { y = vf.maxY - size.height }
        if x < vf.minX { x = vf.minX }
        if y < vf.minY { y = vf.minY }
        panel.setFrameOrigin(NSPoint(x: x, y: y))
    }

    func close() {
        guard !isClosed else { return }
        isClosed = true
        monitors.forEach { NSEvent.removeMonitor($0) }
        monitors.removeAll()
        NSAnimationContext.runAnimationGroup({ ctx in
            ctx.duration = 0.1
            panel.animator().alphaValue = 0
        }, completionHandler: { [panel] in
            panel.orderOut(nil)
            panel.close()
        })
        onClosed?()
    }
}
