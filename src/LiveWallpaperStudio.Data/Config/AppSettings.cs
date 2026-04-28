using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Config;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool KeepWallpaperRunningWhenClosed { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public PerformancePreset PerformancePreset { get; set; } = PerformancePreset.Balanced;
    public WallpaperMode WallpaperMode { get; set; } = WallpaperMode.SameOnAllMonitors;
    public int FpsLimit { get; set; } = 30;
    public ScaleMode DefaultScaleMode { get; set; } = ScaleMode.Cover;
    public bool PauseWhenFullscreen { get; set; } = true;
    public bool PauseWhenGameRunning { get; set; } = true;
    public bool PauseWhenOnBattery { get; set; }
    public bool PauseWhenScreenLocked { get; set; } = true;
    public Guid? LastWallpaperId { get; set; }
    public List<MonitorProfile> MonitorProfiles { get; set; } = [];
}
