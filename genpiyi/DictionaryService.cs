using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace genpiyi
{
    /// <summary>Một mục trong từ điển (một cách đọc của một từ).</summary>
    public sealed class DictEntry
    {
        public string Simplified { get; init; } = "";
        public string Traditional { get; init; } = "";
        /// <summary>Pinyin dạng số như trong CC-CEDICT, vd "yin2 hang2".</summary>
        public string Pinyin { get; init; } = "";
        public string Vi { get; init; } = "";
        public string En { get; init; } = "";
    }

    /// <summary>Thông tin một từ để hiển thị: pinyin, âm Hán Việt, nghĩa.</summary>
    public sealed class WordInfo
    {
        public string Text { get; init; } = "";
        public string Pinyin { get; init; } = "";
        /// <summary>Âm Hán Việt, vd "ngân hàng" (null nếu không có).</summary>
        public string? HanViet { get; init; }
        public string? Vi { get; init; }
        public string? En { get; init; }
        public bool HasMeaning => !string.IsNullOrEmpty(Vi) || !string.IsNullOrEmpty(En);
    }

    /// <summary>
    /// Từ điển offline (CVDICT Trung–Việt, CC-CEDICT Trung–Anh, âm Hán Việt từ Unihan).
    /// Dữ liệu tạo bằng tools/build_dict.py, nhúng vào app dưới dạng tài nguyên "genpiyi-dict.tsv.deflate".
    /// </summary>
    public static class DictionaryService
    {
        public const string ResourceName = "genpiyi-dict.tsv.deflate";
        public const int MaxWordLength = 8;

        private static readonly object Lock = new();
        private static bool _loaded;
        private static Dictionary<string, List<DictEntry>> _words = new();
        private static Dictionary<string, string[]> _hanViet = new();

        /// <summary>Có dữ liệu từ điển trong bản build này không.</summary>
        public static bool Available { get { EnsureLoaded(); return _words.Count > 0; } }
        public static string Attribution { get; private set; } = "";
        public static int EntryCount { get; private set; }

        public static void Warmup()
        {
            try { EnsureLoaded(); } catch (Exception ex) { App.Log(ex); }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (Lock)
            {
                if (_loaded) return;
                try { Load(); }
                catch (Exception ex) { App.Log(ex); }
                _loaded = true;
            }
        }

        private static Stream? OpenData()
        {
            var asm = Assembly.GetExecutingAssembly();
            var s = asm.GetManifestResourceStream(ResourceName);
            if (s != null) return s;
            // Dự phòng: file đặt cạnh GenPiYi.exe
            var path = Path.Combine(AppContext.BaseDirectory, ResourceName);
            return File.Exists(path) ? File.OpenRead(path) : null;
        }

        private static void Load()
        {
            using var raw = OpenData();
            if (raw == null) return;
            using var inflate = new DeflateStream(raw, CompressionMode.Decompress);
            using var reader = new StreamReader(inflate, Encoding.UTF8);

            var words = new Dictionary<string, List<DictEntry>>(140_000, StringComparer.Ordinal);
            var hv = new Dictionary<string, string[]>(20_000, StringComparer.Ordinal);
            int count = 0;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length < 3) continue;
                if (line[0] == '#')
                {
                    var p = line.Split('\t');
                    if (p.Length >= 4) Attribution = p[3];
                    continue;
                }
                var f = line.Split('\t');
                if (f[0] == "H" && f.Length >= 3)
                {
                    hv[f[1]] = f[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
                }
                else if (f[0] == "W" && f.Length >= 6)
                {
                    var e = new DictEntry { Simplified = f[1], Traditional = f[2], Pinyin = f[3], Vi = f[4], En = f[5] };
                    Add(words, e.Simplified, e);
                    if (e.Traditional != e.Simplified) Add(words, e.Traditional, e);
                    count++;
                }
            }
            _hanViet = hv;
            _words = words;
            EntryCount = count;

            static void Add(Dictionary<string, List<DictEntry>> d, string key, DictEntry e)
            {
                if (!d.TryGetValue(key, out var list)) d[key] = list = new List<DictEntry>(1);
                list.Add(e);
            }
        }

        public static IReadOnlyList<DictEntry> Lookup(string word)
        {
            EnsureLoaded();
            return _words.TryGetValue(word, out var list) ? list : Array.Empty<DictEntry>();
        }

        public static bool Contains(string word)
        {
            EnsureLoaded();
            return _words.ContainsKey(word);
        }

        /// <summary>Âm Hán Việt của cả từ (mỗi chữ lấy cách đọc đầu tiên), vd 银行 → "ngân hàng".</summary>
        public static string? HanViet(string word)
        {
            EnsureLoaded();
            if (_hanViet.Count == 0) return null;
            var parts = new List<string>();
            var e = StringInfo.GetTextElementEnumerator(word);
            while (e.MoveNext())
            {
                var ch = (string)e.Current;
                if (!_hanViet.TryGetValue(ch, out var r) || r.Length == 0) return null;
                parts.Add(r[0]);
            }
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        /// <summary>
        /// Tách một cụm chữ Hán liền nhau thành từ bằng so khớp dài nhất hai chiều
        /// (xuôi và ngược, chọn cách tách ít từ hơn / ít chữ lẻ hơn; hoà thì lấy chiều ngược).
        /// Vd 研究生命 → 研究|生命 (không phải 研究生|命). Trả về độ dài (số chữ) của từng từ.
        /// </summary>
        public static List<int> Segment(IReadOnlyList<string> chars)
        {
            EnsureLoaded();
            if (_words.Count == 0 || chars.Count < 2) return Enumerable.Repeat(1, chars.Count).ToList();

            var fw = new List<int>();
            for (int i = 0; i < chars.Count;)
            {
                int best = 1;
                for (int len = Math.Min(MaxWordLength, chars.Count - i); len >= 2; len--)
                    if (_words.ContainsKey(Join(chars, i, len))) { best = len; break; }
                fw.Add(best);
                i += best;
            }

            var bw = new List<int>();
            for (int j = chars.Count; j > 0;)
            {
                int best = 1;
                for (int len = Math.Min(MaxWordLength, j); len >= 2; len--)
                    if (_words.ContainsKey(Join(chars, j - len, len))) { best = len; break; }
                bw.Insert(0, best);
                j -= best;
            }

            if (fw.Count != bw.Count) return fw.Count < bw.Count ? fw : bw;
            int sf = fw.Count(x => x == 1), sb = bw.Count(x => x == 1);
            return sf < sb ? fw : bw;

            static string Join(IReadOnlyList<string> c, int start, int len)
            {
                var sb2 = new StringBuilder();
                for (int k = start; k < start + len; k++) sb2.Append(c[k]);
                return sb2.ToString();
            }
        }

        /// <summary>Khoá so sánh pinyin: "yin2 hang2" / "yínháng" → "yin2hang2".</summary>
        public static string PinyinKey(IEnumerable<string> syllables) =>
            string.Concat(syllables.Select(s => { var (b, t) = PinyinService.Normalize(s); return b + t; }));

        /// <summary>Thông tin của một từ, chọn cách đọc khớp với pinyin đang hiển thị (nếu có).</summary>
        public static WordInfo Describe(string word, IReadOnlyList<PyToken> tokens, string toneStyle)
        {
            var entries = Lookup(word);
            string ourKey = string.Concat(tokens.Select(t => t.Pinyin == null ? "?" : KeyOf(t)));
            DictEntry? pick = entries.FirstOrDefault(e =>
                                  PinyinKey(e.Pinyin.Split(' ', StringSplitOptions.RemoveEmptyEntries)) == ourKey)
                              ?? entries.FirstOrDefault(e => !char.IsUpper(e.Pinyin.FirstOrDefault()))
                              ?? entries.FirstOrDefault();

            // Gộp nghĩa của các mục cùng cách đọc (vd chữ phồn/giản cùng mặt chữ)
            string? vi = pick?.Vi, en = pick?.En;
            if (pick != null && string.IsNullOrEmpty(vi))
                vi = entries.FirstOrDefault(e => e.Pinyin.Equals(pick.Pinyin, StringComparison.OrdinalIgnoreCase) && e.Vi.Length > 0)?.Vi;

            return new WordInfo
            {
                Text = word,
                Pinyin = string.Concat(tokens.Select(t => t.Pinyin ?? "?")),
                HanViet = HanViet(word),
                Vi = string.IsNullOrEmpty(vi) ? null : vi,
                En = string.IsNullOrEmpty(en) ? null : en
            };

            static string KeyOf(PyToken t) => (t.PinyinBase ?? "") + t.Tone;
        }

        /// <summary>Danh sách từ (không trùng) trong đoạn văn, theo thứ tự xuất hiện.</summary>
        public static List<WordInfo> Vocabulary(List<List<PyToken>> lines, string toneStyle, int max = 40)
        {
            var list = new List<WordInfo>();
            var seen = new HashSet<string>();
            foreach (var line in lines)
            {
                foreach (var group in line.Where(t => t.IsHan && t.WordId >= 0).GroupBy(t => t.WordId))
                {
                    var tokens = group.ToList();
                    var word = string.Concat(tokens.Select(t => t.Text));
                    if (!seen.Add(word)) continue;
                    var info = Describe(word, tokens, toneStyle);
                    if (!info.HasMeaning && info.HanViet == null) continue;
                    list.Add(info);
                    if (list.Count >= max) return list;
                }
            }
            return list;
        }

        /// <summary>Nghĩa hiển thị theo lựa chọn "vi" / "en" / "both" (tự lùi về ngôn ngữ còn lại nếu thiếu).</summary>
        public static string MeaningText(WordInfo w, string lang, int maxSenses = 4)
        {
            string Trim(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                var senses = s.Split(" ; ", StringSplitOptions.RemoveEmptyEntries);
                var t = string.Join("; ", senses.Take(maxSenses));
                return senses.Length > maxSenses ? t + "; …" : t;
            }

            var vi = Trim(w.Vi);
            var en = Trim(w.En);
            switch (lang)
            {
                case "en": return en.Length > 0 ? en : vi;
                case "both":
                    if (vi.Length > 0 && en.Length > 0) return vi + "\n" + en;
                    return vi.Length > 0 ? vi : en;
                default: return vi.Length > 0 ? vi : en;
            }
        }
    }
}
