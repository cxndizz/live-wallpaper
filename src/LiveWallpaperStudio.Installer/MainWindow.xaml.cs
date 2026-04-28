using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LiveWallpaperStudio.Installer;

public partial class MainWindow : Window
{
    private const string AppName = "Wallora - Live wallpaper";
    private const string AppRegistryKey = "Wallora";
    private const string LegacyAppRegistryKey = "LiveWallpaperStudio";
    private const string AppExe = "Wallora.exe";
    private const string LegacyAppExe = "LiveWallpaperStudio.App.exe";
    private readonly string _defaultInstallPath;
    private readonly List<InstallStep> _steps;

    public MainWindow()
    {
        InitializeComponent();
        _defaultInstallPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            AppName);
        InstallPathTextBox.Text = _defaultInstallPath;

        _steps =
        [
            new("Installing wallpaper engine"),
            new("Creating shortcuts"),
            new("Registering components"),
            new("Finalizing installation")
        ];
        StepList.ItemsSource = _steps;

        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            WindowTitleText.Text = $"Uninstall {AppName}";
            InstallOptionsView.Visibility = Visibility.Collapsed;
            UninstallView.Visibility = Visibility.Visible;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose install location",
            SelectedPath = InstallPathTextBox.Text
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPathTextBox.Text = Path.Combine(dialog.SelectedPath, AppName);
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        InstallOptionsView.Visibility = Visibility.Collapsed;
        InstallingView.Visibility = Visibility.Visible;
        await InstallAsync();
    }

    private async Task InstallAsync()
    {
        try
        {
            var target = InstallPathTextBox.Text.Trim();
            var payload = ResolvePayloadDirectory();
            if (payload is null)
            {
                System.Windows.MessageBox.Show("Installer payload was not found. Build the package first.", AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                InstallOptionsView.Visibility = Visibility.Visible;
                InstallingView.Visibility = Visibility.Collapsed;
                return;
            }

            Directory.CreateDirectory(target);
            var createDesktopShortcut = DesktopShortcutCheckBox.IsChecked == true;
            var startWithWindows = StartupCheckBox.IsChecked == true;

            SetProgress(0.08, "Stopping running app...", 0);
            await Task.Run(StopRunningAppProcesses);

            await CopyDirectoryAsync(payload, target, progress => SetProgress(progress, "Copying files...", 0));

            SetProgress(0.62, "Creating shortcuts...", 1);
            await Task.Run(() =>
            {
                var exePath = Path.Combine(target, AppExe);
                if (createDesktopShortcut)
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    CreateShortcut(Path.Combine(desktop, $"{AppName}.lnk"), exePath, target);
                }

                if (startWithWindows)
                {
                    using var runKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    runKey?.SetValue(AppRegistryKey, $"\"{exePath}\"");
                    runKey?.DeleteValue(LegacyAppRegistryKey, false);
                }
            });

            SetProgress(0.82, "Registering components...", 2);
            await Task.Run(() => RegisterUninstall(target));

            SetProgress(1, "Installation complete.", 3);
            await Task.Delay(450);
            Process.Start(new ProcessStartInfo(Path.Combine(target, AppExe)) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            InstallOptionsView.Visibility = Visibility.Visible;
            InstallingView.Visibility = Visibility.Collapsed;
        }
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        UninstallView.Visibility = Visibility.Collapsed;
        InstallingView.Visibility = Visibility.Visible;
        WindowTitleText.Text = $"Uninstall {AppName}";
        await UninstallAsync();
    }

    private async Task UninstallAsync()
    {
        var installDir = AppContext.BaseDirectory;
        try
        {
            var removeLibrary = RemoveLibraryCheckBox.IsChecked == true;
            var deleteSettings = DeleteSettingsCheckBox.IsChecked == true;

            SetProgress(0.25, "Stopping application...", 0);
            await Task.Run(StopRunningAppProcesses);

            SetProgress(0.55, "Removing installed files...", 1);
            await Task.Run(() => DeleteInstalledFiles(installDir));

            SetProgress(0.78, "Removing shortcuts and registry entries...", 2);
            await Task.Run(() =>
            {
                DeleteDesktopShortcut();
                using var runKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                runKey?.DeleteValue(AppRegistryKey, false);
                runKey?.DeleteValue(LegacyAppRegistryKey, false);
                Registry.CurrentUser.DeleteSubKeyTree(@$"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppRegistryKey}", false);
                Registry.CurrentUser.DeleteSubKeyTree(@$"Software\Microsoft\Windows\CurrentVersion\Uninstall\{LegacyAppRegistryKey}", false);
            });

            if (removeLibrary || deleteSettings)
            {
                SetProgress(0.92, "Removing app data...", 3);
                await Task.Run(() => DeleteAppData(removeLibrary, deleteSettings));
                RemovedNoteText.Text = removeLibrary && deleteSettings
                    ? "OK: Your wallpapers, settings, and cache were removed."
                    : removeLibrary
                        ? "OK: Your wallpaper library and thumbnails were removed."
                        : "OK: Your settings and cache were removed.";
            }
            else
            {
                SetProgress(0.92, "Keeping wallpapers and settings...", 3);
                RemovedNoteText.Text = "OK: Your wallpapers and settings were kept.";
            }

            SetProgress(1, "Uninstall complete.", 3);
            await Task.Delay(450);
            InstallingView.Visibility = Visibility.Collapsed;
            RemovedView.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private static string? ResolvePayloadDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "payload"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "payload")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "build", "publish")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "build", "publish"))
        };

        return candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, AppExe)) ||
            File.Exists(Path.Combine(path, LegacyAppExe)));
    }

    private async Task CopyDirectoryAsync(string source, string target, Action<double> progress)
    {
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        for (var i = 0; i < files.Length; i++)
        {
            var relative = Path.GetRelativePath(source, files[i]);
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(files[i], destination, true);

            if (i % 12 == 0 || i == files.Length - 1)
            {
                var copyProgress = files.Length == 0 ? 0.55 : 0.55 * (i + 1) / files.Length;
                await Dispatcher.InvokeAsync(() => progress(copyProgress));
                await Task.Delay(1);
            }
        }
    }

    private void SetProgress(double value, string status, int activeStep)
    {
        value = Math.Clamp(value, 0, 1);
        ProgressFill.Width = Math.Max(1, 620 * value);
        ProgressPercentText.Text = $"{Math.Round(value * 100):0}%";
        InstallStatusText.Text = status;

        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].Brush = i <= activeStep
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 135, 255))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(7, 17, 30));
        }

        StepList.Items.Refresh();
    }

    private static void RegisterUninstall(string target)
    {
        var installerCopy = Path.Combine(target, "Wallora.Uninstaller.exe");
        var currentExe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(currentExe) && File.Exists(currentExe))
        {
            File.Copy(currentExe, installerCopy, true);
        }

        using var key = Registry.CurrentUser.CreateSubKey(@$"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppRegistryKey}");
        key?.SetValue("DisplayName", AppName);
        key?.SetValue("DisplayVersion", "0.1.0");
        key?.SetValue("Publisher", AppName);
        key?.SetValue("InstallLocation", target);
        key?.SetValue("DisplayIcon", Path.Combine(target, AppExe));
        key?.SetValue("UninstallString", $"\"{installerCopy}\" --uninstall");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = targetPath;
        shortcut.Save();
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }

    private static void DeleteInstalledFiles(string installDir)
    {
        var currentExe = Environment.ProcessPath;
        foreach (var file in Directory.GetFiles(installDir, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(file, currentExe, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TryDeleteFile(file);
        }

        foreach (var dir in Directory.GetDirectories(installDir, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
        {
            TryDeleteDirectory(dir);
        }
    }

    private static void StopRunningAppProcesses()
    {
        foreach (var processName in new[] { "Wallora", "LiveWallpaperStudio.App" })
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (process.Id == Environment.ProcessId)
                    {
                        continue;
                    }

                    process.CloseMainWindow();
                    if (!process.WaitForExit(2500))
                    {
                        process.Kill(true);
                        process.WaitForExit(10000);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }

    private static void TryDeleteFile(string file)
    {
        const int attempts = 8;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (!File.Exists(file))
                {
                    return;
                }

                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                if (attempt == attempts)
                {
                    return;
                }

                Thread.Sleep(250);
            }
            catch (IOException)
            {
                if (attempt == attempts)
                {
                    return;
                }

                Thread.Sleep(250);
            }
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        const int attempts = 4;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    return;
                }

                Directory.Delete(directory, false);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                if (attempt == attempts)
                {
                    return;
                }

                Thread.Sleep(250);
            }
            catch (IOException)
            {
                if (attempt == attempts)
                {
                    return;
                }

                Thread.Sleep(250);
            }
        }
    }

    private static void DeleteDesktopShortcut()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcut = Path.Combine(desktop, $"{AppName}.lnk");
        TryDeleteFile(shortcut);
    }

    private static void DeleteAppData(bool removeLibrary, bool deleteSettings)
    {
        foreach (var root in GetAppDataRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            if (removeLibrary && deleteSettings)
            {
                DeleteDirectoryContents(root);
                TryDeleteDirectory(root);
                continue;
            }

            if (removeLibrary)
            {
                TryDeleteFile(Path.Combine(root, "library.json"));
                TryDeleteFile(Path.Combine(root, "library.db"));
                DeleteDirectoryContents(Path.Combine(root, "thumbnails"));
                TryDeleteDirectory(Path.Combine(root, "thumbnails"));
            }

            if (deleteSettings)
            {
                TryDeleteFile(Path.Combine(root, "config.json"));
                DeleteDirectoryContents(Path.Combine(root, "cache"));
                TryDeleteDirectory(Path.Combine(root, "cache"));
                DeleteDirectoryContents(Path.Combine(root, "logs"));
                TryDeleteDirectory(Path.Combine(root, "logs"));
            }

            TryDeleteDirectory(root);
        }
    }

    private static IEnumerable<string> GetAppDataRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var appName in new[] { "LiveWallpaperStudio", "Wallora", "Wallora - Live wallpaper" })
        {
            yield return Path.Combine(roaming, appName);
            yield return Path.Combine(local, appName);
        }
    }

    private static void DeleteDirectoryContents(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
        {
            TryDeleteFile(file);
        }

        foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
        {
            TryDeleteDirectory(dir);
        }
    }
}

public sealed class InstallStep(string label)
{
    public string Label { get; } = label;
    public System.Windows.Media.Brush Brush { get; set; } = new SolidColorBrush(System.Windows.Media.Color.FromRgb(7, 17, 30));
}
