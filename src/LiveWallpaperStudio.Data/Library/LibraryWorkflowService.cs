using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Library;

public static class LibraryWorkflowService
{
    public static IReadOnlyList<WallpaperItem> Filter(IEnumerable<WallpaperItem> items, string? query)
    {
        var trimmed = query?.Trim();
        return items
            .Where(item => string.IsNullOrWhiteSpace(trimmed)
                || item.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                || item.FilePath.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                || item.Type.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.DateAdded)
            .ToList();
    }

    public static WallpaperItem? GetNextAvailable(IEnumerable<WallpaperItem> items, Guid? currentWallpaperId)
    {
        return GetNextAvailable(items, currentWallpaperId, File.Exists);
    }

    public static WallpaperItem? GetNextAvailable(IEnumerable<WallpaperItem> items, Guid? currentWallpaperId, Func<string, bool> fileExists)
    {
        var available = items
            .Where(item => fileExists(item.FilePath))
            .OrderBy(item => item.DateAdded)
            .ToList();

        if (available.Count == 0)
        {
            return null;
        }

        var currentIndex = currentWallpaperId is null
            ? -1
            : available.FindIndex(item => item.Id == currentWallpaperId);

        return available[(currentIndex + 1 + available.Count) % available.Count];
    }

    public static WallpaperItem Relink(WallpaperItem item, string newFilePath, string? thumbnailPath)
    {
        var fileInfo = new FileInfo(newFilePath);
        return item with
        {
            FilePath = newFilePath,
            Type = InMemoryWallpaperLibrary.DetectType(newFilePath),
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            ThumbnailPath = thumbnailPath
        };
    }

    public static AppSettings ApplyRemoveToSettings(AppSettings settings, WallpaperItem removedItem)
    {
        if (settings.LastWallpaperId != removedItem.Id)
        {
            return settings;
        }

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
            LastWallpaperId = null,
            MonitorProfiles = settings.MonitorProfiles
                .Select(profile => profile.WallpaperId == removedItem.Id ? profile with { WallpaperId = null } : profile)
                .ToList()
        };
    }

    public static IReadOnlyList<WallpaperItem> ClearThumbnailReferences(IEnumerable<WallpaperItem> items)
    {
        return items.Select(item => item with { ThumbnailPath = null }).ToList();
    }
}
