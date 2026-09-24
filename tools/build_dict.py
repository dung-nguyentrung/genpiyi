#!/usr/bin/env python3
"""
Tạo file từ điển offline cho GenPiYi (dùng chung cho bản Windows và macOS).

    python tools/build_dict.py            # tải dữ liệu nguồn (≈ 25 MB) rồi tạo file
    python tools/build_dict.py --offline  # dùng lại dữ liệu đã tải trong tools/.dict-cache

Kết quả: data/genpiyi-dict.tsv.deflate  (TSV UTF-8, nén DEFLATE thô)

Nguồn dữ liệu:
  * CC-CEDICT  – Trung → Anh   – CC BY-SA 4.0 – https://www.mdbg.net/chinese/dictionary?page=cedict
  * CVDICT     – Trung → Việt  – CC BY-SA 4.0 – https://github.com/ph0ngp/CVDICT
  * Unihan     – âm Hán Việt (kVietnamese) – Unicode License v3 – https://www.unicode.org/charts/unihan.html

Định dạng (mỗi dòng một bản ghi, phân tách bằng TAB):
  #GENPIYI-DICT  <phiên bản>  <ngày tạo>  <ghi công>
  H  <chữ>  <âm Hán Việt 1>,<âm 2>,…
  W  <giản thể>  <phồn thể>  <pinyin số>  <nghĩa tiếng Việt>  <nghĩa tiếng Anh>
     (các nghĩa trong một ô cách nhau bằng " ; ")
Chỉ dùng thư viện chuẩn của Python 3.8+.
"""
from __future__ import annotations

import argparse
import datetime as _dt
import io
import os
import re
import sys
import urllib.request
import zipfile
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CACHE = os.path.join(ROOT, "tools", ".dict-cache")
OUT = os.path.join(ROOT, "data", "genpiyi-dict.tsv.deflate")
EXTRA = os.path.join(ROOT, "data", "extra-words.tsv")  # từ/cụm từ nhắn tin bổ sung (tự biên soạn)

SOURCES = {
    "cedict": ["https://www.mdbg.net/chinese/export/cedict/cedict_1_0_ts_utf-8_mdbg.zip"],
    "cvdict": ["https://raw.githubusercontent.com/ph0ngp/CVDICT/main/CVDICT.u8",
               "https://raw.githubusercontent.com/ph0ngp/CVDICT/master/CVDICT.u8"],
    "unihan": ["https://www.unicode.org/Public/UCD/latest/ucd/Unihan.zip"],
}
FILES = {"cedict": "cedict.zip", "cvdict": "CVDICT.u8", "unihan": "Unihan.zip"}

ATTRIBUTION = ("CC-CEDICT (CC BY-SA 4.0, mdbg.net) · CVDICT (CC BY-SA 4.0, github.com/ph0ngp/CVDICT) · "
               "Unihan kVietnamese (Unicode License v3)")

LINE_RE = re.compile(r"^(\S+) (\S+) \[([^\]]*)\] /(.*)/\s*$")


# ----------------------------------------------------------------------------- tải

def download(key: str) -> str:
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, FILES[key])
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return path
    last = None
    for url in SOURCES[key]:
        try:
            print(f"  tải {url}")
            req = urllib.request.Request(url, headers={"User-Agent": "GenPiYi-dict-builder"})
            with urllib.request.urlopen(req, timeout=120) as r:
                data = r.read()
            with open(path, "wb") as f:
                f.write(data)
            return path
        except Exception as e:  # thử link dự phòng
            last = e
            print(f"    lỗi: {e}")
    raise SystemExit(f"Không tải được {key}: {last}")


def read_text(path: str, member_suffix: str | None = None) -> str:
    if path.endswith(".zip"):
        with zipfile.ZipFile(path) as z:
            name = next(n for n in z.namelist() if n.endswith(member_suffix or ".u8"))
            return z.read(name).decode("utf-8")
    with open(path, encoding="utf-8") as f:
        return f.read()


# ----------------------------------------------------------------------------- phân tích

def parse_cedict(text: str):
    """Trả về dict[(phồn, giản, pinyin)] = [nghĩa…] theo thứ tự trong file."""
    out: dict[tuple[str, str, str], list[str]] = {}
    for line in text.splitlines():
        if not line or line.startswith("#"):
            continue
        m = LINE_RE.match(line)
        if not m:
            continue
        trad, simp, py, defs = m.groups()
        senses = [clean_sense(s) for s in defs.split("/")]
        senses = [s for s in senses if s]
        key = (trad, simp, py.strip())
        out.setdefault(key, [])
        for s in senses:
            if s not in out[key]:
                out[key].append(s)
    return out


def clean_sense(s: str) -> str:
    s = s.strip().replace("\t", " ")
    # bỏ mục lượng từ: "CL:" (CC-CEDICT) / "LT:" (CVDICT)
    if not s or s.startswith(("CL:", "LT:", "Classifier:")):
        return ""
    return s


