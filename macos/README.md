# GenPiYi cho macOS

Bản macOS của GenPiYi — app nhỏ nằm trên **thanh menu** (biểu tượng 拼). Chuột phải vào tin nhắn tiếng Trung trong Zalo / WeChat / LINE… → **Sao chép** (hoặc bôi đen rồi ⌘C), popup hiện **pinyin trên từng chữ Hán** ngay cạnh con trỏ.

*English below.*

## Tính năng (tương đương bản Windows)

| | Windows | macOS |
|---|---|---|
| Tự hiện pinyin khi copy trong app chat | ✅ | ✅ (nhận diện app theo bundle ID) |
| Quét app chat đã cài, bật/tắt từng app, thêm app đang mở | ✅ | ✅ |
| Phím tắt toàn cục (bôi đen → bấm) | `Ctrl+Alt+P` | `⌃⌥P` (đổi được) |
| Chữ đa âm đọc theo ngữ cảnh | ToolGood.Words.Pinyin | Bộ chuyển đổi có sẵn của macOS (`CFStringTransform`) |
| Dấu thanh / số thanh, tô màu thanh điệu, cỡ chữ | ✅ | ✅ |
| 8 mẫu popup (Đêm, Trắng tinh, Sữa dâu, Mèo mướp, Trà xanh, Hoa anh đào, Biển xanh, Ngân hà) | ✅ | ✅ |
| Popup không chiếm focus, click ra ngoài / Esc để đóng, ghim | ✅ | ✅ |
| Copy pinyin / copy chữ Hán + pinyin | ✅ | ✅ |
| Tiếng Việt / English | ✅ | ✅ |
| Khởi động cùng hệ điều hành | Registry | Login Items (`SMAppService`) |
| 100% offline, không cần thư viện ngoài | ✅ | ✅ |

## Cài đặt

1. Tải `GenPiYi-v….-macos.dmg` ở mục [Releases](https://github.com/dung-nguyentrung/genpiyi/releases) (tag `mac-v…`) hoặc ở mục *Artifacts* của lần build gần nhất trong tab **Actions → macOS**.
2. Mở file .dmg, kéo **GenPiYi** vào **Applications**.
3. Lần đầu mở, macOS có thể chặn vì app chưa được Apple công chứng. Vào **Cài đặt hệ thống → Quyền riêng tư & Bảo mật**, kéo xuống và bấm **Vẫn mở**. Hoặc chạy trong Terminal:
   ```bash
   xattr -dr com.apple.quarantine /Applications/GenPiYi.app
   ```
4. (Tuỳ chọn) Để **phím tắt** tự copy đoạn đang bôi đen, cho phép GenPiYi trong **Quyền riêng tư & Bảo mật → Trợ năng**. Tính năng tự hiện khi copy **không cần** quyền này.

Yêu cầu: macOS 13 Ventura trở lên, chip Apple hoặc Intel.

## Tự build

Cần Xcode 15+ (hoặc Command Line Tools có Swift 5.9+).

```bash
cd macos
swift run                    # chạy thử nhanh (bản debug, không có biểu tượng app)
./scripts/build-app.sh       # tạo dist/GenPiYi.app, .dmg và .zip (universal)
ARCHS=arm64 ./scripts/build-app.sh   # chỉ chip Apple, build nhanh hơn
```

Mở bằng Xcode: `open Package.swift`.

## Cấu trúc mã

| File | Vai trò |
|---|---|
| `main.swift`, `AppController.swift` | Vòng đời app, thanh menu, theo dõi clipboard, phím tắt, quản lý popup |
| `PinyinService.swift` | Hán tự → pinyin (CFStringTransform), chuẩn hoá & đặt dấu thanh, bảng chữ đa âm |
| `PinyinPopup.swift` | Popup nổi (NSPanel không chiếm focus) + giao diện SwiftUI |
| `RubyView.swift` | Bố cục "pinyin trên chữ Hán", tự xuống dòng |
| `Themes.swift` | 8 mẫu giao diện + hình trang trí |
| `MainView.swift`, `SettingsPage.swift` | Cửa sổ chính: Tra pinyin, Giao diện, Cài đặt, Hướng dẫn |
| `ChatAppScanner.swift` | Danh sách app chat (bundle ID), quét app đã cài / đang chạy |
| `HotKey.swift` | Phím tắt toàn cục (Carbon), giả lập ⌘C, quyền Trợ năng |
| `AppSettings.swift`, `Loc.swift` | Cài đặt (JSON trong `~/Library/Application Support/GenPiYi`) và đa ngôn ngữ |

---

## English

GenPiYi for macOS lives in the **menu bar** (拼 icon). Right-click a Chinese message in Zalo / WeChat / LINE… → **Copy** (or select it and press ⌘C) and a popup shows **pinyin above every character** next to your cursor. Select text anywhere and press **⌃⌥P** to do the same in any app.

**Install:** download `GenPiYi-v…-macos.dmg` from Releases (tags `mac-v…`) or from the latest **Actions → macOS** run, drag GenPiYi to Applications. If macOS blocks the first launch (the app isn't notarized), go to **System Settings → Privacy & Security → Open Anyway**, or run `xattr -dr com.apple.quarantine /Applications/GenPiYi.app`. For the hotkey to copy the selected text, allow GenPiYi under **Privacy & Security → Accessibility**.

**Build:** `cd macos && ./scripts/build-app.sh` (Xcode 15+, macOS 13+ target).
