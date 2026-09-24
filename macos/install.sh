#!/bin/bash
# GenPiYi — cài nhanh cho macOS / quick installer for macOS
#
#   curl -fsSL https://raw.githubusercontent.com/dung-nguyentrung/genpiyi/main/macos/install.sh | bash
#
# Tải bản macOS mới nhất (tag mac-v…) từ GitHub Releases, chép vào Applications và mở app.
# File tải bằng curl không bị gắn cờ "tải từ Internet" nên macOS không hiện cảnh báo
# "không thể xác minh không có phần mềm độc hại" (app chưa được Apple công chứng).
#
# Downloads the latest macOS build (tag mac-v…) from GitHub Releases, copies it to
# Applications and opens it. Files fetched with curl aren't quarantined, so Gatekeeper
# doesn't show the "can't verify it's free of malware" warning (the app isn't notarized).
#
# Tuỳ chọn / options:  GENPIYI_URL=<link .zip>  để cài một bản cụ thể / to install a specific build.

set -euo pipefail

REPO="dung-nguyentrung/genpiyi"
APP="GenPiYi.app"
BUNDLE_ID="io.github.dung-nguyentrung.genpiyi"

say() { printf '\033[1;32m==>\033[0m %s\n' "$*"; }
die() { printf '\033[1;31mLỗi / Error:\033[0m %s\n' "$*" >&2; exit 1; }

main() {
  [ "$(uname -s)" = "Darwin" ] || die "Script này chỉ dành cho macOS / macOS only."
  local major
  major="$(sw_vers -productVersion | cut -d. -f1)"
  [ "$major" -ge 13 ] || die "Cần macOS 13 Ventura trở lên / Requires macOS 13 Ventura or later."

  local url="${GENPIYI_URL:-}"
  if [ -z "$url" ]; then
    say "Tìm bản mới nhất… / Looking for the latest version…"
    url="$(curl -fsSL "https://api.github.com/repos/$REPO/releases?per_page=30" \
      | grep -o "https://github.com/$REPO/releases/download/mac-v[^/\"]*/GenPiYi-v[^\"]*-macos\.zip" \
      | head -n 1 || true)"
    [ -n "$url" ] || die "Không tìm thấy bản macOS. Tải thủ công tại / No macOS build found. Download manually: https://github.com/$REPO/releases"
  fi

  local tmp
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT

  say "Tải / Downloading ${url##*/}"
  curl -fL --progress-bar "$url" -o "$tmp/genpiyi.zip"
  ditto -x -k "$tmp/genpiyi.zip" "$tmp/x"
  [ -d "$tmp/x/$APP" ] || die "File tải về không có $APP / $APP not found in the download."

  local dest="/Applications"
  if [ ! -w "$dest" ]; then
    dest="$HOME/Applications"
    mkdir -p "$dest"
  fi

  # Thoát bản đang chạy (nếu có) / quit a running copy
  osascript -e "tell application id \"$BUNDLE_ID\" to quit" >/dev/null 2>&1 || true
  pkill -x GenPiYi >/dev/null 2>&1 || true
  sleep 1

  say "Cài vào / Installing to $dest/$APP"
  rm -rf "$dest/$APP"
  ditto "$tmp/x/$APP" "$dest/$APP"
  xattr -dr com.apple.quarantine "$dest/$APP" 2>/dev/null || true

  say "Xong! Mở GenPiYi… (biểu tượng 拼 trên thanh menu) / Done! Opening GenPiYi (拼 in the menu bar)"
  open "$dest/$APP"
}

main "$@"