def parse_unihan(text: str) -> dict[str, list[str]]:
    hv: dict[str, list[str]] = {}
    for line in text.splitlines():
        if "\tkVietnamese\t" not in line:
            continue
        code, _, value = line.split("\t", 2)
        ch = chr(int(code[2:], 16))
        readings = [r.strip().lower() for r in value.split() if r.strip()]
        if readings:
            hv[ch] = readings
    return hv


def read_extra():
    if not os.path.exists(EXTRA):
        return []
    rows = []
    with open(EXTRA, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if not line or line.startswith("#"):
                continue
            p = line.split("\t")
            if len(p) >= 5:
                rows.append(((p[1], p[0], p[2]), (p[3], p[4])))
    return rows


def is_han(ch: str) -> bool:
    c = ord(ch)
    return (0x4E00 <= c <= 0x9FFF or 0x3400 <= c <= 0x4DBF or 0xF900 <= c <= 0xFAFF
            or 0x20000 <= c <= 0x2A6DF)


def make_sort_key(order):
    def key(item):
        (trad, simp, py), _ = item
        # cùng mặt chữ: giữ thứ tự trong CC-CEDICT, tên riêng (pinyin viết hoa) xếp sau
        return (simp, 1 if py[:1].isupper() else 0, order[(trad, simp, py)])
    return key


# ----------------------------------------------------------------------------- tạo file

def build(cedict_text: str, cvdict_text: str, unihan_text: str) -> str:
    en = parse_cedict(cedict_text)
    vi = parse_cedict(cvdict_text)
    hv = parse_unihan(unihan_text)

    # Âm Hán Việt cho chữ giản thể: lấy theo chữ phồn thể tương ứng (từ các mục 1 chữ của CC-CEDICT)
    # (và theo từng cặp chữ của các từ nhiều chữ, khi hai dạng dài bằng nhau)
    for (trad, simp, _py) in list(en.keys()) + list(vi.keys()):
        if len(trad) != len(simp):
            continue
        for t, s_ in zip(trad, simp):
            if t == s_:
                continue
            if s_ not in hv and t in hv:
                hv[s_] = hv[t]
            elif t not in hv and s_ in hv:
                hv[t] = hv[s_]

    # Bổ sung các cụm từ hay dùng khi chat mà từ điển gốc chưa có (好的, 还没, 在吗…)
    known = {k[1] for k in list(en) + list(vi)} | {k[0] for k in list(en) + list(vi)}
    for (trad, simp, py), (v, e) in read_extra():
        if simp in known or trad in known:
            continue
        vi[(trad, simp, py)] = [x.strip() for x in v.split(";") if x.strip()]
        en[(trad, simp, py)] = [x.strip() for x in e.split(";") if x.strip()]

    order = {k: i for i, k in enumerate(list(en) + [k for k in vi if k not in en])}
    rows = []
    for key in order:
        trad, simp, py = key
        if not any(is_han(c) for c in simp):
            continue
        v = " ; ".join(vi.get(key, []))
        e = " ; ".join(en.get(key, []))
        if not v and not e:
            continue
        rows.append(((trad, simp, py), (v, e)))
    rows.sort(key=make_sort_key(order))

    lines = [f"#GENPIYI-DICT\t1\t{_dt.date.today().isoformat()}\t{ATTRIBUTION}"]
    for ch in sorted(hv):
        lines.append(f"H\t{ch}\t{','.join(hv[ch])}")
    for (trad, simp, py), (v, e) in rows:
        lines.append(f"W\t{simp}\t{trad}\t{py}\t{v}\t{e}")
    return "\n".join(lines) + "\n"


def deflate(text: str) -> bytes:
    c = zlib.compressobj(9, zlib.DEFLATED, -15)  # DEFLATE thô: .NET DeflateStream & Apple .zlib đọc trực tiếp
    return c.compress(text.encode("utf-8")) + c.flush()


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--offline", action="store_true", help="không tải, dùng file trong tools/.dict-cache")
    ap.add_argument("--out", default=OUT)
    args = ap.parse_args(argv)

    print("==> Dữ liệu nguồn")
    paths = {}
    for key in SOURCES:
        p = os.path.join(CACHE, FILES[key])
        if args.offline:
            if not os.path.exists(p):
                raise SystemExit(f"Thiếu {p}")
        else:
            p = download(key)
        paths[key] = p

    print("==> Đọc & gộp")
    text = build(read_text(paths["cedict"], ".u8"),
                 read_text(paths["cvdict"]),
                 read_text(paths["unihan"], "Unihan_Readings.txt"))
    data = deflate(text)
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "wb") as f:
        f.write(data)

    n_w = text.count("\nW\t")
    n_h = text.count("\nH\t")
    print(f"==> Xong: {args.out}")
    print(f"    {n_w:,} mục từ · {n_h:,} chữ có âm Hán Việt · {len(text.encode()) / 1e6:.1f} MB → {len(data) / 1e6:.1f} MB nén")


if __name__ == "__main__":
    sys.exit(main())
