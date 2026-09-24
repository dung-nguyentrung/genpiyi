import SwiftUI
import AppKit

enum Page: String, CaseIterable {
    case lookup, themes, settings, guide

    var titleKey: String {
        switch self {
        case .lookup: return "nav.try"
        case .themes: return "nav.theme"
        case .settings: return "nav.settings"
        case .guide: return "nav.guide"
        }
    }

    var symbol: String {
        switch self {
        case .lookup: return "character.book.closed"
        case .themes: return "paintpalette"
        case .settings: return "gearshape"
        case .guide: return "questionmark.circle"
        }
    }
}

final class NavModel: ObservableObject {
    static let shared = NavModel()
    @Published var page: Page = .lookup
}

enum Brand {
    static let accent = Color(hex: "#E5484D")
    static let accent2 = Color(hex: "#FF6B6B")
    static var gradient: LinearGradient {
        LinearGradient(colors: [accent2, Color(hex: "#D93A40")], startPoint: .topLeading, endPoint: .bottomTrailing)
    }
}

struct MainView: View {
    @ObservedObject private var nav = NavModel.shared
    @ObservedObject private var loc = Loc.shared
    @ObservedObject private var settings = AppSettings.shared

    var body: some View {
        HStack(spacing: 0) {
            Sidebar()
                .frame(width: 220)
            Divider()
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    switch nav.page {
                    case .lookup: LookupPage()
                    case .themes: ThemesPage()
                    case .settings: SettingsPage()
                    case .guide: GuidePage()
                    }
                }
                .padding(.horizontal, 32)
                .padding(.top, 36)
                .padding(.bottom, 28)
                .frame(maxWidth: .infinity, alignment: .leading)
            }
            .background(Color(nsColor: .windowBackgroundColor))
        }
        .frame(minWidth: 760, minHeight: 520)
        .tint(Brand.accent)
        .id(loc.lang) // đổi ngôn ngữ → dựng lại toàn bộ chữ
    }
}

// MARK: - Thanh bên

struct Sidebar: View {
    @ObservedObject private var nav = NavModel.shared

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack(spacing: 10) {
                ZStack {
                    RoundedRectangle(cornerRadius: 9, style: .continuous).fill(Brand.gradient)
                    Text("拼").font(.system(size: 20, weight: .bold)).foregroundColor(.white)
                }
                .frame(width: 36, height: 36)
                .shadow(color: Brand.accent.opacity(0.35), radius: 6, y: 3)
                VStack(alignment: .leading, spacing: 1) {
                    Text("GenPiYi").font(.system(size: 15, weight: .bold))
                    Text(Loc.t("app.tagline")).font(.system(size: 10.5)).foregroundColor(.secondary).lineLimit(2)
                }
            }
            .padding(.top, 38)
            .padding(.bottom, 22)
            .padding(.horizontal, 6)

            ForEach(Page.allCases, id: \.self) { p in
                SidebarItem(page: p, selected: nav.page == p) { nav.page = p }
            }

            Spacer()

            HStack(spacing: 6) {
                Circle().fill(Color(hex: "#2EB86E")).frame(width: 7, height: 7)
                Text(Loc.t("engine.ok")).font(.system(size: 11)).foregroundColor(.secondary)
            }
            .padding(.horizontal, 8)
            Text("macOS · v\(Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "dev")")
                .font(.system(size: 10.5)).foregroundColor(.secondary.opacity(0.7))
                .padding(.horizontal, 8).padding(.bottom, 16)
        }
        .padding(.horizontal, 12)
        .frame(maxHeight: .infinity, alignment: .top)
        .background(VisualEffect().ignoresSafeArea())
    }
}

struct SidebarItem: View {
    let page: Page
    let selected: Bool
    let action: () -> Void
    @State private var hover = false

    var body: some View {
        Button(action: action) {
            HStack(spacing: 10) {
                Image(systemName: page.symbol)
                    .font(.system(size: 13, weight: .medium))
                    .frame(width: 18)
                Text(Loc.t(page.titleKey)).font(.system(size: 13, weight: selected ? .semibold : .regular))
                Spacer()
            }
            .foregroundColor(selected ? Brand.accent : .primary)
            .padding(.horizontal, 10).padding(.vertical, 8)
            .background(
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .fill(selected ? Brand.accent.opacity(0.12) : (hover ? Color.primary.opacity(0.06) : .clear))
            )
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .onHover { hover = $0 }
    }
}

struct VisualEffect: NSViewRepresentable {
    func makeNSView(context: Context) -> NSVisualEffectView {
        let v = NSVisualEffectView()
        v.material = .sidebar
        v.blendingMode = .behindWindow
        v.state = .followsWindowActiveState
        return v
    }
    func updateNSView(_ nsView: NSVisualEffectView, context: Context) {}
}

// MARK: - Thành phần chung

struct PageHeader: View {
    let title: String
    let subtitle: String
    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title).font(.system(size: 26, weight: .bold))
            Text(subtitle).font(.system(size: 13)).foregroundColor(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(.bottom, 20)
    }
}

