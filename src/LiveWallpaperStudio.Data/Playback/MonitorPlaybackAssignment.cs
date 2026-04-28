using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Playback;

public sealed record MonitorPlaybackAssignment(
    string MonitorDeviceId,
    WallpaperItem Wallpaper,
    ScaleMode ScaleMode,
    int FpsLimit);
