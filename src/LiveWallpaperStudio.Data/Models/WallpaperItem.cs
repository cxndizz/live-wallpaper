namespace LiveWallpaperStudio.Data.Models;

public sealed record WallpaperItem(
    Guid Id,
    string Name,
    string FilePath,
    WallpaperType Type,
    string? ThumbnailPath,
    long FileSize,
    int? Width,
    int? Height,
    TimeSpan? Duration,
    DateTimeOffset DateAdded,
    bool IsFavorite);
