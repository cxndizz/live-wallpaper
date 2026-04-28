using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Renderers.Video;

public sealed class VideoRenderer : IWallpaperRenderer
{
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public WallpaperSource? Source { get; private set; }
    public int FpsLimit { get; private set; } = 30;

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
    }

    public void SetFpsLimit(int fpsLimit)
    {
        FpsLimit = fpsLimit;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
