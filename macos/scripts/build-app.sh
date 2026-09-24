#!/usr/bin/env bash
# Đóng gói GenPiYi.app (+ .dmg, .zip) từ mã Swift. Chạy trên macOS có Xcode / Command Line Tools.
#
#   cd macos && ./scripts/build-app.sh            # bản universal (Apple Silicon + Intel)
#   VERSION=0.1.0 ./scripts/build-app.sh
#   ARCHS="arm64" ./scripts/build-app.sh          # chỉ build cho chip Apple (nhanh hơn)
#
# Kết quả nằm trong macos/dist/
set -euo pipefail

cd "$(dirname "$0")/.."
ROOT="$(pwd)"
VERSION="${VERSION:-0.1.2}"
ARCHS="${ARCHS:-arm64 x86_64}"
BUNDLE_ID="io.github.dung-nguyentrung.genpiyi"
APP="$ROOT/dist/GenPiYi.app"

echo "==> Build Swift ($ARCHS) · v$VERSION"
ARCH_FLAGS=()
for a in $ARCHS; do ARCH_FLAGS+=(--arch "$a"); done
swift build -c release "${ARCH_FLAGS[@]}"
BIN_DIR="$(swift build -c release "${ARCH_FLAGS[@]}" --show-bin-path)"

echo "==> Tạo GenPiYi.app"
rm -rf "$ROOT/dist"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN_DIR/GenPiYi" "$APP/Contents/MacOS/GenPiYi"

sed -e "s/__VERSION__/$VERSION/g" -e "s/__BUNDLE_ID__/$BUNDLE_ID/g" \
    "$ROOT/Resources/Info.plist" > "$APP/Contents/Info.plist"
printf 'APPL????' > "$APP/Contents/PkgInfo"

# Từ điển offline (tạo bằng: python3 tools/build_dict.py)
DICT="$ROOT/../data/genpiyi-dict.tsv.deflate"
if [ -f "$DICT" ]; then
  cp "$DICT" "$APP/Contents/Resources/"
else
  echo "   (chưa có data/genpiyi-dict.tsv.deflate — app sẽ không có phần nghĩa từ vựng)"
fi

# Biểu tượng app từ assets/logo.png
LOGO="$ROOT/../assets/logo.png"
if [ -f "$LOGO" ]; then
  ICONSET="$ROOT/dist/AppIcon.iconset"
  mkdir -p "$ICONSET"
  for s in 16 32 128 256 512; do
    sips -z $s $s "$LOGO" --out "$ICONSET/icon_${s}x${s}.png" >/dev/null
    d=$((s * 2))
    sips -z $d $d "$LOGO" --out "$ICONSET/icon_${s}x${s}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AppIcon.icns"
  rm -rf "$ICONSET"
fi

echo "==> Ký ad-hoc"
codesign --force --deep --sign - "$APP"
codesign --verify --verbose "$APP"

echo "==> Tạo .zip và .dmg"
cd "$ROOT/dist"
ditto -c -k --keepParent GenPiYi.app "GenPiYi-v$VERSION-macos.zip"

STAGE="$ROOT/dist/dmg"
mkdir -p "$STAGE"
cp -R GenPiYi.app "$STAGE/"
ln -s /Applications "$STAGE/Applications"
hdiutil create -volname "GenPiYi" -srcfolder "$STAGE" -ov -format UDZO "GenPiYi-v$VERSION-macos.dmg" >/dev/null
rm -rf "$STAGE"

echo "==> Xong:"
ls -lh "$ROOT/dist"
