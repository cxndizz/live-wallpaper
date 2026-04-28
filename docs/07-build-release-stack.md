# Build & Release Stack

## คำตอบหลัก

จาก stack ที่เลือกไว้:

```text
Main App UI       : C# + .NET + WPF
Wallpaper Engine  : C# + Win32 API
Installer         : WPF Setup UI
```

การ build ปกติควรเป็น:

```text
เขียนโปรแกรมใน Visual Studio / Rider / VS Code
↓
dotnet publish
↓
ได้ output folder
↓
ใช้ WPF installer แพ็กเป็น Setup.exe
↓
ผู้ใช้ติดตั้งโปรแกรม
```

## ทำไมไม่จำเป็นต้อง single-file

โปรแกรมนี้อาจมีไฟล์หลายชนิด:

```text
- LiveWallpaperStudio.exe
- DLL ของ project
- native renderer dependencies
- WebView2/runtime dependency
- assets
- icons
- default wallpapers
- config defaults
```

ดังนั้นแนะนำ:

```text
publish เป็น folder
→ ใช้ WPF installer ทำ Setup.exe
```

มากกว่า build เป็น single-file อย่างเดียว

## Recommended Build Output

```text
publish/
├─ LiveWallpaperStudio.exe
├─ LiveWallpaperStudio.Engine.dll
├─ LiveWallpaperStudio.Renderers.dll
├─ LiveWallpaperStudio.Data.dll
├─ assets/
├─ runtimes/
└─ dependencies/
```

## Build Requirements

| ID | Requirement | Priority |
|---|---|---|
| BLD-01 | Build แบบ Release ได้ | P0 |
| BLD-02 | Publish เป็น folder สำหรับ installer | P0 |
| BLD-03 | มี version number | P0 |
| BLD-04 | มี app icon | P0 |
| BLD-05 | มี signed executable | P2 |
| BLD-06 | มี CI build script | P2 |
| BLD-07 | มี installer artifact | P1 |

## Versioning

แนะนำ format:

```text
Major.Minor.Patch
```

ตัวอย่าง:

```text
0.1.0  = Core prototype
0.2.0  = MVP internal
0.5.0  = Public beta
1.0.0  = Stable release
```

## Release Artifacts

ควรได้ไฟล์ประมาณนี้:

```text
LiveWallpaperStudio-Setup-0.1.0.exe
LiveWallpaperStudio-Portable-0.1.0.zip   optional
LiveWallpaperStudio-ReleaseNotes-0.1.0.md
```

## Installer Signing

ช่วงแรกยังไม่จำเป็น แต่ถ้าจะปล่อย public จริง ควรพิจารณา code signing

เหตุผล:

```text
- ลด warning จาก Windows SmartScreen
- เพิ่มความน่าเชื่อถือ
- เหมาะกับ commercial app
```

## Installer vs Portable

### Installer

เหมาะกับผู้ใช้ทั่วไป:

```text
- สร้าง shortcut ให้
- ตั้ง startup ได้
- มี uninstall ใน Windows Settings
- จัดการ Program Files
```

### Portable

เหมาะกับ tester/developer:

```text
- unzip แล้วรัน
- ไม่เขียนระบบมาก
- uninstall โดยลบ folder
```

สำหรับ product จริง แนะนำให้เริ่มจาก Installer เป็นหลัก

## Release Checklist

```text
- Build Release ผ่าน
- เปิด app ได้
- Add wallpaper ได้
- Renderer อยู่หลัง desktop icons
- ไม่มี title bar บน renderer
- Resize ตาม monitor ถูก
- Tray menu ใช้งานได้
- Close window แล้ว app ยังรัน
- Exit แล้ว renderer หาย
- Installer ติดตั้งได้
- Uninstaller ลบ startup/shortcut ได้
- Settings ถูกเก็บใน AppData
- Logs เขียนได้
```
