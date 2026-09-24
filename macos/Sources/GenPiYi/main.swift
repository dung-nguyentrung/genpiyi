import AppKit

// Điểm vào: app chạy nền trên thanh menu (không có biểu tượng Dock trừ khi mở cửa sổ chính).
let app = NSApplication.shared
let controller = AppController()
app.delegate = controller
app.setActivationPolicy(.accessory)
app.run()
