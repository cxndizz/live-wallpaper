# UI Design Spec

## รูป mockup หลัก

![Main App UI](../img/01-main-app-ui-mockup.png)

## แนวทางหน้าจอ

โปรแกรมควรแยกเป็น 2 ส่วนใหญ่ ๆ:

```text
1. Main App Window
   หน้าจอที่ผู้ใช้เห็น ใช้เลือก wallpaper / ตั้งค่า / จัดการหลายจอ

2. Wallpaper Renderer Window
   หน้าต่างพิเศษที่ไม่มีกรอบ ไม่มีแถบด้านบน
   ถูกฝังไว้หลัง desktop icons
   ผู้ใช้ไม่เห็นว่าเป็นหน้าต่างโปรแกรม
```

พูดง่าย ๆ:

```text
หน้าจอโปรแกรม = ตัวควบคุม
หน้าจอ wallpaper = ตัวแสดงผลจริง
```

## Main App Layout

แนะนำใช้ sidebar ซ้าย + content ขวา:

```text
┌──────────────────────────────────────────────────┐
│ Live Wallpaper Studio                            │
├──────────────┬───────────────────────────────────┤
│ Home         │                                   │
│ Library      │         Main Content              │
│ Displays     │                                   │
│ Performance  │                                   │
│ Settings     │                                   │
└──────────────┴───────────────────────────────────┘
```

ข้อดี:

```text
- เข้าใจง่าย
- ขยายต่อได้
- เหมาะกับ desktop app
- แยก settings เป็นระบบ
```

## Page 1: Home Dashboard

หน้าแรกควรบอกผู้ใช้ทันทีว่าตอนนี้กำลังใช้ wallpaper อะไรอยู่

```text
Current Wallpaper
┌────────────────────────────────────┐
│            Preview ใหญ่             │
│         วิดีโอ / ภาพตัวอย่าง        │
└────────────────────────────────────┘

ชื่อ wallpaper: Cyber Rain.mp4
สถานะ: Playing
จอที่ใช้: Monitor 1, Monitor 2
โหมดแสดงผล: Fill / Cover
FPS: 30
Performance Mode: Balanced

[Pause] [Change Wallpaper] [Open Library]
```

ควรมี:

| ส่วน | หน้าที่ |
|---|---|
| Preview | ให้เห็นตัวอย่าง wallpaper |
| Current status | Playing / Paused / Error |
| Quick actions | Pause, Resume, Change, Remove |
| Monitor info | ใช้กับจอไหนอยู่ |
| Performance info | FPS, preset |

## Page 2: Library

หน้าจอคลัง wallpaper

```text
Library

[ + Add Wallpaper ] [ Search...             ]

┌──────────┐ ┌──────────┐ ┌──────────┐
│ Preview  │ │ Preview  │ │ Preview  │
│ Rain     │ │ Anime    │ │ Space    │
└──────────┘ └──────────┘ └──────────┘
```

ฟีเจอร์:

```text
- Add Wallpaper
- Drag & Drop
- Thumbnail preview
- Search
- Favorite
- Remove from library
- Open file location
```

เมื่อคลิก wallpaper ควรเปิด detail panel:

```text
Wallpaper Detail

Preview ใหญ่
Name
Type
Resolution
Duration
Size

Display Mode:
- Fill / Cover
- Fit / Contain
- Stretch
- Center

Apply to:
- Monitor 1
- Monitor 2
- All monitors

[Set as Wallpaper] [Add to Playlist] [Favorite]
```

## Page 3: Displays

หน้านี้ใช้จัดการหลายจอและ scale mode

```text
Displays

┌───────────────┐   ┌───────────────┐
│ Monitor 1     │   │ Monitor 2     │
│ 1920x1080     │   │ 2560x1440     │
│ Primary       │   │               │
└───────────────┘   └───────────────┘

Wallpaper Mode:
(•) Same wallpaper on all monitors
( ) Different wallpaper per monitor
( ) Span one wallpaper across all monitors
```

ตัวเลือกต่อจอ:

```text
Monitor 1
Wallpaper: Cyber Rain.mp4
Scale: Fill / Cover
FPS: 30

[Change] [Preview] [Remove]
```

ค่าเริ่มต้นที่แนะนำ:

```text
Mode: Same wallpaper on all monitors
Scale: Fill / Cover
```

## Page 4: Performance

ควรใช้ preset ให้คนทั่วไปเข้าใจง่าย

```text
Performance

Mode:
( ) Eco
(•) Balanced
( ) Quality
( ) Custom
```

Preset:

```text
Eco
- FPS ต่ำ
- pause เมื่อใช้แบตเตอรี่
- pause เมื่อมีโปรแกรม fullscreen

Balanced
- FPS ปานกลาง
- pause ตอนเล่นเกม/fullscreen
- เหมาะกับการใช้งานทั่วไป

Quality
- FPS สูง
- ภาพลื่นที่สุด
- เหมาะกับเครื่องแรง
```

ตัวเลือก:

```text
[✓] Pause when fullscreen app is running
[✓] Pause when game is running
[✓] Pause when laptop is on battery
[✓] Pause when screen is locked
[ ] Mute wallpaper audio by default

FPS Limit:
( ) 15
(•) 30
( ) 60
```

## Page 5: Settings

```text
General

[✓] Start with Windows
[✓] Minimize to system tray
[✓] Keep wallpaper running when app window is closed
[ ] Show desktop notification

Theme:
(•) System
( ) Light
( ) Dark

Language:
Thai / English
```

Advanced:

```text
[Restart Wallpaper Engine]
[Reattach to Desktop]
[Clear Thumbnail Cache]
[Reset Settings]
[Export Diagnostics Log]
[Open Data Folder]
```

## System Tray Menu

เมื่อผู้ใช้กดปิดหน้าต่างหลัก โปรแกรมควรไปอยู่ system tray

เมนู tray:

```text
Open App
Pause Wallpaper
Next Wallpaper
Mute
Exit
```

พฤติกรรม:

```text
กด X ที่ Main Window
→ ซ่อนหน้าต่าง
→ wallpaper ยังเล่นต่อ
→ tray icon ยังอยู่

กด Exit ที่ tray
→ ปิด renderer
→ cleanup
→ ปิดโปรแกรมจริง
```

## First-run / Welcome Flow

```text
1. เปิดหน้า Welcome
2. ให้ Add Wallpaper
3. เลือกไฟล์วิดีโอ/ภาพ
4. แสดง preview
5. เลือก monitor
6. เลือก scale mode
7. กด Set as Wallpaper
8. Main window ซ่อนได้
9. Wallpaper ยังอยู่บน desktop
```

## สิ่งที่ไม่ควรทำ

```text
- ไม่ควรให้หน้าจอเดียวมีทุก setting จนรก
- ไม่ควรใช้ MainWindow เป็น wallpaper
- ไม่ควรให้ renderer window โผล่ใน taskbar
- ไม่ควรให้ผู้ใช้ต้องตั้งค่าหลายอย่างก่อนใช้ครั้งแรก
```
