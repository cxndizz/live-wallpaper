using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Performance;

namespace LiveWallpaperStudio.Tests;

public sealed class PerformanceSettingsServiceTests
{
    [Fact]
    public void ApplyPreset_eco_sets_expected_rules_and_fps()
    {
        var settings = new AppSettings
        {
            MonitorProfiles = [new MonitorProfile("A", null, ScaleMode.Cover, 60, 0)]
        };

        var updated = PerformanceSettingsService.ApplyPreset(settings, PerformancePreset.Eco);

        Assert.Equal(PerformancePreset.Eco, updated.PerformancePreset);
        Assert.Equal(15, updated.FpsLimit);
        Assert.True(updated.PauseWhenOnBattery);
        Assert.True(updated.PauseWhenFullscreen);
        Assert.Equal(15, updated.MonitorProfiles[0].FpsLimit);
    }

    [Fact]
    public void ApplyPreset_quality_keeps_lock_pause_but_disables_other_auto_pauses()
    {
        var updated = PerformanceSettingsService.ApplyPreset(new AppSettings(), PerformancePreset.Quality);

        Assert.Equal(60, updated.FpsLimit);
        Assert.False(updated.PauseWhenFullscreen);
        Assert.False(updated.PauseWhenGameRunning);
        Assert.False(updated.PauseWhenOnBattery);
        Assert.True(updated.PauseWhenScreenLocked);
    }

    [Fact]
    public void SetFpsLimit_marks_settings_as_custom_and_updates_profiles()
    {
        var settings = new AppSettings
        {
            PerformancePreset = PerformancePreset.Balanced,
            MonitorProfiles = [new MonitorProfile("A", null, ScaleMode.Cover, 30, 0)]
        };

        var updated = PerformanceSettingsService.SetFpsLimit(settings, 60);

        Assert.Equal(PerformancePreset.Custom, updated.PerformancePreset);
        Assert.Equal(60, updated.FpsLimit);
        Assert.Equal(60, updated.MonitorProfiles[0].FpsLimit);
    }

    [Fact]
    public void SetPauseRules_marks_settings_as_custom()
    {
        var updated = PerformanceSettingsService.SetPauseRules(new AppSettings(), false, false, true, true);

        Assert.Equal(PerformancePreset.Custom, updated.PerformancePreset);
        Assert.False(updated.PauseWhenFullscreen);
        Assert.False(updated.PauseWhenGameRunning);
        Assert.True(updated.PauseWhenOnBattery);
        Assert.True(updated.PauseWhenScreenLocked);
    }
}
