using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveWallpaperStudio.App.Services;
using LiveWallpaperStudio.Data.Config;
using LiveWallpaperStudio.Data.Library;
using LiveWallpaperStudio.Data.Models;
using LiveWallpaperStudio.Data.Monitors;
using LiveWallpaperStudio.Data.Performance;
using LiveWallpaperStudio.Data.Playback;
using LiveWallpaperStudio.Data.Settings;
using LiveWallpaperStudio.Data.Storage;
using LiveWallpaperStudio.Engine.Monitors;
using Forms = System.Windows.Forms;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfImage = System.Windows.Controls.Image;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfPoint = System.Windows.Point;

namespace LiveWallpaperStudio.App;

public partial class MainWindow : Window
{
    private const string AppDisplayName = "Wallora - Live wallpaper";
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly WallpaperWindowController _wallpaperController = new();
    private readonly AppDataPaths _paths = new();
    private readonly InMemoryWallpaperLibrary _library = new();
    private readonly ThumbnailCacheService _thumbnailCache;
    private readonly DiagnosticsExportService _diagnosticsExport;
    private readonly WindowsStartupService _startupService = new();
    private readonly SemaphoreSlim _applyLock = new(1, 1);
    private readonly SemaphoreSlim _importLock = new(1, 1);
    private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SettingsStore _settingsStore;
    private readonly WallpaperLibraryStore _libraryStore;
    private AppSettings _settings = new();
    private bool _isHydratingSettings;
    private Forms.ToolStripMenuItem? _pauseTrayItem;
    private Forms.ToolStripMenuItem? _nextTrayItem;
    private Forms.ToolStripMenuItem? _muteTrayItem;

    public MainWindow()
    {
        InitializeComponent();
        _settingsStore = new SettingsStore(_paths);
        _libraryStore = new WallpaperLibraryStore(_paths);
        _thumbnailCache = new ThumbnailCacheService(_paths);
        _diagnosticsExport = new DiagnosticsExportService(_paths);

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = AppDisplayName,
            Visible = true,
            Icon = LoadTrayIcon(),
            ContextMenuStrip = BuildTrayMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        Loaded += MainWindow_Loaded;
        RefreshStatus();
    }

