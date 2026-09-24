import Foundation

/// Một ô trong popup: chữ Hán kèm pinyin, hoặc một đoạn chữ khác (Latin, số, dấu câu).
struct PyToken: Identifiable, Hashable {
    let id: Int
    let text: String
    let isHan: Bool
    /// Pinyin đã định dạng theo kiểu người dùng chọn (nil nếu không phải chữ Hán / không tra được).
    let pinyin: String?
    /// 1–4, 5 = thanh nhẹ, 0 = không phải chữ Hán.
    let tone: Int
    /// Các cách đọc khác của chữ (chữ đa âm).
    let alternatives: [String]
}

/// Chuyển Hán tự sang pinyin bằng bộ chuyển đổi có sẵn của macOS (CFStringTransform, 100% offline).
/// Bộ này đọc theo cụm từ nên xử lý được phần lớn chữ đa âm (银行 yínháng / 行走 xíngzǒu).
enum PinyinService {
    private static let lock = NSLock()
    private static var charCache: [Character: String] = [:]

    static func warmup() {
        _ = convert("中文拼音", style: "mark")
    }

    // MARK: - Nhận diện chữ Hán

    static func isHan(_ c: Character) -> Bool {
        guard c.unicodeScalars.count == 1, let v = c.unicodeScalars.first?.value else { return false }
        return (0x4E00...0x9FFF).contains(v)
            || (0x3400...0x4DBF).contains(v)
            || (0xF900...0xFAFF).contains(v)
            || (0x20000...0x2A6DF).contains(v)
    }

    static func containsHan(_ text: String?) -> Bool {
        guard let text, !text.isEmpty else { return false }
        return text.contains(where: isHan)
    }

    // MARK: - Tra pinyin thô

    private static func transform(_ s: String) -> String? {
        let ms = NSMutableString(string: s)
        guard CFStringTransform(ms, nil, kCFStringTransformMandarinLatin, false) else { return nil }
        return ms as String
    }

    private static func syllables(of s: String) -> [String] {
        s.replacingOccurrences(of: "'", with: " ")
            .split(whereSeparator: { $0 == " " || $0 == "\u{00A0}" })
            .map(String.init)
    }

    private static func rawForChar(_ c: Character) -> String? {
        lock.lock()
        if let v = charCache[c] { lock.unlock(); return v }
        lock.unlock()
        guard let out = transform(String(c)), let first = syllables(of: out).first,
              !containsHan(first) else { return nil }
        lock.lock(); charCache[c] = first; lock.unlock()
        return first
    }

    /// Pinyin thô cho một cụm chữ Hán liền nhau; mỗi phần tử ứng với một chữ.
    private static func rawForRun(_ run: [Character]) -> [String?] {
        if let out = transform(String(run)) {
            let parts = syllables(of: out)
            if parts.count == run.count, !parts.contains(where: { containsHan($0) }) {
                return parts
            }
        }
        // Fallback: tra từng chữ (mất ngữ cảnh đa âm nhưng vẫn đúng đa số)
        return run.map { rawForChar($0) }
    }

    // MARK: - Chuyển đổi

    /// Chuyển văn bản thành danh sách dòng, mỗi dòng là danh sách token.
    static func convert(_ input: String, style: String) -> [[PyToken]] {
        let text = input.replacingOccurrences(of: "\r\n", with: "\n").replacingOccurrences(of: "\r", with: "\n")
        let chars = Array(text)

        // Pinyin thô cho từng vị trí (theo cụm chữ Hán liền nhau để giữ ngữ cảnh)
        var raw = [String?](repeating: nil, count: chars.count)
        var i = 0
        while i < chars.count {
            if isHan(chars[i]) {
                var j = i
                while j < chars.count && isHan(chars[j]) { j += 1 }
                let r = rawForRun(Array(chars[i..<j]))
                for k in 0..<r.count { raw[i + k] = r[k] }
                i = j
            } else {
                i += 1
            }
        }

        var lines: [[PyToken]] = []
        var line: [PyToken] = []
        var buffer = ""
        var nextId = 0

        func flush() {
            guard !buffer.isEmpty else { return }
            line.append(PyToken(id: nextId, text: buffer, isHan: false, pinyin: nil, tone: 0, alternatives: []))
            nextId += 1
            buffer = ""
        }

        for (idx, c) in chars.enumerated() {
            if c == "\n" {
                flush()
                lines.append(line)
                line = []
                continue
            }
            if isHan(c) {
                flush()
                var py: String?
                var tone = 0
                if let r = raw[idx], !r.trimmingCharacters(in: .whitespaces).isEmpty {
                    let (b, t) = normalize(r)
                    if !b.isEmpty { py = formatSyllable(b, tone: t, style: style); tone = t }
                }
                line.append(PyToken(id: nextId, text: String(c), isHan: true, pinyin: py, tone: tone,
                                    alternatives: alternatives(for: c, current: py, style: style)))
                nextId += 1
            } else if c.isLetter || c.isNumber {
                buffer.append(c) // gom chữ Latin/số thành 1 từ
            } else {
                flush()
                line.append(PyToken(id: nextId, text: String(c), isHan: false, pinyin: nil, tone: 0, alternatives: []))
                nextId += 1
            }
        }
        flush()
        lines.append(line)
        return lines
    }

