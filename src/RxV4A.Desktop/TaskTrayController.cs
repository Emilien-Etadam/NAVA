using System.Drawing;
using RxV4A.Core;
using RxV4A.Host;
using RxV4A.Host.Localization;
using Forms = System.Windows.Forms;

namespace RxV4A.Desktop;

public sealed class TaskTrayController : IDisposable
{
    private readonly IDeviceManager _deviceManager;
    private readonly IControlOrchestrator _orchestrator;
    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly CompactPowerWindow _compactPowerWindow;
    private readonly Icon? _applicationIcon;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _statusItem;

    public TaskTrayController(
        IDeviceManager deviceManager,
        IControlOrchestrator orchestrator,
        MainWindow mainWindow,
        SettingsWindow settingsWindow,
        CompactPowerWindow compactPowerWindow,
        Action requestExit)
    {
        _deviceManager = deviceManager;
        _orchestrator = orchestrator;
        _mainWindow = mainWindow;
        _settingsWindow = settingsWindow;
        _compactPowerWindow = compactPowerWindow;
        _statusItem = new Forms.ToolStripMenuItem(Strings.Status_Disconnected) { Enabled = false };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.Tray_Compact, null, (_, _) => _compactPowerWindow.ShowFromTray());
        menu.Items.Add(Strings.Tray_OpenMain, null, (_, _) => ShowMainWindow());
        menu.Items.Add(Strings.Tray_OpenSettings, null, (_, _) => _settingsWindow.ShowFromTray());
        menu.Items.Add(Strings.Settings_Refresh, null, async (_, _) => await RefreshAsync());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.Settings_AmpOn, null, async (_, _) => await _mainWindow.ExecutePowerAsync(MainPower.On));
        menu.Items.Add(Strings.Settings_Standby, null, async (_, _) => await _mainWindow.ExecutePowerAsync(MainPower.Standby));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.Tray_Exit, null, (_, _) => requestExit());

        _applicationIcon = string.IsNullOrWhiteSpace(Environment.ProcessPath)
            ? null
            : Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _applicationIcon ?? SystemIcons.Application,
            Text = Strings.Format(Strings.Tray_Icon, Strings.Status_Disconnected),
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.MouseDoubleClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == Forms.MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };
        _deviceManager.SnapshotChanged += DeviceManager_SnapshotChanged;
        _orchestrator.StateChanged += Orchestrator_StateChanged;
        Update(_deviceManager.Snapshot);
    }

    public void Dispose()
    {
        _deviceManager.SnapshotChanged -= DeviceManager_SnapshotChanged;
        _orchestrator.StateChanged -= Orchestrator_StateChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _applicationIcon?.Dispose();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _deviceManager.RefreshAsync();
        }
        catch
        {
            // The shared snapshot and structured log expose the failure state.
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow.Dispatcher.CheckAccess())
        {
            _mainWindow.ShowFromTray();
        }
        else
        {
            _mainWindow.Dispatcher.BeginInvoke(_mainWindow.ShowFromTray);
        }
    }

    private void DeviceManager_SnapshotChanged(object? sender, DeviceSnapshot snapshot)
    {
        if (_mainWindow.Dispatcher.CheckAccess())
        {
            Update(snapshot);
        }
        else
        {
            _mainWindow.Dispatcher.BeginInvoke(() => Update(snapshot));
        }
    }

    private void Orchestrator_StateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow.Dispatcher.CheckAccess())
        {
            Update(_deviceManager.Snapshot);
        }
        else
        {
            _mainWindow.Dispatcher.BeginInvoke(() => Update(_deviceManager.Snapshot));
        }
    }

    private void Update(DeviceSnapshot snapshot)
    {
        var state = snapshot.ConnectionState == DeviceConnectionState.Connected
            ? snapshot.MainZone?.Power ?? Strings.Tray_Connecting
            : Strings.Status_Disconnected;
        var contexts = _orchestrator.GetContexts();
        var blockers = contexts.Where(item => item.Active).Select(item => item.Id).ToArray();
        var mismatch = contexts.Any(item => item.PowerMismatchWarning);
        _statusItem.Text = blockers.Length == 0
            ? Strings.Format(Strings.Tray_Status, state)
            : Strings.Format(Strings.Tray_StatusBlocked, state, string.Join(",", blockers));
        var suffix = blockers.Length == 0 ? state : mismatch ? Strings.Tray_ExternalOn : Strings.Tray_PowerBlocked;
        _notifyIcon.Text = Strings.Format(Strings.Tray_Icon, suffix);
    }
}
