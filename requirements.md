# Requirements — Live Wallpaper Studio

เอกสารนี้คือ requirements หลักของโปรแกรม **Live Wallpaper Studio** ตาม stack ที่เลือกไว้:

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

---

## 1. Product Requirements

### 1.1 เป้าหมายของโปรแกรม

โปรแกรมต้องเป็น Windows desktop app สำหรับตั้งและจัดการ Live Wallpaper โดยเน้น 4 เรื่องหลัก:

```text
1. สวย
2. ใช้ง่าย
3. ไม่บัง desktop icons
4. ไม่กินเครื่องเกินจำเป็น
```

ตัวโปรแกรมต้องแยกเป็น 2 ส่วนชัดเจน:

```text
Main App Window
- หน้าจอควบคุม
- ใช้เลือก wallpaper
- ตั้งค่า monitor
- ตั้งค่า performance
- เปิด/ปิด/ซ่อนลง system tray ได้

Wallpaper Renderer Window
- หน้าต่างพิเศษสำหรับแสดง wallpaper
- ไม่มี title bar
- ไม่มี border
- ไม่อยู่ taskbar
- ไม่โผล่ Alt+Tab
- ฝังไว้หลัง desktop icons
```

---

## 2. Target Platform Requirements

| รายการ | Requirement |
|---|---|
| OS หลัก | Windows 10 / Windows 11 |
| Architecture | x64 เป็นหลัก |
| Permission | ใช้งานทั่วไปไม่ควรต้อง Run as Administrator |
| Installer | มี Setup.exe และ Uninstaller |
| Startup | เลือกเปิดพร้อม Windows ได้ |
| Multi-monitor | ต้องรองรับตั้งแต่ MVP |
| Offline usage | โปรแกรมหลักต้องใช้ได้โดยไม่ต้องต่ออินเทอร์เน็ต |

---

## 3. Recommended Tech Requirements

### 3.1 Main App

| ส่วน | ใช้อะไร |
|---|---|
| Language | C# |
| Runtime | .NET |
| UI Framework | WPF |
| UI Markup | XAML |
| Pattern | MVVM |
| Theme | Custom dark theme |
| Local data | SQLite + JSON config |
| Tray | Windows system tray integration |

ข้อกำหนดสำคัญ:

```text
WPF ใช้ทำหน้าจอสวยแบบ mockup:
- Sidebar
- Card layout
- Dark theme
- Thumbnail grid
- Toggle switch
- Installer style screens
```

### 3.2 Wallpaper Engine

| ส่วน | ใช้อะไร |
|---|---|
| Desktop embedding | Win32 API |
| WorkerW / Progman handling | C# Win32 interop |
| Window management | Native HWND |
| Monitor detection | Win32 monitor APIs |
| DPI handling | Per-monitor DPI aware |
| Renderer window | Borderless native/WPF/Win32 hosted window |

ข้อกำหนดสำคัญ:

```text
ห้ามใช้ MainWindow เป็น wallpaper window
ต้องมี WallpaperRendererWindow แยกต่างหาก
```

### 3.3 Renderer

ควรแยก renderer เป็นหลายชนิดตั้งแต่ใน architecture แม้ MVP จะทำแค่บางตัวก่อน:

```text
IRenderer
├─ ImageRenderer
├─ VideoRenderer
├─ GifRenderer
├─ WebRenderer
└─ FutureShaderRenderer
```

| ประเภท Wallpaper | Renderer ที่แนะนำ |
|---|---|
| JPG / PNG / BMP | ImageRenderer |
| MP4 / WebM | VideoRenderer |
| GIF / WebP animated | AnimationRenderer |
| HTML / CSS / JS | WebView2 |
| Shader / visualizer | ทำทีหลัง |

---

## 4. Functional Requirements

### 4.1 Home Screen

หน้าจอ Home ต้องแสดงสถานะ wallpaper ปัจจุบัน

