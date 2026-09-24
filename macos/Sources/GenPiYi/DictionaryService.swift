import Foundation

/// Một mục trong từ điển (một cách đọc của một từ).
struct DictEntry {
    let simplified: String
    let traditional: String
    /// Pinyin dạng số như trong CC-CEDICT, vd "yin2 hang2".
    let pinyin: String
    let vi: String
    let en: String
}

/// Thông tin một từ để hiển thị: pinyin, âm Hán Việt, nghĩa.
struct WordInfo: Identifiable, Hashable {
    var id: String { text }
    let text: String
    let pinyin: String
    let hanViet: String?
    let vi: String?
    let en: String?
    var hasMeaning: Bool { !(vi ?? "").isEmpty || !(en ?? "").isEmpty }
}

/// Từ điển offline (CVDICT Trung–Việt, CC-CEDICT Trung–Anh, âm Hán Việt từ Unihan).
/// Dữ liệu tạo bằng tools/build_dict.py → data/genpiyi-dict.tsv.deflate (được chép vào GenPiYi.app/Contents/Resources).
enum DictionaryService {
    static let resourceName = "genpiyi-dict.tsv"
    static let resourceExt = "deflate"
    static let maxWordLength = 8

    private static let lock = NSLock()
    private static var loaded = false
    private static var words: [String: [DictEntry]] = [:]
    private static var hanVietMap: [Character: [String]] = [:]
    private(set) static var entryCount = 0

    static var available: Bool { ensureLoaded(); return !words.isEmpty }

    static func warmup() { ensureLoaded() }

    private static func ensureLoaded() {
        lock.lock()
        defer { lock.unlock() }
        guard !loaded else { return }
        load()
        loaded = true
    }

    private static func dataURL() -> URL? {
        if let u = Bundle.main.url(forResource: resourceName, withExtension: resourceExt) { return u }
        // Khi chạy bằng `swift run`: lấy thẳng từ thư mục data/ của repo
        let repo = URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent().deletingLastPathComponent()
            .deletingLastPathComponent().deletingLastPathComponent()
        let dev = repo.appendingPathComponent("data/\(resourceName).\(resourceExt)")
        return FileManager.default.fileExists(atPath: dev.path) ? dev : nil
    }

    private static func load() {
        guard let url = dataURL(), let raw = try? Data(contentsOf: url) else { return }
        guard let inflated = try? (raw as NSData).decompressed(using: .zlib) as Data,
              let text = String(data: inflated, encoding: .utf8) else {
            AppLog.write("dictionary: cannot decompress \(url.path)")
            return
        }
        var w: [String: [DictEntry]] = [:]
        w.reserveCapacity(140_000)
        var hv: [Character: [String]] = [:]
        var count = 0

        text.enumerateLines { line, _ in
            guard line.count > 2, !line.hasPrefix("#") else { return }
            let f = line.split(separator: "\t", omittingEmptySubsequences: false)
            if f[0] == "H", f.count >= 3, let ch = f[1].first {
                hv[ch] = f[2].split(separator: ",").map(String.init)
            } else if f[0] == "W", f.count >= 6 {
                let e = DictEntry(simplified: String(f[1]), traditional: String(f[2]), pinyin: String(f[3]),
                                  vi: String(f[4]), en: String(f[5]))
                w[e.simplified, default: []].append(e)
                if e.traditional != e.simplified { w[e.traditional, default: []].append(e) }
                count += 1
            }
        }
        words = w
        hanVietMap = hv
        entryCount = count
    }

    static func lookup(_ word: String) -> [DictEntry] {
        ensureLoaded()
        return words[word] ?? []
    }

    /// Âm Hán Việt của cả từ (mỗi chữ lấy cách đọc đầu tiên), vd 银行 → "ngân hàng".
    static func hanViet(_ word: String) -> String? {
        ensureLoaded()
        var parts: [String] = []
        for c in word {
            guard let r = hanVietMap[c]?.first else { return nil }
            parts.append(r)
        }
        return parts.isEmpty ? nil : parts.joined(separator: " ")
    }

