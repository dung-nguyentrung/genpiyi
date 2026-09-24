import SwiftUI

extension Color {
    /// "#RRGGBB" hoặc "#AARRGGBB" (giống WPF).
    init(hex: String) {
        var s = hex.trimmingCharacters(in: .whitespaces)
        if s.hasPrefix("#") { s.removeFirst() }
        var v: UInt64 = 0
        Scanner(string: s).scanHexInt64(&v)
        let a, r, g, b: Double
        if s.count == 8 {
            a = Double((v >> 24) & 0xFF) / 255
            r = Double((v >> 16) & 0xFF) / 255
            g = Double((v >> 8) & 0xFF) / 255
            b = Double(v & 0xFF) / 255
        } else {
            a = 1
            r = Double((v >> 16) & 0xFF) / 255
            g = Double((v >> 8) & 0xFF) / 255
            b = Double(v & 0xFF) / 255
        }
        self.init(.sRGB, red: r, green: g, blue: b, opacity: a)
    }
}

/// Một mẫu giao diện (skin) cho popup pinyin — cùng bộ mẫu với bản Windows.
struct PopupTheme: Identifiable, Equatable {
    var id: String
    var name: String
    var description: String
    var nameEn: String
    var descriptionEn: String
    var badge: String = ""
    var dark: Bool = false

    var background: [String] = ["#FFFFFF", "#FFFFFF"]
    var border: String = "#DDE1E6"
    var radius: CGFloat = 12

    var fg: String = "#1B1F27"
    var sub: String = "#5B6573"
    var hover: String = "#0F000000"
    var chip: String = "#F0F2F5"
    var footer: String = "#F7F8FA"
    var divider: String = "#EEF0F3"
    var accent: String = "#E5484D"

    var hanzi: String = "#1B1F27"
    var pinyinDefault: String = "#2F6FEB"
    var other: String = "#5B6573"
    var missing: String = "#A3ACB8"
    var cellHover: String = "#0D000000"
    /// [0]=không dùng, [1..4], [5]=thanh nhẹ
    var tones: [String] = ["#5B6573", "#E5484D", "#D97A00", "#1E9E5A", "#2F6FEB", "#8A94A3"]

    var emblem: String = "拼"
    var emblemColors: [String] = ["#FF6B6B", "#D93A40"]
    var deco: String = "none"   // hearts, paws, petals, leaves, bubbles, sparkles
    var decoColor: String = "#FF6B6B"
    var decoColor2: String = ""
    var catEars: Bool = false

    var locName: String { Loc.shared.isEn ? nameEn : name }
    var locDescription: String { Loc.shared.isEn ? descriptionEn : description }
    var locBadge: String { badge.isEmpty ? "" : Loc.t("badge." + badge) }

    func toneColor(_ t: Int) -> Color { Color(hex: tones[max(0, min(5, t))]) }

    var backgroundStyle: LinearGradient {
        LinearGradient(colors: background.map { Color(hex: $0) }, startPoint: .topLeading, endPoint: .bottomTrailing)
    }

    var emblemGradient: LinearGradient {
        LinearGradient(colors: [Color(hex: emblemColors.first!), Color(hex: emblemColors.last!)],
                       startPoint: .topLeading, endPoint: .bottomTrailing)
    }
}

