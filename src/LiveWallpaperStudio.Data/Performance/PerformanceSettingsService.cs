using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Monitors;

namespace LiveWallpaperStudio.Data.Performance;

public static class PerformanceSettingsService
{
    public static AppSettings ApplyPreset(AppSettings settings, PerformancePreset preset)
    {
        var updated = Clone(settings);
        updated.PerformancePreset = preset;

        switch (preset)
        {
            case PerformancePreset.Eco:
                updated.FpsLimit = 15;
                updated.PauseWhenFullscreen = true;
                updated.PauseWhenGameRunning = true;
                updated.PauseWhenOnBattery = true;
                updated.PauseWhenScreenLocked = true;
                break;
            case PerformancePreset.Quality:
                updated.FpsLimit = 60;
                updated.PauseWhenFullscreen = false;
                updated.PauseWhenGameRunning = false;
                updated.PauseWhenOnBattery = false;
                updated.PauseWhenScreenLocked = true;
                break;
            case PerformancePreset.Balanced:
                updated.FpsLimit = 30;
                updated.PauseWhenFullscreen = true;
                updated.PauseWhenGameRunning = true;
                updated.PauseWhenOnBattery = false;
                updated.PauseWhenScreenLocked = true;
                break;
        }

        updated.MonitorProfiles = MonitorProfileService.SetFpsLimit(updated.MonitorProfiles, updated.FpsLimit);
        return updated;
    }

    public static AppSettings SetFpsLimit(AppSettings settings, int fpsLimit)
    {
        var updated = Clone(settings);
        updated.FpsLimit = fpsLimit;
        updated.PerformancePreset = PerformancePreset.Custom;
        updated.MonitorProfiles = MonitorProfileService.SetFpsLimit(updated.MonitorProfiles, fpsLimit);
        return updated;
    }

    public static AppSettings SetPauseRules(
        AppSettings settings,
        bool pauseWhenFullscreen,
        bool pauseWhenGameRunning,
        bool pauseWhenOnBattery,
        bool pauseWhenScreenLocked)
    {
        var updated = Clone(settings);
        updated.PauseWhenFullscreen = pauseWhenFullscreen;
        updated.PauseWhenGameRunning = pauseWhenGameRunning;
        updated.PauseWhenOnBattery = pauseWhenOnBattery;
        updated.PauseWhenScreenLocked = pauseWhenScreenLocked;
        updated.PerformancePreset = PerformancePreset.Custom;
        return updated;
    }

    private static AppSettings Clone(AppSettings settings)
    {
        return new AppSettings
        {
            StartWithWindows = settings.StartWithWindows,
            MinimizeToTray = settings.MinimizeToTray,
            KeepWallpaperRunningWhenClosed = settings.KeepWallpaperRunningWhenClosed,
            Theme = settings.Theme,
            PerformancePreset = settings.PerformancePreset,
            WallpaperMode = settings.WallpaperMode,
            FpsLimit = settings.FpsLimit,
            DefaultScaleMode = settings.DefaultScaleMode,
            PauseWhenFullscreen = settings.PauseWhenFullscreen,
            PauseWhenGameRunning = settings.PauseWhenGameRunning,
            PauseWhenOnBattery = settings.PauseWhenOnBattery,
            PauseWhenScreenLocked = settings.PauseWhenScreenLocked,
            LastWallpaperId = settings.LastWallpaperId,
            MonitorProfiles = settings.MonitorProfiles.ToList()
        };
    }
}