struct Card<Content: View>: View {
    @ViewBuilder var content: Content
    var body: some View {
        VStack(alignment: .leading, spacing: 0) { content }
            .padding(16)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(RoundedRectangle(cornerRadius: 12, style: .continuous).fill(Color(nsColor: .controlBackgroundColor)))
            .overlay(RoundedRectangle(cornerRadius: 12, style: .continuous).stroke(Color.primary.opacity(0.08), lineWidth: 1))
    }
}

struct PillButton: View {
    let title: String
    var symbol: String? = nil
    var prominent = false
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 6) {
                if let s = symbol { Image(systemName: s).font(.system(size: 11, weight: .semibold)) }
                Text(title).font(.system(size: 12.5, weight: .medium))
            }
            .padding(.horizontal, 12).padding(.vertical, 6)
            .foregroundColor(prominent ? .white : .primary)
            .background(
                Capsule().fill(prominent ? AnyShapeStyle(Brand.gradient) : AnyShapeStyle(Color.primary.opacity(0.07)))
            )
            .contentShape(Capsule())
        }
        .buttonStyle(.plain)
    }
}

// MARK: - Trang Tra pinyin

struct LookupPage: View {
    @ObservedObject private var settings = AppSettings.shared
    @State private var input = ""
    @State private var copied = false

    private var lines: [[PyToken]] { PinyinService.convert(input, style: settings.data.toneStyle) }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            PageHeader(title: Loc.t("nav.try"), subtitle: Loc.t("try.subtitle"))

            Card {
                ZStack(alignment: .topLeading) {
                    if input.isEmpty {
                        Text(Loc.t("try.placeholder"))
                            .font(.system(size: 16))
                            .foregroundColor(.secondary.opacity(0.7))
                            .padding(.top, 1).padding(.leading, 5)
                            .allowsHitTesting(false)
                    }
                    TextEditor(text: $input)
                        .font(.custom("PingFang SC", size: 16))
                        .scrollContentBackground(.hidden)
                        .frame(minHeight: 90, maxHeight: 150)
                }
                HStack(spacing: 8) {
                    PillButton(title: Loc.t("try.paste"), symbol: "doc.on.clipboard") {
                        input = NSPasteboard.general.string(forType: .string) ?? input
                    }
                    PillButton(title: Loc.t("try.clear"), symbol: "xmark") { input = "" }
                    Spacer()
                    PillButton(title: Loc.t("try.preview"), symbol: "sparkles", prominent: true) {
                        let text = input.trimmingCharacters(in: .whitespacesAndNewlines)
                        AppController.shared.showPopup(text: text.isEmpty ? "你好！今天晚上我们去哪里吃饭？" : text,
                                                       source: Loc.t("theme.previewSource"))
                    }
                }
                .padding(.top, 10)
            }

            HStack {
                Text(Loc.t("try.result")).font(.system(size: 13, weight: .semibold)).foregroundColor(.secondary)
                Spacer()
                if PinyinService.containsHan(input) {
                    PillButton(title: copied ? Loc.t("common.copied") : Loc.t("common.copyPinyin"),
                               symbol: copied ? "checkmark" : "doc.on.doc") {
                        AppController.shared.copyToClipboard(PinyinService.toPlainPinyin(lines))
                        copied = true
                        DispatchQueue.main.asyncAfter(deadline: .now() + 1.2) { copied = false }
                    }
                }
            }
            .padding(.top, 4)

