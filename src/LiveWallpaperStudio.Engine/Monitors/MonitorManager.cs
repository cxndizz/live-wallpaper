using System.Windows.Forms;

namespace LiveWallpaperStudio.Engine.Monitors;

public sealed class MonitorManager
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Screen.AllScreens
            .Select(screen => new MonitorInfo(
                screen.DeviceName,
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height,
                screen.Primary))
            .ToList();
    }
}
