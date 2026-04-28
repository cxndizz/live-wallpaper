using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Engine.DesktopHost;
using VlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace LiveWallpaperStudio.App;

public partial class WallpaperRendererWindow : Window, IDisposable
{
    private static readonly object LibVlcInitializationLock = new();
    private static bool _isLibVlcInitialized;
    private static string? _libVlcDirectoryPath;
    private static string? _libVlcPluginsDirectoryPath;

    private LibVLC? _libVlc;
    private VlcMediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private VideoView? _videoView;
    private ScaleMode _scaleMode = ScaleMode.Cover;
    private int _targetPixelWidth = 1;
    private int _targetPixelHeight = 1;
    private string _targetAspectRatio = "1:1";
    private bool _disposed;

    public WallpaperRendererWindow()
    {
        InitializeComponent();
        EnsureLibVlcInitialized();
        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) => Dispose();
    }

    public static Task WarmUpVideoEngineAsync()
    {
        return Task.Run(() =>
        {
            EnsureLibVlcInitialized();
            using var libVlc = new LibVLC(CreateLibVlcOptions());
        });
    }

    public IntPtr Handle { get; private set; }
    public bool IsVideoActive => VideoHost.Visibility == Visibility.Visible;
    public bool IsMuted { get; private set; } = true;

    public void HideRenderer()
    {
        Opacity = 0;
    }

    public void ShowRenderer()
    {
        Opacity = 1;
    }

    public void Load(string filePath, WallpaperType type, ScaleMode scaleMode)
    {
        _scaleMode = scaleMode;
        ApplyScaleMode(scaleMode);

        if (type == WallpaperType.Video)
        {
            ImageSurface.Source = null;
            ImageSurface.Visibility = Visibility.Collapsed;
            VideoHost.Visibility = Visibility.Visible;
            LoadVideo(filePath);
            return;
        }

        StopVideo();
        VideoHost.Visibility = Visibility.Collapsed;
        ImageSurface.Visibility = Visibility.Visible;
        ImageSurface.Source = new BitmapImage(new Uri(filePath, UriKind.Absolute));
    }

    public void Pause()
    {
        if (IsVideoActive)
        {
            _mediaPlayer?.Pause();
        }
    }

    public void Resume()
    {
        if (IsVideoActive)
        {
            _mediaPlayer?.Play();
        }
    }

    public void StopPlayback()
    {
        StopVideo();
        ImageSurface.Source = null;
    }

    public Task CloseRendererAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _disposed = true;

        var mediaPlayer = _mediaPlayer;
        var currentMedia = _currentMedia;
        var libVlc = _libVlc;
        _mediaPlayer = null;
        _currentMedia = null;
        _libVlc = null;

        if (_videoView is not null)
        {
            _videoView.MediaPlayer = null;
            VideoHost.Child = null;
            _videoView.Dispose();
            _videoView = null;
        }

        ImageSurface.Source = null;
        Hide();
        Close();
        GC.SuppressFinalize(this);

        return Task.Run(() =>
        {
            try
            {
                if (mediaPlayer is not null)
                {
                    mediaPlayer.EndReached -= OnVideoEnded;
                    mediaPlayer.Playing -= OnVideoOutputReady;
                }

                mediaPlayer?.Stop();
            }
            catch
            {
                // Best effort cleanup; the renderer window is already detached.
            }

            currentMedia?.Dispose();
            mediaPlayer?.Dispose();
            libVlc?.Dispose();
        });
    }

    public void SetMuted(bool isMuted)
    {
        IsMuted = isMuted;
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Mute = isMuted;
            _mediaPlayer.Volume = isMuted ? 0 : 100;
        }
    }

    public void SetRenderTargetSize(int pixelWidth, int pixelHeight)
    {
        _targetPixelWidth = Math.Max(1, pixelWidth);
        _targetPixelHeight = Math.Max(1, pixelHeight);
        _targetAspectRatio = FormatAspectRatio(_targetPixelWidth, _targetPixelHeight);
        ApplyVideoScaleMode();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopVideo();
        if (_videoView is not null)
        {
            _videoView.MediaPlayer = null;
            _videoView.Dispose();
            _videoView = null;
        }

        VideoHost.Child = null;
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.EndReached -= OnVideoEnded;
            _mediaPlayer.Playing -= OnVideoOutputReady;
        }

        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _libVlc?.Dispose();
        _libVlc = null;
        GC.SuppressFinalize(this);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        Handle = new WindowInteropHelper(this).Handle;
        DesktopHostService.StripWindowChrome(Handle);
    }

    private void ApplyScaleMode(ScaleMode scaleMode)
    {
        var stretch = scaleMode switch
        {
            ScaleMode.Contain => Stretch.Uniform,
            ScaleMode.Stretch => Stretch.Fill,
            ScaleMode.Center => Stretch.None,
            _ => Stretch.UniformToFill
        };

        ImageSurface.Stretch = stretch;
        ApplyVideoScaleMode();
    }

    private void LoadVideo(string filePath)
    {
        _libVlc ??= new LibVLC(CreateLibVlcOptions());
        EnsureVideoView();
        if (_mediaPlayer is null)
        {
            _mediaPlayer = new VlcMediaPlayer(_libVlc);
            _mediaPlayer.EndReached += OnVideoEnded;
            _mediaPlayer.Playing += OnVideoOutputReady;
            _videoView!.MediaPlayer = _mediaPlayer;
        }

        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, new Uri(filePath));
        _currentMedia.AddOption(":input-repeat=65535");
        _currentMedia.AddOption(":no-video-title-show");
        if (_scaleMode == ScaleMode.Cover || _scaleMode == ScaleMode.Stretch)
        {
            _currentMedia.AddOption($":aspect-ratio={_targetAspectRatio}");
        }

        if (_scaleMode == ScaleMode.Cover)
        {
            _currentMedia.AddOption($":crop={_targetAspectRatio}");
        }

        _mediaPlayer.Media = _currentMedia;
        SetMuted(IsMuted);
        ApplyVideoScaleMode();
        Dispatcher.BeginInvoke(() =>
        {
            if (!_disposed)
            {
                _mediaPlayer?.Play();
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void EnsureVideoView()
    {
        if (_videoView is not null)
        {
            return;
        }

        _videoView = new VideoView
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            BackColor = System.Drawing.Color.Black
        };
        VideoHost.Child = _videoView;
    }

    private static void EnsureLibVlcInitialized()
    {
        if (_isLibVlcInitialized)
        {
            return;
        }

        lock (LibVlcInitializationLock)
        {
            if (_isLibVlcInitialized)
            {
                return;
            }

            var libVlcDirectory = ResolveLibVlcDirectory();
            if (libVlcDirectory is not null)
            {
                var pluginsDirectory = Path.Combine(libVlcDirectory.FullName, "plugins");
                _libVlcDirectoryPath = libVlcDirectory.FullName;
                _libVlcPluginsDirectoryPath = Directory.Exists(pluginsDirectory) ? pluginsDirectory : null;
                ConfigureNativeDllSearchPath(libVlcDirectory.FullName);
                if (Directory.Exists(pluginsDirectory))
                {
                    Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginsDirectory);
                }

                Core.Initialize(libVlcDirectory.FullName);
            }
            else
            {
                Core.Initialize();
            }

            _isLibVlcInitialized = true;
        }
    }

    private static string[] CreateLibVlcOptions()
    {
        var options = new List<string>
        {
            "--no-video-title-show",
            "--quiet",
            "--avcodec-hw=any"
        };

        return [.. options];
    }

    private static DirectoryInfo? ResolveLibVlcDirectory()
    {
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.X86 => "win-x86",
            Architecture.Arm64 => "win-arm64",
            _ => "win-x64"
        };

        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "libvlc", architecture),
            Path.Combine(baseDirectory, "win-x64", "libvlc", architecture),
            Path.Combine(baseDirectory, "runtimes", architecture, "native"),
            Path.Combine(baseDirectory, "libvlc")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "libvlc.dll")))
            {
                return new DirectoryInfo(candidate);
            }
        }

        return null;
    }

    private static void ConfigureNativeDllSearchPath(string directory)
    {
        SetDllDirectory(directory);

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var entries = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!entries.Any(entry => string.Equals(entry, directory, StringComparison.OrdinalIgnoreCase)))
        {
            Environment.SetEnvironmentVariable("PATH", string.Concat(directory, Path.PathSeparator, path));
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string? lpPathName);

    private void ApplyVideoScaleMode()
    {
        if (_mediaPlayer is null)
        {
            return;
        }

        _mediaPlayer.Scale = _scaleMode == ScaleMode.Center ? 1 : 0;
        _mediaPlayer.CropGeometry = _scaleMode switch
        {
            ScaleMode.Cover => _targetAspectRatio,
            _ => string.Empty
        };
        _mediaPlayer.AspectRatio = _scaleMode switch
        {
            ScaleMode.Cover => _targetAspectRatio,
            ScaleMode.Stretch => _targetAspectRatio,
            _ => null
        };
    }

    private void ApplyVideoScaleModeAfterOutputReady()
    {
        _ = Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
            {
                return;
            }

            ApplyVideoScaleMode();
            _ = Task.Delay(150).ContinueWith(_ =>
            {
                if (!_disposed)
                {
                    Dispatcher.BeginInvoke(ApplyVideoScaleMode);
                }
            });
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void StopVideo()
    {
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Media = null;
        }

        _currentMedia?.Dispose();
        _currentMedia = null;
    }

    private void OnVideoEnded(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_disposed || _mediaPlayer?.Media is null)
            {
                return;
            }

            _mediaPlayer.Stop();
            _mediaPlayer.Play();
        });
    }

    private void OnVideoOutputReady(object? sender, EventArgs e)
    {
        ApplyVideoScaleModeAfterOutputReady();
    }

    private static string FormatAspectRatio(int width, int height)
    {
        var divisor = GreatestCommonDivisor(Math.Abs(width), Math.Abs(height));
        return $"{width / divisor}:{height / divisor}";
    }

    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0)
        {
            var next = left % right;
            left = right;
            right = next;
        }

        return Math.Max(left, 1);
    }
}