            let theme = settings.theme
            VStack(alignment: .leading, spacing: 10) {
                if PinyinService.containsHan(input) {
                    let ls = lines
                    RubyView(lines: ls, theme: theme, hanziSize: CGFloat(settings.data.hanziFontSize),
                             toneColors: settings.data.toneColors, maxWidth: 620,
                             infos: DictionaryService.wordInfos(ls), meaningLang: settings.data.resolvedMeaningLang,
                             showHanViet: settings.data.showHanViet)
                    Divider().overlay(Color(hex: theme.divider))
                    Text(PinyinService.toPlainPinyin(ls))
                        .font(.system(size: 13))
                        .foregroundColor(Color(hex: theme.sub))
                        .textSelection(.enabled)
                    let infos = DictionaryService.wordInfos(ls)
                    let vocab = DictionaryService.vocabulary(ls, infos: infos)
                    if !vocab.isEmpty {
                        Divider().overlay(Color(hex: theme.divider))
                        Text(Loc.t("try.vocab")).font(.system(size: 12, weight: .semibold)).foregroundColor(Color(hex: theme.sub))
                        VocabListView(words: vocab, theme: theme, meaningLang: settings.data.resolvedMeaningLang,
                                      showHanViet: settings.data.showHanViet, width: nil)
                    }
                } else {
                    Text(Loc.t("try.empty")).font(.system(size: 13)).foregroundColor(Color(hex: theme.sub))
                        .frame(maxWidth: .infinity, minHeight: 80)
                }
            }
            .padding(18)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(DecorationsView(theme: theme))
            .background(theme.backgroundStyle)
            .clipShape(RoundedRectangle(cornerRadius: theme.radius, style: .continuous))
            .overlay(RoundedRectangle(cornerRadius: theme.radius, style: .continuous).stroke(Color(hex: theme.border), lineWidth: 1))
        }
    }
}

// MARK: - Trang Giao diện

struct ThemesPage: View {
    @ObservedObject private var settings = AppSettings.shared

    private let columns = [GridItem(.adaptive(minimum: 250, maximum: 360), spacing: 16)]

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            PageHeader(title: Loc.t("nav.theme"), subtitle: Loc.t("theme.subtitle"))

            Text(Loc.t("theme.featured")).font(.system(size: 13, weight: .semibold)).foregroundColor(.secondary)
            LazyVGrid(columns: columns, alignment: .leading, spacing: 16) {
                ForEach(ThemeCatalog.all.filter { $0.deco != "none" }) { t in ThemeCard(theme: t) }
            }
            Text(Loc.t("theme.basic")).font(.system(size: 13, weight: .semibold)).foregroundColor(.secondary)
                .padding(.top, 8)
            LazyVGrid(columns: columns, alignment: .leading, spacing: 16) {
                ForEach(ThemeCatalog.all.filter { $0.deco == "none" }) { t in ThemeCard(theme: t) }
            }
        }
    }
}

struct ThemeCard: View {
    let theme: PopupTheme
    @ObservedObject private var settings = AppSettings.shared
    @State private var hover = false

    private var inUse: Bool { settings.data.popupTheme == theme.id }

    private static let sample = PinyinService.convert("你好朋友", style: "mark")

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Button {
                AppController.shared.showPopup(text: "你好！今天晚上我们去哪里吃饭？", source: Loc.t("theme.previewSource"), theme: theme)
            } label: {
                preview
            }
            .buttonStyle(.plain)
            .help(Loc.t("theme.previewSource"))

