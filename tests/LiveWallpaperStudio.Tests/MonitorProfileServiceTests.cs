using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Monitors;

namespace LiveWallpaperStudio.Tests;

public sealed class MonitorProfileServiceTests
{
    [Fact]
    public void SyncProfiles_adds_new_monitors_and_removes_disconnected_ones()
    {
        var wallpaperId = Guid.NewGuid();
        var profiles = new[]
        {
            new MonitorProfile(@"\\.\DISPLAY1", wallpaperId, ScaleMode.Cover, 30, 0),
            new MonitorProfile(@"\\.\DISPLAY3", wallpaperId, ScaleMode.Stretch, 60, 0)
        };

        var synced = MonitorProfileService.SyncProfiles(
            [@"\\.\DISPLAY1", @"\\.\DISPLAY2"],
            profiles,
            null,
            ScaleMode.Contain,
            15);

        Assert.Equal(2, synced.Count);
        Assert.Equal(ScaleMode.Cover, synced[0].ScaleMode);
        Assert.Equal(@"\\.\DISPLAY2", synced[1].MonitorDeviceId);
        Assert.Equal(ScaleMode.Contain, synced[1].ScaleMode);
        Assert.DoesNotContain(synced, profile => profile.MonitorDeviceId == @"\\.\DISPLAY3");
    }

    [Fact]
    public void SetWallpaper_updates_existing_profile()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var profiles = new[]
        {
            new MonitorProfile("A", first, ScaleMode.Cover, 30, 0)
        };

        var updated = MonitorProfileService.SetWallpaper(profiles, "A", second, ScaleMode.Center, 60);

        Assert.Single(updated);
        Assert.Equal(second, updated[0].WallpaperId);
        Assert.Equal(ScaleMode.Center, updated[0].ScaleMode);
        Assert.Equal(60, updated[0].FpsLimit);
    }

    [Fact]
    public void SetScaleMode_adds_profile_when_missing()
    {
        var updated = MonitorProfileService.SetScaleMode([], "A", ScaleMode.Stretch, 15);

        Assert.Single(updated);
        Assert.Null(updated[0].WallpaperId);
        Assert.Equal(ScaleMode.Stretch, updated[0].ScaleMode);
        Assert.Equal(15, updated[0].FpsLimit);
    }
}