| ID | Requirement | Priority |
|---|---|---|
| HOME-01 | แสดง preview wallpaper ปัจจุบัน | P0 |
| HOME-02 | แสดงชื่อไฟล์ wallpaper | P0 |
| HOME-03 | แสดงสถานะ Playing / Paused / Error | P0 |
| HOME-04 | แสดง monitor ที่กำลังใช้งาน | P0 |
| HOME-05 | แสดง scale mode เช่น Fill / Cover | P0 |
| HOME-06 | มีปุ่ม Pause / Resume | P0 |
| HOME-07 | มีปุ่ม Change Wallpaper | P0 |
| HOME-08 | มีปุ่ม Open Library | P1 |
| HOME-09 | แสดง performance mode ปัจจุบัน | P1 |

โครงหน้า:

```text
Home
├─ Large Preview
├─ Current Wallpaper Card
├─ Pause Button
├─ Change Button
└─ Open Library Button
```

### 4.2 Library Screen

หน้าจอ Library ใช้จัดการ wallpaper ทั้งหมด

| ID | Requirement | Priority |
|---|---|---|
| LIB-01 | เพิ่ม wallpaper จากไฟล์ได้ | P0 |
| LIB-02 | Drag & drop ไฟล์เข้ามาได้ | P0 |
| LIB-03 | แสดง wallpaper เป็น grid card | P0 |
| LIB-04 | แสดง thumbnail preview | P0 |
| LIB-05 | Search wallpaper ได้ | P1 |
| LIB-06 | Favorite wallpaper ได้ | P1 |
| LIB-07 | Remove from library ได้ | P0 |
| LIB-08 | เปิด file location ได้ | P1 |
| LIB-09 | แสดงชนิดไฟล์ วิดีโอ/ภาพ/GIF/Web | P1 |
| LIB-10 | แสดง resolution / duration / file size | P1 |
| LIB-11 | ทำ playlist ได้ | P2 |
| LIB-12 | Tag/category ได้ | P2 |

ไฟล์ที่ควรรองรับใน MVP:

```text
ภาพนิ่ง:
.jpg, .jpeg, .png, .bmp

วิดีโอ:
.mp4, .webm

ทำทีหลัง:
.gif, .webp, .html, .url
```

### 4.3 Wallpaper Detail Screen

เมื่อกด wallpaper card ควรมี detail panel หรือ detail page

| ID | Requirement | Priority |
|---|---|---|
| WPD-01 | แสดง preview ใหญ่ | P0 |
| WPD-02 | แสดงชื่อ wallpaper | P0 |
| WPD-03 | เลือก Apply to monitor ได้ | P0 |
| WPD-04 | เลือก scale mode ได้ | P0 |
| WPD-05 | Set as Wallpaper ได้ | P0 |
| WPD-06 | Add to Favorite ได้ | P1 |
| WPD-07 | Add to Playlist ได้ | P2 |
| WPD-08 | Rename display name ได้ | P2 |

Scale mode ที่ต้องมี:

```text
Fill / Cover   : เต็มจอ อาจ crop
Fit / Contain  : เห็นครบ อาจมีขอบ
Stretch        : ยืดเต็มจอ ภาพอาจเพี้ยน
Center         : วางกลาง ไม่ขยาย
```

ค่า default:

```text
Fill / Cover
```

---

## 5. Display Requirements

หน้าจอ Displays สำคัญมาก เพราะแก้ปัญหาเรื่อง virtual screen และหลายจอ

### 5.1 Monitor Detection

| ID | Requirement | Priority |
|---|---|---|
| DSP-01 | ตรวจจับจำนวน monitor ได้ | P0 |
| DSP-02 | แสดงชื่อ Monitor 1, Monitor 2 ฯลฯ | P0 |
| DSP-03 | แสดง resolution ของแต่ละจอ | P0 |
| DSP-04 | แสดงว่า monitor ไหนเป็น primary | P1 |
| DSP-05 | รองรับจอพิกัดติดลบ | P0 |
| DSP-06 | รองรับจอแนวตั้ง/แนวนอน | P1 |
| DSP-07 | รองรับ DPI scaling ต่างกันต่อจอ | P1 |

### 5.2 Wallpaper Mode

ต้องมีอย่างน้อย 3 โหมด:

| Mode | Requirement | Priority |
|---|---|---|
| Same wallpaper on all monitors | ใช้ wallpaper เดียวกันทุกจอ | P0 |
| Different wallpaper per monitor | แต่ละจอใช้ wallpaper ต่างกัน | P0 |
| Span one wallpaper across all monitors | wallpaper เดียวข้ามทุกจอ | P1 |

