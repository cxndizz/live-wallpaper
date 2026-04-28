using System.Text.Json;
using LiveWallpaperStudio.Data.Config;

namespace LiveWallpaperStudio.Data.Storage;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppDataPaths _paths;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SettingsStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();

        if (!File.Exists(_paths.ConfigFile))
        {
            var defaults = new AppSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.ConfigFile);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        await _writeLock.WaitAsync(cancellationToken);
        string? tempFile = null;
        try
        {
            tempFile = Path.Combine(_paths.Root, $"{Path.GetFileName(_paths.ConfigFile)}.{Guid.NewGuid():N}.tmp");
            await using (var stream = File.Create(tempFile))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
            }

            File.Move(tempFile, _paths.ConfigFile, overwrite: true);
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
