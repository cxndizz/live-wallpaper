# MVP Acceptance Criteria

โปรแกรม MVP ถือว่าผ่านถ้าทำได้ตามรายการนี้

## Main App

```text
1. เปิดโปรแกรมแล้วเห็น Main UI แบบ sidebar
2. มีหน้า Home, Library, Displays, Performance, Settings
3. Add wallpaper จากไฟล์ได้
4. Drag & drop wallpaper ได้
5. แสดง thumbnail ใน Library ได้
6. Search wallpaper ได้อย่างน้อยตามชื่อ
7. Set as Wallpaper ได้จาก UI
```

## Wallpaper Engine

```text
1. ตั้งภาพนิ่งเป็น wallpaper ได้
2. ตั้ง MP4 เป็น live wallpaper ได้
3. wallpaper อยู่หลัง desktop icons
4. ไม่มีแถบขาว/title bar บน wallpaper
5. desktop icons ยังคลิกได้
6. right-click desktop ได้
7. taskbar ใช้งานได้ปกติ
8. renderer window ไม่โผล่ใน taskbar
9. renderer window ไม่โผล่ใน Alt+Tab
10. กด Exit แล้วปิด renderer จริง
```

## Scaling

```text
1. wallpaper เต็มจอด้วย Fill / Cover
2. มี Fit / Contain เป็น option
3. ไม่ใช้ work area แทน monitor area
4. จอที่มี taskbar ยังมี wallpaper เต็มหลัง taskbar
5. เปลี่ยน resolution แล้ว resize ใหม่ได้
```

## Multi-monitor

```text
1. ตรวจจับ monitor ได้อย่างน้อย 2 จอ
2. แสดง resolution ของแต่ละจอ
3. ใช้ wallpaper เดียวกันทุกจอได้
4. ใช้ wallpaper ต่างกันต่อจอได้
5. ถอด/เสียบจอแล้วไม่ crash
```

## Performance

```text
1. Pause/Resume ได้จาก UI
2. Pause/Resume ได้จาก tray
3. ตั้ง FPS 15 / 30 / 60 ได้
4. Default เป็น Balanced + 30 FPS
5. Pause เมื่อ fullscreen app ทำงานได้
```

## Tray

```text
1. มี tray icon
2. กด X แล้วซ่อนไป tray
3. wallpaper ยังเล่นต่อหลังปิด MainWindow
4. Tray menu มี Open App
5. Tray menu มี Pause/Resume
6. Tray menu มี Exit
```

## Settings

```text
1. Start with Windows เปิด/ปิดได้
2. Minimize to tray เปิด/ปิดได้
3. Keep wallpaper running when app window is closed
4. Restart wallpaper engine ได้
5. Reattach to desktop ได้
```

## Installer / Uninstaller

```text
1. ติดตั้งผ่าน Setup.exe ได้
2. ติดตั้งลง Program Files ได้
3. สร้าง Desktop shortcut ได้
4. สร้าง Start Menu shortcut ได้
5. ตั้ง Start with Windows จาก installer ได้
6. uninstall แล้วลบ app files ได้
7. uninstall แล้วลบ startup entry ได้
8. uninstall แล้วไม่ลบ wallpaper ส่วนตัวโดยไม่ตั้งใจ
```

## Reliability

```text
1. Restart app แล้วจำ wallpaper เดิมได้
2. Restart เครื่องแล้ว restore wallpaper เดิมได้ ถ้าเปิด startup
3. Explorer restart แล้ว reattach ได้ในเวอร์ชัน stable
4. Renderer crash แล้วไม่ทำให้ Main App พัง
5. มี log file สำหรับ debug
```

## UX

```text
1. ผู้ใช้เพิ่ม wallpaper แรกได้ภายในไม่กี่คลิก
2. ข้อความ error อ่านเข้าใจได้
3. UI สอดคล้องกับ dark/neon mockup
4. ปุ่มสำคัญชัดเจน เช่น Pause, Change, Set as Wallpaper
5. ค่า default ใช้งานได้โดยไม่ต้องปรับเยอะ
```