MVP แนะนำทำก่อน 2 โหมด:

```text
1. Same wallpaper on all monitors
2. Different wallpaper per monitor
```

ส่วน Span ข้ามทุกจอทำทีหลัง เพราะ virtual screen, negative coordinate และ aspect ratio จะยุ่งกว่า

### 5.3 Renderer Window Per Monitor

MVP ควรใช้ renderer window แยกต่อจอ:

```text
Monitor 1 → WallpaperRendererWindow 1
Monitor 2 → WallpaperRendererWindow 2
Monitor 3 → WallpaperRendererWindow 3
```

ข้อกำหนด:

| ID | Requirement | Priority |
|---|---|---|
| RWIN-01 | Renderer window ต้องไม่มี border | P0 |
| RWIN-02 | Renderer window ต้องไม่มี title bar | P0 |
| RWIN-03 | Renderer window ต้องไม่แสดงใน taskbar | P0 |
| RWIN-04 | Renderer window ต้องไม่แย่ง focus | P0 |
| RWIN-05 | Renderer window ต้องอยู่หลัง desktop icons | P0 |
| RWIN-06 | Renderer window ต้อง resize ตาม monitor | P0 |
| RWIN-07 | Renderer window ต้อง reattach หลัง Explorer restart | P1 |
| RWIN-08 | Renderer window ต้อง recreate เมื่อ display layout เปลี่ยน | P1 |

---

## 6. Performance Requirements

### 6.1 Performance Presets

ต้องมี preset ให้ผู้ใช้เข้าใจง่าย

| Preset | Behavior |
|---|---|
| Eco | FPS ต่ำ, pause ตอนใช้แบตเตอรี่, ลด resource |
| Balanced | FPS กลาง, pause ตอน fullscreen/game |
| Quality | FPS สูง, ภาพลื่นสุด |
| Custom | ผู้ใช้ตั้งค่าเอง |

| ID | Requirement | Priority |
|---|---|---|
| PERF-01 | มี preset Eco / Balanced / Quality / Custom | P0 |
| PERF-02 | ตั้ง FPS limit ได้ | P0 |
| PERF-03 | ค่า FPS ต้องมี 15 / 30 / 60 | P0 |
| PERF-04 | Default เป็น Balanced + 30 FPS | P0 |
| PERF-05 | Pause เมื่อ fullscreen app ทำงาน | P0 |
| PERF-06 | Pause เมื่อ game ทำงาน | P1 |
| PERF-07 | Pause เมื่อ laptop ใช้ battery | P1 |
| PERF-08 | Pause เมื่อ lock screen | P1 |
| PERF-09 | Mute audio by default | P1 |
| PERF-10 | แสดง resource usage คร่าว ๆ | P2 |

### 6.2 Pause Rules

ต้องมีระบบ rule แบบนี้:

```text
PauseRule
├─ Fullscreen detected
├─ Game detected
├─ On battery
├─ Screen locked
├─ Remote Desktop session
└─ Manual pause
```

Priority สำหรับ MVP:

```text
P0:
- Manual pause
- Pause when fullscreen

P1:
- Pause when on battery
- Pause when screen locked
- Pause when game detected
```

---

## 7. Main App UI Requirements

### 7.1 Layout

หน้าหลักควรใช้โครงแบบ sidebar

```text
MainWindow
├─ Sidebar
│  ├─ Home
│  ├─ Library
│  ├─ Displays
│  ├─ Performance
│  └─ Settings
│
└─ Content Area
```

| ID | Requirement | Priority |
|---|---|---|
| UI-01 | ใช้ dark theme | P0 |
| UI-02 | มี blue/purple accent | P0 |
| UI-03 | Sidebar navigation | P0 |
| UI-04 | Rounded cards | P0 |
| UI-05 | Thumbnail grid | P0 |
| UI-06 | Toggle switch component | P1 |
| UI-07 | Toast notification | P1 |
| UI-08 | Loading/progress state | P1 |
| UI-09 | Empty state สำหรับ Library ว่าง | P0 |
| UI-10 | Error state เมื่อไฟล์เล่นไม่ได้ | P0 |

### 7.2 Required Pages

MVP ต้องมี 5 หน้า:

```text
1. Home
2. Library
3. Displays
4. Performance
5. Settings
```

