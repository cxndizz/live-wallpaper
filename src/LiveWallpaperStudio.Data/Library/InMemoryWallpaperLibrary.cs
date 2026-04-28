using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Library;

public sealed class InMemoryWallpaperLibrary
{
    private readonly List<WallpaperItem> _items = [];

    public IReadOnlyList<WallpaperItem> Items => _items;

    public void ReplaceAll(IEnumerable<WallpaperItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public WallpaperItem AddFile(string filePath)
    {
        var existing = _items.FirstOrDefault(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var type = DetectType(filePath);
        var fileInfo = new FileInfo(filePath);
        var item = new WallpaperItem(
            Guid.NewGuid(),
            Path.GetFileNameWithoutExtension(filePath),
            filePath,
            type,
            null,
            fileInfo.Exists ? fileInfo.Length : 0,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            false);

        _items.Add(item);
        return item;
    }

    public void Upsert(WallpaperItem item)
    {
        var index = _items.FindIndex(existing => existing.Id == item.Id);
        if (index < 0)
        {
            _items.Add(item);
            return;
        }

        _items[index] = item;
    }

    public bool Remove(Guid id)
    {
        var item = _items.FirstOrDefault(existing => existing.Id == id);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        return true;
    }

    public static WallpaperType DetectType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".bmp" => WallpaperType.Image,
            ".mp4" or ".webm" => WallpaperType.Video,
            ".gif" or ".webp" => WallpaperType.Gif,
            ".html" or ".htm" => WallpaperType.Web,
            _ => WallpaperType.Image
        };
    }
}