enum ThemeCatalog {
    static let all: [PopupTheme] = [
        PopupTheme(id: "dark", name: "Đêm", description: "Tối giản, dễ nhìn", nameEn: "Night",
                   descriptionEn: "Minimal and easy on the eyes", dark: true,
                   background: ["#FA1C2029", "#FA1C2029"], border: "#33FFFFFF",
                   fg: "#F3F4F6", sub: "#A0A8B5", hover: "#1FFFFFFF", chip: "#26FFFFFF", footer: "#10FFFFFF", divider: "#1AFFFFFF",
                   hanzi: "#F5F7FA", pinyinDefault: "#9EC5FF", other: "#AEB6C2", missing: "#6B7480", cellHover: "#1AFFFFFF",
                   tones: ["#AEB6C2", "#FF6B6B", "#FFB454", "#5FD38D", "#5AA9FF", "#98A2AF"]),
        PopupTheme(id: "light", name: "Trắng tinh", description: "Sáng sủa, gọn gàng", nameEn: "Pure White",
                   descriptionEn: "Bright and tidy"),
        PopupTheme(id: "strawberry", name: "Sữa dâu", description: "Hồng ngọt ngào", nameEn: "Strawberry Milk",
                   descriptionEn: "Sweet and pink", badge: "new",
                   background: ["#FFF6F8", "#FFE1EA"], border: "#FFC9D8", radius: 16,
                   fg: "#6B2C3E", sub: "#A0667A", hover: "#14FF5C8A", chip: "#FFE3EC", footer: "#80FFFFFF", divider: "#FFD3E0",
                   accent: "#FF5C8A", hanzi: "#4A1F2C", pinyinDefault: "#E0457B", other: "#A0667A", cellHover: "#14FF5C8A",
                   tones: ["#A0667A", "#E8365D", "#E27A12", "#2E9A62", "#3C6FE0", "#B08A98"],
                   emblem: "莓", emblemColors: ["#FF8FB1", "#FF4F7E"],
                   deco: "hearts", decoColor: "#FF7AA2", decoColor2: "#FFB3C9"),
        PopupTheme(id: "tabby", name: "Mèo mướp", description: "Có tai mèo và dấu chân", nameEn: "Tabby Cat",
                   descriptionEn: "Cat ears and paw prints", badge: "hot",
                   background: ["#FFFBF2", "#FFEBCB"], border: "#F6D7A6", radius: 16,
                   fg: "#5A3A17", sub: "#9A7447", hover: "#18F08A24", chip: "#FDE7C4", footer: "#80FFFFFF", divider: "#F6DDB5",
                   accent: "#F08A24", hanzi: "#4A2F12", pinyinDefault: "#D46F0E", other: "#9A7447", cellHover: "#18F08A24",
                   tones: ["#9A7447", "#E0453A", "#D97A00", "#2F9A56", "#2F6FEB", "#A68B6A"],
                   emblem: "喵", emblemColors: ["#FFB25B", "#F08A24"],
                   deco: "paws", decoColor: "#E9A45E", decoColor2: "#F6C997", catEars: true),
        PopupTheme(id: "matcha", name: "Trà xanh", description: "Xanh dịu mắt", nameEn: "Matcha",
                   descriptionEn: "Soft, calming green", badge: "new",
                   background: ["#F6FBF0", "#E1F0D2"], border: "#CDE3B8", radius: 14,
                   fg: "#2F4A22", sub: "#6A8757", hover: "#185A9E4B", chip: "#E3F1D6", footer: "#80FFFFFF", divider: "#D6E8C5",
                   accent: "#5A9E4B", hanzi: "#233A18", pinyinDefault: "#3F8A34", other: "#6A8757", cellHover: "#185A9E4B",
                   tones: ["#6A8757", "#D9463E", "#C97A10", "#23904F", "#2E6BD6", "#8C9C80"],
                   emblem: "茶", emblemColors: ["#8CC77A", "#4E9442"],
                   deco: "leaves", decoColor: "#7DB46A", decoColor2: "#A9D196"),
        PopupTheme(id: "sakura", name: "Hoa anh đào", description: "Cánh hoa bay nhẹ", nameEn: "Cherry Blossom",
                   descriptionEn: "Gently drifting petals",
                   background: ["#FFFFFF", "#FCE6EF"], border: "#F7D2E0", radius: 16,
                   fg: "#5C3446", sub: "#9C7385", hover: "#14E86A9A", chip: "#FBE3EC", footer: "#80FFFFFF", divider: "#F6DCE6",
                   accent: "#E86A9A", hanzi: "#43222F", pinyinDefault: "#D0578A", other: "#9C7385", cellHover: "#14E86A9A",
                   tones: ["#9C7385", "#E23D5E", "#DB7A10", "#2C955C", "#3A6BE0", "#B294A2"],
                   emblem: "樱", emblemColors: ["#F8A5C2", "#E86A9A"],
                   deco: "petals", decoColor: "#F4A3C0", decoColor2: "#FAD0DF"),
        PopupTheme(id: "ocean", name: "Biển xanh", description: "Mát lạnh như sóng biển", nameEn: "Ocean Blue",
                   descriptionEn: "Cool as the sea breeze",
                   background: ["#F2FAFF", "#D5ECFF"], border: "#BFDDF7", radius: 14,
                   fg: "#173A5C", sub: "#5C7E9E", hover: "#182F8FE0", chip: "#DCEEFF", footer: "#80FFFFFF", divider: "#CDE4F7",
                   accent: "#2F8FE0", hanzi: "#10304F", pinyinDefault: "#1F7BD0", other: "#5C7E9E", cellHover: "#182F8FE0",
                   tones: ["#5C7E9E", "#E0454F", "#D27A0E", "#1E9460", "#2C5FD8", "#8499AD"],
                   emblem: "海", emblemColors: ["#6EC1FF", "#2F8FE0"],
                   deco: "bubbles", decoColor: "#6FB7F0", decoColor2: "#A8D6FA"),
        PopupTheme(id: "galaxy", name: "Ngân hà", description: "Bầu trời đêm lấp lánh", nameEn: "Galaxy",
                   descriptionEn: "A sparkling night sky", badge: "new", dark: true,
                   background: ["#FA221A4A", "#FA3A2470"], border: "#40B69CFF", radius: 14,
                   fg: "#F2EEFF", sub: "#B7AEDB", hover: "#22FFFFFF", chip: "#2EFFFFFF", footer: "#14FFFFFF", divider: "#22FFFFFF",
                   accent: "#B69CFF", hanzi: "#FFFFFF", pinyinDefault: "#C9B8FF", other: "#B7AEDB", missing: "#7C73A3", cellHover: "#1FFFFFFF",
                   tones: ["#B7AEDB", "#FF7A93", "#FFC46B", "#6FE3A5", "#7DB8FF", "#A59DC6"],
                   emblem: "星", emblemColors: ["#C9B8FF", "#8A6CF0"],
                   deco: "sparkles", decoColor: "#FFE9A8", decoColor2: "#C9B8FF"),
    ]

