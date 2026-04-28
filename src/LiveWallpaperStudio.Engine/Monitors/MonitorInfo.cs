namespace LiveWallpaperStudio.Engine.Monitors;

public sealed record MonitorInfo(
    string DeviceName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary);