    protected override void OnClosed(EventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        _wallpaperController.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.OnClosed(e);
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Wallora", null, (_, _) => ShowFromTray());
        _pauseTrayItem = new Forms.ToolStripMenuItem("Pause Wallpaper", null, (_, _) => Pause_Click(this, new RoutedEventArgs()));
        menu.Items.Add(_pauseTrayItem);
        _nextTrayItem = new Forms.ToolStripMenuItem("Next Wallpaper", null, (_, _) => ApplyNextWallpaper());
        menu.Items.Add(_nextTrayItem);
        _muteTrayItem = new Forms.ToolStripMenuItem("Unmute", null, (_, _) => ToggleMuteFromTray());
        menu.Items.Add(_muteTrayItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _wallpaperController.Stop();
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        });
        return menu;
    }

    private static Icon LoadTrayIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Brand/app-icon.ico"));
        return resource?.Stream is null ? SystemIcons.Application : new Icon(resource.Stream);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        HomePage.Visibility = sender == HomeNav ? Visibility.Visible : Visibility.Collapsed;
        LibraryPage.Visibility = sender == LibraryNav ? Visibility.Visible : Visibility.Collapsed;
        DisplaysPage.Visibility = sender == DisplaysNav ? Visibility.Visible : Visibility.Collapsed;
        PerformancePage.Visibility = sender == PerformanceNav ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = sender == SettingsNav ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        if (_settings.MinimizeToTray)
        {
            if (!_settings.KeepWallpaperRunningWhenClosed)
            {
                _wallpaperController.Stop();
                RefreshStatus();
            }

            Hide();
            _notifyIcon.ShowBalloonTip(1800, AppDisplayName, "The app is still running in the tray.", Forms.ToolTipIcon.Info);
            return;
        }

        Close();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OpenLibrary_Click(object sender, RoutedEventArgs e)
    {
        LibraryNav.IsChecked = true;
    }

    private async void AddWallpaper_Click(object sender, RoutedEventArgs e)
    {
        if (!await _importLock.WaitAsync(0))
        {
            ShowStatus("Import is busy", "Please wait for the current wallpaper import to finish.", isError: false);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Add wallpaper",
            Filter = "Wallpaper files|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.webm|Images|*.jpg;*.jpeg;*.png;*.bmp|Videos|*.mp4;*.webm|All files|*.*",
            Multiselect = true
        };

        try
        {
            if (dialog.ShowDialog(this) != true || dialog.FileNames.Length == 0)
            {
                return;
            }

            ShowStatus("Adding wallpaper", "Importing files into your library...", isError: false);
            await Dispatcher.Yield(DispatcherPriority.Background);

            var existingByPath = _library.Items
                .GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var selectedPaths = dialog.FileNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var newPaths = selectedPaths
                .Where(path => !existingByPath.ContainsKey(path))
                .ToList();
            var newItems = await Task.Run(() => newPaths.Select(CreateWallpaperItem).ToList());
            var importedItems = new List<WallpaperItem>();
            foreach (var fileName in selectedPaths)
            {
                if (existingByPath.TryGetValue(fileName, out var existing))
                {
                    importedItems.Add(existing);
                    continue;
                }

                var item = newItems.First(newItem => string.Equals(newItem.FilePath, fileName, StringComparison.OrdinalIgnoreCase));
                _library.Upsert(item);
                importedItems.Add(item);
            }

            await PersistLibraryAsync();
            RenderLibrary();
            ShowStatus(
                "Wallpaper added",
                importedItems.Count == 1 ? importedItems[0].Name : $"{importedItems.Count} wallpapers were added.",
                isError: false);

            _ = RefreshImportedThumbnailsAsync(importedItems);
        }
        catch (Exception ex)
        {
            ShowStatus("Wallpaper import failed", ex.Message, isError: true);
        }
        finally
        {
            _importLock.Release();
        }
    }

    private async Task ApplyWallpaperAsync(WallpaperItem item, bool saveAsLast)
    {
        if (!await _applyLock.WaitAsync(0))
        {
            ShowStatus("Wallpaper is busy", "Please wait for the current apply operation to finish.", isError: false);
            return;
        }

        try
        {
            if (!File.Exists(item.FilePath))
            {
                throw new FileNotFoundException("This wallpaper file is no longer available.", item.FilePath);
            }

            ShowStatus("Applying wallpaper", item.Name, isError: false);
            await Dispatcher.Yield(DispatcherPriority.Background);

            await _wallpaperController.ApplyToAllMonitorsAsync(item.FilePath, _settings.DefaultScaleMode);
            if (saveAsLast)
            {
                _settings = SettingsWorkflowService.SetLastWallpaper(_settings, item.Id);
                if (_settings.WallpaperMode == WallpaperMode.SameOnAllMonitors)
                {
                    SetAllMonitorProfiles(item.Id, _settings.DefaultScaleMode);
                }

                _ = _settingsStore.SaveAsync(_settings);
                RenderDisplays();
            }

            _notifyIcon.ShowBalloonTip(1200, "Wallpaper applied", item.Name, Forms.ToolTipIcon.Info);
            ShowStatus("Wallpaper applied", item.Name, isError: false);
            UpdateHomePreview(item);
            RenderLibrary();
        }
        catch (Exception ex)
        {
            ShowStatus("Wallpaper could not be applied", ex.Message, isError: true);
        }
        finally
        {
            _applyLock.Release();
            RefreshStatus();
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        _wallpaperController.TogglePause();
        RefreshStatus();
    }

    private void ToggleMuteFromTray()
    {
        _wallpaperController.ToggleMute();
        RefreshStatus();
        ShowStatus(
            _wallpaperController.IsMuted ? "Wallpaper muted" : "Wallpaper unmuted",
            _wallpaperController.IsMuted ? "Video wallpaper audio is off." : "Video wallpaper audio is on.",
            isError: false);
    }

    private void ShowStatus(string title, string message, bool isError)
    {
        StatusBanner.Visibility = Visibility.Visible;
        StatusBanner.BorderBrush = isError ? (WpfBrush)FindResource("Danger") : (WpfBrush)FindResource("AccentBlue");
        StatusBannerIcon.Text = isError ? "\uE783" : "\uE946";
        StatusBannerIcon.Foreground = isError ? (WpfBrush)FindResource("Danger") : (WpfBrush)FindResource("AccentBlue");
        StatusBannerText.Text = $"{title}: {message}";
    }

    private void StopEngine_Click(object sender, RoutedEventArgs e)
    {
        _wallpaperController.Stop();
        RefreshStatus();
    }

    private void Reattach_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _wallpaperController.Reattach();
            _notifyIcon.ShowBalloonTip(1200, "Reattached", "Renderer windows were reattached to the desktop.", Forms.ToolTipIcon.Info);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            ShowStatus("Reattach failed", ex.Message, isError: true);
        }
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _wallpaperController.ReflowToCurrentMonitors();
            RenderDisplays();
            RefreshStatus();
        }, DispatcherPriority.Background);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsStore.LoadAsync();
        var items = await _libraryStore.LoadAsync();
        _library.ReplaceAll(items);

        RenderLibrary();
        RenderDisplays();
        RenderScaleModes();
        RenderPerformanceSettings();
        RenderSettingsState();
        UpdateHomePreviewFromCurrent();
        RefreshStatus();
        _ = WallpaperRendererWindow.WarmUpVideoEngineAsync();
        _ = RestoreLastWallpaperAfterStartupAsync();
    }

    private async Task PersistLibraryAsync()
    {
        await _libraryStore.SaveAsync(_library.Items);
    }

    private static WallpaperItem CreateWallpaperItem(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return new WallpaperItem(
            Guid.NewGuid(),
            Path.GetFileNameWithoutExtension(filePath),
            filePath,
            InMemoryWallpaperLibrary.DetectType(filePath),
            null,
            fileInfo.Exists ? fileInfo.Length : 0,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            false);
    }

    private async Task RestoreLastWallpaperAsync()
    {
        if (_settings.WallpaperMode == WallpaperMode.DifferentPerMonitor)
        {
            await ApplyMonitorProfilesAsync(showErrors: false);
            return;
        }

        var item = WallpaperPlaybackWorkflowService.GetRestorableWallpaper(_settings, _library.Items, File.Exists);
        if (item is not null)
        {
            await ApplyWallpaperAsync(item, saveAsLast: false);
        }
    }

    private async Task RestoreLastWallpaperAfterStartupAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);
        await RestoreLastWallpaperAsync();
    }

    private async Task ApplyMonitorProfilesAsync(bool showErrors)
    {
        var assignments = WallpaperPlaybackWorkflowService
            .GetMonitorAssignments(_settings, _library.Items, File.Exists)
            .ToDictionary(
                assignment => assignment.MonitorDeviceId,
                assignment => new MonitorWallpaperAssignment(assignment.Wallpaper.FilePath, assignment.ScaleMode),
                StringComparer.OrdinalIgnoreCase);

        if (assignments.Count == 0)
        {
            if (showErrors)
            {
                ShowStatus("No monitor wallpaper", "No monitor has an available wallpaper assigned yet.", isError: true);
            }

            return;
        }

        if (!await _applyLock.WaitAsync(0))
        {
            ShowStatus("Wallpaper is busy", "Please wait for the current apply operation to finish.", isError: false);
            return;
        }

        try
        {
            ShowStatus("Applying monitor wallpapers", "Preparing renderer windows...", isError: false);
            await Dispatcher.Yield(DispatcherPriority.Background);
            await _wallpaperController.ApplyPerMonitorAsync(assignments);
            ShowStatus("Wallpaper applied", $"{assignments.Count} monitor{(assignments.Count == 1 ? string.Empty : "s")} updated.", isError: false);
        }
        catch (Exception ex)
        {
            if (showErrors)
            {
                ShowStatus("Per-monitor wallpaper failed", ex.Message, isError: true);
            }
        }
        finally
        {
            _applyLock.Release();
            RefreshStatus();
        }
    }

    private void RenderLibrary()
    {
        LibraryItemsPanel.Children.Clear();
        var query = SearchBox.Text?.Trim();
        var items = LibraryWorkflowService.Filter(_library.Items, query);

        LibraryEmptyState.Visibility = _library.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var item in items)
        {
            LibraryItemsPanel.Children.Add(CreateWallpaperCard(item));
        }
    }

    private void RenderDisplays()
    {
        var monitors = _wallpaperController.GetMonitors();
        if (EnsureMonitorProfiles(monitors))
        {
            _ = _settingsStore.SaveAsync(_settings);
        }

        MonitorCardsPanel.Children.Clear();
        for (var index = 0; index < monitors.Count; index++)
        {
            MonitorCardsPanel.Children.Add(CreateMonitorCard(monitors[index], index + 1));
        }

        WallpaperModePanel.Children.Clear();
        WallpaperModePanel.Children.Add(CreateWallpaperModeCard(
            WallpaperMode.SameOnAllMonitors,
            "Same wallpaper on all monitors",
            "Show the same wallpaper on every connected monitor."));
        WallpaperModePanel.Children.Add(CreateWallpaperModeCard(
            WallpaperMode.DifferentPerMonitor,
            "Different wallpaper per monitor",
            "Set a unique wallpaper for each monitor."));
        WallpaperModePanel.Children.Add(CreateWallpaperModeCard(
            WallpaperMode.SpanAcrossAllMonitors,
            "Span one wallpaper across all monitors",
            "Stretch one wallpaper across monitors."));

        MonitorProfilesPanel.Children.Clear();
        MonitorProfilesEmptyState.Visibility = _settings.MonitorProfiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var profile in _settings.MonitorProfiles)
        {
            MonitorProfilesPanel.Children.Add(CreateMonitorProfileRow(profile));
        }
    }

    private bool EnsureMonitorProfiles(IReadOnlyList<MonitorInfo> monitors)
    {
        var updated = SettingsWorkflowService.SyncMonitorProfiles(_settings, monitors.Select(monitor => monitor.DeviceName));
        var changed = updated.MonitorProfiles.Count != _settings.MonitorProfiles.Count
            || updated.MonitorProfiles.Where((profile, index) => profile != _settings.MonitorProfiles[index]).Any();
        _settings = updated;
        return changed;
    }

    private Border CreateMonitorCard(MonitorInfo monitor, int index)
    {
        var card = new Border
        {
            Width = 300,
            Height = 132,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(18),
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14)
        };

        var aspect = CalculateAspectRatio(monitor.Width, monitor.Height);
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(118) }
            }
        };
        card.Child = grid;

        var textStack = new StackPanel();
        grid.Children.Add(textStack);
        var titleRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
        titleRow.Children.Add(new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(13),
            Background = (WpfBrush)FindResource("AccentGradient"),
            Child = new TextBlock
            {
                Text = index.ToString(),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        titleRow.Children.Add(new TextBlock
        {
            Text = $"Monitor {index}{(monitor.IsPrimary ? "  Primary" : "")}",
            Margin = new Thickness(10, 0, 0, 0),
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        textStack.Children.Add(titleRow);
        textStack.Children.Add(new TextBlock
        {
            Text = $"{monitor.Width} x {monitor.Height} ({aspect})",
            Foreground = (WpfBrush)FindResource("MutedText"),
            Margin = new Thickness(36, 5, 0, 0)
        });
        textStack.Children.Add(new TextBlock
        {
            Text = $"{monitor.DeviceName}  |  X {monitor.X}, Y {monitor.Y}",
            Foreground = (WpfBrush)FindResource("MutedText"),
            FontSize = 11,
            Margin = new Thickness(36, 12, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var monitorPreview = new Grid
        {
            Width = 112,
            Height = 70,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        Grid.SetColumn(monitorPreview, 1);
        monitorPreview.Children.Add(new Border
        {
            Width = 104,
            Height = 58,
            CornerRadius = new CornerRadius(8),
            BorderBrush = (WpfBrush)FindResource("AccentBlue"),
            BorderThickness = new Thickness(1),
            Background = CreateTypeBrush(index % 2 == 0 ? WallpaperType.Video : WallpaperType.Image),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        });
        monitorPreview.Children.Add(new Border
        {
            Width = 30,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = (WpfBrush)FindResource("MutedText"),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        });
        grid.Children.Add(monitorPreview);

        return card;
    }

    private TextBlock CreateIcon(string glyph, double fontSize = 16)
    {
        return UiElementFactory.CreateIcon(FindResource, glyph, fontSize);
    }

    private StackPanel CreateIconText(string glyph, string label, double iconSize = 14)
    {
        return UiElementFactory.CreateIconText(FindResource, glyph, label, iconSize);
    }

    private StackPanel CreateOptionContent(bool isSelected, string glyph, string title, string description)
    {
        return UiElementFactory.CreateOptionContent(FindResource, isSelected, glyph, title, description);
    }
    private Border CreateWallpaperModeCard(WallpaperMode mode, string title, string description)
    {
        var isSelected = _settings.WallpaperMode == mode;
        var card = new Border
        {
            MinHeight = 92,
            Margin = new Thickness(0, 0, 12, 0),
            Padding = new Thickness(16),
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = isSelected ? (WpfBrush)FindResource("AccentBlue") : (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Cursor = WpfCursors.Hand
        };

        var icon = mode switch
        {
            WallpaperMode.DifferentPerMonitor => "\uE7F4",
            WallpaperMode.SpanAcrossAllMonitors => "\uE8A9",
            _ => "\uE8B7"
        };
        card.Child = CreateOptionContent(isSelected, icon, title, description);
        card.MouseLeftButtonUp += async (_, _) =>
        {
            _settings = SettingsWorkflowService.SetWallpaperMode(_settings, mode);
            await _settingsStore.SaveAsync(_settings);
            if (mode == WallpaperMode.DifferentPerMonitor)
            {
                await ApplyMonitorProfilesAsync(showErrors: false);
            }
            else if (mode == WallpaperMode.SameOnAllMonitors)
            {
                await RestoreLastWallpaperAsync();
            }

            RenderDisplays();
        };
        return card;
    }
    private void RenderScaleModes()
    {
        ScaleModePanel.Children.Clear();
        ScaleModePanel.Children.Add(CreateScaleModeCard(ScaleMode.Cover, "Fill / Cover", "Fill the screen and crop edges if needed."));
        ScaleModePanel.Children.Add(CreateScaleModeCard(ScaleMode.Contain, "Fit / Contain", "Show the full wallpaper with possible bars."));
        ScaleModePanel.Children.Add(CreateScaleModeCard(ScaleMode.Stretch, "Stretch", "Fill the screen by stretching the image."));
        ScaleModePanel.Children.Add(CreateScaleModeCard(ScaleMode.Center, "Center", "Keep the wallpaper at original size."));
    }

    private Border CreateScaleModeCard(ScaleMode scaleMode, string title, string description)
    {
        var isSelected = _settings.DefaultScaleMode == scaleMode;
        var card = new Border
        {
            MinHeight = 88,
            Margin = new Thickness(0, 0, 12, 0),
            Padding = new Thickness(14),
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = isSelected ? (WpfBrush)FindResource("AccentBlue") : (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Cursor = WpfCursors.Hand
        };

        var icon = scaleMode switch
        {
            ScaleMode.Contain => "\uE9A6",
            ScaleMode.Stretch => "\uE8A9",
            ScaleMode.Center => "\uE8B5",
            _ => "\uE8B7"
        };
        card.Child = CreateOptionContent(isSelected, icon, title, description);
        card.MouseLeftButtonUp += async (_, _) =>
        {
            _settings = SettingsWorkflowService.SetDefaultScaleMode(_settings, scaleMode);
            await _settingsStore.SaveAsync(_settings);
            RenderScaleModes();
            RefreshStatus();
        };

        return card;
    }
    private void RenderPerformanceSettings()
    {
        PerformancePresetPanel.Children.Clear();
        PerformancePresetPanel.Children.Add(CreatePerformancePresetCard(PerformancePreset.Eco, "Eco", "Lower FPS and pause more aggressively."));
        PerformancePresetPanel.Children.Add(CreatePerformancePresetCard(PerformancePreset.Balanced, "Balanced", "Smooth wallpaper with sensible defaults."));
        PerformancePresetPanel.Children.Add(CreatePerformancePresetCard(PerformancePreset.Quality, "Quality", "Higher FPS and fewer automatic pauses."));
        PerformancePresetPanel.Children.Add(CreatePerformancePresetCard(PerformancePreset.Custom, "Custom", "Use your own pause rules and FPS."));

        FpsPanel.Children.Clear();
        FpsPanel.Children.Add(CreateFpsButton(15));
        FpsPanel.Children.Add(CreateFpsButton(30));
        FpsPanel.Children.Add(CreateFpsButton(60));

        _isHydratingSettings = true;
        PauseFullscreenCheckBox.IsChecked = _settings.PauseWhenFullscreen;
        PauseGameCheckBox.IsChecked = _settings.PauseWhenGameRunning;
        PauseBatteryCheckBox.IsChecked = _settings.PauseWhenOnBattery;
        PauseLockCheckBox.IsChecked = _settings.PauseWhenScreenLocked;
        _isHydratingSettings = false;
    }

    private Border CreatePerformancePresetCard(PerformancePreset preset, string title, string description)
    {
        var isSelected = _settings.PerformancePreset == preset;
        var card = new Border
        {
            MinHeight = 98,
            Margin = new Thickness(0, 0, 12, 0),
            Padding = new Thickness(14),
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = isSelected ? (WpfBrush)FindResource("AccentBlue") : (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Cursor = WpfCursors.Hand
        };

        var icon = preset switch
        {
            PerformancePreset.Eco => "\uE90A",
            PerformancePreset.Quality => "\uE734",
            PerformancePreset.Custom => "\uE9D9",
            _ => "\uE9D2"
        };
        card.Child = CreateOptionContent(isSelected, icon, title, description);
        card.MouseLeftButtonUp += async (_, _) =>
        {
            _settings = PerformanceSettingsService.ApplyPreset(_settings, preset);
            await _settingsStore.SaveAsync(_settings);
            RenderPerformanceSettings();
            RenderDisplays();
            RefreshStatus();
        };

        return card;
    }
    private WpfButton CreateFpsButton(int fps)
    {
        var button = new WpfButton
        {
            Content = fps.ToString(),
            Width = 80,
            Height = 36,
            Margin = new Thickness(0, 0, 10, 0),
            Foreground = (WpfBrush)FindResource("Text"),
            Background = _settings.FpsLimit == fps ? (WpfBrush)FindResource("AccentGradient") : new SolidColorBrush(WpfColor.FromRgb(24, 35, 51)),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            Cursor = WpfCursors.Hand
        };
        button.Click += async (_, _) =>
        {
            _settings = PerformanceSettingsService.SetFpsLimit(_settings, fps);
            await _settingsStore.SaveAsync(_settings);
            RenderPerformanceSettings();
            RenderDisplays();
            RefreshStatus();
        };
        return button;
    }

    private async void PauseRule_Changed(object sender, RoutedEventArgs e)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings = PerformanceSettingsService.SetPauseRules(
            _settings,
            PauseFullscreenCheckBox.IsChecked == true,
            PauseGameCheckBox.IsChecked == true,
            PauseBatteryCheckBox.IsChecked == true,
            PauseLockCheckBox.IsChecked == true);
        await _settingsStore.SaveAsync(_settings);
        RenderPerformanceSettings();
        RefreshStatus();
    }

    private void RenderSettingsState()
    {
        _isHydratingSettings = true;
        StartWithWindowsCheckBox.IsChecked = _startupService.IsEnabled();
        MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
        KeepRunningCheckBox.IsChecked = _settings.KeepWallpaperRunningWhenClosed;
        _isHydratingSettings = false;
    }

    private async void GeneralSetting_Changed(object sender, RoutedEventArgs e)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings = SettingsWorkflowService.SetGeneralSettings(
            _settings,
            StartWithWindowsCheckBox.IsChecked == true,
            MinimizeToTrayCheckBox.IsChecked == true,
            KeepRunningCheckBox.IsChecked == true);
        _startupService.SetEnabled(_settings.StartWithWindows);
        await _settingsStore.SaveAsync(_settings);
    }

    private async void ApplyNextWallpaper()
    {
        var next = LibraryWorkflowService.GetNextAvailable(_library.Items, _settings.LastWallpaperId, File.Exists);
        if (next is null)
        {
            _notifyIcon.ShowBalloonTip(1200, "No wallpapers", "Add wallpapers to the Library first.", Forms.ToolTipIcon.Info);
            ShowStatus("No wallpapers", "Add wallpapers to the Library first.", isError: true);
            return;
        }

        await ApplyWallpaperAsync(next, saveAsLast: true);
    }

    private async void ClearThumbnailCache_Click(object sender, RoutedEventArgs e)
    {
        if (!ShowConfirmDialog("Clear thumbnail cache", "Clear generated thumbnails? They will be recreated when needed."))
        {
            return;
        }

        _paths.EnsureCreated();
        if (Directory.Exists(_paths.Thumbnails))
        {
            await Task.Run(() =>
            {
                foreach (var file in Directory.EnumerateFiles(_paths.Thumbnails))
                {
                    File.Delete(file);
                }
            });
        }
        _bitmapCache.Clear();

        foreach (var item in LibraryWorkflowService.ClearThumbnailReferences(_library.Items))
        {
            _library.Upsert(item);
        }

        await PersistLibraryAsync();
        RenderLibrary();
        ShowStatus("Thumbnail cache cleared", "Thumbnails will be recreated as wallpapers appear.", isError: false);
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        _paths.EnsureCreated();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{_paths.Root}\"",
            UseShellExecute = true
        });
    }

    private void ExportDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _diagnosticsExport.Export(_settings, _library.Items, _wallpaperController.GetMonitors());
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true
        });
    }

    private async void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!ShowConfirmDialog("Reset settings", "Reset app settings? Your wallpaper library will be kept.", isDanger: true))
        {
            return;
        }

        _settings = SettingsWorkflowService.Reset();
        _startupService.SetEnabled(false);
        await _settingsStore.SaveAsync(_settings);
        RenderSettingsState();
        RenderPerformanceSettings();
        RenderScaleModes();
        RenderDisplays();
        RefreshStatus();
    }

    private Border CreateMonitorProfileRow(MonitorProfile profile)
    {
        var wallpaperName = _library.Items.FirstOrDefault(item => item.Id == profile.WallpaperId)?.Name ?? "No wallpaper assigned";
        var border = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(15, 25, 38)),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        grid.Children.Add(new TextBlock
        {
            Text = $"{profile.MonitorDeviceId}  |  {wallpaperName}  |  {profile.ScaleMode}  |  {profile.FpsLimit} FPS",
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });

        var chooseButton = CreateInlineButton("Choose Wallpaper", async (_, e) =>
        {
            e.Handled = true;
            await ChooseWallpaperForMonitorAsync(profile.MonitorDeviceId);
        });
        Grid.SetColumn(chooseButton, 1);
        grid.Children.Add(chooseButton);

        var scaleButton = CreateInlineButton("Scale", (_, e) =>
        {
            e.Handled = true;
            ShowMonitorScaleMenu(profile.MonitorDeviceId, (FrameworkElement)e.Source);
        });
        Grid.SetColumn(scaleButton, 2);
        grid.Children.Add(scaleButton);

        var applyButton = CreateInlineButton("Apply Profiles", async (_, e) =>
        {
            e.Handled = true;
            _settings = SettingsWorkflowService.SetWallpaperMode(_settings, WallpaperMode.DifferentPerMonitor);
            await _settingsStore.SaveAsync(_settings);
            await ApplyMonitorProfilesAsync(showErrors: true);
            RenderDisplays();
        });
        Grid.SetColumn(applyButton, 3);
        grid.Children.Add(applyButton);

        border.Child = grid;
        return border;
    }

    private WpfButton CreateInlineButton(string text, MouseButtonEventHandler handler)
    {
        var button = new WpfButton
        {
            Content = text,
            Height = 28,
            MinWidth = 112,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 12,
            Foreground = (WpfBrush)FindResource("Text"),
            Background = new SolidColorBrush(WpfColor.FromRgb(24, 35, 51)),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            Cursor = WpfCursors.Hand
        };
        button.PreviewMouseLeftButtonUp += handler;
        return button;
    }

    private async Task ChooseWallpaperForMonitorAsync(string monitorDeviceId)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Choose wallpaper for {monitorDeviceId}",
            Filter = "Wallpaper files|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.webm|Images|*.jpg;*.jpeg;*.png;*.bmp|Videos|*.mp4;*.webm|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var item = _library.AddFile(dialog.FileName);
        item = await EnsureThumbnailAsync(item);
        _library.Upsert(item);
        SetMonitorProfileWallpaper(monitorDeviceId, item.Id, _settings.DefaultScaleMode);
        _settings = SettingsWorkflowService.SetWallpaperMode(_settings, WallpaperMode.DifferentPerMonitor);

        await PersistLibraryAsync();
        await _settingsStore.SaveAsync(_settings);
        RenderLibrary();
        RenderDisplays();
        await ApplyMonitorProfilesAsync(showErrors: true);
    }

    private void SetMonitorProfileWallpaper(string monitorDeviceId, Guid wallpaperId, ScaleMode scaleMode)
    {
        _settings = SettingsWorkflowService.SetMonitorWallpaper(
            _settings,
            monitorDeviceId,
            wallpaperId,
            scaleMode);
    }

    private void SetMonitorProfileScaleMode(string monitorDeviceId, ScaleMode scaleMode)
    {
        _settings = SettingsWorkflowService.SetMonitorScaleMode(
            _settings,
            monitorDeviceId,
            scaleMode);
    }

    private void ShowMonitorScaleMenu(string monitorDeviceId, FrameworkElement placementTarget)
    {
        var menu = new ContextMenu();
        foreach (var scaleMode in Enum.GetValues<ScaleMode>())
        {
            var item = new MenuItem
            {
                Header = FormatScaleMode(scaleMode),
                Tag = scaleMode
            };
            item.Click += async (_, _) =>
            {
                SetMonitorProfileScaleMode(monitorDeviceId, scaleMode);
                await _settingsStore.SaveAsync(_settings);
                if (_settings.WallpaperMode == WallpaperMode.DifferentPerMonitor)
                {
                    await ApplyMonitorProfilesAsync(showErrors: false);
                }

                RenderDisplays();
                RefreshStatus();
            };
            menu.Items.Add(item);
        }

        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
    }

    private void SetAllMonitorProfiles(Guid wallpaperId, ScaleMode scaleMode)
    {
        var monitors = _wallpaperController.GetMonitors();
        EnsureMonitorProfiles(monitors);
        _settings = SettingsWorkflowService.SetAllMonitorWallpapers(
            _settings,
            wallpaperId,
            scaleMode);
    }

    private static string CalculateAspectRatio(int width, int height)
    {
        var divisor = GreatestCommonDivisor(width, height);
        return $"{width / divisor}:{height / divisor}";
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return Math.Abs(a);
    }

    private Border CreateWallpaperCard(WallpaperItem item)
    {
        var fileExists = File.Exists(item.FilePath);
        var isSelected = IsWallpaperActive(item);
        var card = new Border
        {
            Width = 282,
            Height = 226,
            Margin = new Thickness(0, 0, 14, 14),
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = isSelected
                ? (WpfBrush)FindResource("AccentBlue")
                : fileExists ? (WpfBrush)FindResource("Border") : (WpfBrush)FindResource("Danger"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            ClipToBounds = true,
            Cursor = WpfCursors.Hand
        };

        var grid = new Grid();
        card.Child = grid;

        var thumbnailPath = item.ThumbnailPath;
        if (!string.IsNullOrWhiteSpace(thumbnailPath) && File.Exists(thumbnailPath))
        {
            grid.Children.Add(new WpfImage
            {
                Source = LoadBitmap(thumbnailPath),
                Stretch = Stretch.UniformToFill
            });
        }
        else
        {
            grid.Children.Add(new Border
            {
                Background = CreateTypeBrush(item.Type)
            });

            grid.Children.Add(new TextBlock
            {
                Text = fileExists ? item.Type == WallpaperType.Video ? "MP4" : item.Type.ToString().ToUpperInvariant() : "MISSING",
                FontSize = 34,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(WpfColor.FromArgb(95, 255, 255, 255)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        if (isSelected)
        {
            grid.Children.Add(new Border
            {
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = (WpfBrush)FindResource("AccentGradient"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 10, 0, 0),
                Padding = new Thickness(10, 0, 10, 0),
                Child = new StackPanel
                {
                    Orientation = WpfOrientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 8,
                            Height = 8,
                            CornerRadius = new CornerRadius(4),
                            Background = new SolidColorBrush(WpfColor.FromRgb(72, 215, 122)),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Active",
                            Margin = new Thickness(8, 0, 0, 0),
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            });
        }

        var footer = new Border
        {
            Height = 66,
            CornerRadius = new CornerRadius(0, 0, 14, 14),
            Background = new SolidColorBrush(WpfColor.FromArgb(220, 7, 16, 27)),
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(12, 8, 12, 8)
        };
        var footerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        var titleDock = new DockPanel { LastChildFill = true };
        titleDock.Children.Add(new TextBlock
        {
            Text = item.Type == WallpaperType.Video ? "Video" : item.Type.ToString(),
            Foreground = (WpfBrush)FindResource("MutedText"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        });
        titleDock.Children.Add(new TextBlock
        {
            Text = item.Name,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });
        footerGrid.Children.Add(titleDock);

        var metaText = new TextBlock
        {
            Text = fileExists ? $"{item.Type}  |  {FormatFileSize(item.FileSize)}" : "Missing file",
            Foreground = fileExists ? (WpfBrush)FindResource("MutedText") : (WpfBrush)FindResource("Danger"),
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(metaText, 1);
        footerGrid.Children.Add(metaText);

        var actionsButton = new WpfButton
        {
            Content = CreateIcon("\uE712", 15),
            Width = 34,
            Height = 34,
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(0),
            Foreground = (WpfBrush)FindResource("Text"),
            Background = (WpfBrush)FindResource("PanelSoft"),
            BorderBrush = new SolidColorBrush(WpfColor.FromRgb(62, 80, 107)),
            BorderThickness = new Thickness(1),
            Cursor = WpfCursors.Hand,
            ToolTip = "Wallpaper actions"
        };
        actionsButton.Click += (_, e) =>
        {
            e.Handled = true;
            ShowWallpaperActionsMenu(item, actionsButton);
        };
        Grid.SetColumn(actionsButton, 1);
        Grid.SetRowSpan(actionsButton, 2);
        footerGrid.Children.Add(actionsButton);
        footer.Child = footerGrid;

        grid.Children.Add(footer);
        card.MouseLeftButtonUp += async (_, _) => await ApplyWallpaperAsync(item, saveAsLast: true);
        return card;
    }

    private void ShowWallpaperActionsMenu(WallpaperItem item, FrameworkElement placementTarget)
    {
        var menu = new ContextMenu();
        AddMenuItem(menu, "Apply", async () =>
        {
            await ApplyWallpaperAsync(item, saveAsLast: true);
        });
        AddMenuItem(menu, "Apply to monitor...", async () =>
        {
            ShowApplyToMonitorMenu(item, placementTarget);
            await Task.CompletedTask;
        });
        AddMenuItem(menu, "Rename...", async () => await RenameWallpaperAsync(item));
        AddMenuItem(menu, "Open file location", async () =>
        {
            OpenFileLocation(item);
            await Task.CompletedTask;
        });

        if (!File.Exists(item.FilePath))
        {
            AddMenuItem(menu, "Relink...", async () =>
            {
                RelinkWallpaper(item);
                await Task.CompletedTask;
            });
        }

        menu.Items.Add(new Separator());
        AddMenuItem(menu, "Remove from library", async () =>
        {
            RemoveWallpaper(item);
            await Task.CompletedTask;
        });

        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
    }

    private static void AddMenuItem(ContextMenu menu, string header, Func<Task> action)
    {
        var menuItem = new MenuItem { Header = header };
        menuItem.Click += async (_, _) => await action();
        menu.Items.Add(menuItem);
    }

    private void ShowApplyToMonitorMenu(WallpaperItem item, FrameworkElement placementTarget)
    {
        if (!File.Exists(item.FilePath))
        {
            ShowStatus("File missing", "This wallpaper file is no longer available.", isError: true);
            return;
        }

        var menu = new ContextMenu();
        var monitors = _wallpaperController.GetMonitors();
        for (var index = 0; index < monitors.Count; index++)
        {
            var monitor = monitors[index];
            var menuItem = new MenuItem
            {
                Header = $"Monitor {index + 1}  ({monitor.Width} x {monitor.Height})",
                Tag = monitor.DeviceName
            };
            menuItem.Click += async (_, _) => await ApplyWallpaperToMonitorAsync(item, monitor.DeviceName);
            menu.Items.Add(menuItem);
        }

        if (menu.Items.Count == 0)
        {
            return;
        }

        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
    }

    private async Task ApplyWallpaperToMonitorAsync(WallpaperItem item, string monitorDeviceId)
    {
        SetMonitorProfileWallpaper(monitorDeviceId, item.Id, _settings.DefaultScaleMode);
        _settings = SettingsWorkflowService.SetWallpaperMode(_settings, WallpaperMode.DifferentPerMonitor);
        await _settingsStore.SaveAsync(_settings);
        await ApplyMonitorProfilesAsync(showErrors: true);
        UpdateHomePreview(item);
        RenderLibrary();
        RenderDisplays();
        RefreshStatus();
    }

    private WallpaperItem EnsureThumbnail(WallpaperItem item)
    {
        if (item.Type is not (WallpaperType.Image or WallpaperType.Video))
        {
            return item;
        }

        try
        {
            var thumbnailPath = _thumbnailCache.EnsureThumbnail(item);
            return thumbnailPath is null ? item : item with { ThumbnailPath = thumbnailPath };
        }
        catch
        {
            return item;
        }
    }

    private Task<WallpaperItem> EnsureThumbnailAsync(WallpaperItem item)
    {
        return Task.Run(() => EnsureThumbnail(item));
    }

    private async Task RefreshImportedThumbnailsAsync(IReadOnlyList<WallpaperItem> items)
    {
        try
        {
            var candidates = items
                .Where(item => item.Type is WallpaperType.Image or WallpaperType.Video)
                .Where(item => string.IsNullOrWhiteSpace(item.ThumbnailPath) || !File.Exists(item.ThumbnailPath))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            ShowStatus("Preparing thumbnails", $"{candidates.Count} wallpaper preview{(candidates.Count == 1 ? "" : "s")} will update shortly.", isError: false);

            var updatedItems = await Task.Run(() => candidates.Select(EnsureThumbnail).ToList());
            foreach (var updated in updatedItems)
            {
                _library.Upsert(updated);
                if (!string.IsNullOrWhiteSpace(updated.ThumbnailPath))
                {
                    _bitmapCache.Remove(updated.ThumbnailPath);
                }
            }

            await PersistLibraryAsync();
            RenderLibrary();
            ShowStatus("Thumbnails ready", $"{updatedItems.Count} wallpaper preview{(updatedItems.Count == 1 ? "" : "s")} updated.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus("Thumbnail update failed", ex.Message, isError: true);
        }
    }

    private async Task RenameWallpaperAsync(WallpaperItem item)
    {
        var newName = PromptForName(item.Name);
        if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName.Trim(), item.Name, StringComparison.Ordinal))
        {
            return;
        }

        var renamed = item with { Name = newName.Trim() };
        _library.Upsert(renamed);
        await PersistLibraryAsync();
        RenderLibrary();
        RefreshStatus();
        ShowStatus("Wallpaper renamed", renamed.Name, isError: false);
    }

    private string? PromptForName(string currentName)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = "Rename wallpaper",
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = (WpfBrush)FindResource("Panel")
        };

        var input = new System.Windows.Controls.TextBox
        {
            Text = currentName,
            Margin = new Thickness(0, 10, 0, 18),
            Height = 34,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var okButton = new WpfButton { Content = "Rename", Width = 96, Height = 32, IsDefault = true };
        var cancelButton = new WpfButton { Content = "Cancel", Width = 96, Height = 32, Margin = new Thickness(10, 0, 0, 0), IsCancel = true };
        okButton.Click += (_, _) => dialog.DialogResult = true;

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = "Wallpaper name", Foreground = (WpfBrush)FindResource("Text"), FontWeight = FontWeights.SemiBold });
        root.Children.Add(input);
        root.Children.Add(new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Children = { okButton, cancelButton }
        });
        dialog.Content = root;
        input.SelectAll();

        return dialog.ShowDialog() == true ? input.Text : null;
    }

    private bool ShowConfirmDialog(string title, string message, bool isDanger = false)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = title,
            Width = 440,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = WpfBrushes.Transparent
        };

        var confirmButton = new WpfButton
        {
            Content = isDanger ? "Confirm" : "Continue",
            Width = 132,
            Height = 40,
            IsDefault = true,
            Background = isDanger ? (WpfBrush)FindResource("Danger") : (WpfBrush)FindResource("AccentGradient"),
            BorderThickness = new Thickness(0)
        };
        var cancelButton = new WpfButton
        {
            Content = "Cancel",
            Width = 112,
            Height = 40,
            Margin = new Thickness(12, 0, 0, 0),
            IsCancel = true
        };
        confirmButton.Click += (_, _) => dialog.DialogResult = true;

        dialog.Content = new Border
        {
            Background = (WpfBrush)FindResource("Panel"),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(22),
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Text = message,
                        Foreground = (WpfBrush)FindResource("MutedText"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 44, 0, 0)
                    },
                    new StackPanel
                    {
                        Orientation = WpfOrientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        Children = { confirmButton, cancelButton }
                    }
                }
            }
        };
        Grid.SetRow(((Grid)((Border)dialog.Content).Child).Children[2], 2);

        return dialog.ShowDialog() == true;
    }

    private BitmapImage LoadBitmap(string filePath)
    {
        if (_bitmapCache.TryGetValue(filePath, out var cached))
        {
            return cached;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        _bitmapCache[filePath] = bitmap;
        return bitmap;
    }

    private WpfButton CreateCardAction(string glyph, string text, MouseButtonEventHandler handler)
    {
        var button = new WpfButton
        {
            Content = CreateIconText(glyph, text, 11),
            Height = 24,
            MinWidth = 58,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(7, 0, 7, 0),
            FontSize = 10,
            Foreground = (WpfBrush)FindResource("Text"),
            Background = new SolidColorBrush(WpfColor.FromRgb(24, 35, 51)),
            BorderBrush = (WpfBrush)FindResource("Border"),
            BorderThickness = new Thickness(1),
            Cursor = WpfCursors.Hand
        };
        button.PreviewMouseLeftButtonUp += handler;
        return button;
    }

    private async void RemoveWallpaper(WallpaperItem item)
    {
        if (!ShowConfirmDialog("Remove wallpaper", $"Remove '{item.Name}' from the library?\n\nThe original file will not be deleted.", isDanger: true))
        {
            return;
        }

        _thumbnailCache.DeleteThumbnail(item);
        if (!string.IsNullOrWhiteSpace(item.ThumbnailPath))
        {
            _bitmapCache.Remove(item.ThumbnailPath);
        }

        _library.Remove(item.Id);
        if (_settings.LastWallpaperId == item.Id)
        {
            _settings = LibraryWorkflowService.ApplyRemoveToSettings(_settings, item);
            await _settingsStore.SaveAsync(_settings);
            if (_wallpaperController.CurrentFilePath == item.FilePath)
            {
                _wallpaperController.Stop();
            }
        }

        await PersistLibraryAsync();
        RenderLibrary();
        RefreshStatus();
        ShowStatus("Wallpaper removed", item.Name, isError: false);
    }

    private void OpenFileLocation(WallpaperItem item)
    {
        if (!File.Exists(item.FilePath))
        {
            ShowStatus("File missing", "This wallpaper file is no longer available.", isError: true);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{item.FilePath}\"",
            UseShellExecute = true
        });
    }

    private async void RelinkWallpaper(WallpaperItem item)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Relink {item.Name}",
            Filter = "Wallpaper files|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.webm|Images|*.jpg;*.jpeg;*.png;*.bmp|Videos|*.mp4;*.webm|All files|*.*",
            FileName = Path.GetFileName(item.FilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var relinked = LibraryWorkflowService.Relink(item, dialog.FileName, null);
        relinked = await EnsureThumbnailAsync(relinked);
        _library.Upsert(relinked);

        await PersistLibraryAsync();
        if (_settings.LastWallpaperId == item.Id)
        {
            _settings = SettingsWorkflowService.SetLastWallpaper(_settings, relinked.Id);
            await _settingsStore.SaveAsync(_settings);
        }

        RenderLibrary();
        RenderDisplays();
        ShowStatus("Wallpaper relinked", relinked.Name, isError: false);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RenderLibrary();
        }
    }

    private static WpfBrush CreateTypeBrush(WallpaperType type)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new WpfPoint(0, 0),
            EndPoint = new WpfPoint(1, 1)
        };

        if (type == WallpaperType.Video)
        {
            brush.GradientStops.Add(new GradientStop(WpfColor.FromRgb(27, 45, 84), 0));
            brush.GradientStops.Add(new GradientStop(WpfColor.FromRgb(99, 56, 245), 1));
            return brush;
        }

        brush.GradientStops.Add(new GradientStop(WpfColor.FromRgb(13, 48, 64), 0));
        brush.GradientStops.Add(new GradientStop(WpfColor.FromRgb(47, 128, 255), 1));
        return brush;
    }

    private void RefreshStatus()
    {
        var monitorCount = _wallpaperController.GetMonitors().Count;
        SetStatusText(MonitorStatus, "\uE7F4", monitorCount == 1 ? "Monitor 1" : $"{monitorCount} monitors");

        if (_wallpaperController.CurrentFilePath is null)
        {
            CurrentWallpaperName.Text = "No wallpaper selected";
        }
        else
        {
            CurrentWallpaperName.Text = Path.GetFileName(_wallpaperController.CurrentFilePath);
        }

        var playbackText = _wallpaperController.State switch
        {
            PlaybackState.Playing => "Playing",
            PlaybackState.Paused => "Paused",
            PlaybackState.Error => "Error",
            _ => "Stopped"
        };
        var playbackIcon = _wallpaperController.State switch
        {
            PlaybackState.Playing => "\uE768",
            PlaybackState.Paused => "\uE769",
            PlaybackState.Error => "\uE783",
            _ => "\uE71A"
        };
        var playbackBrush = _wallpaperController.State switch
        {
            PlaybackState.Playing => (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 215, 122)),
            PlaybackState.Paused => (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 192, 94)),
            PlaybackState.Error => (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 93, 93)),
            _ => (System.Windows.Media.Brush)FindResource("MutedText")
        };
        SetStatusText(PlaybackStatus, playbackIcon, playbackText, playbackBrush);

        SetStatusText(ScaleModeStatus, "\uE8B7", FormatScaleMode(_settings.DefaultScaleMode));
        SetStatusText(PerformanceModeStatus, "\uE9D9", $"{_settings.PerformancePreset} Mode ({_settings.FpsLimit} FPS)");
        var rendererStatuses = _wallpaperController.RendererStatuses;
        var attachedCount = rendererStatuses.Count(status => status.IsAttachedBelowDesktopIcons);
        RendererAttachStatus.Text = rendererStatuses.Count == 0
            ? "Desktop layer not attached"
            : attachedCount == rendererStatuses.Count
                ? $"Desktop layer attached below icons ({attachedCount}/{rendererStatuses.Count})"
                : $"Desktop layer partial ({attachedCount}/{rendererStatuses.Count})";
        RendererAttachStatus.Foreground = rendererStatuses.Count > 0 && attachedCount == rendererStatuses.Count
            ? (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 215, 122))
            : (System.Windows.Media.Brush)FindResource("MutedText");
        PauseButton.Content = _wallpaperController.State == PlaybackState.Paused ? CreateIconText("\uE768", "Resume") : CreateIconText("\uE769", "Pause");
        if (_pauseTrayItem is not null)
        {
            _pauseTrayItem.Text = _wallpaperController.State == PlaybackState.Paused ? "Resume Wallpaper" : "Pause Wallpaper";
        }

        if (_nextTrayItem is not null)
        {
            _nextTrayItem.Text = "Next Wallpaper";
            _nextTrayItem.Enabled = _library.Items.Any(item => File.Exists(item.FilePath));
        }

        if (_muteTrayItem is not null)
        {
            _muteTrayItem.Text = _wallpaperController.IsMuted ? "Unmute" : "Mute";
        }

        UpdateHomePreviewFromCurrent();
    }

    private void SetStatusText(TextBlock target, string glyph, string text, WpfBrush? foreground = null)
    {
        var brush = foreground ?? (WpfBrush)FindResource("Text");
        target.Inlines.Clear();
        target.Foreground = brush;
        target.Inlines.Add(new System.Windows.Documents.Run(glyph)
        {
            FontFamily = (WpfFontFamily)FindResource("FluentIconFont"),
            FontSize = 14
        });
        target.Inlines.Add(new System.Windows.Documents.Run($"  {text}")
        {
            FontFamily = (WpfFontFamily)FindResource("AppFont")
        });
    }

    private bool IsWallpaperActive(WallpaperItem item)
    {
        if (_settings.WallpaperMode == WallpaperMode.DifferentPerMonitor)
        {
            return _settings.MonitorProfiles.Any(profile => profile.WallpaperId == item.Id);
        }

        return _settings.LastWallpaperId == item.Id;
    }

    private void UpdateHomePreview(WallpaperItem? item)
    {
        if (item is null || !File.Exists(item.FilePath))
        {
            HomePreviewImage.Source = new BitmapImage(new Uri("Assets/Mockups/01-main-app-ui-mockup.png", UriKind.Relative));
            return;
        }

        var previewPath = !string.IsNullOrWhiteSpace(item.ThumbnailPath) && File.Exists(item.ThumbnailPath)
            ? item.ThumbnailPath
            : item.Type == WallpaperType.Image
                ? item.FilePath
                : null;

        if (previewPath is not null)
        {
            HomePreviewImage.Source = LoadBitmap(previewPath);
        }
    }

    private void UpdateHomePreviewFromCurrent()
    {
        var active = _library.Items.FirstOrDefault(item => IsWallpaperActive(item));
        UpdateHomePreview(active);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024 * 1024)
        {
            return $"{Math.Max(1, bytes / 1024)} KB";
        }

        return $"{bytes / 1024d / 1024d:0.#} MB";
    }

    private static string FormatScaleMode(ScaleMode scaleMode)
    {
        return scaleMode switch
        {
            ScaleMode.Cover => "Fill / Cover",
            ScaleMode.Contain => "Fit / Contain",
            ScaleMode.Stretch => "Stretch",
            ScaleMode.Center => "Center",
            _ => scaleMode.ToString()
        };
    }
}


