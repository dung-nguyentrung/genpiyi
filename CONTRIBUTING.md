# Contributing to GenPiYi

Thanks for your interest! / Cảm ơn bạn đã muốn đóng góp! Issues and PRs in **English or Vietnamese** are both fine.

## Branches / Quy trình nhánh

| Branch | Purpose |
|---|---|
| `main` | Stable code. Every release tag (`v0.0.2`, …) is cut from here. **No direct pushes.** |
| `develop` | Integration branch: all feature/fix PRs target this branch. |
| `feature/<short-name>` | New features, e.g. `feature/ocr`, `feature/theme-sunset` |
| `fix/<short-name>` | Bug fixes, e.g. `fix/wechat4-detect` |
| `docs/<short-name>` | Documentation only |
| `hotfix/<short-name>` | Urgent fix branched from `main`, merged back into both `main` and `develop` |

Flow: `feature/*` → PR into `develop` → (release) PR `develop` → `main` → tag `vX.Y.Z` → GitHub Actions builds the release.

Luồng làm việc: tạo nhánh `feature/...` hoặc `fix/...` từ `develop`, mở Pull Request vào `develop`. Khi phát hành, maintainer merge `develop` → `main` rồi gắn tag.

## Getting started

1. Fork the repo (or clone it, if you are a collaborator).
2. Branch from `develop`:
   ```bash
   git checkout develop
   git pull
   git checkout -b feature/my-change
   ```
3. Build: `dotnet build genpiyi.sln` (Windows, .NET 8 SDK)
4. Run: `dotnet run --project genpiyi/genpiyi.csproj`
5. Commit with a clear message, e.g. `feat: add sunset theme` or `fix: detect WeChat 4 on ARM64`, push the branch, and open a pull request **into `develop`**.

Please keep PRs focused (one feature or fix per PR) and describe how you tested them. For UI changes, a screenshot helps a lot.

## Code style

- C# with `Nullable` enabled and **implicit usings disabled**. Add `using` directives explicitly, and avoid `using System.Windows.Forms;` in WPF files. Use the `WinForms` / `Drawing` aliases like `TrayIcon.cs` does.
- Put user-visible strings in `Loc.cs`, never hard-coded:
  - XAML: `Text="{DynamicResource S.my.key}"`
  - C#: `Loc.T("my.key")`, or `Loc.F("my.key", arg0, …)` for format strings
- `.editorconfig` covers indentation and line endings.

## Adding a popup theme

Add a `new PopupTheme { … }` entry to `ThemeCatalog.All` in `genpiyi/Themes.cs`:

| Field | Meaning |
|---|---|
| `Id` | Unique id saved in settings |
| `Name` / `NameEn`, `Description` / `DescriptionEn` | Vietnamese / English labels |
| `Badge` | `"new"`, `"hot"` or empty |
| `Background` | Two colors for the diagonal gradient |
| `Fg`, `Sub`, `Hover`, `Chip`, `Footer`, `Divider`, `Accent`, `Border`, `Radius` | Popup chrome |
| `Hanzi`, `PinyinDefault`, `Tones[0..5]` | Text colors; `Tones[1..4]` are the four tones, `Tones[5]` is the neutral tone |
| `Emblem`, `EmblemColors` | The one-character badge in the header |
| `Deco`, `DecoColor`, `DecoColor2`, `CatEars` | Decorations: `hearts`, `paws`, `petals`, `leaves`, `bubbles`, `sparkles`, `none` |

Please use **original artwork only**, not copyrighted characters or logos.

## Adding a UI language

1. In `genpiyi/Loc.cs`, extend the tuple `(Vi, En)` with the new language, or refactor it to a dictionary per language, and translate every key.
2. Add the language option to `Loc.Apply` / `DetectDefault` and to the Language row in `MainWindow.xaml`.

## Adding a chat app to the scanner

Add a `KnownChatApp` entry in `genpiyi/ChatAppScanner.cs` with:
- its process name(s), without `.exe`,
- a regex for its "Apps & features" display name,
- common install paths and, if it has one, its Microsoft Store package prefix.

## Reporting bugs

Please include your Windows version, the chat app and its version, what you copied, and the contents of `%AppData%\GenPiYi\error.log` if it exists.