ทำทีหลัง:

```text
6. Playlist
7. Effects
8. About
9. Help / Diagnostics
```

---

## 8. System Tray Requirements

โปรแกรมต้องอยู่ system tray ได้

| ID | Requirement | Priority |
|---|---|---|
| TRAY-01 | มี tray icon | P0 |
| TRAY-02 | กด X แล้วซ่อนไป tray ได้ | P0 |
| TRAY-03 | Wallpaper ยังเล่นต่อหลังปิด MainWindow | P0 |
| TRAY-04 | Tray menu มี Open App | P0 |
| TRAY-05 | Tray menu มี Pause / Resume | P0 |
| TRAY-06 | Tray menu มี Next Wallpaper | P1 |
| TRAY-07 | Tray menu มี Mute / Unmute | P1 |
| TRAY-08 | Tray menu มี Exit | P0 |

พฤติกรรมที่ต้องชัด:

```text
กด X:
- ซ่อน MainWindow
- ไม่ปิด process
- ไม่หยุด wallpaper

กด Exit:
- หยุด renderer
- cleanup
- ปิด process
```

---

## 9. Settings Requirements

### 9.1 General Settings

| ID | Requirement | Priority |
|---|---|---|
| SET-01 | Start with Windows | P0 |
| SET-02 | Minimize to tray | P0 |
| SET-03 | Keep wallpaper running when window is closed | P0 |
| SET-04 | Theme: System / Light / Dark | P1 |
| SET-05 | Language: English / Thai | P2 |
| SET-06 | Notification on/off | P2 |

### 9.2 Advanced Settings

| ID | Requirement | Priority |
|---|---|---|
| ADV-01 | Restart wallpaper engine | P0 |
| ADV-02 | Reattach to desktop | P0 |
| ADV-03 | Clear thumbnail cache | P1 |
| ADV-04 | Reset settings | P1 |
| ADV-05 | Export diagnostics log | P1 |
| ADV-06 | Open data folder | P1 |

ปุ่ม **Reattach to desktop** ควรมีตั้งแต่แรก เพราะช่วยแก้กรณี Explorer restart หรือ WorkerW เพี้ยน

---

## 10. Installer Requirements

### 10.1 Installer Tool

แนะนำใช้:

```text
WPF Setup UI
```

เหมาะกับการทำ `Setup.exe`, copy files, สร้าง shortcut, เขียน uninstall entry และสร้าง uninstaller จริงบน Windows

### 10.2 Installer Screens

จากรูป mockup ที่ออกแบบไว้ ควรมี flow นี้:

```text
Welcome
↓
Install Options
↓
Installing Progress
↓
Finish
```

| ID | Requirement | Priority |
|---|---|---|
| INS-01 | เลือก install location ได้ | P0 |
| INS-02 | Create desktop shortcut checkbox | P0 |
| INS-03 | Start with Windows checkbox | P0 |
| INS-04 | แสดง install progress | P0 |
| INS-05 | Cancel installation ได้ | P0 |
| INS-06 | Launch app after install checkbox | P1 |
| INS-07 | Custom dark UI แบบ mockup | P2 |

MVP ใช้ WPF Setup UI เป็น installer หลัก
Custom installer UI แบบในรูปค่อยทำภายหลังด้วย WPF

---

## 11. Uninstaller Requirements

Uninstaller ต้องทำได้มากกว่าแค่ลบไฟล์

| ID | Requirement | Priority |
|---|---|---|
| UNI-01 | ปิด running app ก่อน uninstall | P0 |
| UNI-02 | หยุด wallpaper renderer ก่อน uninstall | P0 |
| UNI-03 | ลบ app files | P0 |
| UNI-04 | ลบ desktop shortcut | P0 |
| UNI-05 | ลบ Start Menu shortcut | P0 |
| UNI-06 | ลบ startup entry | P0 |
| UNI-07 | ให้เลือกว่าจะเก็บ settings ไว้ไหม | P1 |
| UNI-08 | ให้เลือกว่าจะลบ wallpaper library ไหม | P1 |
| UNI-09 | แสดง uninstall complete screen | P1 |
| UNI-10 | Custom uninstall UI แบบ mockup | P2 |

ค่า default ที่แนะนำ:

