namespace LiveWallpaperStudio.Data.Models;

public sealed record MonitorProfile(
    string MonitorDeviceId,
    Guid? WallpaperId,
    ScaleMode ScaleMode,
    int FpsLimit,
    double Volume);
