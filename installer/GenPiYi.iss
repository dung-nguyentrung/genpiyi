; ============================================================
;  GenPiYi – bộ cài đặt Windows (Inno Setup 6)
;  Build:  iscc /DAppVersion=0.0.2 /DSourceDir=..\publish\win-x64 installer\GenPiYi.iss
;  - Cài cho người dùng hiện tại, KHÔNG cần quyền Administrator
;  - Tạo shortcut Start Menu (+ Desktop tuỳ chọn), tuỳ chọn khởi động cùng Windows
;  - Gỡ cài đặt trong Settings → Apps như mọi phần mềm khác
; ============================================================

#define AppName "GenPiYi"
#define AppExe "GenPiYi.exe"
#define AppUrl "https://github.com/dung-nguyentrung/genpiyi"

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\win-x64"
#endif

[Setup]
AppId={{6F1C2B8E-3A4D-4E7B-9C21-5D8A7E0F4B12}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=GenPiYi contributors
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=GenPiYi-Setup-{#AppVersion}
SetupIconFile=..\genpiyi\GenPiYi.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
; Tự đóng GenPiYi đang chạy khi cài bản mới / gỡ cài đặt (tên mutex trong App.xaml.cs)
AppMutex=GenPiYi_SingleInstance_7C2E
CloseApplications=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
en.StartupTask=Start GenPiYi with Windows (Khởi động cùng Windows)
en.OtherTasks=Other options (Tuỳ chọn khác):
en.DesktopTask=Create a desktop shortcut (Tạo biểu tượng ngoài màn hình)

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopTask}"; GroupDescription: "{cm:OtherTasks}"
Name: "startup"; Description: "{cm:StartupTask}"; GroupDescription: "{cm:OtherTasks}"

[Files]
Source: "{#SourceDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Giống hệt khoá mà app tự ghi khi bật "Khởi động cùng Windows" trong Cài đặt
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"" --tray"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Xoá khoá khởi động nếu người dùng bật trong app (không qua bộ cài)
Filename: "{sys}\reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v {#AppName} /f"; Flags: runhidden; RunOnceId: "RemoveRunKey"
