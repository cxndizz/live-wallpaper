using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Playback;

namespace LiveWallpaperStudio.Tests;

public sealed class WallpaperPlaybackWorkflowServiceTests
{
    [Fact]
    public void GetRestorableWallpaper_returns_last_wallpaper_when_file_exists()
    {
        var wallpaper = Item("Rain", @"D:\rain.mp4");
        var settings = new AppSettings { LastWallpaperId = wallpaper.Id };

        var restored = WallpaperPlaybackWorkflowService.GetRestorableWallpaper(
            settings,
            [wallpaper],
            _ => true);

        Assert.Equal(wallpaper, restored);
    }

    [Fact]
    public void GetRestorableWallpaper_returns_null_when_last_file_is_missing()
    {
        var wallpaper = Item("Rain", @"D:\rain.mp4");
        var settings = new AppSettings { LastWallpaperId = wallpaper.Id };

        var restored = WallpaperPlaybackWorkflowService.GetRestorableWallpaper(
            settings,
            [wallpaper],
            _ => false);

        Assert.Null(restored);
    }

    [Fact]
    public void GetMonitorAssignments_skips_empty_missing_and_unavailable_profiles()
    {
        var available = Item("Available", @"D:\available.png");
        var missingFile = Item("Missing File", @"D:\missing.png");
        var missingLibraryId = Guid.NewGuid();
        var settings = new AppSettings
        {
            MonitorProfiles =
            [
                new MonitorProfile("DISPLAY1", available.Id, ScaleMode.Cover, 30, 0),
                new MonitorProfile("DISPLAY2", missingFile.Id, ScaleMode.Contain, 60, 0),
                new MonitorProfile("DISPLAY3", missingLibraryId, ScaleMode.Stretch, 15, 0),
                new MonitorProfile("DISPLAY4", null, ScaleMode.Center, 30, 0)
            ]
        };

        var assignments = WallpaperPlaybackWorkflowService.GetMonitorAssignments(
            settings,
            [available, missingFile],
            path => path == available.FilePath);

        Assert.Single(assignments);
        Assert.Equal("DISPLAY1", assignments[0].MonitorDeviceId);
        Assert.Equal(available, assignments[0].Wallpaper);
        Assert.Equal(ScaleMode.Cover, assignments[0].ScaleMode);
        Assert.Equal(30, assignments[0].FpsLimit);
    }

    private static WallpaperItem Item(string name, string path)
    {
        return new WallpaperItem(
            Guid.NewGuid(),
            name,
            path,
            WallpaperType.Image,
            null,
            0,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            false);
    }
}
