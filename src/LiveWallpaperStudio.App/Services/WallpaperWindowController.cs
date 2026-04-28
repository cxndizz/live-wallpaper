using System.Windows;
using System.Windows.Threading;
using LiveWallpaperStudio.Data.Library;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Engine.DesktopHost;
using LiveWallpaperStudio.Engine.Monitors;

namespace LiveWallpaperStudio.App.Services;

public sealed class WallpaperWindowController
{
    private readonly DesktopHostService _desktopHost = new();
    private readonly MonitorManager _monitorManager = new();
    private readonly List<RendererSession> _sessions = [];

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public string? CurrentFilePath { get; private set; }
    public ScaleMode CurrentScaleMode { get; private set; } = ScaleMode.Cover;
    public bool IsMuted { get; private set; } = true;
    public IReadOnlyList<RendererSessionStatus> RendererStatuses => _sessions
        .Select(session => session.ToStatus())
        .ToList();

    public IReadOnlyList<MonitorInfo> GetMonitors() => _monitorManager.GetMonitors();

    public async Task ApplyToAllMonitorsAsync(string filePath, ScaleMode scaleMode)
    {
        var newSessions = new List<RendererSession>();

        try
        {
            var type = InMemoryWallpaperLibrary.DetectType(filePath);
            var monitors = _monitorManager.GetMonitors();
            var host = _desktopHost.EnsureWorkerW();
            if (host == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find the Windows desktop host. If Explorer was restarted, try Reattach or restart Explorer.");
            }

            foreach (var monitor in monitors.OrderByDescending(monitor => monitor.IsPrimary))
            {
                newSessions.Add(await CreateRendererSessionAsync(host, monitor, filePath, type, scaleMode));
                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            var oldSessions = ReplaceSessions(newSessions);
            ReflowSessions(newSessions, host);
            CurrentFilePath = filePath;
            CurrentScaleMode = scaleMode;
            State = PlaybackState.Playing;
            SchedulePostApplyReflow();
            _ = CloseSessionsAsync(oldSessions);
        }
        catch
        {
            await CloseSessionsAsync(newSessions);
            State = _sessions.Count > 0 ? State : PlaybackState.Error;
            throw;
        }
    }

    public async Task ApplyPerMonitorAsync(IReadOnlyDictionary<string, MonitorWallpaperAssignment> assignments)
    {
        var newSessions = new List<RendererSession>();

        try
        {
            var host = _desktopHost.EnsureWorkerW();
            if (host == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find the Windows desktop host. If Explorer was restarted, try Reattach or restart Explorer.");
            }

            foreach (var monitor in _monitorManager.GetMonitors().OrderByDescending(monitor => monitor.IsPrimary))
            {
                if (!assignments.TryGetValue(monitor.DeviceName, out var assignment))
                {
                    continue;
                }

                var type = InMemoryWallpaperLibrary.DetectType(assignment.FilePath);
                newSessions.Add(await CreateRendererSessionAsync(host, monitor, assignment.FilePath, type, assignment.ScaleMode));
                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            var oldSessions = ReplaceSessions(newSessions);
            ReflowSessions(newSessions, host);
            CurrentFilePath = assignments.Count switch
            {
                0 => null,
                1 => assignments.Values.First().FilePath,
                _ => "Multiple wallpapers"
            };
            CurrentScaleMode = assignments.Count == 1 ? assignments.Values.First().ScaleMode : ScaleMode.Cover;
            State = _sessions.Count > 0 ? PlaybackState.Playing : PlaybackState.Stopped;
            SchedulePostApplyReflow();
            _ = CloseSessionsAsync(oldSessions);
        }
        catch
        {
            await CloseSessionsAsync(newSessions);
            State = _sessions.Count > 0 ? State : PlaybackState.Error;
            throw;
        }
    }

    public void Pause()
    {
        foreach (var session in _sessions)
        {
            session.Window.Pause();
        }

        if (_sessions.Count > 0)
        {
            State = PlaybackState.Paused;
        }
    }

    public void Resume()
    {
        foreach (var session in _sessions)
        {
            session.Window.Resume();
        }

        if (_sessions.Count > 0)
        {
            State = PlaybackState.Playing;
        }
    }

    public void TogglePause()
    {
        if (State == PlaybackState.Playing)
        {
            Pause();
            return;
        }

        if (State == PlaybackState.Paused)
        {
            Resume();
        }
    }

    public void ToggleMute()
    {
        SetMuted(!IsMuted);
    }

    public void SetMuted(bool isMuted)
    {
        IsMuted = isMuted;
        foreach (var session in _sessions)
        {
            session.Window.SetMuted(isMuted);
        }
    }

    public void Stop()
    {
        _ = StopAsync();
    }

    public async Task StopAsync()
    {
        var sessions = ReplaceSessions([]);
        State = PlaybackState.Stopped;
        CurrentFilePath = null;
        await CloseSessionsAsync(sessions);
    }

    private RendererSession[] ReplaceSessions(IReadOnlyList<RendererSession> newSessions)
    {
        var oldSessions = _sessions.ToArray();
        _sessions.Clear();
        _sessions.AddRange(newSessions);

        foreach (var session in newSessions)
        {
            session.Window.ShowRenderer();
        }

        return oldSessions;
    }

    private void SchedulePostApplyReflow()
    {
        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            ReattachToDesktopAndReflow();
            _ = ReattachAfterDelayAsync(180);
            _ = ReattachAfterDelayAsync(650);
            _ = ReattachAfterDelayAsync(1400);
        }, DispatcherPriority.ApplicationIdle);
    }