            HStack(alignment: .center) {
                VStack(alignment: .leading, spacing: 2) {
                    HStack(spacing: 6) {
                        Text(theme.locName).font(.system(size: 13.5, weight: .semibold))
                        if !theme.badge.isEmpty {
                            Text(theme.locBadge)
                                .font(.system(size: 9.5, weight: .bold))
                                .foregroundColor(.white)
                                .padding(.horizontal, 6).padding(.vertical, 1)
                                .background(Capsule().fill(theme.badge == "hot" ? Color(hex: "#F08A24") : Brand.accent))
                        }
                    }
                    Text(theme.locDescription).font(.system(size: 11.5)).foregroundColor(.secondary)
                }
                Spacer()
                if inUse {
                    Label(Loc.t("theme.inUse"), systemImage: "checkmark.circle.fill")
                        .font(.system(size: 12, weight: .medium))
                        .foregroundColor(Color(hex: "#2EB86E"))
                } else {
                    PillButton(title: Loc.t("theme.use")) { settings.data.popupTheme = theme.id }
                }
            }
            .padding(12)
        }
        .background(RoundedRectangle(cornerRadius: 14, style: .continuous).fill(Color(nsColor: .controlBackgroundColor)))
        .overlay(
            RoundedRectangle(cornerRadius: 14, style: .continuous)
                .stroke(inUse ? Brand.accent : Color.primary.opacity(hover ? 0.18 : 0.08), lineWidth: inUse ? 2 : 1)
        )
        .scaleEffect(hover ? 1.01 : 1)
        .animation(.easeOut(duration: 0.12), value: hover)
        .onHover { hover = $0 }
    }

    private var preview: some View {
        ZStack {
            LinearGradient(colors: [Color.primary.opacity(0.04), Color.primary.opacity(0.08)], startPoint: .top, endPoint: .bottom)
            ZStack(alignment: .topLeading) {
                VStack(alignment: .leading, spacing: 6) {
                    HStack(spacing: 6) {
                        EmblemView(theme: theme, size: 16)
                        Text("Pinyin").font(.system(size: 11, weight: .semibold)).foregroundColor(Color(hex: theme.fg))
                        Spacer()
                        Image(systemName: "pin").font(.system(size: 10)).foregroundColor(Color(hex: theme.sub))
                    }
                    RubyView(lines: Self.sample, theme: theme, hanziSize: 22, toneColors: true, maxWidth: 200)
                }
                .padding(10)
                .frame(width: 190, alignment: .leading)
                .background(DecorationsView(theme: theme, scale: 0.7))
                .background(theme.backgroundStyle)
                .clipShape(RoundedRectangle(cornerRadius: theme.radius * 0.8, style: .continuous))
                .overlay(RoundedRectangle(cornerRadius: theme.radius * 0.8, style: .continuous).stroke(Color(hex: theme.border), lineWidth: 1))
                .shadow(color: .black.opacity(0.15), radius: 8, y: 3)
                .padding(.top, theme.catEars ? 9 : 0)
                if theme.catEars { CatEarsView(theme: theme, scale: 0.75).padding(.leading, 20) }
            }
            .padding(.vertical, 16)
        }
        .frame(maxWidth: .infinity)
        .frame(height: 150)
        .clipShape(UnevenRoundedRectangleCompat(radius: 14))
        .contentShape(Rectangle())
    }
}

/// Bo 2 góc trên (UnevenRoundedRectangle chỉ có từ macOS 14).
struct UnevenRoundedRectangleCompat: Shape {
    let radius: CGFloat
    func path(in r: CGRect) -> Path {
        var p = Path()
        p.move(to: CGPoint(x: r.minX, y: r.maxY))
        p.addLine(to: CGPoint(x: r.minX, y: r.minY + radius))
        p.addArc(center: CGPoint(x: r.minX + radius, y: r.minY + radius), radius: radius,
                 startAngle: .degrees(180), endAngle: .degrees(270), clockwise: false)
        p.addLine(to: CGPoint(x: r.maxX - radius, y: r.minY))
        p.addArc(center: CGPoint(x: r.maxX - radius, y: r.minY + radius), radius: radius,
                 startAngle: .degrees(270), endAngle: .degrees(0), clockwise: false)
        p.addLine(to: CGPoint(x: r.maxX, y: r.maxY))
        p.closeSubpath()
        return p
    }
}

// MARK: - Trang Hướng dẫn

struct GuidePage: View {
    @ObservedObject private var settings = AppSettings.shared

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            PageHeader(title: Loc.t("nav.guide"), subtitle: Loc.t("guide.subtitle"))
            step(1, "cursorarrow.click.2", Loc.t("guide.s1"), Loc.t("guide.s1Desc"))
            step(2, "keyboard", Loc.t("guide.s2") + KeyCombo.display(settings.data.hotkey), Loc.t("guide.s2Desc"))
            step(3, "rectangle.and.hand.point.up.left", Loc.t("guide.s3"), Loc.t("guide.s3Desc"))
            HStack(alignment: .top, spacing: 10) {
                Image(systemName: "menubar.rectangle").foregroundColor(.secondary)
                Text(Loc.t("guide.tray")).font(.system(size: 12.5)).foregroundColor(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
            .padding(14)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(RoundedRectangle(cornerRadius: 10).fill(Color.primary.opacity(0.04)))
        }
    }

    private func step(_ n: Int, _ symbol: String, _ title: String, _ desc: String) -> some View {
        Card {
            HStack(alignment: .top, spacing: 14) {
                ZStack {
                    Circle().fill(Brand.gradient)
                    Text("\(n)").font(.system(size: 15, weight: .bold)).foregroundColor(.white)
                }
                .frame(width: 32, height: 32)
                VStack(alignment: .leading, spacing: 5) {
                    HStack(spacing: 6) {
                        Image(systemName: symbol).foregroundColor(Brand.accent)
                        Text(title).font(.system(size: 14, weight: .semibold))
                    }
                    Text(desc).font(.system(size: 12.5)).foregroundColor(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
        }
    }
}
