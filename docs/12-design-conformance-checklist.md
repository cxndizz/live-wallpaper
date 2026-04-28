# Design Conformance Checklist

## Matches the Mockup Direction

- Dark custom WPF shell with rounded outer window.
- Left sidebar navigation with Home, Library, Displays, Performance, and Settings using icon glyphs.
- Home dashboard shows current wallpaper, playback state, monitor count, scale mode, performance mode, and key actions.
- Home dashboard now reports whether active renderer windows are attached below the desktop icons.
- Wallora logo artwork is integrated into the app sidebar, app title area, executable icon, installer title area, and installer welcome screen.
- Library page has add/search controls, grid/list affordance buttons, dynamic wallpaper cards, thumbnails, selected state, missing-file state, and actions.
- Library cards now use larger thumbnails, a compact actions menu, active status, and rename support.
- Displays page uses real monitor data, monitor preview cards, wallpaper modes, and per-monitor profile rows.
- Performance page has preset cards, toggle-style pause rules, and FPS controls.
- Settings page includes startup/tray behavior, scale modes, engine recovery, cache/data/diagnostics actions.
- Tray menu supports Open, Pause/Resume, Next Wallpaper, Mute/Unmute, and Exit.
- Inline status banner provides success/error feedback closer to production UX.
- Home preview reflects the applied wallpaper when a thumbnail or image source is available.

## Functionally Implemented

- Apply image/video wallpapers to all monitors.
- Apply different wallpapers per monitor.
- Renderer windows are verified after attaching to WorkerW so the app can detect whether they are truly below desktop icons.
- Renderer bounds are applied per monitor with DPI-aware WPF sizing and native physical coordinates.
- Persist `config.json` and `library.json` under `%AppData%\LiveWallpaperStudio`.
- Restore last wallpaper or per-monitor profiles at startup.
- Generate image thumbnails and video thumbnails; video uses `ffmpeg` frame extraction when available and falls back to generated placeholders.
- Relink missing wallpaper files.
- Export diagnostics.
- Publish release output to `build/publish`.
- The install page header spacing has been adjusted to avoid clipping, and the installing page now uses a custom accent progress bar instead of the default green Windows progress bar.
- A WPF installer/uninstaller project is the active installer path for the high-fidelity mockup direction.
- WPF installer supports custom setup, installing, uninstall, and removed screens with frameless chrome, gradient buttons, custom checkboxes, and mockup-like dark panels.
- WPF installer no longer uses placeholder/vector illustration panels; the layout is intentionally clean until final production artwork is available.
- WPF installer uses the Wallora app icon and primary horizontal logo artwork.
- WPF installer package output exists at `build/wpf-installer` with the app payload under `build/wpf-installer/payload`.
- Packaging helper script exists for build/test/publish/setup compilation flow.
- Unit-tested monitor profile and library workflow helpers.
- Unit-tested playback restore and per-monitor assignment helpers.
- Unit-tested performance preset/FPS/pause-rule helpers.
- Unit-tested settings workflow helpers for general flags, wallpaper mode, scale mode, monitor profile assignments, and reset defaults.
- UTF-8/icon cleanup completed for the primary app shell; corrupted inline icon text was removed from main UI files.
- Supplied Wallora PNG artwork has been adjusted into app-ready square icon, `.ico`, 256px icon, and horizontal logo assets.

## Still Not Pixel Perfect

- Icons now use free Windows Segoe MDL2 Assets glyphs, but the exact icon set may still differ from the mockup artwork.
- Exact mockup spacing, typography, and card proportions still need screenshot-based QA and final tuning.
- Video thumbnail extraction depends on `ffmpeg`; fallback placeholders are still used on machines without it.
- Video playback now uses LibVLCSharp, but still needs broader codec/multi-monitor QA.
- Live WorkerW behavior still needs visual QA on the target Windows builds, because desktop shell behavior can differ across Windows versions, multi-monitor layouts, and Explorer restarts.
- WPF installer/uninstaller still needs screenshot-based pixel tuning on the target machine after final artwork changes.
- Some UI is still built in code-behind rather than MVVM view models, though shared icon/card composition has moved to `UiElementFactory`.
- Library and monitor business rules are now partly extracted, but visual composition still lives mostly in `MainWindow.xaml.cs`.
- Playback restore/assignment decisions are extracted; renderer hosting and UI error presentation remain in the WPF layer.
- Performance business rules are now extracted; remaining code-behind is mostly UI composition and event routing.
- Settings and monitor-profile mutation rules are now extracted; remaining code-behind is mostly UI state hydration, visual composition, and command routing.
