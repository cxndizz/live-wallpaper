# Wallpaper Engine & Desktop Embedding

## แนวคิดสำคัญ

มี 2 กรณีที่ต้องแยกกัน:

```text
ภาพนิ่ง:
ใช้ Windows API ตั้ง wallpaper จริงของระบบ

Live Wallpaper:
สร้าง window renderer ของโปรแกรมเอง แล้วฝังไว้หลัง desktop icons
```

ถ้าเป็นภาพนิ่ง โปรแกรมสามารถเรียก Windows API เพื่อตั้ง wallpaper ได้โดยตรง

แต่ถ้าเป็น live wallpaper เช่น MP4, WebM, GIF, HTML, WebGL หรือ shader ส่วนใหญ่จะไม่ได้ตั้งเป็น wallpaper ของ Windows ตรง ๆ แต่จะใช้วิธี:

```text
สร้าง borderless renderer window
↓
render video/web/image ข้างใน
↓
ฝัง window เข้า desktop layer
↓
ทำให้ window อยู่หลัง desktop icons
```

## Window Layer ที่ต้องการ

เป้าหมายคือ:

```text
Layer 4: หน้าต่างโปรแกรมทั่วไป
Layer 3: Desktop icons / SysListView32
Layer 2: Live wallpaper renderer window
Layer 1: Windows wallpaper/background เดิม
```

ดังนั้น desktop icons ต้องยังอยู่ข้างหน้า wallpaper และคลิกได้ตามปกติ

## โครงสร้าง Desktop โดยทั่วไป

Windows desktop icons มักเกี่ยวกับ window/control เช่น:

```text
Progman
 └─ SHELLDLL_DefView
     └─ SysListView32  ← desktop icons
```

บางกรณีจะมี WorkerW:

```text
Progman
WorkerW
 └─ SHELLDLL_DefView
     └─ SysListView32  ← desktop icons
WorkerW                 ← layer ว่างที่มักใช้วาง live wallpaper
```

โปรแกรม live wallpaper มักใช้เทคนิค WorkerW / Progman เพื่อวาง renderer window ไว้หลัง desktop icons

## Renderer Window

ห้ามใช้หน้าต่างหลักของโปรแกรมเป็น wallpaper window

ต้องแยก:

```text
Main App Window
- มี title bar ได้
- ใช้เป็น settings/library UI
- ปิดแล้วซ่อนไป tray ได้

Wallpaper Renderer Window
- ไม่มี title bar
- ไม่มี border
- ไม่อยู่ taskbar
- ไม่โผล่ Alt+Tab
- ไม่แย่ง focus
- ถูกฝังหลัง desktop icons
```

## สาเหตุของแถบขาวด้านบน

ถ้าเห็นแถบขาว/title bar แปลว่า renderer window ยังเป็นหน้าต่างปกติอยู่

ต้องเอา window style กลุ่มนี้ออก:

```text
WS_CAPTION
WS_THICKFRAME
WS_SYSMENU
WS_MINIMIZEBOX
WS_MAXIMIZEBOX
WS_OVERLAPPEDWINDOW
```

และหลังเปลี่ยน style ต้อง refresh frame ใหม่ ไม่งั้น non-client area อาจค้างอยู่

## Scaling / Adjust ตามจอ

ต้องจัดการ 2 ชั้น:

```text
1. Wallpaper renderer window
   ต้องมีขนาดเท่า monitor หรือ virtual screen

2. Content ข้างใน
   ต้อง scale ตาม mode เช่น Fill / Cover / Fit / Stretch / Center
```

ถ้า window เต็มจอแต่ video ข้างในไม่ fill จะเห็นว่า wallpaper ไม่ adjust เอง

## Scale Modes

| Mode | พฤติกรรม | เหมาะกับ |
|---|---|---|
| Fill / Cover | เต็มจอ ไม่เหลือขอบ แต่อาจ crop | live wallpaper ทั่วไป |
| Fit / Contain | เห็นภาพครบ แต่อาจมีขอบ | ภาพที่ไม่อยากถูกตัด |
| Stretch | ยืดเต็มจอ ภาพอาจเพี้ยน | กรณีผู้ใช้ยอมให้ภาพบิด |
| Center | วางกลาง ไม่ scale | pixel art / ภาพเล็ก |

ค่า default ที่แนะนำ:

```text
Fill / Cover
```

## Multi-monitor Strategy

MVP แนะนำใช้ renderer window แยกต่อจอ:

```text
Monitor 1 → Renderer Window 1
Monitor 2 → Renderer Window 2
Monitor 3 → Renderer Window 3
```

ข้อดี:

```text
- จัดขนาดง่าย
- รองรับ wallpaper ต่างกันต่อจอ
- รองรับ DPI ต่อจอได้ดีกว่า
- pause เฉพาะจอได้
- debug ง่ายกว่า span mode
```

Span mode ข้ามทุกจอทำทีหลัง:

```text
Virtual Screen → Renderer Window เดียว
```

เพราะต้องจัดการ coordinate ติดลบ, aspect ratio, monitor layout, DPI และการ crop ที่ซับซ้อนกว่า

## เหตุการณ์ที่ต้อง handle

```text
- Explorer restart
- display resolution change
- ต่อจอเพิ่ม
- ถอดจอ
- เปลี่ยน primary monitor
- เปลี่ยน DPI scaling
- sleep/wake
- lock/unlock screen
- fullscreen app/game
- renderer crash
```

เมื่อเกิดเหตุการณ์เหล่านี้ โปรแกรมต้อง:

```text
- re-detect monitor layout
- resize renderer window
- reattach to WorkerW ถ้าจำเป็น
- pause/resume ตาม rule
```

## Engine Lifecycle

```text
Start
↓
Create renderer window
↓
Remove border/title bar
↓
Find desktop host / WorkerW
↓
Attach renderer window
↓
Resize to monitor
↓
Start renderer playback
```

Pause/Resume:

```text
Manual pause
Fullscreen pause
Game pause
Battery pause
Lock screen pause
```

Stop:

```text
Stop playback
Destroy renderer windows
Cleanup handles
Restore state if needed
```

## Acceptance Criteria ด้าน Desktop Embedding

โปรแกรมถือว่าทำ desktop embedding ถูก ถ้า:

```text
- wallpaper อยู่หลัง desktop icons
- ไม่เห็น title bar / border
- desktop icons คลิกได้
- right-click desktop ได้
- taskbar ใช้งานได้
- renderer window ไม่โผล่ใน Alt+Tab
- renderer window ไม่โผล่ใน taskbar
- app window ปิดแล้ว wallpaper ยังเล่นต่อ
- Exit จาก tray แล้ว renderer หายจริง
```