    private static func alternatives(for c: Character, current: String?, style: String) -> [String] {
        guard let list = Polyphones.table[c] else { return [] }
        var out = list.map { r -> String in
            let (b, t) = normalize(r)
            return formatSyllable(b, tone: t, style: style)
        }
        // Đưa cách đọc đang dùng lên đầu
        if let cur = current, let k = out.firstIndex(of: cur) { out.remove(at: k); out.insert(cur, at: 0) }
        else if let cur = current { out.insert(cur, at: 0) }
        var seen = Set<String>()
        return out.filter { seen.insert($0).inserted }
    }

    private static let punctMap: [Character: Character] = [
        "，": ",", "。": ".", "！": "!", "？": "?", "：": ":", "；": ";", "、": ",",
        "（": "(", "）": ")", "“": "\"", "”": "\"", "‘": "'", "’": "'",
        "《": "<", "》": ">", "～": "~", "　": " ",
    ]

    /// Chuỗi pinyin thuần để copy, vd: "nǐ hǎo, jīntiān qù nǎlǐ?"
    static func toPlainPinyin(_ lines: [[PyToken]]) -> String {
        var result: [String] = []
        for line in lines {
            var s = ""
            var prevWord = false
            for t in line {
                let isWord = t.isHan || (t.text.first.map { $0.isLetter || $0.isNumber } ?? false)
                if isWord {
                    let word = t.isHan ? (t.pinyin ?? t.text) : t.text
                    if prevWord || (s.last.map { ",.!?:;)".contains($0) } ?? false) { s.append(" ") }
                    s += word
                    prevWord = true
                } else {
                    for ch in t.text {
                        let m = punctMap[ch] ?? ch
                        if m == " " && s.last == " " { continue }
                        s.append(m)
                    }
                    prevWord = false
                }
            }
            result.append(s.trimmingCharacters(in: .whitespaces))
        }
        return result.joined(separator: "\n")
    }

    // MARK: - Chuẩn hoá & định dạng thanh điệu

    private static let toneVowels: [(Character, [Character])] = [
        ("a", ["ā", "á", "ǎ", "à"]), ("e", ["ē", "é", "ě", "è"]), ("i", ["ī", "í", "ǐ", "ì"]),
        ("o", ["ō", "ó", "ǒ", "ò"]), ("u", ["ū", "ú", "ǔ", "ù"]), ("ü", ["ǖ", "ǘ", "ǚ", "ǜ"]),
    ]

    /// Tách âm tiết thô ("Zhōng", "zhong1", "lv4") thành (gốc không dấu, thanh 1–5).
    static func normalize(_ raw: String) -> (String, Int) {
        var s = raw.trimmingCharacters(in: .whitespaces).precomposedStringWithCanonicalMapping.lowercased()
        var tone = 0
        if s.count > 1, let last = s.last, let d = last.wholeNumberValue {
            tone = d == 0 ? 5 : d
            s.removeLast()
        }
        var out = ""
        for ch in s {
            var found = false
            for (b, marks) in toneVowels {
                if let idx = marks.firstIndex(of: ch) {
                    out.append(b)
                    if tone == 0 { tone = idx + 1 }
                    found = true
                    break
                }
            }
            if found { continue }
            switch ch {
            case "ń": out.append("n"); if tone == 0 { tone = 2 }
            case "ň": out.append("n"); if tone == 0 { tone = 3 }
            case "ǹ": out.append("n"); if tone == 0 { tone = 4 }
            case "ḿ": out.append("m"); if tone == 0 { tone = 2 }
            case "v": out.append("ü")
            default: if ch.isLetter { out.append(ch) }
            }
        }
        if tone < 1 || tone > 5 { tone = 5 }
        return (out, tone)
    }

    static func formatSyllable(_ b: String, tone: Int, style: String) -> String {
        if b.isEmpty { return "" }
        if style == "number" { return b + ((1...5).contains(tone) ? String(tone) : "") }
        return addToneMark(b, tone: tone)
    }

    /// Đặt dấu thanh: a/e trước, rồi "ou" → o, còn lại nguyên âm cuối.
    static func addToneMark(_ b: String, tone: Int) -> String {
        guard (1...4).contains(tone) else { return b }
        var chars = Array(b)
        var idx = chars.firstIndex(of: "a") ?? chars.firstIndex(of: "e")
        if idx == nil && b.contains("ou") { idx = chars.firstIndex(of: "o") }
        if idx == nil { idx = chars.lastIndex(where: { "aeiouü".contains($0) }) }
        guard let i = idx else { return b }
        for (v, marks) in toneVowels where v == chars[i] {
            chars[i] = marks[tone - 1]
            return String(chars)
        }
        return b
    }
}

