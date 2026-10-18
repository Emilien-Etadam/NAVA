using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using RxV4A.Core;
using RxV4A.Host;
using RxV4A.Host.Localization;

namespace RxV4A.Desktop;

public partial class HotkeySettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ISettingsStore _settingsStore;
    private readonly GlobalHotkeyService _globalHotkeys;
    private readonly IDeviceManager _deviceManager;
    private readonly IRegisteredActionService _registeredActions;
    private bool _capturing;

    public HotkeySettingsWindow(
        AppSettings settings,
        ISettingsStore settingsStore,
        GlobalHotkeyService globalHotkeys,
        IDeviceManager deviceManager,
        IRegisteredActionService registeredActions)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _globalHotkeys = globalHotkeys;
        _deviceManager = deviceManager;
        _registeredActions = registeredActions;
        KeyOptions = BuildKeyOptions();
        ActionOptions = BuildActionOptions();
        LoadRows();
        DataContext = this;
        InitializeComponent();
    }

    public ObservableCollection<GlobalHotkeyRow> GlobalHotkeys { get; } = [];

    public IReadOnlyList<HotkeyKeyOption> KeyOptions { get; }

    public IReadOnlyList<HotkeyActionOption> ActionOptions { get; }

    private void LoadRows()
    {
        lock (_settings.SyncRoot)
        {
            foreach (var binding in _settings.GlobalHotkeys)
            {
                GlobalHotkeys.Add(ToRow(binding));
            }
        }
    }

    private static GlobalHotkeyRow ToRow(GlobalHotkeyBinding binding) => new()
    {
        VirtualKey = binding.Gesture.VirtualKey,
        Ctrl = binding.Gesture.Ctrl,
        Alt = binding.Gesture.Alt,
        Shift = binding.Gesture.Shift,
        ActionId = binding.ActionId ?? string.Empty,
        AmountText = binding.Amount?.ToString("0.###", CultureInfo.CurrentCulture) ?? string.Empty
    };

    private void AddHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        var usedKeys = GlobalHotkeys
            .Where(item => item.Ctrl && item.Alt && !item.Shift)
            .Select(item => item.VirtualKey)
            .ToHashSet();
        var virtualKey = KeyOptions.FirstOrDefault(option => !usedKeys.Contains(option.VirtualKey))?.VirtualKey
                         ?? KeyOptions[0].VirtualKey;
        var row = new GlobalHotkeyRow
        {
            VirtualKey = virtualKey,
            Ctrl = true,
            Alt = true
        };
        GlobalHotkeys.Add(row);
        GlobalHotkeysGrid.SelectedItem = row;
        GlobalHotkeysGrid.ScrollIntoView(row);
        CaptureStatusText.Text = Strings.Hotkey_Added;
    }

    private void RemoveHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (GlobalHotkeysGrid.SelectedItem is not GlobalHotkeyRow row)
        {
            CaptureStatusText.Text = Strings.Hotkey_SelectRowToDelete;
            return;
        }

        GlobalHotkeys.Remove(row);
        CaptureStatusText.Text = Strings.Hotkey_Removed;
    }

    private void ResetHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        GlobalHotkeys.Clear();
        foreach (var binding in GlobalHotkeyDefaults.Create())
        {
            GlobalHotkeys.Add(ToRow(binding));
        }

        CaptureStatusText.Text = Strings.Hotkey_ResetDone;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        GlobalHotkeysGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
        GlobalHotkeysGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        List<GlobalHotkeyBinding>? previous = null;
        try
        {
            var hotkeys = GlobalHotkeys.Select(ToBinding).ToList();
            lock (_settings.SyncRoot)
            {
                previous = _settings.GlobalHotkeys;
                _settings.GlobalHotkeys = hotkeys;
                SettingsValidator.Validate(_settings);
            }

            await _settingsStore.SaveAsync(_settings);

            _globalHotkeys.Reload();
            System.Windows.MessageBox.Show(
                Strings.Hotkey_Saved,
                "NAVA",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or
                                           UnauthorizedAccessException)
        {
            if (previous is not null)
            {
                lock (_settings.SyncRoot)
                {
                    _settings.GlobalHotkeys = previous;
                }
            }

            System.Windows.MessageBox.Show(
                Strings.Format(Strings.Hotkey_SaveFailed, exception.Message),
                "NAVA",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CaptureGlobalButton_Click(object sender, RoutedEventArgs e)
    {
        GlobalHotkeysGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
        GlobalHotkeysGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        if (GlobalHotkeysGrid.SelectedItem is not GlobalHotkeyRow)
        {
            CaptureStatusText.Text = Strings.Hotkey_SelectRowFirst;
            return;
        }

        _capturing = true;
        _globalHotkeys.Suspend();
        Activate();
        Focus();
        CaptureStatusText.Text = Strings.Hotkey_PressKey;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_capturing)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            EndCapture(Strings.Hotkey_CaptureCancelled);
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (!HotkeyGesture.IsSupportedVirtualKey(virtualKey))
        {
            CaptureStatusText.Text = Strings.Hotkey_KeyUnsupported;
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var gesture = new HotkeyGesture
        {
            Ctrl = modifiers.HasFlag(ModifierKeys.Control),
            Alt = modifiers.HasFlag(ModifierKeys.Alt),
            Shift = modifiers.HasFlag(ModifierKeys.Shift),
            VirtualKey = virtualKey
        };
        if (GlobalHotkeysGrid.SelectedItem is GlobalHotkeyRow hotkey)
        {
            hotkey.VirtualKey = virtualKey;
            hotkey.Ctrl = gesture.Ctrl;
            hotkey.Alt = gesture.Alt;
            hotkey.Shift = gesture.Shift;
        }

        EndCapture(Strings.Format(Strings.Hotkey_Captured, gesture.DisplayName));
    }

    private void EndCapture(string message)
    {
        _capturing = false;
        CaptureStatusText.Text = message;
        _globalHotkeys.Reload();
    }

    private static GlobalHotkeyBinding ToBinding(GlobalHotkeyRow row)
    {
        if (!HotkeyGesture.IsSupportedVirtualKey(row.VirtualKey))
        {
            throw new InvalidDataException(Strings.Hotkey_ChooseFromList);
        }

        var actionId = string.IsNullOrWhiteSpace(row.ActionId) ? null : row.ActionId.Trim();
        decimal? amount = null;
        if (HotkeyActionIds.RequiresAmount(actionId))
        {
            if ((!decimal.TryParse(row.AmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) &&
                 !decimal.TryParse(row.AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed)) ||
                parsed is <= 0 or > 100)
            {
                throw new InvalidDataException(Strings.Hotkey_AmountInvalid);
            }

            amount = parsed;
        }

        return new GlobalHotkeyBinding
        {
            Gesture = new HotkeyGesture
            {
                Ctrl = row.Ctrl,
                Alt = row.Alt,
                Shift = row.Shift,
                VirtualKey = row.VirtualKey
            },
            ActionId = actionId,
            Amount = amount
        };
    }

    private IReadOnlyList<HotkeyActionOption> BuildActionOptions()
    {
        var options = new List<HotkeyActionOption>();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string id, string displayName)
        {
            if (known.Add(id))
            {
                options.Add(new HotkeyActionOption(id, displayName));
            }
        }

        Add(string.Empty, Strings.Hotkey_Unassigned);
        Add(HotkeyActionIds.PowerToggle, Strings.Hotkey_PowerToggle);
        Add(HotkeyActionIds.PowerOn, Strings.Hotkey_PowerOn);
        Add(HotkeyActionIds.PowerStandby, Strings.Hotkey_PowerStandby);
        Add(HotkeyActionIds.MuteToggle, Strings.Hotkey_MuteToggle);
        Add(HotkeyActionIds.MuteOn, Strings.Hotkey_MuteOn);
        Add(HotkeyActionIds.MuteOff, Strings.Hotkey_MuteOff);
        Add(HotkeyActionIds.VolumeUp, Strings.Hotkey_VolumeUp);
        Add(HotkeyActionIds.VolumeDown, Strings.Hotkey_VolumeDown);

        var zone = _deviceManager.Snapshot.Capabilities?.FindZone("main");
        foreach (var input in zone?.Inputs.Where(item => ApplicationScope.IsOperationalInput(item.Id)) ?? [])
        {
            Add(HotkeyActionIds.SelectInput(input.Id), Strings.Format(Strings.Hotkey_Input, input.Id));
        }

        foreach (var program in zone?.SoundPrograms ?? [])
        {
            Add(HotkeyActionIds.SelectSoundProgram(program), Strings.Format(Strings.Hotkey_Sound, program));
        }

        lock (_settings.SyncRoot)
        {
            foreach (var activityId in _settings.Activities.Keys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
            {
                Add(HotkeyActionIds.ActivateActivity(activityId), Strings.Format(Strings.Hotkey_Activity, activityId));
            }

            foreach (var blockerId in _settings.PowerOnBlockers.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
            {
                Add(HotkeyActionIds.ToggleBlocker(blockerId), Strings.Format(Strings.Hotkey_Blocker, blockerId));
            }
        }

        foreach (var action in _registeredActions.GetRegisteredActions())
        {
            Add(HotkeyActionIds.RegisteredAction(action.Id), Strings.Format(Strings.Hotkey_RegisteredAction, action.DisplayName));
        }

        lock (_settings.SyncRoot)
        {
            foreach (var savedActionId in _settings.GlobalHotkeys.Select(item => item.ActionId)
                         .Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                Add(savedActionId!, Strings.Format(Strings.Hotkey_SavedAction, savedActionId));
            }
        }

        return options;
    }

    private static IReadOnlyList<HotkeyKeyOption> BuildKeyOptions()
    {
        var virtualKeys = new List<int>();
        virtualKeys.AddRange(Enumerable.Range(0x70, 24));
        virtualKeys.AddRange(Enumerable.Range(0x41, 26));
        virtualKeys.AddRange(Enumerable.Range(0x30, 10));
        virtualKeys.AddRange(Enumerable.Range(0x60, 10));
        virtualKeys.AddRange([0x6A, 0x6B, 0x6D, 0x6E, 0x6F]);
        virtualKeys.AddRange([0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2D, 0x2E]);
        virtualKeys.AddRange(Enumerable.Range(0xAD, 7));
        return virtualKeys.Select(key => new HotkeyKeyOption(key, HotkeyGesture.VirtualKeyName(key))).ToArray();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_capturing)
        {
            _capturing = false;
            _globalHotkeys.Reload();
        }
    }
}

public sealed class GlobalHotkeyRow : ObservableRow
{
    private int _virtualKey = 0x7C;
    private bool _ctrl;
    private bool _alt;
    private bool _shift;
    private string _actionId = string.Empty;
    private string _amountText = string.Empty;

    public int VirtualKey { get => _virtualKey; set => Set(ref _virtualKey, value); }
    public bool Ctrl { get => _ctrl; set => Set(ref _ctrl, value); }
    public bool Alt { get => _alt; set => Set(ref _alt, value); }
    public bool Shift { get => _shift; set => Set(ref _shift, value); }
    public string ActionId
    {
        get => _actionId;
        set
        {
            if (!Set(ref _actionId, value))
            {
                return;
            }

            if (RequiresAmount && string.IsNullOrWhiteSpace(AmountText))
            {
                AmountText = "1";
            }

            RaisePropertyChanged(nameof(RequiresAmount));
        }
    }
    public string AmountText { get => _amountText; set => Set(ref _amountText, value); }
    public bool RequiresAmount => HotkeyActionIds.RequiresAmount(ActionId);
}

public sealed record HotkeyKeyOption(int VirtualKey, string DisplayName);

public sealed record HotkeyActionOption(string Id, string DisplayName);

public abstract class ObservableRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected void RaisePropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
