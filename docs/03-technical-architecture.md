# Technical Architecture

## Stack ที่แนะนำ

```text
Main App UI       : C# + .NET + WPF
Wallpaper Engine  : C# + Win32 API
Video Renderer    : libVLC / mpv / Media Foundation
Web Wallpaper     : WebView2
Database          : SQLite
Config            : JSON
Build             : dotnet publish
Installer         : WPF Setup UI
Custom Installer  : LiveWallpaperStudio.Installer
```

## โครงสร้างใหญ่ของโปรแกรม

```text
Live Wallpaper Studio
├─ Main App UI
│  ├─ Home
│  ├─ Library
│  ├─ Displays
│  ├─ Performance
│  ├─ Settings
│  └─ Tray menu
│
├─ Wallpaper Engine
│  ├─ DesktopHost / WorkerW bridge
│  ├─ MonitorManager
│  ├─ RendererWindowManager
│  ├─ PlaybackController
│  ├─ PerformanceManager
│  └─ PauseRuleEngine
│
├─ Renderers
│  ├─ ImageRenderer
│  ├─ VideoRenderer
│  ├─ WebRenderer
│  └─ FutureShaderRenderer
│
├─ Data Layer
│  ├─ SQLite database
│  ├─ JSON config
│  ├─ Thumbnail cache
│  └─ Logs
│
└─ Installer
   ├─ WPF Setup UI
   └─ WPF custom setup launcher ในอนาคต
```

## เหตุผลที่เลือก C# + WPF

```text
- เหมาะกับ Windows desktop app
- ทำ UI สวย ๆ แบบ mockup ได้ดี
- XAML แยก UI กับ logic ได้ชัด
- Win32 interop ง่ายกว่า Python ในระยะยาว
- build/publish เป็น Windows app ได้จริง
- เชื่อมกับ WebView2, SQLite, tray, native APIs ได้ดี
```

## เหตุผลที่ไม่เลือก Python เป็น stack หลัก

Python ทำได้ แต่เหมาะกับ prototype มากกว่า

```text
ข้อดี Python:
- ทำเร็ว
- ทดลองง่าย
- UI ด้วย PySide6 ทำได้
- เรียก Win32 API ได้ผ่าน ctypes/pywin32

ข้อเสียสำหรับโปรแกรมนี้:
- EXE ใหญ่กว่า
- startup ช้ากว่า
- packaging Qt/video/WebView ยุ่งกว่า
- debug native window issue ยากกว่า
- performance ของ renderer ต้องพึ่ง native library อยู่ดี
```

ดังนั้นถ้าจะทำเป็นโปรแกรมที่ดู commercial และใช้จริงบน Windows:

```text
แนะนำ: C# + WPF + Win32 API + WPF Setup UI
```

## โครง project ที่แนะนำ

```text
LiveWallpaperStudio/
├─ src/
│  ├─ LiveWallpaperStudio.App/
│  │  ├─ Views/
│  │  ├─ ViewModels/
│  │  ├─ Controls/
│  │  ├─ Themes/
│  │  └─ Assets/
│  │
│  ├─ LiveWallpaperStudio.Engine/
│  │  ├─ DesktopHost/
│  │  ├─ Monitors/
│  │  ├─ Playback/
│  │  └─ Performance/
│  │
│  ├─ LiveWallpaperStudio.Renderers/
│  │  ├─ Image/
│  │  ├─ Video/
│  │  ├─ Web/
│  │  └─ Common/
│  │
│  ├─ LiveWallpaperStudio.Data/
│  │  ├─ Database/
│  │  ├─ Config/
│  │  └─ Repositories/
│  │
│  └─ LiveWallpaperStudio.SetupUi/
│     └─ ทำทีหลังสำหรับ custom installer
│
├─ installer/
│  └─ LiveWallpaperStudio.Installer/
│
├─ assets/
│  ├─ icons/
│  ├─ default-wallpapers/
│  └─ mockups/
│
├─ docs/
│  ├─ requirements.md
│  ├─ architecture.md
│  └─ release-plan.md
│
└─ build/
   ├─ publish/
   └─ setup/
```

## Component Responsibilities

### Main App

```text
- แสดง UI หลัก
- จัดการ navigation
- ควบคุม settings
- แสดง library
- ส่งคำสั่งให้ Wallpaper Engine
- ควบคุม tray menu
```

### Wallpaper Engine

```text
- สร้าง renderer window
- attach renderer window เข้า desktop layer
- จัดการหลายจอ
- start / stop / pause / resume wallpaper
- ตรวจ display change
- recover เมื่อ Explorer restart
```

### Renderer

```text
- แสดงภาพ/วิดีโอ/web content
- scale content ให้เต็ม window
- คุม playback
- mute/volume
- FPS limit ตามที่ engine กำหนด
```

### Data Layer

```text
- config.json
- library.db
- thumbnail cache
- log files
- monitor mapping
```

### Installer

```text
- copy files ไป Program Files
- สร้าง Start Menu shortcut
- สร้าง Desktop shortcut
- ตั้งค่า Start with Windows
- ลงทะเบียน uninstaller
- uninstall และ cleanup
```

## Data Storage

### Install Directory

```text
C:\Program Files\Live Wallpaper Studio\
```

เก็บไฟล์โปรแกรม:

```text
- EXE
- DLL
- assets default
- renderer dependencies
- uninstaller
```

### User Data Directory

```text
%AppData%\LiveWallpaperStudio\
```

เก็บข้อมูลผู้ใช้:

```text
- config.json
- library.db
- logs/
- cache/
- thumbnails/
- playlists/
```

### Wallpaper File Strategy

MVP แนะนำ:

```text
เก็บ path อ้างอิงไฟล์ wallpaper เดิม
ไม่ copy ไฟล์เข้ามาใน app โดยอัตโนมัติ
```

ทำทีหลัง:

```text
Import into managed library
```

## Database Model เบื้องต้น

```text
Wallpapers
- Id
- Name
- FilePath
- Type
- ThumbnailPath
- Duration
- Width
- Height
- FileSize
- DateAdded
- IsFavorite

MonitorProfiles
- Id
- MonitorDeviceId
- WallpaperId
- ScaleMode
- Volume
- FpsLimit

Playlists
- Id
- Name
- Shuffle
- IntervalMinutes

PlaylistItems
- PlaylistId
- WallpaperId
- SortOrder
```