```text
Uninstall app files: always
Keep user wallpapers: default yes
Keep settings: default yes
Delete cache: default yes
```

เหตุผลคือ wallpaper ของผู้ใช้อาจเป็นไฟล์ส่วนตัว ไม่ควรลบโดยไม่ตั้งใจ

---

## 12. Build & Release Requirements

Recommended build output:

```text
publish/
├─ LiveWallpaperStudio.exe
├─ LiveWallpaperStudio.Engine.dll
├─ LiveWallpaperStudio.Renderers.dll
├─ assets/
├─ runtimes/
└─ dependencies/
```

| ID | Requirement | Priority |
|---|---|---|
| BLD-01 | Build แบบ Release ได้ | P0 |
| BLD-02 | Publish เป็น folder สำหรับ installer | P0 |
| BLD-03 | มี version number | P0 |
| BLD-04 | มี app icon | P0 |
| BLD-05 | มี signed executable | P2 |
| BLD-06 | มี CI build script | P2 |
| BLD-07 | มี installer artifact | P1 |

ไม่จำเป็นต้องทำ single-file ตั้งแต่แรก เพราะ app นี้อาจมี renderer dependencies, native libraries, assets และ runtime files หลายตัว

---

## 13. File Storage Requirements

ควรแยกไฟล์โปรแกรมกับไฟล์ผู้ใช้ออกจากกัน

### 13.1 Install Directory

```text
C:\Program Files\Live Wallpaper Studio\
```

ใช้เก็บ:

```text
- exe
- dll
- renderer dependencies
- default assets
- uninstaller
```

### 13.2 User Data Directory

```text
%AppData%\LiveWallpaperStudio\
```

ใช้เก็บ:

```text
config.json
library.db
logs/
cache/
thumbnails/
playlists/
```

### 13.3 Wallpaper Library

แนะนำให้เริ่มด้วยวิธีนี้:

```text
ไม่ copy ไฟล์ wallpaper เข้ามาใน app โดยอัตโนมัติ
เก็บ path อ้างอิงไว้ก่อน
```

ทำทีหลังค่อยเพิ่ม option:

```text
Import into managed library
```

---

## 14. Data Requirements

### 14.1 Config

ใช้ JSON สำหรับ settings ที่อ่านง่าย

```text
config.json
- startup enabled
- theme
- performance preset
- default scale mode
- pause rules
- last selected wallpaper
- monitor mapping
```

### 14.2 Database

ใช้ SQLite สำหรับ library

ตารางหลักที่ควรมี:

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

---

## 15. Wallpaper Engine Requirements

### 15.1 Engine Lifecycle

| ID | Requirement | Priority |
|---|---|---|
| ENG-01 | Start engine | P0 |
| ENG-02 | Stop engine | P0 |
| ENG-03 | Pause engine | P0 |
| ENG-04 | Resume engine | P0 |
| ENG-05 | Restart engine | P0 |
| ENG-06 | Reattach to desktop | P0 |
| ENG-07 | Recover after renderer crash | P1 |
| ENG-08 | Recover after Explorer restart | P1 |

### 15.2 Desktop Embedding

| ID | Requirement | Priority |
|---|---|---|
| EMB-01 | หา desktop host window ได้ | P0 |
| EMB-02 | สร้าง WorkerW/หา WorkerW ที่ถูกต้องได้ | P0 |
| EMB-03 | Set parent ของ renderer window เข้า desktop layer ได้ | P0 |
| EMB-04 | Renderer ต้องอยู่หลัง desktop icons | P0 |
| EMB-05 | Desktop icons ต้องคลิกได้ตามปกติ | P0 |
| EMB-06 | Taskbar ต้องทำงานปกติ | P0 |
| EMB-07 | Wallpaper ต้องไม่บัง app อื่น | P0 |

Acceptance criteria สำคัญ:

```text
เมื่อ wallpaper เล่นอยู่:
- เห็น desktop icons
- คลิก desktop icons ได้
- ลาก icon ได้
- right-click desktop ได้
- taskbar ใช้งานได้
- Alt+Tab ไม่เห็น renderer window
```

---

## 16. Scaling Requirements

เพราะโปรแกรมแนวนี้มักเจอปัญหา wallpaper ไม่ adjust ตามหน้าจอ จึงต้องกำหนด requirement ชัดเจน

