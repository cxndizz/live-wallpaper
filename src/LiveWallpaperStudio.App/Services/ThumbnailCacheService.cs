using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Storage;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfPoint = System.Windows.Point;

namespace LiveWallpaperStudio.App.Services;

public sealed class ThumbnailCacheService
{
    private const int ThumbnailWidth = 480;
    private const int ThumbnailHeight = 270;
    private static readonly object FfmpegResolutionLock = new();
    private static bool _hasResolvedFfmpeg;
    private static string? _resolvedFfmpegPath;
    private readonly AppDataPaths _paths;

    public ThumbnailCacheService(AppDataPaths paths)
    {
        _paths = paths;
    }

    public string? EnsureThumbnail(WallpaperItem item)
    {
        if (!File.Exists(item.FilePath))
        {
            return null;
        }

        _paths.EnsureCreated();
        var thumbnailPath = Path.Combine(_paths.Thumbnails, $"{item.Id:N}.jpg");
        if (File.Exists(thumbnailPath))
        {
            return thumbnailPath;
        }

        if (item.Type == WallpaperType.Video)
        {
            if (TrySaveVideoFrame(item.FilePath, thumbnailPath))
            {
                return thumbnailPath;
            }

            SaveVideoPlaceholder(item, thumbnailPath);
            return thumbnailPath;
        }

        if (item.Type != WallpaperType.Image)
        {
            return null;
        }

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.UriSource = new Uri(item.FilePath, UriKind.Absolute);
        source.DecodePixelWidth = ThumbnailWidth;
        source.EndInit();
        source.Freeze();

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(0, 0, ThumbnailWidth, ThumbnailHeight));

            var scale = Math.Max(ThumbnailWidth / source.Width, ThumbnailHeight / source.Height);
            var width = source.Width * scale;
            var height = source.Height * scale;
            var x = (ThumbnailWidth - width) / 2;
            var y = (ThumbnailHeight - height) / 2;
            context.DrawImage(source, new Rect(x, y, width, height));
        }

        SaveVisual(visual, thumbnailPath);
        return thumbnailPath;
    }

    private static bool TrySaveVideoFrame(string videoPath, string thumbnailPath)
    {
        var ffmpegPath = ResolveFfmpegPath();
        if (ffmpegPath is null)
        {
            return false;
        }

        var tempPath = Path.Combine(Path.GetDirectoryName(thumbnailPath)!, $"{Path.GetFileNameWithoutExtension(thumbnailPath)}.tmp.jpg");
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add("00:00:01");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(videoPath);
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-vf");
            startInfo.ArgumentList.Add($"scale={ThumbnailWidth}:{ThumbnailHeight}:force_original_aspect_ratio=increase,crop={ThumbnailWidth}:{ThumbnailHeight}");
            startInfo.ArgumentList.Add("-q:v");
            startInfo.ArgumentList.Add("3");
            startInfo.ArgumentList.Add(tempPath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(7000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort cleanup only.
                }

                return false;
            }

            if (process.ExitCode != 0 || !File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
            {
                return false;
            }

            File.Move(tempPath, thumbnailPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }

    private static string? ResolveFfmpegPath()
    {
        if (_hasResolvedFfmpeg)
        {
            return _resolvedFfmpegPath;
        }

        lock (FfmpegResolutionLock)
        {
            if (_hasResolvedFfmpeg)
            {
                return _resolvedFfmpegPath;
            }

            _resolvedFfmpegPath = ResolveFfmpegPathCore();
            _hasResolvedFfmpeg = true;
            return _resolvedFfmpegPath;
        }
    }

    private static string? ResolveFfmpegPathCore()
    {
        var configuredPath = Environment.GetEnvironmentVariable("LIVE_WALLPAPER_STUDIO_FFMPEG");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("ffmpeg");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            if (process.ExitCode != 0)
            {
                return null;
            }

            return output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(File.Exists);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveVideoPlaceholder(WallpaperItem item, string thumbnailPath)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush
            {
                StartPoint = new WpfPoint(0, 0),
                EndPoint = new WpfPoint(1, 1),
                GradientStops =
                {
                    new GradientStop(WpfColor.FromRgb(18, 28, 54), 0),
                    new GradientStop(WpfColor.FromRgb(47, 128, 255), 0.55),
                    new GradientStop(WpfColor.FromRgb(107, 56, 245), 1)
                }
            };
            context.DrawRectangle(background, null, new Rect(0, 0, ThumbnailWidth, ThumbnailHeight));
            context.DrawEllipse(new SolidColorBrush(WpfColor.FromArgb(48, 255, 255, 255)), null, new WpfPoint(410, 48), 110, 110);
            context.DrawEllipse(new SolidColorBrush(WpfColor.FromArgb(34, 0, 0, 0)), null, new WpfPoint(80, 225), 140, 90);

            var label = Path.GetExtension(item.FilePath).TrimStart('.').ToUpperInvariant();
            DrawText(context, label.Length == 0 ? "VIDEO" : label, 42, 28, 34, FontWeights.Bold, WpfBrushes.White);
            DrawText(context, item.Name, 42, 82, 26, FontWeights.SemiBold, WpfBrushes.White);
            DrawText(context, "Video wallpaper", 42, 122, 18, FontWeights.Normal, new SolidColorBrush(WpfColor.FromArgb(210, 255, 255, 255)));

            var playGeometry = Geometry.Parse("M 0 0 L 0 58 L 48 29 Z");
            playGeometry.Transform = new TranslateTransform(220, 176);
            context.DrawGeometry(new SolidColorBrush(WpfColor.FromArgb(225, 255, 255, 255)), null, playGeometry);
        }

        SaveVisual(visual, thumbnailPath);
    }

    private static void DrawText(DrawingContext context, string text, double x, double y, double size, FontWeight weight, WpfBrush brush)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            WpfFlowDirection.LeftToRight,
            new Typeface(new WpfFontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            1.0)
        {
            MaxTextWidth = ThumbnailWidth - x - 32,
            Trimming = TextTrimming.CharacterEllipsis
        };
        context.DrawText(formatted, new WpfPoint(x, y));
    }

    private static void SaveVisual(DrawingVisual visual, string thumbnailPath)
    {
        var bitmap = new RenderTargetBitmap(ThumbnailWidth, ThumbnailHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new JpegBitmapEncoder { QualityLevel = 88 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(thumbnailPath);
        encoder.Save(output);
    }

    public void DeleteThumbnail(WallpaperItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
        {
            File.Delete(item.ThumbnailPath);
        }
    }
}
