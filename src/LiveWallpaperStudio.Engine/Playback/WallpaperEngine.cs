using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Engine.DesktopHost;
using LiveWallpaperStudio.Engine.Monitors;
using LiveWallpaperStudio.Renderers;

namespace LiveWallpaperStudio.Engine.Playback;

public sealed class WallpaperEngine
{
    private readonly DesktopHostService _desktopHost;
    private readonly MonitorManager _monitorManager;
    private readonly Func<WallpaperType, IWallpaperRenderer> _rendererFactory;
    private readonly List<IWallpaperRenderer> _renderers = [];

    public WallpaperEngine(
        DesktopHostService desktopHost,
        MonitorManager monitorManager,
        Func<WallpaperType, IWallpaperRenderer> rendererFactory)
    {
        _desktopHost = desktopHost;
        _monitorManager = monitorManager;
        _rendererFactory = rendererFactory;
    }

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public IReadOnlyList<MonitorInfo> GetMonitors() => _monitorManager.GetMonitors();

    public async Task ApplyAsync(WallpaperSource source, ScaleMode scaleMode, CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);

        foreach (var monitor in _monitorManager.GetMonitors())
        {
            var renderer = _rendererFactory(source.Type);
            await renderer.LoadAsync(source, cancellationToken);
            renderer.Resize(new RendererBounds(monitor.X, monitor.Y, monitor.Width, monitor.Height), scaleMode);
            await renderer.PlayAsync(cancellationToken);
            _renderers.Add(renderer);
        }

        State = PlaybackState.Playing;
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        foreach (var renderer in _renderers)
        {
            await renderer.PauseAsync(cancellationToken);
        }

        State = PlaybackState.Paused;
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var renderer in _renderers)
        {
            await renderer.PlayAsync(cancellationToken);
        }

        State = PlaybackState.Playing;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (var renderer in _renderers)
        {
            await renderer.StopAsync(cancellationToken);
            await renderer.DisposeAsync();
        }

        _renderers.Clear();
        State = PlaybackState.Stopped;
    }

    public bool AttachWindow(IntPtr rendererHandle) => _desktopHost.AttachRendererWindow(rendererHandle).IsAttached;
}
