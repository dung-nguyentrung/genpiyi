<p align="center">
  <img src="assets/logo.png" width="96" alt="GenPiYi logo">
</p>

<h1 align="center">GenPiYi</h1>

<p align="center">
  <b>See the pinyin of any Chinese chat message on Windows — just copy it.</b><br>
  Works with WeChat, Zalo, LINE, Telegram, QQ and more.
</p>

<p align="center">
  <a href="https://github.com/dung-nguyentrung/genpiyi/actions/workflows/build.yml"><img src="https://github.com/dung-nguyentrung/genpiyi/actions/workflows/build.yml/badge.svg" alt="Build"></a>
  <a href="https://github.com/dung-nguyentrung/genpiyi/releases/latest"><img src="https://img.shields.io/github/v/release/dung-nguyentrung/genpiyi?label=download" alt="Latest release"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4" alt="Windows 10/11">
  <img src="https://img.shields.io/badge/.NET-8-512BD4" alt=".NET 8">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="MIT"></a>
</p>

<p align="center">
  <b>English</b> · <a href="README.vi.md">Tiếng Việt</a>
</p>

---

GenPiYi is a small tray app. Right-click a Chinese message in your chat app, choose **Copy**, and a popup appears next to your cursor with **pinyin above every character**, colored by tone.

<!-- Screenshot: add docs/screenshot.png and uncomment
<p align="center"><img src="docs/screenshot.png" width="720" alt="GenPiYi popup"></p>
-->

## Features

- **Copy to see pinyin**: works in WeChat (微信), WeCom, Zalo, LINE, Telegram, QQ, DingTalk, Feishu/Lark, WhatsApp, Messenger, Discord, and others.
- **Chat-app scanner**: finds the chat apps installed on your PC and lets you switch each one on or off. By default only WeChat and Zalo are on.
- **Global hotkey** (`Ctrl+Alt+P`, configurable): select text in any app, like a browser, Word or a PDF, and press the hotkey.
- **Polyphonic characters** are read from context (银行 *yínháng* / 行走 *xíngzǒu*). Hover a character to see its other readings.
- **Tone marks or numbers** (`nǐ hǎo` / `ni3 hao3`), with tone colors and adjustable size.
- **8 popup themes**: Night, Pure White, Strawberry Milk, Tabby Cat, Matcha, Cherry Blossom, Ocean Blue, Galaxy.
- **Doesn't steal focus**: you can keep typing while the popup is open. Click outside or press Esc to close it.
- **Copy pinyin**, or copy hanzi and pinyin together.
- **English / Vietnamese** interface.
- **100% offline**: no telemetry, no network access.

## Download

Download `GenPiYi-*-win-x64.exe` (or the `win-arm64` / `.zip` variants) from [**Releases**](https://github.com/dung-nguyentrung/genpiyi/releases/latest), and run it. You don't need to install anything, because the .NET runtime is bundled.

## Usage

| How | What happens |
|---|---|
| Right-click a message → **Copy** | A pinyin popup appears next to the cursor (only for the chat apps you enabled). |
| Select text anywhere → `Ctrl+Alt+P` | Shows pinyin for the selected text, or for what's on the clipboard if nothing is selected. |
| Tray icon 拼 → left click | Opens the main window: lookup, themes, settings, guide. |

Settings are stored in `%AppData%\GenPiYi\settings.json`. To start hidden in the tray, launch with `--tray`.

## Build from source

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), and Visual Studio 2022 (".NET desktop development" workload) or just the CLI.

```powershell
git clone https://github.com/dung-nguyentrung/genpiyi.git
cd genpiyi
dotnet build genpiyi.sln -c Release
dotnet run --project genpiyi/genpiyi.csproj
```

To produce a single-file, self-contained build:

```powershell
dotnet publish genpiyi/genpiyi.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Project structure

```
genpiyi/
├─ App.xaml(.cs)          App entry: clipboard watcher, hotkey, chat-app filter, popup
├─ MainWindow.xaml(.cs)   Main window: lookup · themes · settings · guide
├─ PinyinPopup.xaml(.cs)  Popup next to the cursor (closes on outside click)
├─ PinyinService.cs       Hanzi → pinyin (ToolGood.Words.Pinyin), tone normalisation
├─ RubyBuilder.cs         Renders pinyin above characters
├─ Themes.cs              Popup themes + vector decorations
├─ ChatAppScanner.cs      Detects installed / running chat apps
├─ Loc.cs                 English / Vietnamese strings
├─ MessageWindow.cs       WM_CLIPBOARDUPDATE + global hotkey
├─ TrayIcon.cs            System tray icon & menu
└─ AppSettings.cs, StartupHelper.cs, NativeMethods.cs, MouseHook.cs, UiHelpers.cs
```

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md), which includes how to add a new theme or a new UI language.

## Known limitations

- Chat apps draw their own right-click menus, so GenPiYi can't add a "Show pinyin" item to them. It reacts to **Copy** instead.
- It can't read text inside images yet (no OCR).

## Credits

- Pinyin conversion: [ToolGood.Words.Pinyin](https://github.com/toolgood/ToolGood.Words)
- Icons: Segoe Fluent Icons / Segoe MDL2 Assets (built into Windows)

## License

[MIT](LICENSE)
