# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/), versions follow [SemVer](https://semver.org/).

## [0.0.4] - 2026-09-24 (Windows) · mac-v0.1.2 (macOS)

### Fixed
- Common chat phrases are now kept together as one word with a meaning (好的, 好吧, 还没, 在吗, 谢谢你, 不知道, 多少钱…): added `data/extra-words.tsv`, a hand-written supplement merged into the dictionary.
- Better word segmentation: bidirectional maximum matching (研究生命 → 研究 | 生命, 他说的确实在理 → 的 | 确实 | 在理).

## [0.0.3] - 2026-09-24 (Windows) · mac-v0.1.1 (macOS)

### Added
- **Word meanings, offline**: the popup (and the lookup page) lists each word of the message with its **Vietnamese and/or English meaning** and its **Sino-Vietnamese reading** (银行 → *ngân hàng* · NGÂN HÀNG). Shown under the plain pinyin; toggle it with the book button.
- Words are segmented with the dictionary: hovering a character highlights the whole word and shows its meaning.
- Settings: show/hide meanings, meaning language (Vietnamese / English / both), Sino-Vietnamese readings on/off.
- Dictionary data built by `tools/build_dict.py` from CVDICT and CC-CEDICT (CC BY-SA 4.0) and Unihan; see `data/README.md`.
- **macOS app** (menu bar, macOS 13+, Apple silicon & Intel) with the same features as the Windows app. First macOS release was `mac-v0.1.0`; `mac-v0.1.1` adds word meanings.
- Landing page: macOS download, platform-aware download buttons.

## [0.0.2] - 2026-09-23

### Added
- **Windows installer** `GenPiYi-Setup-<version>.exe` (Inno Setup): per-user install, no admin rights, Start menu and optional desktop shortcut, optional "Start with Windows", clean uninstall from Settings → Apps.
- Releases also include standalone portable `.exe` files (win-x64, win-arm64) and zip archives.
- Release page shows simple download instructions (Vietnamese and English).

### Changed
- The "Start with Windows" switch now reflects the real registry state, for example when it was enabled by the installer.

## [0.0.1] - 2026-09-23

First public release.

### Features
- Pinyin popup on copy, shown next to the cursor, with tone colors and polyphonic character handling.
- Global hotkey (default `Ctrl+Alt+P`): copies the current selection and shows its pinyin.
- Chat-app scanner (WeChat, Zalo, LINE, Telegram, QQ, DingTalk, Feishu, WhatsApp, Messenger, Discord, Skype, Teams, Slack, KakaoTalk, Viber, WeCom). Each app can be switched on or off; WeChat and Zalo are on by default.
- Option to add any other running app.
- 8 popup themes with vector decorations.
- Tone marks or tone numbers, adjustable hanzi size.
- English / Vietnamese UI.
- Tray icon, start with Windows, single instance.
