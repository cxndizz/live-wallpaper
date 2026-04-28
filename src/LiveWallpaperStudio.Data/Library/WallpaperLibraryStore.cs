using System.Text.Json;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Storage;

namespace LiveWallpaperStudio.Data.Library;

public sealed class WallpaperLibraryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppDataPaths _paths;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public WallpaperLibraryStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<List<WallpaperItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();

        if (!File.Exists(_paths.LibraryFile))
        {
            await SaveAsync([], cancellationToken);
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_paths.LibraryFile);
            return await JsonSerializer.DeserializeAsync<List<WallpaperItem>>(stream, JsonOptions, cancellationToken)
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public async Task SaveAsync(IEnumerable<WallpaperItem> wallpapers, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        await _writeLock.WaitAsync(cancellationToken);
        string? tempFile = null;
        try
        {
            tempFile = Path.Combine(_paths.Root, $"{Path.GetFileName(_paths.LibraryFile)}.{Guid.NewGuid():N}.tmp");
            await using (var stream = File.Create(tempFile))
            {
                await JsonSerializer.SerializeAsync(stream, wallpapers.ToList(), JsonOptions, cancellationToken);
            }

            File.Move(tempFile, _paths.LibraryFile, overwrite: true);
            tempFile = null;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempFile) && File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Temporary save files are best-effort cleanup.
                }
            }

            _writeLock.Release();
        }
    }
}