| ID | Requirement | Priority |
|---|---|---|
| SCALE-01 | Wallpaper window ต้องมีขนาดเท่า monitor จริง | P0 |
| SCALE-02 | Content ต้อง fill parent window ได้ | P0 |
| SCALE-03 | รองรับ Fill / Cover | P0 |
| SCALE-04 | รองรับ Fit / Contain | P0 |
| SCALE-05 | รองรับ Stretch | P1 |
| SCALE-06 | รองรับ Center | P1 |
| SCALE-07 | ต้อง resize เมื่อ resolution เปลี่ยน | P0 |
| SCALE-08 | ต้อง resize เมื่อเสียบ/ถอด monitor | P0 |
| SCALE-09 | ต้อง handle DPI scaling | P1 |
| SCALE-10 | ต้องไม่ใช้ work area แทน monitor area | P0 |

Default:

```text
ScaleMode = Fill / Cover
```

---

## 17. Error Handling Requirements

| ID | Requirement | Priority |
|---|---|---|
| ERR-01 | ไฟล์หายต้องแจ้งผู้ใช้ | P0 |
| ERR-02 | ไฟล์เล่นไม่ได้ต้องมี error message | P0 |
| ERR-03 | Renderer crash ต้อง restart ได้ | P1 |
| ERR-04 | WorkerW attach fail ต้อง fallback ได้ | P1 |
| ERR-05 | Display change fail ต้อง reset layout ได้ | P1 |
| ERR-06 | มี log file | P0 |
| ERR-07 | มีปุ่ม Export diagnostics | P1 |

ตัวอย่าง error ที่ควรแสดงแบบมนุษย์อ่านรู้เรื่อง:

```text
ไม่ดี:
HRESULT 0x80070002

ดี:
ไม่พบไฟล์ wallpaper นี้แล้ว กรุณาเลือกไฟล์ใหม่
```

---

## 18. Security Requirements

| ID | Requirement | Priority |
|---|---|---|
| SEC-01 | ไม่รันไฟล์ wallpaper เป็น executable | P0 |
| SEC-02 | Web wallpaper ต้อง sandbox | P1 |
| SEC-03 | ปิด file system access สำหรับ web wallpaper | P1 |
| SEC-04 | ไม่โหลด remote URL โดยไม่ขอผู้ใช้ | P1 |
| SEC-05 | ไม่เก็บข้อมูลส่วนตัวโดยไม่จำเป็น | P0 |
| SEC-06 | Log ต้องไม่เก็บ path sensitive เกินจำเป็น | P2 |

สำหรับ MVP:

```text
ยังไม่ต้องรองรับ remote web wallpaper
เริ่มจาก local file ก่อน
```

---

## 19. Non-functional Requirements

### 19.1 Performance

| ID | Requirement |
|---|---|
| NFR-PERF-01 | UI เปิดได้เร็วพอสำหรับ desktop utility |
| NFR-PERF-02 | Wallpaper ต้อง pause ได้ทันที |
| NFR-PERF-03 | ไม่ควรใช้ CPU สูงตอน idle |
| NFR-PERF-04 | ต้องมี FPS limit |
| NFR-PERF-05 | ต้องไม่ทำให้ fullscreen game กระตุกอย่างชัดเจน |
| NFR-PERF-06 | Thumbnail generation ต้องไม่ block UI |

### 19.2 Reliability

| ID | Requirement |
|---|---|
| NFR-REL-01 | Restart เครื่องแล้ว restore wallpaper เดิมได้ |
| NFR-REL-02 | Explorer restart แล้ว reattach ได้ |
| NFR-REL-03 | ถอด/เสียบจอแล้ว layout ไม่พัง |
| NFR-REL-04 | ปิด app แบบผิดปกติแล้วไม่ทิ้ง window ค้าง |
| NFR-REL-05 | Uninstall แล้วไม่เหลือ startup entry |

### 19.3 UX

