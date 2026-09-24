import SwiftUI

/// Xếp các ô theo hàng, tự xuống dòng khi vượt maxWidth. Trả về đúng bề rộng đã dùng
/// (để popup co gọn theo nội dung).
struct FlowLayout: Layout {
    var maxWidth: CGFloat
    var rowSpacing: CGFloat = 4

    private func rows(_ subviews: Subviews, limit: CGFloat) -> [[(Int, CGSize)]] {
        var rows: [[(Int, CGSize)]] = [[]]
        var x: CGFloat = 0
        for (i, sv) in subviews.enumerated() {
            let size = sv.sizeThatFits(.unspecified)
            if x + size.width > limit && !rows[rows.count - 1].isEmpty {
                rows.append([])
                x = 0
            }
            rows[rows.count - 1].append((i, size))
            x += size.width
        }
        return rows
    }

    func sizeThatFits(proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) -> CGSize {
        let limit = min(proposal.width ?? maxWidth, maxWidth)
        let rs = rows(subviews, limit: limit)
        var w: CGFloat = 0, h: CGFloat = 0
        for (k, r) in rs.enumerated() {
            w = max(w, r.reduce(0) { $0 + $1.1.width })
            h += (r.map { $0.1.height }.max() ?? 0) + (k > 0 ? rowSpacing : 0)
        }
        return CGSize(width: w, height: h)
    }

    func placeSubviews(in bounds: CGRect, proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) {
        let limit = min(bounds.width > 0 ? bounds.width : maxWidth, maxWidth)
        var y = bounds.minY
        for r in rows(subviews, limit: limit) {
            var x = bounds.minX
            let rowH = r.map { $0.1.height }.max() ?? 0
            for (i, size) in r {
                // căn đáy để chữ Hán và chữ thường thẳng hàng
                subviews[i].place(at: CGPoint(x: x, y: y + rowH - size.height), proposal: ProposedViewSize(size))
                x += size.width
            }
            y += rowH + rowSpacing
        }
    }
}

/// Giao diện "pinyin nằm trên chữ Hán" (kiểu ruby).
struct RubyView: View {
    let lines: [[PyToken]]
    let theme: PopupTheme
    let hanziSize: CGFloat
    let toneColors: Bool
    var maxWidth: CGFloat = 530
    /// Chữ đang rê chuột / vừa bấm (để hiện cách đọc khác).
    var onFocus: ((PyToken?) -> Void)? = nil
    /// Nghĩa theo mã từ (để hiện chú thích khi rê chuột).
    var infos: [Int: WordInfo] = [:]
    var meaningLang: String = "vi"
    var showHanViet: Bool = true
    /// Mã từ đang được chọn → cả từ sáng lên.
    var highlightWord: Int? = nil

    var body: some View {
        VStack(alignment: .leading, spacing: 2) {
            ForEach(0..<lines.count, id: \.self) { li in
                if lines[li].isEmpty {
                    Color.clear.frame(width: 1, height: hanziSize * 0.5)
                } else {
                    FlowLayout(maxWidth: maxWidth) {
                        ForEach(lines[li]) { t in
                            RubyCell(token: t, theme: theme, hanziSize: hanziSize, toneColors: toneColors, onFocus: onFocus,
                                     info: infos[t.wordId], meaningLang: meaningLang, showHanViet: showHanViet,
                                     highlighted: highlightWord != nil && highlightWord == t.wordId && t.isHan)
                        }
                    }
                }
            }
        }
    }
}

struct RubyCell: View {
    let token: PyToken
    let theme: PopupTheme
    let hanziSize: CGFloat
    let toneColors: Bool
    var onFocus: ((PyToken?) -> Void)?
    var info: WordInfo? = nil
    var meaningLang: String = "vi"
    var showHanViet: Bool = true
    var highlighted: Bool = false

    @State private var hover = false

    private var pySize: CGFloat { max(11, (hanziSize * 0.5).rounded()) }

