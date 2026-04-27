# MVP Roadmap

## Phase 1 — Core Prototype

เป้าหมายคือพิสูจน์ว่า engine หลักทำงานได้จริง

```text
- WPF MainWindow
- Borderless WallpaperRendererWindow
- WorkerW attach
- Image wallpaper
- MP4 wallpaper
- Fill / Cover
- Manual pause/resume
```

Acceptance:

```text
- ตั้ง wallpaper ได้
- wallpaper อยู่หลัง desktop icons
- ไม่มี title bar
- icons คลิกได้
- window เต็มจอ
```

## Phase 2 — MVP App

เพิ่มส่วนที่ทำให้โปรแกรมใช้งานจริงได้

```text
- Library
- Thumbnail cache
- Add / remove wallpaper
- Multi-monitor same/per-monitor
- Displays page
- Performance page
- Settings page
- Tray icon
- Pause fullscreen
- FPS limit
```

Acceptance:

```text
- ผู้ใช้เพิ่ม wallpaper ผ่าน UI ได้
- ตั้ง wallpaper ต่างกันต่อจอได้
- ปิด MainWindow แล้ว wallpaper ยังเล่น
- Pause/Resume จาก tray ได้
- Restart app แล้วจำ wallpaper เดิมได้
```

## Phase 3 — Stable Release

ทำให้พร้อมปล่อย beta/public

```text
- Explorer restart recovery
- Display change handling
- Better video renderer
- Logs
- Diagnostics
- Auto-start
- Inno Setup installer
- Uninstall options
```

Acceptance:

```text
- restart Explorer แล้ว reattach ได้
- ถอด/เสียบจอแล้ว layout ไม่พัง
- installer/uninstaller ทำงานได้
- มี diagnostics log
```

## Phase 4 — Premium Features

ฟีเจอร์เพิ่มมูลค่า

```text
- Web wallpaper with WebView2
- Playlist
- Schedule
- Effects
- Audio visualizer
- Custom WPF installer UI
- Wallpaper health score
- One-click optimize
```

## Phase 5 — Creator / Community

ทำทีหลังเมื่อ core เสถียรและมีผู้ใช้แล้ว

```text
- Wallpaper editor
- Template system
- Community gallery
- Rating/download
- Safe pack validation
- Cloud sync
```

## สิ่งที่ควรเลี่ยงในช่วงแรก

```text
- Marketplace
- Account login
- Remote URL wallpaper
- Plugin system
- Shader editor
- Online community
```

เหตุผล:

```text
- งานเยอะ
- security สูง
- moderation สูง
- ทำให้ engine หลักเสร็จช้า
```
