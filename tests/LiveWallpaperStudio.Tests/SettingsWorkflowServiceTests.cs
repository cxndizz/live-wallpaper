using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Settings;

namespace LiveWallpaperStudio.Tests;

public sealed class SettingsWorkflowServiceTests
{
    [Fact]
    public void SetGeneralSettings_updates_only_general_flags()
    {
        var lastWallpaperId = Guid.NewGuid();
        var settings = new AppSettings
        {
            StartWithWindows = false,
            MinimizeToTray = true,
            KeepWallpaperRunningWhenClosed = true,
            PerformancePreset = PerformancePreset.Quality,
            WallpaperMode = WallpaperMode.DifferentPerMonitor,
            FpsLimit = 60,
            DefaultScaleMode = ScaleMode.Contain,
            LastWallpaperId = lastWallpaperId,
            MonitorProfiles = [new MonitorProfile("DISPLAY1", lastWallpaperId, ScaleMode.Cover, 60, 0)]
        };

        var updated = SettingsWorkflowService.SetGeneralSettings(settings, true, false, false);

        Assert.True(updated.StartWithWindows);
        Assert.False(updated.MinimizeToTray);
        Assert.False(updated.KeepWallpaperRunningWhenClosed);
        Assert.Equal(PerformancePreset.Quality, updated.PerformancePreset);
        Assert.Equal(WallpaperMode.DifferentPerMonitor, updated.WallpaperMode);
        Assert.Equal(60, updated.FpsLimit);
        Assert.Equal(ScaleMode.Contain, updated.DefaultScaleMode);
        Assert.Equal(lastWallpaperId, updated.LastWallpaperId);
        Assert.Single(updated.MonitorProfiles);
    }

    [Fact]
    public void SetDefaultScaleMode_preserves_wallpaper_mode_and_last_wallpaper()
    {
        var lastWallpaperId = Guid.NewGuid();
        var settings = new AppSettings
        {
            WallpaperMode = WallpaperMode.SpanAcrossAllMonitors,
            DefaultScaleMode = ScaleMode.Cover,
            LastWallpaperId = lastWallpaperId
        };

        var updated = SettingsWorkflowService.SetDefaultScaleMode(settings, ScaleMode.Stretch);

        Assert.Equal(ScaleMode.Stretch, updated.DefaultScaleMode);
        Assert.Equal(WallpaperMode.SpanAcrossAllMonitors, updated.WallpaperMode);
        Assert.Equal(lastWallpaperId, updated.LastWallpaperId);
    }

    [Fact]
    public void SetWallpaperMode_preserves_scale_mode_and_monitor_profiles()
    {
        var profile = new MonitorProfile("DISPLAY1", null, ScaleMode.Center, 30, 0);
        var settings = new AppSettings
        {
            WallpaperMode = WallpaperMode.SameOnAllMonitors,
            DefaultScaleMode = ScaleMode.Center,
            MonitorProfiles = [profile]
        };

        var updated = SettingsWorkflowService.SetWallpaperMode(settings, WallpaperMode.DifferentPerMonitor);

        Assert.Equal(WallpaperMode.DifferentPerMonitor, updated.WallpaperMode);
        Assert.Equal(ScaleMode.Center, updated.DefaultScaleMode);
        Assert.Equal(profile, updated.MonitorProfiles[0]);
    }

    [Fact]
    public void SyncMonitorProfiles_uses_last_wallpaper_and_defaults_for_new_monitors()
    {
        var lastWallpaperId = Guid.NewGuid();
        var settings = new AppSettings
        {
            LastWallpaperId = lastWallpaperId,
            DefaultScaleMode = ScaleMode.Contain,
            FpsLimit = 15,
            MonitorProfiles = [new MonitorProfile("DISPLAY1", null, ScaleMode.Cover, 30, 0)]
        };

        var updated = SettingsWorkflowService.SyncMonitorProfiles(settings, ["DISPLAY1", "DISPLAY2"]);

        Assert.Equal(2, updated.MonitorProfiles.Count);
        Assert.Null(updated.MonitorProfiles[0].WallpaperId);
        Assert.Equal(lastWallpaperId, updated.MonitorProfiles[1].WallpaperId);
        Assert.Equal(ScaleMode.Contain, updated.MonitorProfiles[1].ScaleMode);
        Assert.Equal(15, updated.MonitorProfiles[1].FpsLimit);
    }

    [Fact]
    public void SetMonitorWallpaper_updates_profile_using_settings_fps()
    {
        var wallpaperId = Guid.NewGuid();
        var settings = new AppSettings
        {
            FpsLimit = 60,
            MonitorProfiles = [new MonitorProfile("DISPLAY1", null, ScaleMode.Cover, 30, 0)]
        };

        var updated = SettingsWorkflowService.SetMonitorWallpaper(settings, "DISPLAY1", wallpaperId, ScaleMode.Center);

        Assert.Equal(wallpaperId, updated.MonitorProfiles[0].WallpaperId);
        Assert.Equal(ScaleMode.Center, updated.MonitorProfiles[0].ScaleMode);
        Assert.Equal(60, updated.MonitorProfiles[0].FpsLimit);
    }

    [Fact]
    public void SetAllMonitorWallpapers_updates_every_profile()
    {
        var wallpaperId = Guid.NewGuid();
        var settings = new AppSettings
        {
            FpsLimit = 15,
            MonitorProfiles =
            [
                new MonitorProfile("DISPLAY1", null, ScaleMode.Cover, 30, 0),
                new MonitorProfile("DISPLAY2", null, ScaleMode.Center, 60, 0)
            ]
        };

        var updated = SettingsWorkflowService.SetAllMonitorWallpapers(settings, wallpaperId, ScaleMode.Stretch);

        Assert.All(updated.MonitorProfiles, profile =>
        {
            Assert.Equal(wallpaperId, profile.WallpaperId);
            Assert.Equal(ScaleMode.Stretch, profile.ScaleMode);
            Assert.Equal(15, profile.FpsLimit);
        });
    }

    [Fact]
    public void Reset_returns_default_app_settings()
    {
        var updated = SettingsWorkflowService.Reset();

        Assert.False(updated.StartWithWindows);
        Assert.True(updated.MinimizeToTray);
        Assert.True(updated.KeepWallpaperRunningWhenClosed);
        Assert.Equal(PerformancePreset.Balanced, updated.PerformancePreset);
        Assert.Equal(WallpaperMode.SameOnAllMonitors, updated.WallpaperMode);
        Assert.Equal(30, updated.FpsLimit);
        Assert.Equal(ScaleMode.Cover, updated.DefaultScaleMode);
        Assert.True(updated.PauseWhenFullscreen);
        Assert.True(updated.PauseWhenGameRunning);
        Assert.False(updated.PauseWhenOnBattery);
        Assert.True(updated.PauseWhenScreenLocked);
        Assert.Null(updated.LastWallpaperId);
        Assert.Empty(updated.MonitorProfiles);
    }
}
