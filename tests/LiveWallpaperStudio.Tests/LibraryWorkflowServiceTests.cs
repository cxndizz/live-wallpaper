using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Library;
using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Tests;

public sealed class LibraryWorkflowServiceTests
{
    [Fact]
    public void Filter_matches_name_path_or_type()
    {
        var items = new[]
        {
            Item("Cyber Rain", @"D:\Wallpapers\rain.mp4", WallpaperType.Video),
            Item("Forest", @"D:\Wallpapers\green.png", WallpaperType.Image)
        };

        Assert.Single(LibraryWorkflowService.Filter(items, "cyber"));
        Assert.Single(LibraryWorkflowService.Filter(items, "green"));
        Assert.Single(LibraryWorkflowService.Filter(items, "video"));
    }

    [Fact]
    public void GetNextAvailable_skips_missing_files_and_wraps()
    {
        var first = Item("First", @"D:\first.png", WallpaperType.Image, DateTimeOffset.UtcNow.AddMinutes(-2));
        var missing = Item("Missing", @"D:\missing.mp4", WallpaperType.Video, DateTimeOffset.UtcNow.AddMinutes(-1));
        var second = Item("Second", @"D:\second.png", WallpaperType.Image, DateTimeOffset.UtcNow);
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            first.FilePath,
            second.FilePath
        };

        var next = LibraryWorkflowService.GetNextAvailable([first, missing, second], first.Id, existingPaths.Contains);
        var wrapped = LibraryWorkflowService.GetNextAvailable([first, missing, second], second.Id, existingPaths.Contains);

        Assert.Equal(second.Id, next?.Id);
        Assert.Equal(first.Id, wrapped?.Id);
    }

    [Fact]
    public void ApplyRemoveToSettings_clears_last_wallpaper_and_monitor_references()
    {
        var wallpaper = Item("Rain", @"D:\rain.mp4", WallpaperType.Video);
        var settings = new AppSettings
        {
            LastWallpaperId = wallpaper.Id,
            MonitorProfiles =
            [
                new MonitorProfile("A", wallpaper.Id, ScaleMode.Cover, 30, 0),
                new MonitorProfile("B", Guid.NewGuid(), ScaleMode.Contain, 30, 0)
            ]
        };

        var updated = LibraryWorkflowService.ApplyRemoveToSettings(settings, wallpaper);

        Assert.Null(updated.LastWallpaperId);
        Assert.Null(updated.MonitorProfiles[0].WallpaperId);
        Assert.NotNull(updated.MonitorProfiles[1].WallpaperId);
    }

    private static WallpaperItem Item(string name, string path, WallpaperType type, DateTimeOffset? added = null)
    {
        return new WallpaperItem(
            Guid.NewGuid(),
            name,
            path,
            type,
            null,
            0,
            null,
            null,
            null,
            added ?? DateTimeOffset.UtcNow,
            false);
    }
}
