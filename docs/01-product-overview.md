# Product Overview — Live Wallpaper Studio

## แนวคิดผลิตภัณฑ์

**Live Wallpaper Studio** คือ Windows desktop app สำหรับเปลี่ยนและจัดการ Live Wallpaper โดยเน้นประสบการณ์ที่สวย เบา และควบคุมง่าย

โปรแกรมไม่ควรเป็นแค่ “เปิดวิดีโอไว้หลัง desktop” แต่ควรเป็น:

```text
Wallpaper Manager + Live Wallpaper Engine
```

หมายความว่าต้องมีทั้ง:

```text
- คลัง wallpaper
- การตั้งค่าหลายจอ
- performance preset
- tray control
- pause rule
- installer/uninstaller ที่ใช้งานจริง
```

## เป้าหมายหลัก

```text
1. ตั้ง wallpaper ได้ง่าย
2. Live wallpaper ต้องอยู่หลัง desktop icons
3. ไม่บังไอคอนและไม่แย่ง focus
4. wallpaper ต้องปรับขนาดตามจอได้
5. ใช้ resource อย่างเหมาะสม
6. โปรแกรมปิดหน้าต่างหลักแล้ว wallpaper ยังเล่นต่อได้
7. มี Setup.exe และ Uninstaller
```

## ปัญหาหลักที่ต้องแก้

### 1. แถบขาว / title bar บน wallpaper

สาเหตุคือเอาหน้าต่างโปรแกรมปกติไปฝังเป็น wallpaper โดยยังมี window chrome อยู่

วิธีแก้:

```text
Main App Window      = หน้าจอควบคุม
Renderer Window      = borderless window ที่ฝังหลัง desktop
```

ห้ามใช้ MainWindow เป็น wallpaper โดยตรง

### 2. Wallpaper ไม่ adjust ตามขนาดหน้าจอ

ต้องแก้ 2 ชั้น:

```text
1. Renderer window ต้องมีขนาดเท่า monitor จริง
2. Content ข้างใน เช่น video/image/web ต้อง scale ให้เต็ม window
```

Windows ไม่รู้เองว่าภาพ/วิดีโอควร crop, fit, stretch หรือ center โปรแกรมต้องกำหนดเอง

### 3. Live wallpaper ต้องไม่กินเครื่อง

ต้องมี performance preset และ pause rule:

```text
- Pause when fullscreen app is running
- Pause when game is running
- Pause when laptop is on battery
- FPS limit
- Manual pause/resume
```

## Target User

```text
- คนที่อยากแต่ง desktop ให้สวย
- คนที่ใช้หลายจอ
- คนที่อยากได้ live wallpaper แต่ไม่อยากให้เกมกระตุก
- คนที่อยากจัดการ wallpaper เป็นคลัง
- คนที่อยากได้โปรแกรม native Windows UI สวย ๆ
```

## Product Positioning

โปรแกรมควรอยู่ตรงกลางระหว่าง:

```text
Wallpaper player ธรรมดา
↓
Live Wallpaper Manager
↓
Wallpaper Engine / Creator Platform
```

สำหรับเวอร์ชันแรก ไม่ควรเริ่มใหญ่เกินไป ไม่ต้องมี marketplace, account, cloud sync หรือ editor เต็มรูปแบบ ให้เน้น engine หลักให้เสถียรก่อน
