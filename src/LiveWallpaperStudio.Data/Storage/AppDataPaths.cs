namespace LiveWallpaperStudio.Data.Storage;

public sealed class AppDataPaths
{
    public AppDataPaths(string? baseDirectory = null)
    {
        Root = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveWallpaperStudio");

        ConfigFile = Path.Combine(Root, "config.json");
        DatabaseFile = Path.Combine(Root, "library.db");
        LibraryFile = Path.Combine(Root, "library.json");
        Logs = Path.Combine(Root, "logs");
        Cache = Path.Combine(Root, "cache");
        Thumbnails = Path.Combine(Root, "thumbnails");
    }

    public string Root { get; }
    public string ConfigFile { get; }
    public string DatabaseFile { get; }
    public string LibraryFile { get; }
    public string Logs { get; }
    public string Cache { get; }
    public string Thumbnails { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(Thumbnails);
    }
}