/// Các chữ đa âm thường gặp (dùng để hiện "cách đọc khác" khi rê chuột).
enum Polyphones {
    static let table: [Character: [String]] = {
        let src: [(Character, String)] = [
            ("行", "xing2 hang2"), ("长", "chang2 zhang3"), ("重", "zhong4 chong2"), ("还", "hai2 huan2"),
            ("好", "hao3 hao4"), ("了", "le5 liao3"), ("得", "de5 de2 dei3"), ("着", "zhe5 zhao2 zhuo2"),
            ("地", "de5 di4"), ("的", "de5 di2 di4"), ("都", "dou1 du1"), ("为", "wei4 wei2"),
            ("觉", "jue2 jiao4"), ("乐", "le4 yue4"), ("还", "hai2 huan2"), ("和", "he2 he4 huo2 huo4 hu2"),
            ("便", "bian4 pian2"), ("差", "cha4 cha1 chai1 ci1"), ("传", "chuan2 zhuan4"), ("调", "diao4 tiao2"),
            ("发", "fa1 fa4"), ("分", "fen1 fen4"), ("干", "gan4 gan1"), ("给", "gei3 ji3"),
            ("种", "zhong3 zhong4"), ("中", "zhong1 zhong4"), ("只", "zhi3 zhi1"), ("数", "shu4 shu3"),
            ("少", "shao3 shao4"), ("大", "da4 dai4"), ("看", "kan4 kan1"), ("空", "kong1 kong4"),
            ("量", "liang4 liang2"), ("没", "mei2 mo4"), ("难", "nan2 nan4"), ("便", "bian4 pian2"),
            ("朝", "chao2 zhao1"), ("处", "chu4 chu3"), ("当", "dang1 dang4"), ("倒", "dao3 dao4"),
            ("度", "du4 duo2"), ("更", "geng4 geng1"), ("假", "jia3 jia4"), ("间", "jian1 jian4"),
            ("将", "jiang1 jiang4"), ("教", "jiao4 jiao1"), ("结", "jie2 jie1"), ("解", "jie3 jie4 xie4"),
            ("累", "lei4 lei3 lei2"), ("落", "luo4 la4 lao4"), ("强", "qiang2 qiang3 jiang4"), ("曲", "qu3 qu1"),
            ("上", "shang4 shang3"), ("省", "sheng3 xing3"), ("似", "si4 shi4"), ("相", "xiang1 xiang4"),
            ("血", "xue4 xie3"), ("要", "yao4 yao1"), ("应", "ying1 ying4"), ("与", "yu3 yu4"),
            ("载", "zai4 zai3"), ("正", "zheng4 zheng1"), ("转", "zhuan3 zhuan4"), ("仔", "zi3 zai3"),
            ("作", "zuo4 zuo1"), ("曾", "ceng2 zeng1"), ("藏", "cang2 zang4"), ("参", "can1 shen1 cen1"),
            ("称", "cheng1 chen4"), ("冲", "chong1 chong4"), ("答", "da2 da1"), ("担", "dan1 dan4"),
            ("弹", "tan2 dan4"), ("恶", "e4 wu4 e3"), ("供", "gong1 gong4"), ("还", "hai2 huan2"),
            ("号", "hao4 hao2"), ("喝", "he1 he4"), ("华", "hua2 hua4"), ("几", "ji3 ji1"),
            ("见", "jian4 xian4"), ("卷", "juan3 juan4"), ("看", "kan4 kan1"), ("率", "lv4 shuai4"),
            ("绿", "lv4 lu4"), ("模", "mo2 mu2"), ("宁", "ning2 ning4"), ("便", "bian4 pian2"),
            ("期", "qi1 ji1"), ("奇", "qi2 ji1"), ("塞", "sai1 sai4 se4"), ("系", "xi4 ji4"),
            ("兴", "xing4 xing1"), ("悄", "qiao1 qiao3"), ("剥", "bo1 bao1"), ("薄", "bao2 bo2 bo4"),
            ("背", "bei4 bei1"), ("尽", "jin4 jin3"), ("吗", "ma5 ma2"), ("呢", "ne5 ni2"),
            ("哪", "na3 na5 ne2"), ("那", "na4 nei4"), ("什", "shen2 shi2"), ("么", "me5 mo2"),
            ("识", "shi2 zhi4"), ("散", "san4 san3"), ("舍", "she3 she4"), ("盛", "sheng4 cheng2"),
            ("说", "shuo1 shui4"), ("听", "ting1 ting4"), ("吐", "tu3 tu4"), ("尾", "wei3 yi3"),
            ("鲜", "xian1 xian3"), ("吓", "xia4 he4"), ("压", "ya1 ya4"), ("咽", "yan4 yan1 ye4"),
            ("钥", "yao4 yue4"), ("一", "yi1 yi2 yi4"), ("不", "bu4 bu2"), ("占", "zhan4 zhan1"),
            ("露", "lu4 lou4"), ("剂", "ji4 ji1"),
        ]
        var d: [Character: [String]] = [:]
        for (c, s) in src where d[c] == nil {
            d[c] = s.split(separator: " ").map(String.init)
        }
        return d
    }()
}
