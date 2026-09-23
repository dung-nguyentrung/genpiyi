using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace genpiyi
{
    public sealed class PyToken
    {
        public string Text { get; init; } = "";
        public bool IsHan { get; init; }
        /// <summary>Pinyin đã định dạng theo kiểu người dùng chọn (null nếu không phải chữ Hán / không tra được).</summary>
        public string? Pinyin { get; init; }
        /// <summary>1–4, 5 = thanh nhẹ, 0 = không phải chữ Hán.</summary>
        public int Tone { get; init; }
        /// <summary>Các cách đọc khác của chữ (chữ đa âm).</summary>
        public string[] Alternatives { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Chuyển Hán tự sang pinyin bằng thư viện ToolGood.Words.Pinyin (offline).
    /// Gọi qua reflection để chịu được khác biệt chữ ký hàm giữa các phiên bản thư viện.
    /// </summary>
    public static class PinyinService
    {
        private static readonly object Lock = new();
        private static bool _initialized;
        private static MethodInfo? _getList;   // GetPinyinList(string, ...)
        private static MethodInfo? _getOne;    // GetPinyin(string, ...)
        private static MethodInfo? _getAll;    // GetAllPinyin(char, ...)
        private static readonly Dictionary<char, string[]> AltCache = new();

        public static string? LoadError { get; private set; }
        public static bool Available { get { EnsureInit(); return _getList != null || _getOne != null; } }

        public static void Warmup()
        {
            try { Convert("中文拼音", "mark"); } catch { /* ignore */ }
        }

        private static void EnsureInit()
        {
            if (_initialized) return;
            lock (Lock)
            {
                if (_initialized) return;
                try
                {
                    var asm = Assembly.Load("ToolGood.Words.Pinyin");
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).Cast<Type>().ToArray(); }

                    var helper = types
                        .Where(t => t.Name == "WordsHelper" && t.IsClass)
                        .OrderByDescending(t => (t.Namespace ?? "").Contains("Pinyin"))
                        .FirstOrDefault();

                    if (helper == null)
                    {
                        LoadError = Loc.T("engine.errNoClass");
                    }
                    else
                    {
                        var methods = helper.GetMethods(BindingFlags.Public | BindingFlags.Static);
                        _getList = PickMethod(methods, "GetPinyinList", typeof(string));
                        _getOne = PickMethod(methods, "GetPinyin", typeof(string));
                        _getAll = PickMethod(methods, "GetAllPinyin", typeof(char));
                        if (_getList == null && _getOne == null)
                            LoadError = Loc.T("engine.errNoMethod");
                    }
                }
                catch (Exception ex)
                {
                    LoadError = Loc.T("engine.errLoad") + ex.Message;
                }
                _initialized = true;
            }
        }

        private static MethodInfo? PickMethod(MethodInfo[] methods, string name, Type firstParam)
        {
            return methods
                .Where(m => m.Name == name)
                .Where(m => { var p = m.GetParameters(); return p.Length >= 1 && p[0].ParameterType == firstParam; })
                // ưu tiên overload có tham số thứ 2 kiểu bool/int (bật dấu thanh)
                .OrderByDescending(m =>
                {
                    var p = m.GetParameters();
                    return p.Length >= 2 && (p[1].ParameterType == typeof(bool) || p[1].ParameterType == typeof(int));
                })
                .ThenBy(m => m.GetParameters().Length)
                .FirstOrDefault();
        }

        private static object? Invoke(MethodInfo m, object first)
        {
            var ps = m.GetParameters();
            var args = new object?[ps.Length];
            args[0] = first;
            for (int i = 1; i < ps.Length; i++)
            {
                var t = ps[i].ParameterType;
                if (i == 1 && t == typeof(bool)) args[i] = true;        // tone = true
                else if (i == 1 && t == typeof(int)) args[i] = 1;       // tone = 1 (có dấu)
                else if (ps[i].HasDefaultValue) args[i] = ps[i].DefaultValue;
                else args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            return m.Invoke(null, args);
        }

        private static List<string> ToStringList(object? result)
        {
            var list = new List<string>();
            if (result is string s)
            {
                list.AddRange(s.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else if (result is IEnumerable e)
            {
                foreach (var o in e) list.Add(o?.ToString() ?? "");
            }
            return list;
        }

        // ------------------------------------------------------------------

        public static bool IsHan(char c) =>
            (c >= '一' && c <= '鿿') ||
            (c >= '㐀' && c <= '䶿') ||
            (c >= '豈' && c <= '﫿');

        public static bool ContainsHan(string? text) => !string.IsNullOrEmpty(text) && text.Any(IsHan);

        /// <summary>Trả về pinyin thô (theo thư viện) cho từng ký tự của chuỗi; null cho ký tự không phải Hán.</summary>
        private static string?[] GetRaw(string text)
        {
            var result = new string?[text.Length];
            EnsureInit();
            if (_getList == null && _getOne == null) return result;

            bool done = false;
            if (_getList != null)
            {
                try
                {
                    var list = ToStringList(Invoke(_getList, text));
                    if (list.Count == text.Length)
                    {
                        for (int i = 0; i < text.Length; i++)
                            if (IsHan(text[i])) result[i] = list[i];
                        done = true;
                    }
                }
                catch { /* fallback bên dưới */ }
            }

            if (!done)
            {
                // Fallback: tra từng chữ (mất ngữ cảnh đa âm nhưng vẫn đúng đa số)
                for (int i = 0; i < text.Length; i++)
                {
                    if (!IsHan(text[i])) continue;
                    try
                    {
                        if (_getList != null)
                            result[i] = ToStringList(Invoke(_getList, text[i].ToString())).FirstOrDefault();
                        else if (_getOne != null)
                            result[i] = Invoke(_getOne, text[i].ToString())?.ToString();
                    }
                    catch { /* ignore */ }
                }
            }
            return result;
        }

        private static string[] GetAlternatives(char c, string style)
        {
            if (_getAll == null) return Array.Empty<string>();
            lock (AltCache)
            {
                if (AltCache.TryGetValue(c, out var cached)) return Format(cached, style);
            }
            string[] raw;
            try
            {
                raw = ToStringList(Invoke(_getAll, c))
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0 && !ContainsHan(s))
                    .ToArray();
            }
            catch { raw = Array.Empty<string>(); }
            lock (AltCache) { AltCache[c] = raw; }
            return Format(raw, style);

            static string[] Format(string[] raws, string st) => raws
                .Select(r => { var (b, t) = Normalize(r); return FormatSyllable(b, t, st); })
                .Where(s => s.Length > 0)
                .Distinct()
                .ToArray();
        }

        /// <summary>Chuyển văn bản thành danh sách dòng, mỗi dòng là danh sách token (chữ Hán kèm pinyin, hoặc đoạn chữ khác).</summary>
        public static List<List<PyToken>> Convert(string text, string toneStyle)
        {
            text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            var raw = GetRaw(text);
            var lines = new List<List<PyToken>>();
            var line = new List<PyToken>();
            var buffer = new StringBuilder();

            void FlushBuffer()
            {
                if (buffer.Length == 0) return;
                line.Add(new PyToken { Text = buffer.ToString() });
                buffer.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    FlushBuffer();
                    lines.Add(line);
                    line = new List<PyToken>();
                    continue;
                }

                if (IsHan(c))
                {
                    FlushBuffer();
                    string? py = null;
                    int tone = 0;
                    var r = raw[i];
                    if (!string.IsNullOrWhiteSpace(r) && !ContainsHan(r))
                    {
                        var (b, t) = Normalize(r);
                        if (b.Length > 0)
                        {
                            py = FormatSyllable(b, t, toneStyle);
                            tone = t;
                        }
                    }
                    line.Add(new PyToken
                    {
                        Text = c.ToString(),
                        IsHan = true,
                        Pinyin = py,
                        Tone = tone,
                        Alternatives = GetAlternatives(c, toneStyle)
                    });
                }
                else if (char.IsLetterOrDigit(c))
                {
                    buffer.Append(c); // gom chữ Latin/số thành 1 từ
                }
                else
                {
                    FlushBuffer();
                    line.Add(new PyToken { Text = c.ToString() });
                }
            }
            FlushBuffer();
            lines.Add(line);
            return lines;
        }

        private static readonly Dictionary<char, char> PunctMap = new()
        {
            ['，'] = ',', ['。'] = '.', ['！'] = '!', ['？'] = '?', ['：'] = ':', ['；'] = ';',
            ['、'] = ',', ['（'] = '(', ['）'] = ')', ['“'] = '"', ['”'] = '"', ['‘'] = '\'', ['’'] = '\'',
            ['《'] = '<', ['》'] = '>', ['～'] = '~', ['　'] = ' '
        };

        /// <summary>Chuỗi pinyin thuần để copy, vd: "nǐ hǎo, jīntiān qù nǎlǐ?" (mỗi âm tiết cách nhau 1 dấu cách).</summary>
        public static string ToPlainPinyin(List<List<PyToken>> lines)
        {
            var sb = new StringBuilder();
            for (int li = 0; li < lines.Count; li++)
            {
                if (li > 0) sb.Append('\n');
                var lineSb = new StringBuilder();
                bool prevWord = false;
                foreach (var t in lines[li])
                {
                    if (t.IsHan || (t.Text.Length > 0 && char.IsLetterOrDigit(t.Text[0])))
                    {
                        var word = t.IsHan ? (t.Pinyin ?? t.Text) : t.Text;
                        if (prevWord || (lineSb.Length > 0 && ",.!?:;)".Contains(lineSb[^1]))) lineSb.Append(' ');
                        lineSb.Append(word);
                        prevWord = true;
                    }
                    else
                    {
                        foreach (var ch in t.Text)
                        {
                            var mapped = PunctMap.TryGetValue(ch, out var m) ? m : ch;
                            if (mapped == ' ' && lineSb.Length > 0 && lineSb[^1] == ' ') continue;
                            lineSb.Append(mapped);
                        }
                        prevWord = false;
                    }
                }
                sb.Append(lineSb.ToString().Trim());
            }
            return sb.ToString();
        }

        // ------------------- Chuẩn hoá & định dạng thanh điệu -------------------

        private static readonly (char Base, string Marks)[] ToneVowels =
        {
            ('a', "āáǎà"), ('e', "ēéěè"), ('i', "īíǐì"), ('o', "ōóǒò"), ('u', "ūúǔù"), ('ü', "ǖǘǚǜ")
        };

        /// <summary>Tách âm tiết thô (vd "Zhōng", "zhong1", "lv4") thành (gốc không dấu, thanh 1–5).</summary>
        public static (string Base, int Tone) Normalize(string raw)
        {
            var s = raw.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            int tone = 0;

            // Dạng số: zhong1 / lv4 / ma0 / ma5
            if (s.Length > 1 && char.IsDigit(s[^1]))
            {
                tone = s[^1] - '0';
                if (tone == 0) tone = 5;
                s = s[..^1];
            }

            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                bool found = false;
                foreach (var (b, marks) in ToneVowels)
                {
                    int idx = marks.IndexOf(ch);
                    if (idx >= 0) { sb.Append(b); if (tone == 0) tone = idx + 1; found = true; break; }
                }
                if (found) continue;
                switch (ch)
                {
                    case 'ń': sb.Append('n'); if (tone == 0) tone = 2; break;
                    case 'ň': sb.Append('n'); if (tone == 0) tone = 3; break;
                    case 'ǹ': sb.Append('n'); if (tone == 0) tone = 4; break;
                    case 'ḿ': sb.Append('m'); if (tone == 0) tone = 2; break;
                    case 'v': sb.Append('ü'); break;
                    default:
                        if (char.IsLetter(ch)) sb.Append(ch);
                        break;
                }
            }
            if (tone < 1 || tone > 5) tone = 5;
            return (sb.ToString(), tone);
        }

        public static string FormatSyllable(string b, int tone, string style)
        {
            if (b.Length == 0) return "";
            if (style == "number") return b + (tone >= 1 && tone <= 5 ? tone.ToString() : "");
            return AddToneMark(b, tone);
        }

        /// <summary>Đặt dấu thanh theo quy tắc: a/e trước, rồi "ou" → o, còn lại nguyên âm cuối.</summary>
        public static string AddToneMark(string b, int tone)
        {
            if (tone < 1 || tone > 4) return b;
            int idx = b.IndexOf('a');
            if (idx < 0) idx = b.IndexOf('e');
            if (idx < 0 && b.Contains("ou")) idx = b.IndexOf('o');
            if (idx < 0) idx = b.LastIndexOfAny(new[] { 'a', 'e', 'i', 'o', 'u', 'ü' });
            if (idx < 0) return b; // vd: "m", "ng", "hm"
            char v = b[idx];
            foreach (var (bv, marks) in ToneVowels)
            {
                if (bv == v) return b[..idx] + marks[tone - 1] + b[(idx + 1)..];
            }
            return b;
        }
    }
}
