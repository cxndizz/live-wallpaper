using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Storage;

namespace LiveWallpaperStudio.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public async Task LoadAsync_creates_default_config_when_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "lws-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppDataPaths(root);
        var store = new SettingsStore(paths);

        var settings = await store.LoadAsync();

        Assert.Equal(PerformancePreset.Balanced, settings.PerformancePreset);
        Assert.Equal(WallpaperMode.SameOnAllMonitors, settings.WallpaperMode);
        Assert.Equal(30, settings.FpsLimit);
        Assert.True(File.Exists(paths.ConfigFile));
    }

    [Fact]
    public void AppDataPaths_exposes_library_json_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "lws-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppDataPaths(root);

        Assert.Equal(Path.Combine(root, "library.json"), paths.LibraryFile);
    }

    [Fact]
    public async Task LoadAsync_returns_defaults_when_config_is_corrupt()
    {
        var root = Path.Combine(Path.GetTempPath(), "lws-tests", Guid.NewGuid().ToString("N"));
        var paths = new AppDataPaths(root);
        paths.EnsureCreated();
        await File.WriteAllTextAsync(paths.ConfigFile, "{ bad json");

        var settings = await new SettingsStore(paths).LoadAsync();

        Assert.Equal(PerformancePreset.Balanced, settings.PerformancePreset);
        Assert.Equal(WallpaperMode.SameOnAllMonitors, settings.WallpaperMode);
    }
}
