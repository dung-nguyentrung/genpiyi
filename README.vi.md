<p align="center">
  <img src="assets/logo.png" width="96" alt="GenPiYi logo">
</p>

<h1 align="center">GenPiYi</h1>

<p align="center">
  <b>Xem pinyin của tin nhắn tiếng Trung trên Windows — chỉ cần copy.</b><br>
  Dùng được với Zalo, WeChat, LINE, Telegram, QQ…
</p>

<p align="center">
  <a href="README.md">English</a> · <b>Tiếng Việt</b>
</p>

---

GenPiYi là app nhỏ chạy ở khay hệ thống. Chuột phải vào tin nhắn tiếng Trung trong app chat, chọn **Sao chép**, một popup hiện ngay cạnh con trỏ với **pinyin nằm trên từng chữ Hán**, tô màu theo thanh điệu.

## Tính năng

- **Copy là thấy pinyin**: dùng với Zalo, WeChat (微信), WeCom, LINE, Telegram, QQ, DingTalk, Feishu/Lark, WhatsApp, Messenger, Discord…
- **Tự quét app chat trên máy**, bật/tắt từng app. Mặc định chỉ bật Zalo và WeChat.
- **Phím tắt toàn cục** `Ctrl+Alt+P` (đổi được): bôi đen chữ ở bất kỳ đâu (trình duyệt, Word, PDF…) rồi bấm.
- **Chữ đa âm được đọc theo ngữ cảnh** (银行 *yínháng* / 行走 *xíngzǒu*). Rê chuột vào chữ để xem các cách đọc khác.
- **Dấu thanh hoặc số** (`nǐ hǎo` / `ni3 hao3`), có tô màu thanh điệu và chỉnh được cỡ chữ.
- **8 mẫu giao diện popup**: Đêm, Trắng tinh, Sữa dâu, Mèo mướp, Trà xanh, Hoa anh đào, Biển xanh, Ngân hà.
- **Popup không chiếm focus**: vẫn gõ chat bình thường. Click ra ngoài hoặc bấm Esc để đóng.
- **Copy pinyin**, hoặc copy cả chữ Hán lẫn pinyin.
- Giao diện **Tiếng Việt / English**.
- **Chạy offline 100%**, không thu thập dữ liệu.

## Tải về

Vào [**Releases**](https://github.com/dung-nguyentrung/genpiyi/releases/latest), tải `GenPiYi-*-win-x64.zip` (hoặc bản `win-arm64`), giải nén rồi chạy `GenPiYi.exe`. Không cần cài gì thêm vì đã kèm sẵn .NET.

## Cách dùng

| Thao tác | Kết quả |
|---|---|
| Chuột phải tin nhắn → **Sao chép** | Popup pinyin hiện cạnh con trỏ (chỉ với các app chat đang bật). |
| Bôi đen chữ bất kỳ → `Ctrl+Alt+P` | Hiện pinyin của đoạn đang bôi đen, hoặc của nội dung trong clipboard nếu không bôi đen gì. |
| Biểu tượng 拼 ở khay → click | Mở cửa sổ chính: tra pinyin, giao diện, cài đặt, hướng dẫn. |

Cài đặt lưu ở `%AppData%\GenPiYi\settings.json`. Muốn khởi động ẩn ở khay thì chạy với tham số `--tray`.

## Build từ mã nguồn

Cần có Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), và Visual Studio 2022 (workload ".NET desktop development") hoặc chỉ dùng dòng lệnh.

```powershell
git clone https://github.com/dung-nguyentrung/genpiyi.git
cd genpiyi
dotnet build genpiyi.sln -c Release
dotnet run --project genpiyi/genpiyi.csproj
```

Để tạo bản một file, kèm sẵn .NET:

```powershell
dotnet publish genpiyi/genpiyi.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Đóng góp

Rất hoan nghênh issue và pull request. Xem [CONTRIBUTING.md](CONTRIBUTING.md), trong đó có hướng dẫn thêm mẫu giao diện hoặc thêm ngôn ngữ mới.

## Giới hạn

- App chat tự vẽ menu chuột phải riêng nên GenPiYi không chèn thêm mục "Xem pinyin" được. Thay vào đó app bắt thao tác **Sao chép** từ menu đó.
- Chưa đọc được chữ trong ảnh (chưa có OCR).

## Giấy phép

[MIT](LICENSE). Chuyển pinyin bằng thư viện [ToolGood.Words.Pinyin](https://github.com/toolgood/ToolGood.Words).
