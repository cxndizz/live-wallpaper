using System.IO;
using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Storage;
using LiveWallpaperStudio.Engine.Monitors;

namespace LiveWallpaperStudio.App.Services;

public sealed class DiagnosticsExportService
{
    private readonly AppDataPaths _paths;

    public DiagnosticsExportService(AppDataPaths paths)
    {
        _paths = paths;
    }

    public string Export(AppSettings settings, IReadOnlyList<WallpaperItem> libraryItems, IReadOnlyList<MonitorInfo> monitors)
    {
        _paths.EnsureCreated();
        Directory.CreateDirectory(_paths.Logs);

        var filePath = Path.Combine(_paths.Logs, $"diagnostics-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt");
        var lines = new List<string>
        {
            "Wallora - Live wallpaper Diagnostics",
            $"Created: {DateTimeOffset.Now}",
            $"Wallpaper mode: {settings.WallpaperMode}",
            $"Performance preset: {settings.PerformancePreset}",
            $"FPS limit: {settings.FpsLimit}",
            $"Default scale mode: {settings.DefaultScaleMode}",
            $"Pause fullscreen: {settings.PauseWhenFullscreen}",
            $"Pause game: {settings.PauseWhenGameRunning}",
            $"Pause battery: {settings.PauseWhenOnBattery}",
            $"Pause locked: {settings.PauseWhenScreenLocked}",
            $"Library count: {libraryItems.Count}",
            $"Monitor count: {monitors.Count}",
            ""
        };

        lines.AddRange(monitors.Select((monitor, index) =>
            $"Monitor {index + 1}: {monitor.DeviceName}, {monitor.Width}x{monitor.Height}, X={monitor.X}, Y={monitor.Y}, Primary={monitor.IsPrimary}"));
        lines.Add("");
        lines.AddRange(settings.MonitorProfiles.Select(profile =>
            $"MonitorProfile: {profile.MonitorDeviceId}, WallpaperId={profile.WallpaperId}, Scale={profile.ScaleMode}, FPS={profile.FpsLimit}, Volume={profile.Volume}"));
        lines.Add("");
        lines.AddRange(libraryItems.Select(item =>
            $"Wallpaper: {item.Id}, {item.Name}, {item.Type}, Exists={File.Exists(item.FilePath)}, Size={item.FileSize}, Path={item.FilePath}"));

        File.WriteAllLines(filePath, lines);
        return filePath;
    }
}
