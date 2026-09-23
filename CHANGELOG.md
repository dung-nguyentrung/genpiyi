# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/), versions follow [SemVer](https://semver.org/).

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
