using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RxV4A.Host;
using RxV4A.Host.Localization;

namespace RxV4A.Desktop;

public partial class App : System.Windows.Application
{
    private WebApplication? _host;
    private TaskTrayController? _taskTray;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private CompactPowerWindow? _compactPowerWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        UiLanguageSelection.ApplyPersistedOrSystem();
        base.OnStartup(e);

        try
        {
            _host = await LocalApiApplication.BuildAsync(e.Args);
            await _host.StartAsync();
            TryClearStartupError();

            var manager = _host.Services.GetRequiredService<IDeviceManager>();
            var orchestrator = _host.Services.GetRequiredService<IControlOrchestrator>();
            var settings = _host.Services.GetRequiredService<AppSettings>();
            var settingsStore = _host.Services.GetRequiredService<ISettingsStore>();
            var registeredActions = _host.Services.GetRequiredService<IRegisteredActionService>();
            _settingsWindow = new SettingsWindow(
                manager,
                orchestrator,
                registeredActions,
                settings,
                settingsStore);
            _mainWindow = new MainWindow(manager, orchestrator, _settingsWindow);
            _compactPowerWindow = new CompactPowerWindow(
                manager,
                orchestrator,
                settings,
                settingsStore);
            MainWindow = _mainWindow;
            _taskTray = new TaskTrayController(
                manager,
                orchestrator,
                _mainWindow,
                _settingsWindow,
                _compactPowerWindow,
                RequestExit);
            _settingsWindow.StartMinimizedToTray();
            _mainWindow.Show();
            if (settings.ShowCompactPowerOnStartup)
            {
                _compactPowerWindow.ShowFromTray();
            }
        }
        catch (Exception exception)
        {
            TryWriteStartupError(exception);
            System.Windows.MessageBox.Show(
                Strings.Format(Strings.App_StartupFailed, exception.Message),
                "NAVA",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _taskTray?.Dispose();
        if (_host is not null)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                _host.StopAsync(timeout.Token).GetAwaiter().GetResult();
                _host.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // Process shutdown must continue even if a hosted service is already unavailable.
            }
        }

        base.OnExit(e);
    }

    private void RequestExit()
    {
        _mainWindow?.AllowClose();
        _settingsWindow?.AllowClose();
        _compactPowerWindow?.AllowClose();
        Shutdown();
    }

    private static void TryWriteStartupError(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StartupErrorPath)!);
            File.WriteAllText(
                StartupErrorPath,
                $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}");
        }
        catch
        {
            // A failed diagnostic log must not hide the startup error dialog.
        }
    }

    private static void TryClearStartupError()
    {
        try
        {
            File.Delete(StartupErrorPath);
        }
        catch
        {
            // A successful start continues even if an old diagnostic log cannot be deleted.
        }
    }

    private static string StartupErrorPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NAVA",
        "logs",
        "startup-error.log");
}
