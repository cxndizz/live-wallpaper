using LiveWallpaperStudio.Data.Library;
using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Tests;

public sealed class WallpaperLibraryTests
{
    [Theory]
    [InlineData("rain.mp4", WallpaperType.Video)]
    [InlineData("wallpaper.webm", WallpaperType.Video)]
    [InlineData("city.png", WallpaperType.Image)]
    [InlineData("loop.gif", WallpaperType.Gif)]
    [InlineData("scene.html", WallpaperType.Web)]
    public void DetectType_maps_supported_extensions(string fileName, WallpaperType expected)
    {
        Assert.Equal(expected, InMemoryWallpaperLibrary.DetectType(fileName));
    }

    [Fact]
    public void Remove_deletes_item_by_id()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        File.WriteAllText(tempFile, "not a real image");
        var library = new InMemoryWallpaperLibrary();
        var item = library.AddFile(tempFile);

        Assert.True(library.Remove(item.Id));
        Assert.Empty(library.Items);

        File.Delete(tempFile);
    }
}
