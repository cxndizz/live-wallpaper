using LiveWallpaperStudio.Data.Models;

namespace LiveWallpaperStudio.Data.Monitors;

public static class MonitorProfileService
{
    public static List<MonitorProfile> SyncProfiles(
        IEnumerable<string> monitorDeviceIds,
        IEnumerable<MonitorProfile> existingProfiles,
        Guid? defaultWallpaperId,
        ScaleMode defaultScaleMode,
        int defaultFpsLimit)
    {
        var existing = existingProfiles.ToDictionary(profile => profile.MonitorDeviceId, StringComparer.OrdinalIgnoreCase);
        var synced = new List<MonitorProfile>();

        foreach (var monitorDeviceId in monitorDeviceIds)
        {
            if (existing.TryGetValue(monitorDeviceId, out var profile))
            {
                synced.Add(profile);
                continue;
            }

            synced.Add(new MonitorProfile(monitorDeviceId, defaultWallpaperId, defaultScaleMode, defaultFpsLimit, 0));
        }

        return synced;
    }

    public static List<MonitorProfile> SetWallpaper(
        IEnumerable<MonitorProfile> profiles,
        string monitorDeviceId,
        Guid wallpaperId,
        ScaleMode scaleMode,
        int fpsLimit)
    {
        var updated = profiles.ToList();
        var index = updated.FindIndex(profile => string.Equals(profile.MonitorDeviceId, monitorDeviceId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            updated.Add(new MonitorProfile(monitorDeviceId, wallpaperId, scaleMode, fpsLimit, 0));
            return updated;
        }

        updated[index] = updated[index] with
        {
            WallpaperId = wallpaperId,
            ScaleMode = scaleMode,
            FpsLimit = fpsLimit
        };
        return updated;
    }

    public static List<MonitorProfile> SetScaleMode(
        IEnumerable<MonitorProfile> profiles,
        string monitorDeviceId,
        ScaleMode scaleMode,
        int fpsLimit)
    {
        var updated = profiles.ToList();
        var index = updated.FindIndex(profile => string.Equals(profile.MonitorDeviceId, monitorDeviceId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            updated.Add(new MonitorProfile(monitorDeviceId, null, scaleMode, fpsLimit, 0));
            return updated;
        }

        updated[index] = updated[index] with
        {
            ScaleMode = scaleMode,
            FpsLimit = fpsLimit
        };
        return updated;
    }

    public static List<MonitorProfile> SetAllWallpaper(
        IEnumerable<MonitorProfile> profiles,
        Guid wallpaperId,
        ScaleMode scaleMode,
        int fpsLimit)
    {
        return profiles
            .Select(profile => profile with
            {
                WallpaperId = wallpaperId,
                ScaleMode = scaleMode,
                FpsLimit = fpsLimit
            })
            .ToList();
    }

    public static List<MonitorProfile> SetFpsLimit(IEnumerable<MonitorProfile> profiles, int fpsLimit)
    {
        return profiles.Select(profile => profile with { FpsLimit = fpsLimit }).ToList();
    }
}
