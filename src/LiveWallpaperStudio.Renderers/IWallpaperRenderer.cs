using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Renderers;

public interface IWallpaperRenderer : IAsyncDisposable
{
    PlaybackState State { get; }
    Task LoadAsync(WallpaperSource source, CancellationToken cancellationToken = default);
    Task PlayAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void Resize(RendererBounds bounds, ScaleMode scaleMode);
}
