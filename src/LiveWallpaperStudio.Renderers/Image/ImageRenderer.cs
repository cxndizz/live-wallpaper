using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Renderers.Image;

public sealed class ImageRenderer : IWallpaperRenderer
{
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public WallpaperSource? Source { get; private set; }
    public RendererBounds Bounds { get; private set; }
    public ScaleMode ScaleMode { get; private set; } = ScaleMode.Cover;

    public Task LoadAsync(WallpaperSource source, CancellationToken cancellationToken = default)
    {
        Source = source;
        State = PlaybackState.Stopped;
        return Task.CompletedTask;
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        State = PlaybackState.Playing;
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        State = PlaybackState.Paused;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        State = PlaybackState.Stopped;
        return Task.CompletedTask;
    }

    public void Resize(RendererBounds bounds, ScaleMode scaleMode)
    {
        Bounds = bounds;
        ScaleMode = scaleMode;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