    private var pinyinColor: Color {
        if !token.isHan { return Color(hex: theme.other) }
        if token.pinyin == nil { return Color(hex: theme.missing) }
        if toneColors && (1...5).contains(token.tone) { return theme.toneColor(token.tone) }
        return Color(hex: theme.pinyinDefault)
    }

    private var tooltip: String {
        guard token.isHan else { return "" }
        if let w = info, w.hasMeaning || w.hanViet != nil {
            var s = "\(w.text)  \(w.pinyin)"
            if showHanViet, let hv = w.hanViet { s += "  ·  " + hv.uppercased() }
            let m = DictionaryService.meaningText(w, lang: meaningLang)
            if !m.isEmpty { s += "\n" + m }
            if token.alternatives.count > 1 { s += "\n\(token.text): " + token.alternatives.joined(separator: " / ") }
            return s
        }
        if token.alternatives.count > 1 { return "\(token.text)  ·  \(token.alternatives.joined(separator: " / "))" }
        if let p = token.pinyin { return "\(token.text)  ·  \(p)" }
        return ""
    }

    var body: some View {
        VStack(spacing: 0) {
            Text(token.isHan ? (token.pinyin ?? "?") : " ")
                .font(.system(size: pySize))
                .underline(token.alternatives.count > 1, pattern: .dot, color: pinyinColor)
                .foregroundColor(pinyinColor)
                .lineLimit(1)
                .fixedSize()
            Text(token.text)
                .font(.custom("PingFang SC", size: token.isHan ? hanziSize : hanziSize * 0.72))
                .foregroundColor(token.isHan ? Color(hex: theme.hanzi) : Color(hex: theme.other))
                .lineLimit(1)
                .fixedSize()
                .frame(height: hanziSize * 1.35, alignment: .bottom)
        }
        .padding(.horizontal, token.isHan ? 3 : 1)
        .padding(.top, 2)
        .background(
            RoundedRectangle(cornerRadius: 5, style: .continuous)
                .fill((hover || highlighted) && token.isHan ? Color(hex: theme.cellHover) : Color.clear)
        )
        .padding(.bottom, 4)
        .contentShape(Rectangle())
        .help(tooltip)
        .onHover { h in
            guard token.isHan else { return }
            hover = h
            onFocus?(h ? token : nil)
        }
        .onTapGesture {
            if token.isHan { onFocus?(token) }
        }
    }
}

/// Danh sách từ vựng: chữ Hán + pinyin + âm Hán Việt | nghĩa.
struct VocabListView: View {
    let words: [WordInfo]
    let theme: PopupTheme
    var meaningLang: String = "vi"
    var showHanViet: Bool = true
    var width: CGFloat? = 510

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            ForEach(Array(words.enumerated()), id: \.offset) { i, w in
                HStack(alignment: .top, spacing: 14) {
                    VStack(alignment: .leading, spacing: 1) {
                        Text(w.text)
                            .font(.custom("PingFang SC", size: 17))
                            .foregroundColor(Color(hex: theme.hanzi))
                        Text(w.pinyin)
                            .font(.system(size: 12))
                            .foregroundColor(Color(hex: theme.pinyinDefault))
                        if showHanViet, let hv = w.hanViet {
                            Text(hv.uppercased())
                                .font(.system(size: 10.5, weight: .medium))
                                .foregroundColor(Color(hex: theme.sub))
                        }
                    }
                    .frame(minWidth: 86, maxWidth: 170, alignment: .leading)
                    .fixedSize(horizontal: true, vertical: false)

                    let m = DictionaryService.meaningText(w, lang: meaningLang)
                    Text(m.isEmpty ? "—" : m)
                        .font(.system(size: 12.5))
                        .foregroundColor(Color(hex: m.isEmpty ? theme.sub : theme.fg))
                        .fixedSize(horizontal: false, vertical: true)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .padding(.top, 2)
                }
                .padding(.vertical, 6)
                if i < words.count - 1 {
                    Rectangle().fill(Color(hex: theme.divider)).frame(height: 1)
                }
            }
        }
        .frame(width: width, alignment: .leading)
    }
}
