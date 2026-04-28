using LiveWallpaperStudio.Data.Library;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Storage;

namespace LiveWallpaperStudio.Tests;

public sealed class WallpaperLibraryStoreTests
{
    [Fact]
    public async Task SaveAsync_round_trips_wallpaper_items()
    {
        var root = Path.Combine(Path.GetTempPath(), "lws-tests", Guid.NewGuid().ToString("N"));
        var store = new WallpaperLibraryStore(new AppDataPaths(root));
        var item = new WallpaperItem(
            Guid.NewGuid(),
            "Cyber Rain",
            @"D:\Wallpapers\Cyber Rain.mp4",
            WallpaperType.Video,
            null,
            1024,
            1920,
            1080,
            TimeSpan.FromSeconds(30),
            DateTimeOffset.UtcNow,
            false);

        await store.SaveAsync([item]);
        var loaded = await store.LoadAsync();

        Assert.Single(loaded);
        Assert.Equal(item.Id, loaded[0].Id);
        Assert.Equal(item.FilePath, loaded[0].FilePath);
        Assert.Equal(WallpaperType.Video, loaded[0].Type);
    }

    [Fact]
    public async Task LoadAsync_returns_empty_list_when_library_is_corrupt()
    {
        var root = Path.Combine(Path.GetTempPath(), "lws-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppDataPaths(root);
        paths.EnsureCreated();
        await File.WriteAllTextAsync(paths.LibraryFile, "[ not json");

        var loaded = await new WallpaperLibraryStore(paths).LoadAsync();

        Assert.Empty(loaded);
    }
}