    static func get(_ id: String?) -> PopupTheme {
        all.first { $0.id.caseInsensitiveCompare(id ?? "") == .orderedSame } ?? all[0]
    }
}

// MARK: - Hình trang trí

/// Biểu tượng tròn có chữ (拼, 莓, 喵…) ở góc trái tiêu đề popup.
struct EmblemView: View {
    let theme: PopupTheme
    var size: CGFloat = 20

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: size * 0.3, style: .continuous).fill(theme.emblemGradient)
            Text(theme.emblem)
                .font(.system(size: size * 0.6, weight: .bold))
                .foregroundColor(.white)
        }
        .frame(width: size, height: size)
    }
}

/// Hình trang trí rải ở các góc thẻ (vẽ bằng SF Symbols).
struct DecorationsView: View {
    let theme: PopupTheme
    var scale: CGFloat = 1

    private var symbol: String {
        switch theme.deco {
        case "hearts": return "heart.fill"
        case "paws": return "pawprint.fill"
        case "petals": return "camera.macro"
        case "leaves": return "leaf.fill"
        case "sparkles": return "sparkle"
        case "bubbles": return "circle"
        default: return ""
        }
    }

    private struct Spot {
        let alignment: Alignment
        let x: CGFloat, y: CGFloat, size: CGFloat, angle: Double, opacity: Double, alt: Bool
    }

    // (căn, lệch x, lệch y, cỡ, góc xoay, độ mờ, màu phụ) — không che nút ở góc trên phải
    private let spots: [Spot] = [
        Spot(alignment: .bottomTrailing, x: -10, y: -42, size: 36, angle: 14, opacity: 0.45, alt: false),
        Spot(alignment: .bottomTrailing, x: -50, y: -66, size: 16, angle: -18, opacity: 0.40, alt: true),
        Spot(alignment: .bottomTrailing, x: -18, y: -88, size: 11, angle: 25, opacity: 0.35, alt: true),
        Spot(alignment: .bottomLeading, x: 8, y: -44, size: 18, angle: -12, opacity: 0.30, alt: true),
        Spot(alignment: .trailing, x: -6, y: -10, size: 12, angle: 8, opacity: 0.28, alt: false),
        Spot(alignment: .topLeading, x: 140, y: 8, size: 10, angle: -20, opacity: 0.35, alt: true),
    ]

    var body: some View {
        ZStack {
            if theme.deco != "none" {
                ForEach(0..<spots.count, id: \.self) { i in
                    let s = spots[i]
                    let color = Color(hex: s.alt && !theme.decoColor2.isEmpty ? theme.decoColor2 : theme.decoColor)
                    Image(systemName: symbol)
                        .resizable()
                        .scaledToFit()
                        .font(.system(size: s.size * scale, weight: theme.deco == "bubbles" ? .light : .regular))
                        .foregroundColor(color)
                        .frame(width: s.size * scale, height: s.size * scale)
                        .rotationEffect(.degrees(s.angle))
                        .opacity(s.opacity)
                        .offset(x: s.x * scale, y: s.y * scale)
                        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: s.alignment)
                }
            }
        }
        .allowsHitTesting(false)
        .clipped()
    }
}

/// Hai tai mèo nhô lên trên mép thẻ (mẫu Mèo mướp).
struct CatEarsView: View {
    let theme: PopupTheme
    var scale: CGFloat = 1

    private struct Ear: Shape {
        func path(in r: CGRect) -> Path {
            var p = Path()
            p.move(to: CGPoint(x: r.minX, y: r.maxY))
            p.addLine(to: CGPoint(x: r.width * 0.375, y: r.height * 0.1))
            p.addQuadCurve(to: CGPoint(x: r.width * 0.58, y: r.height * 0.1),
                           control: CGPoint(x: r.width * 0.46, y: -r.height * 0.05))
            p.addLine(to: CGPoint(x: r.maxX, y: r.maxY))
            p.closeSubpath()
            return p
        }
    }

    private func ear() -> some View {
        ZStack(alignment: .bottom) {
            Ear().fill(Color(hex: theme.background[0]))
            Ear().stroke(Color(hex: theme.border), lineWidth: 1)
            Ear().fill(Color(hex: "#FFC9B0")).frame(width: 12 * scale, height: 12 * scale)
        }
        .frame(width: 24 * scale, height: 20 * scale)
    }

    var body: some View {
        HStack(spacing: 28 * scale) {
            ear().rotationEffect(.degrees(-8))
            ear().rotationEffect(.degrees(8))
        }
        .allowsHitTesting(false)
    }
}