    private async Task ReattachAfterDelayAsync(int delayMs)
    {
        await Task.Delay(delayMs);
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(ReattachToDesktopAndReflow, DispatcherPriority.Background);
    }

    private static async Task CloseSessionsAsync(IReadOnlyList<RendererSession> sessions)
    {
        foreach (var session in sessions)
        {
            await session.Window.CloseRendererAsync();
            await Dispatcher.Yield(DispatcherPriority.Background);
        }
    }

    private static async Task CloseSessionsAsync(List<RendererSession> sessions)
    {
        await CloseSessionsAsync((IReadOnlyList<RendererSession>)sessions);
        sessions.Clear();
    }

    public void Reattach()
    {
        ReattachToDesktopAndReflow();
    }

    private void ReattachToDesktopAndReflow()
    {
        foreach (var session in _sessions)
        {
            session.Attachment = _desktopHost.AttachRendererWindow(session.Window.Handle);
        }

        ReflowToCurrentMonitors();
    }

    public void ReflowToCurrentMonitors()
    {
        ReflowSessions(_sessions, null);
    }

    private void ReflowSessions(IReadOnlyList<RendererSession> sessions, IntPtr? host)
    {
        var monitors = _monitorManager.GetMonitors();
        foreach (var session in sessions)
        {
            var monitor = monitors.FirstOrDefault(item => item.DeviceName == session.MonitorDeviceName);
            if (monitor is null)
            {
                continue;
            }

            if (host is { } desktopHost)
            {
                session.Attachment = _desktopHost.AttachRendererWindow(session.Window.Handle, desktopHost);
            }

            ApplyMonitorBounds(session.Window, monitor);
            session.Bounds = monitor;
        }
    }

    private sealed class RendererSession
    {
        public RendererSession(WallpaperRendererWindow window, MonitorInfo bounds, DesktopAttachmentResult attachment)
        {
            Window = window;
            Bounds = bounds;
            Attachment = attachment;
        }

        public WallpaperRendererWindow Window { get; }
        public MonitorInfo Bounds { get; set; }
        public DesktopAttachmentResult Attachment { get; set; }
        public string MonitorDeviceName => Bounds.DeviceName;

        public RendererSessionStatus ToStatus()
        {
            return new RendererSessionStatus(
                MonitorDeviceName,
                Attachment.IsAttached,
                Attachment.HostHandle,
                Bounds.X,
                Bounds.Y,
                Bounds.Width,
                Bounds.Height,
                Attachment.ErrorMessage);
        }
    }

    private async Task<RendererSession> CreateRendererSessionAsync(IntPtr host, MonitorInfo monitor, string filePath, WallpaperType type, ScaleMode scaleMode)
    {
        WallpaperRendererWindow? window = null;

        try
        {
            window = new WallpaperRendererWindow
            {
                Left = -32000,
                Top = -32000,
                Width = 1,
                Height = 1,
                Opacity = 0,
                ShowActivated = false,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            window.Show();
            await Dispatcher.Yield(DispatcherPriority.Render);

            var attachment = _desktopHost.AttachRendererWindow(window.Handle, host);
            if (!attachment.IsAttached)
            {
                throw new InvalidOperationException(attachment.ErrorMessage ?? "Could not attach renderer below the desktop icons.");
            }

            ApplyMonitorBounds(window, monitor);
            await Dispatcher.Yield(DispatcherPriority.Background);
            window.Load(filePath, type, scaleMode);
            window.SetMuted(IsMuted);
            window.HideRenderer();
            return new RendererSession(window, monitor, attachment);
        }
        catch
        {
            if (window is not null)
            {
                await window.CloseRendererAsync();
            }

            State = PlaybackState.Error;
            throw;
        }
    }

    private static void ApplyMonitorBounds(WallpaperRendererWindow window, MonitorInfo monitor)
    {
        // Use Win32 SetWindowPos as the sole positioning authority.
        // After SetParent to the desktop host, WPF's TransformFromDevice uses a single DPI
        // value (from the host / primary monitor), which produces wrong sizes when monitors
        // have different DPIs or resolutions.  Win32 pixel coordinates are always correct.
        // WPF receives WM_SIZE from SetWindowPos and updates its layout automatically.
        DesktopHostService.SetRendererBounds(window.Handle, monitor.X, monitor.Y, monitor.Width, monitor.Height);
        window.SetRenderTargetSize(monitor.Width, monitor.Height);
    }
}

public sealed record MonitorWallpaperAssignment(string FilePath, ScaleMode ScaleMode);

public sealed record RendererSessionStatus(
    string MonitorDeviceName,
    bool IsAttachedBelowDesktopIcons,
    IntPtr HostHandle,
    int X,
    int Y,
    int Width,
    int Height,
    string? ErrorMessage);