    /// Tách cụm chữ Hán thành từ (so khớp dài nhất từ trái sang). Trả về độ dài từng từ.
    static func segment(_ chars: [String]) -> [Int] {
        ensureLoaded()
        var out: [Int] = []
        var i = 0
        while i < chars.count {
            var best = 1
            if !words.isEmpty {
                let maxLen = min(maxWordLength, chars.count - i)
                if maxLen >= 2 {
                    for len in stride(from: maxLen, through: 2, by: -1) where words[chars[i..<(i + len)].joined()] != nil {
                        best = len
                        break
                    }
                }
            }
            out.append(best)
            i += best
        }
        return out
    }

    /// "yin2 hang2" / ["yín","háng"] → "yin2hang2"
    static func pinyinKey<S: Sequence>(_ syllables: S) -> String where S.Element == String {
        syllables.map { s -> String in let (b, t) = PinyinService.normalize(s); return b + String(t) }.joined()
    }

    /// Thông tin của một từ, chọn cách đọc khớp với pinyin đang hiển thị.
    static func describe(_ word: String, tokens: [PyToken]) -> WordInfo {
        let entries = lookup(word)
        let ours = tokens.map { t in t.pinyin == nil ? "?" : (t.pinyinBase ?? "") + String(t.tone) }.joined()
        let pick = entries.first { pinyinKey($0.pinyin.split(separator: " ").map(String.init)) == ours }
            ?? entries.first { !($0.pinyin.first?.isUppercase ?? false) }
            ?? entries.first
        var vi = pick?.vi ?? ""
        if let p = pick, vi.isEmpty {
            vi = entries.first { $0.pinyin.caseInsensitiveCompare(p.pinyin) == .orderedSame && !$0.vi.isEmpty }?.vi ?? ""
        }
        let en = pick?.en ?? ""
        return WordInfo(text: word,
                        pinyin: tokens.map { $0.pinyin ?? "?" }.joined(),
                        hanViet: hanViet(word),
                        vi: vi.isEmpty ? nil : vi,
                        en: en.isEmpty ? nil : en)
    }

    /// Thông tin theo mã từ cho cả đoạn văn.
    static func wordInfos(_ lines: [[PyToken]]) -> [Int: WordInfo] {
        guard available else { return [:] }
        var out: [Int: WordInfo] = [:]
        for line in lines {
            var groups: [Int: [PyToken]] = [:]
            var order: [Int] = []
            for t in line where t.isHan && t.wordId >= 0 {
                if groups[t.wordId] == nil { order.append(t.wordId) }
                groups[t.wordId, default: []].append(t)
            }
            for id in order {
                let toks = groups[id]!
                out[id] = describe(toks.map(\.text).joined(), tokens: toks)
            }
        }
        return out
    }

    /// Danh sách từ (không trùng) có nghĩa hoặc âm Hán Việt, theo thứ tự xuất hiện.
    static func vocabulary(_ lines: [[PyToken]], infos: [Int: WordInfo]? = nil, max: Int = 40) -> [WordInfo] {
        let infos = infos ?? wordInfos(lines)
        var seen = Set<String>()
        var list: [WordInfo] = []
        for line in lines {
            var ids: [Int] = []
            for t in line where t.isHan && t.wordId >= 0 && ids.last != t.wordId { ids.append(t.wordId) }
            for id in ids {
                guard let w = infos[id], seen.insert(w.text).inserted, w.hasMeaning || w.hanViet != nil else { continue }
                list.append(w)
                if list.count >= max { return list }
            }
        }
        return list
    }

    /// Nghĩa hiển thị theo lựa chọn "vi" / "en" / "both" (tự lùi về ngôn ngữ còn lại nếu thiếu).
    static func meaningText(_ w: WordInfo, lang: String, maxSenses: Int = 4) -> String {
        func trim(_ s: String?) -> String {
            guard let s, !s.isEmpty else { return "" }
            let senses = s.components(separatedBy: " ; ").filter { !$0.isEmpty }
            let t = senses.prefix(maxSenses).joined(separator: "; ")
            return senses.count > maxSenses ? t + "; …" : t
        }
        let vi = trim(w.vi), en = trim(w.en)
        switch lang {
        case "en": return en.isEmpty ? vi : en
        case "both":
            if !vi.isEmpty && !en.isEmpty { return vi + "\n" + en }
            return vi.isEmpty ? en : vi
        default: return vi.isEmpty ? en : vi
        }
    }
}
