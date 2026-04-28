using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Playback;

public static class WallpaperPlaybackWorkflowService
{
    public static WallpaperItem? GetRestorableWallpaper(
        AppSettings settings,
        IEnumerable<WallpaperItem> libraryItems,
        Func<string, bool> fileExists)
    {
        if (settings.LastWallpaperId is null)
        {
            return null;
        }

        var item = libraryItems.FirstOrDefault(wallpaper => wallpaper.Id == settings.LastWallpaperId);
        if (item is null || !fileExists(item.FilePath))
        {
            return null;
        }

        return item;
    }

    public static IReadOnlyList<MonitorPlaybackAssignment> GetMonitorAssignments(
        AppSettings settings,
        IEnumerable<WallpaperItem> libraryItems,
        Func<string, bool> fileExists)
    {
        var itemsById = libraryItems.ToDictionary(item => item.Id);
        var assignments = new List<MonitorPlaybackAssignment>();

        foreach (var profile in settings.MonitorProfiles)
        {
            if (profile.WallpaperId is null)
            {
                continue;
            }

            if (!itemsById.TryGetValue(profile.WallpaperId.Value, out var wallpaper))
            {
                continue;
            }

            if (!fileExists(wallpaper.FilePath))
            {
                continue;
            }

            assignments.Add(new MonitorPlaybackAssignment(
                profile.MonitorDeviceId,
                wallpaper,
                profile.ScaleMode,
                profile.FpsLimit));
        }

        return assignments;
    }
}
