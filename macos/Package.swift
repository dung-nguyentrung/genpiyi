// swift-tools-version:5.9
// GenPiYi cho macOS — app thanh menu (menu bar) hiện pinyin khi copy tin nhắn tiếng Trung.
// Build nhanh:  swift build -c release
// Đóng gói .app/.dmg:  ./scripts/build-app.sh
import PackageDescription

let package = Package(
    name: "GenPiYi",
    platforms: [.macOS(.v13)],
    targets: [
        .executableTarget(
            name: "GenPiYi",
            path: "Sources/GenPiYi",
            linkerSettings: [
                .linkedFramework("AppKit"),
                .linkedFramework("Carbon"),
                .linkedFramework("ServiceManagement"),
            ]
        ),
    ]
)
