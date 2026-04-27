# Feature Analysis

## ฟีเจอร์หลักที่ควรมีในเวอร์ชันแรก

| หมวด | ฟีเจอร์ที่ควรมี | เหตุผล |
|---|---|---|
| ตั้งวอลเปเปอร์ | รองรับภาพนิ่งและวิดีโอ เช่น JPG/PNG/MP4/WebM | เป็นความคาดหวังพื้นฐานของ Live Wallpaper |
| การเล่น | loop, pause/resume, mute, fit/fill/stretch/crop | ควบคุมเหมือน media player แต่ต้องไม่รก |
| Multi-monitor | ตั้งภาพเดียวทุกจอ, แยกภาพแต่ละจอ, span ข้ามหลายจอ | กลุ่มผู้ใช้สายแต่ง desktop มักใช้หลายจอ |
| Performance mode | จำกัด FPS, pause fullscreen, pause เกม, pause ตอนใช้แบต | ลดความกังวลเรื่องกิน CPU/GPU |
| Library | คลัง wallpaper, favorite, search, thumbnail preview | ทำให้จัดการ wallpaper จำนวนมากได้ง่าย |
| Playlist | สลับ wallpaper ตามเวลา, shuffle, schedule | ทำให้ app เป็น manager ไม่ใช่แค่ player |
| System tray | pause, เปลี่ยน wallpaper, เปิด settings, exit | ควบคุมได้เร็วโดยไม่ต้องเปิดหน้าต่างหลัก |
| Startup | เปิดพร้อม Windows, จำค่าเดิม, restore wallpaper | ผู้ใช้คาดหวังว่า restart แล้ว wallpaper กลับมา |

## ฟีเจอร์ที่ควรให้ความสำคัญมากเป็นพิเศษ

### 1. Smart Pause / Performance Rules

อันนี้สำคัญที่สุด เพราะผู้ใช้กลัวว่า live wallpaper จะทำให้เครื่องช้า เกมกระตุก พัดลมดัง หรือแบตหมดเร็ว

ควรมี rule เช่น:

```text
- Pause เมื่อเปิด fullscreen app
- Pause เมื่อเข้าเกม
- Pause เมื่อใช้แบตเตอรี่
- Pause เมื่อหน้าจอล็อก
- Pause เมื่อใช้ Remote Desktop
- Pause เฉพาะจอที่มี fullscreen app
```

### 2. Multi-monitor ที่ละเอียดจริง

ไม่ใช่แค่รองรับหลายจอ แต่ควรทำได้:

```text
- จอ 1 ใช้ wallpaper A
- จอ 2 ใช้ wallpaper B
- ใช้ wallpaper เดียวทุกจอ
- span ข้ามหลายจอ
- จำ profile ตาม display layout
```

สำหรับ MVP แนะนำทำ renderer แยกต่อจอก่อน เพราะควบคุมง่ายกว่า span

### 3. Library + Playlist + Schedule

ถ้าผู้ใช้ต้องเลือกไฟล์เองทุกครั้ง แอปจะดูเหมือน tool ทดลอง ควรมีคลัง:

```text
- Thumbnail grid
- Search
- Favorite
- Recently used
- Tag/category ในอนาคต
- Playlist
- Schedule กลางวัน/กลางคืน
```

## ฟีเจอร์ขั้นต่อไปที่ทำให้ดูพรีเมียม

| ฟีเจอร์ | ควรทำเมื่อไหร่ | คุณค่า |
|---|---:|---|
| Web wallpaper | หลัง MVP เสถียร | ใช้ HTML/CSS/JS, dashboard, interactive wallpaper ได้ |
| Audio visualizer | หลังรองรับ video ดีแล้ว | wallpaper ขยับตามเพลง |
| Wallpaper editor | ระยะกลางถึงยาว | ให้ผู้ใช้สร้างเอง |
| Effect presets | ระยะกลาง | blur, color filter, brightness, contrast |
| Depth/parallax | หลังมี editor | แปลงภาพนิ่งให้ดูมีมิติ |
| Community gallery | หลังมีฐานผู้ใช้ | แชร์/ดาวน์โหลด wallpaper pack |
| Import URL | ระวัง security | ใช้ remote content แต่ต้อง sandbox ดี |

## ฟีเจอร์ที่ยังไม่ควรทำใน MVP

```text
- Online marketplace
- Account login
- Cloud sync
- Wallpaper editor เต็มรูปแบบ
- Shader engine
- Plugin system
- Remote web wallpaper
- User-generated content gallery
```

เหตุผลคือจะเพิ่มความซับซ้อนด้าน performance, security, moderation และ support มากเกินไปก่อน engine หลักเสถียร

## MVP ที่แนะนำ

```text
1. ตั้งวิดีโอ/ภาพนิ่งเป็น wallpaper
2. Library พร้อม thumbnail
3. Multi-monitor แบบ same/per-monitor
4. Pause when fullscreen
5. FPS limit และ performance preset
6. System tray control
7. Autostart with Windows
8. Drag-and-drop import
9. Fill / Cover และ Fit / Contain
10. Installer / Uninstaller พื้นฐาน
```