| ID | Requirement |
|---|---|
| NFR-UX-01 | ผู้ใช้เพิ่ม wallpaper แรกได้ภายในไม่กี่คลิก |
| NFR-UX-02 | ค่า default ต้องใช้งานได้ดีโดยไม่ต้องปรับเยอะ |
| NFR-UX-03 | ข้อความ error ต้องเข้าใจง่าย |
| NFR-UX-04 | UI ต้องสอดคล้องกับ mockup dark/neon |
| NFR-UX-05 | ปุ่มสำคัญต้องชัด เช่น Pause, Change, Set as Wallpaper |

---

## 20. Project Structure Requirements

โครง project ที่แนะนำ:

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

---

## 21. MVP Scope

### MVP v0.1 ควรมีแค่นี้ก่อน

```text
Main App:
- Home
- Library
- Displays
- Performance
- Settings

Wallpaper:
- รองรับภาพนิ่ง
- รองรับ MP4
- Renderer window แยก
- ฝังหลัง desktop icons
- ไม่บัง icons
- Fill / Cover
- Fit / Contain

Monitor:
- Same wallpaper all monitors
- Different wallpaper per monitor

Performance:
- Pause / Resume
- FPS limit
- Pause fullscreen

Tray:
- Open App
- Pause / Resume
- Exit

Installer:
- WPF Setup UI
- Desktop shortcut
- Start with Windows
- Uninstall
```

### ยังไม่ต้องทำใน MVP

```text
- Online gallery
- Account login
- Cloud sync
- Community wallpaper
- Wallpaper editor
- Audio visualizer
- Shader wallpaper
- Marketplace
- Custom installer UI สวยเต็มรูปแบบ
```

---

## 22. Acceptance Criteria สำหรับ MVP

โปรแกรม MVP ถือว่าผ่านถ้าทำได้ตามนี้:

```text
1. เปิดโปรแกรมแล้วเห็น Main UI แบบ sidebar
2. Add wallpaper จากไฟล์ได้
3. ตั้ง MP4 เป็น live wallpaper ได้
4. wallpaper อยู่หลัง desktop icons
5. ไม่มีแถบขาว/title bar บน wallpaper
6. desktop icons ยังคลิกได้
7. wallpaper เต็มจอด้วย Fill / Cover
8. รองรับอย่างน้อย 2 monitor
9. pause/resume ได้จาก UI และ tray
10. ปิด MainWindow แล้ว wallpaper ยังเล่นต่อ
11. กด Exit แล้วปิด wallpaper renderer จริง
12. restart app แล้วจำค่า wallpaper เดิมได้
13. ติดตั้งผ่าน Setup.exe ได้
14. uninstall แล้วลบ app และ startup entry ได้
```

---

## 23. Roadmap Requirements

### Phase 1 — Core Prototype

```text
- WPF MainWindow
- Borderless WallpaperRendererWindow
- WorkerW attach
- Image wallpaper
- MP4 wallpaper
- Fill / Cover
```

### Phase 2 — MVP

```text
- Library
- Thumbnail cache
- Multi-monitor
- Tray icon
- Pause rules
- Settings
- WPF installer
```

### Phase 3 — Stable Release

```text
- Explorer restart recovery
- Display change handling
- Better video renderer
- Logs
- Diagnostics
- Auto-start
- Uninstall options
```

### Phase 4 — Premium Features

```text
- Web wallpaper with WebView2
- Playlist
- Schedule
- Effects
- Audio visualizer
- Custom WPF installer UI
```

---

## 24. Final Recommended Requirements Set

สำหรับเริ่มทำจริง ให้ล็อก requirements เวอร์ชันแรกไว้แบบนี้:

```text
Stack:
- C#
- .NET
- WPF
- XAML
- MVVM
- Win32 API interop
- SQLite
- JSON config
- WPF installer

Core Features:
- Home dashboard
- Library
- Displays
- Performance
- Settings
- Tray menu
- Wallpaper renderer window
- WorkerW desktop embed
- Image wallpaper
- MP4 wallpaper
- Multi-monitor
- Fill / Cover scaling
- Pause / Resume
- Start with Windows
- Setup.exe / Uninstall.exe

Not in MVP:
- Online gallery
- Account system
- Wallpaper editor
- Shader
- Visualizer
- Marketplace
```

โครงนี้กำลังดีสำหรับทำจริง เพราะไม่ใหญ่เกินไป แต่ฐาน architecture พร้อมต่อยอดเป็นโปรแกรมสวย ๆ แบบใน mockup
