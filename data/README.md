# Dữ liệu từ điển / Dictionary data

`genpiyi-dict.tsv.deflate` là từ điển offline dùng cho phần **nghĩa từ vựng** của GenPiYi (cả bản Windows và macOS).
Tạo lại bằng lệnh (cần Python 3.8+ và Internet):

```bash
python tools/build_dict.py
```

File là văn bản TSV UTF-8 nén DEFLATE thô (xem mô tả định dạng ở đầu `tools/build_dict.py`).

## Nguồn & giấy phép / Sources & licenses

| Nguồn | Nội dung | Giấy phép |
|---|---|---|
| [CVDICT](https://github.com/ph0ngp/CVDICT) | Nghĩa tiếng Việt | CC BY-SA 4.0 |
| [CC-CEDICT](https://www.mdbg.net/chinese/dictionary?page=cedict) | Nghĩa tiếng Anh, chữ phồn/giản, pinyin | CC BY-SA 4.0 |
| [Unihan](https://www.unicode.org/charts/unihan.html) (`kVietnamese`) | Âm Hán Việt | Unicode License v3 |

File dữ liệu trong thư mục này là tác phẩm phái sinh từ CVDICT và CC-CEDICT nên được phân phối theo
**CC BY-SA 4.0** (khác với mã nguồn app, dùng giấy phép MIT).

The data file in this folder is derived from CVDICT and CC-CEDICT and is therefore distributed under
**CC BY-SA 4.0** (the app source code itself is MIT-licensed).
