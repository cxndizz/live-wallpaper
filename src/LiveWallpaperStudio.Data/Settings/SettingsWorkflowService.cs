using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Monitors;

namespace LiveWallpaperStudio.Data.Settings;

public static class SettingsWorkflowService
{
    public static AppSettings SetGeneralSettings(
        AppSettings settings,
        bool startWithWindows,
        bool minimizeToTray,
        bool keepWallpaperRunningWhenClosed)
    {
        var updated = Clone(settings);
        updated.StartWithWindows = startWithWindows;
        updated.MinimizeToTray = minimizeToTray;
        updated.KeepWallpaperRunningWhenClosed = keepWallpaperRunningWhenClosed;
        return updated;
    }

    public static AppSettings SetDefaultScaleMode(AppSettings settings, ScaleMode scaleMode)
    {
        var updated = Clone(settings);
        updated.DefaultScaleMode = scaleMode;
        return updated;
    }

    public static AppSettings SetWallpaperMode(AppSettings settings, WallpaperMode wallpaperMode)
    {
        var updated = Clone(settings);
        updated.WallpaperMode = wallpaperMode;
        return updated;
    }

    public static AppSettings SetLastWallpaper(AppSettings settings, Guid? wallpaperId)
    {
        var updated = Clone(settings);
        updated.LastWallpaperId = wallpaperId;
        return updated;
    }

    public static AppSettings SyncMonitorProfiles(AppSettings settings, IEnumerable<string> monitorDeviceIds)
    {
        var updated = Clone(settings);
        updated.MonitorProfiles = MonitorProfileService.SyncProfiles(
            monitorDeviceIds,
            updated.MonitorProfiles,
            updated.LastWallpaperId,
            updated.DefaultScaleMode,
            updated.FpsLimit);
        return updated;
    }

    public static AppSettings SetMonitorWallpaper(
        AppSettings settings,
        string monitorDeviceId,
        Guid wallpaperId,
        ScaleMode scaleMode)
    {
        var updated = Clone(settings);
        updated.MonitorProfiles = MonitorProfileService.SetWallpaper(
            updated.MonitorProfiles,
            monitorDeviceId,
            wallpaperId,
            scaleMode,
            updated.FpsLimit);
        return updated;
    }

    public static AppSettings SetMonitorScaleMode(AppSettings settings, string monitorDeviceId, ScaleMode scaleMode)
    {
        var updated = Clone(settings);
        updated.MonitorProfiles = MonitorProfileService.SetScaleMode(
            updated.MonitorProfiles,
            monitorDeviceId,
            scaleMode,
            updated.FpsLimit);
        return updated;
    }

    public static AppSettings SetAllMonitorWallpapers(AppSettings settings, Guid wallpaperId, ScaleMode scaleMode)
    {
        var updated = Clone(settings);
        updated.MonitorProfiles = MonitorProfileService.SetAllWallpaper(
            updated.MonitorProfiles,
            wallpaperId,
            scaleMode,
            updated.FpsLimit);
        return updated;
    }

    public static AppSettings Reset()
    {
        return new AppSettings();
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
