using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using AutoClicker.Core;
using AutoClicker.Core.Input;
using AutoClicker.Core.Hotkeys;
using AutoClicker.Enums;
using AutoClicker.Models;
using AutoClicker.Input;
using AutoClicker.Utils;
using Serilog;
using CheckBox = System.Windows.Controls.CheckBox;
using MouseAction = AutoClicker.Enums.MouseAction;
using MouseButton = AutoClicker.Enums.MouseButton;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using Point = System.Drawing.Point;

namespace AutoClicker.Views
{
    public partial class MainWindow : Window
    {
        public AutoClickerSettings AutoClickerSettings
        {
            get { return (AutoClickerSettings)GetValue(CurrentSettingsProperty); }
            set { SetValue(CurrentSettingsProperty, value); }
        }

        public static readonly DependencyProperty CurrentSettingsProperty =
           DependencyProperty.Register(nameof(AutoClickerSettings), typeof(AutoClickerSettings), typeof(MainWindow),
               new UIPropertyMetadata(SettingsUtils.CurrentSettings.AutoClickerSettings));

        private readonly ClickEngine clickEngine;
        private bool shutdownPending;
        private bool allowClose;
        private IHotkeyRegistrationBackend hotkeyBackend;
        private readonly Dictionary<Operation, HotkeyRegistrationSlot> registeredHotkeys = new Dictionary<Operation, HotkeyRegistrationSlot>();
        private readonly Uri runningIconUri =
            new Uri(Constants.RUNNING_ICON_RESOURCE_PATH, UriKind.Relative);

        private NotifyIcon systemTrayIcon;
        private SystemTrayMenu systemTrayMenu;
        private AboutWindow aboutWindow = null;
        private SettingsWindow settingsWindow = null;
        private CaptureMouseScreenCoordinatesWindow captureMouseCoordinatesWindow;

        private ImageSource _defaultIcon;
        private nint _mainWindowHandle;
        private HwndSource _source;

        #region Life Cycle

        public MainWindow()
        {
            IMouseInput mouseInput = new MouseInputDispatcher(new WindowsMouseNativeApi());
            clickEngine = new ClickEngine(mouseInput, new SystemDelayProvider());
            clickEngine.Faulted += ClickEngine_Faulted;
            clickEngine.Stopped += ClickEngine_Stopped;

            DataContext = this;
            ResetTitle();
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _mainWindowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_mainWindowHandle);
            _source.AddHook(StartStopHooks);
            _defaultIcon = Icon;

            hotkeyBackend = new WindowsHotkeyRegistrationBackend(_mainWindowHandle);
            RegisterInitialHotkeys(SettingsUtils.CurrentSettings.HotkeySettings);

            RadioButtonSelectedLocationMode_CurrentLocation.Checked += RadioButtonSelectedLocationMode_CurrentLocationOnChecked;

            InitializeSystemTrayMenu();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowClose && clickEngine.IsRunning)
            {
                e.Cancel = true;
                if (!shutdownPending)
                {
                    shutdownPending = true;
                    _ = StopBeforeCloseAsync();
                }
                return;
            }

            base.OnClosing(e);
        }

        private async Task StopBeforeCloseAsync()
        {
            await clickEngine.StopAsync();
            allowClose = true;
            await Dispatcher.InvokeAsync(Close);
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(StartStopHooks);

            clickEngine.Faulted -= ClickEngine_Faulted;
            clickEngine.Stopped -= ClickEngine_Stopped;

            if (hotkeyBackend != null)
            {
                foreach (HotkeyRegistrationSlot slot in registeredHotkeys.Values)
                    HotkeyRegistrationTransaction.UnregisterSlot(hotkeyBackend, slot);
                registeredHotkeys.Clear();
            }

            RadioButtonSelectedLocationMode_CurrentLocation.Checked -= RadioButtonSelectedLocationMode_CurrentLocationOnChecked;

            DisposeSystemTrayMenu();

            Log.Information("Application closing");
            Log.Debug("==================================================");
            Log.CloseAndFlush();

            base.OnClosed(e);
        }

        #endregion Life Cycle

        #region Commands

        private async void StartCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            await StartClickingAsync();
        }

        private void StartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanStartOperation();
        }

        private async void StopCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            await StopClickingAsync();
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = clickEngine.IsRunning;
        }

        private async void ToggleCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            await ToggleClickingAsync();
        }

        private void ToggleCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = clickEngine.IsRunning || CanStartOperation();
        }

        private void SaveSettingsCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("Saving Settings");
            SettingsUtils.SetApplicationSettings(AutoClickerSettings);
        }

        private void HotkeySettingsCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (clickEngine.IsRunning)
            {
                MessageBox.Show(this, "Stop clicking before changing global hotkeys.", "Hotkeys in use",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow { Owner = this };
                settingsWindow.Closed += (o, args) => settingsWindow = null;
            }

            settingsWindow.Show();
        }

        private void ExitCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            Close();
        }

        private void AboutCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (aboutWindow == null)
            {
                aboutWindow = new AboutWindow();
                aboutWindow.Closed += (o, args) => aboutWindow = null;
            }

            aboutWindow.Show();
        }

        private void CaptureMouseScreenCoordinatesCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (captureMouseCoordinatesWindow == null)
            {
                captureMouseCoordinatesWindow = new CaptureMouseScreenCoordinatesWindow();
                captureMouseCoordinatesWindow.OnCoordinatesCaptured += CaptureMouseCoordinatesWindow_OnCoordinatesCaptured;
                captureMouseCoordinatesWindow.Closed += (o, args) =>
                {
                    captureMouseCoordinatesWindow.OnCoordinatesCaptured -= CaptureMouseCoordinatesWindow_OnCoordinatesCaptured;
                    captureMouseCoordinatesWindow = null;
                };
            }

            captureMouseCoordinatesWindow.Show();
        }

        private void CaptureMouseCoordinatesWindow_OnCoordinatesCaptured(object sender, Point point)
        {
            TextBoxPickedXValue.Text = point.X.ToString();
            TextBoxPickedYValue.Text = point.Y.ToString();
            RadioButtonSelectedLocationMode_PickedLocation.IsChecked = true;
        }

        #endregion Commands

        #region Helper Methods

        private ClickRunOptions CreateRunOptions()
        {
            int interval = ClickIntervalCalculator.ToMilliseconds(
                AutoClickerSettings.Hours,
                AutoClickerSettings.Minutes,
                AutoClickerSettings.Seconds,
                AutoClickerSettings.Milliseconds);

            int? repeatCount = AutoClickerSettings.SelectedRepeatMode switch
            {
                RepeatMode.Infinite => null,
                RepeatMode.Count => AutoClickerSettings.SelectedTimesToRepeat,
                _ => throw new ArgumentOutOfRangeException(nameof(AutoClickerSettings.SelectedRepeatMode))
            };

            return ClickRunOptions.Create(
                interval,
                AutoClickerSettings.VarianceMilliseconds,
                GetSelectedCoreButton(AutoClickerSettings.SelectedMouseButton),
                GetSelectedCoreAction(AutoClickerSettings.SelectedMouseAction),
                repeatCount,
                GetFixedPosition());
        }

        private bool CanStartOperation()
        {
            if (clickEngine.IsRunning)
                return false;

            try
            {
                _ = CreateRunOptions();
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private async Task StartClickingAsync()
        {
            try
            {
                ClickRunOptions options = CreateRunOptions();
                bool started = await clickEngine.StartAsync(options);
                if (!started)
                    return;

                Log.Information("Starting operation, interval={Interval}ms, variance={Variance}ms",
                    options.IntervalMilliseconds, options.VarianceMilliseconds);
                Icon = new BitmapImage(runningIconUri);
                Title = Constants.MAIN_WINDOW_TITLE_DEFAULT + Constants.MAIN_WINDOW_TITLE_RUNNING;
                systemTrayIcon.Text = Constants.MAIN_WINDOW_TITLE_DEFAULT + Constants.MAIN_WINDOW_TITLE_RUNNING;
                CommandManager.InvalidateRequerySuggested();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Log.Warning(ex, "Invalid click settings");
                MessageBox.Show(this, ex.Message, "Invalid click settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task StopClickingAsync()
        {
            if (!clickEngine.IsRunning)
                return;

            Log.Information("Stopping operation");
            await clickEngine.StopAsync();
        }

        private Task ToggleClickingAsync()
        {
            return clickEngine.IsRunning ? StopClickingAsync() : StartClickingAsync();
        }

        private Point? GetFixedPosition()
        {
            return AutoClickerSettings.SelectedLocationMode == LocationMode.PickedLocation
                ? new Point(AutoClickerSettings.PickedXValue, AutoClickerSettings.PickedYValue)
                : null;
        }

        private static ClickButton GetSelectedCoreButton(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => ClickButton.Left,
                MouseButton.Right => ClickButton.Right,
                MouseButton.Middle => ClickButton.Middle,
                MouseButton.X1 => ClickButton.X1,
                MouseButton.X2 => ClickButton.X2,
                _ => throw new ArgumentOutOfRangeException(nameof(button))
            };
        }

        private static ClickAction GetSelectedCoreAction(MouseAction action)
        {
            return action switch
            {
                MouseAction.Single => ClickAction.Single,
                MouseAction.Double => ClickAction.Double,
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };
        }


        private void ResetTitle()
        {
            Title = Constants.MAIN_WINDOW_TITLE_DEFAULT;
            if (systemTrayIcon != null)
            {
                systemTrayIcon.Text = Constants.MAIN_WINDOW_TITLE_DEFAULT;
            }
        }

        private void InitializeSystemTrayMenu()
        {
            systemTrayIcon = new NotifyIcon
            {
                Visible = true,
                Icon = AssemblyUtils.GetApplicationIcon(),
                Text = Constants.MAIN_WINDOW_TITLE_DEFAULT
            };
            systemTrayIcon.Click += SystemTrayIcon_Click;

            systemTrayMenu = new SystemTrayMenu();
            systemTrayMenu.SystemTrayMenuActionEvent += SystemTrayMenu_SystemTrayMenuActionEvent;
        }

        private void DisposeSystemTrayMenu()
        {
            systemTrayIcon.Click -= SystemTrayIcon_Click;
            systemTrayIcon.Dispose();

            systemTrayMenu.SystemTrayMenuActionEvent -= SystemTrayMenu_SystemTrayMenuActionEvent;
            systemTrayMenu.Dispose();
        }

        private HotkeyRegistrationSlot BuildHotkeySlot(Operation operation, KeyMapping hotkey, bool includeModifiers)
        {
            IEnumerable<int> ids = operation switch
            {
                Operation.Start => Constants.START_HOTKEY_IDS,
                Operation.Stop => Constants.STOP_HOTKEY_IDS,
                Operation.Toggle => Constants.TOGGLE_HOTKEY_IDS,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };

            return new HotkeyRegistrationSlot(
                operation.ToString(),
                ids.ToArray(),
                new HotkeyRegistrationBinding(hotkey.VirtualKeyCode, includeModifiers));
        }

        private void RegisterInitialHotkeys(HotkeySettings settings)
        {
            IReadOnlyList<HotkeyConflict> conflicts = HotkeyBindingValidator.Validate(
                new HotkeyBinding("Start", settings.StartHotkey.VirtualKeyCode),
                new HotkeyBinding("Stop", settings.StopHotkey.VirtualKeyCode),
                new HotkeyBinding("Toggle", settings.ToggleHotkey.VirtualKeyCode));
            if (conflicts.Count > 0)
            {
                Log.Warning("Saved hotkeys contain conflicts; restoring safe defaults");
                settings = new HotkeySettings
                {
                    StartHotkey = HotkeySettings.defaultStartKeyMapping,
                    StopHotkey = HotkeySettings.defaultStopKeyMapping,
                    ToggleHotkey = HotkeySettings.defaultToggleKeyMapping,
                    IncludeModifiers = HotkeySettings.defaultIncludeModifiers
                };
                SettingsUtils.SetHotkeySettings(settings);
                MessageBox.Show(this, "Saved global hotkeys conflicted, so AutoClicker restored the safe F6 / F7 / F8 defaults.",
                    "Hotkeys restored", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            TryRegisterInitialHotkey(Operation.Start, settings.StartHotkey, settings.IncludeModifiers);
            TryRegisterInitialHotkey(Operation.Stop, settings.StopHotkey, settings.IncludeModifiers);
            TryRegisterInitialHotkey(Operation.Toggle, settings.ToggleHotkey, settings.IncludeModifiers);
            UpdateHotkeyButtonLabels(settings);
        }

        private void TryRegisterInitialHotkey(Operation operation, KeyMapping hotkey, bool includeModifiers)
        {
            HotkeyRegistrationSlot slot = BuildHotkeySlot(operation, hotkey, includeModifiers);
            HotkeyRegistrationResult result = HotkeyRegistrationTransaction.TryRegisterSlot(hotkeyBackend, slot, Constants.MODIFIERS);
            if (result.Success)
            {
                registeredHotkeys[operation] = slot;
                return;
            }

            Log.Warning("Could not register {Operation} hotkey {Hotkey}; Win32 error {Error}", operation, hotkey.DisplayName, result.NativeError);
            MessageBox.Show(this, $"The {operation} hotkey ({hotkey.DisplayName}) could not be registered. Windows error: {result.NativeError}.",
                "Hotkey unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        internal bool TryApplyHotkeySettings(HotkeySettings proposed, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (clickEngine.IsRunning)
            {
                errorMessage = "Stop clicking before changing global hotkeys.";
                return false;
            }

            IReadOnlyList<HotkeyConflict> conflicts = HotkeyBindingValidator.Validate(
                new HotkeyBinding("Start", proposed.StartHotkey.VirtualKeyCode),
                new HotkeyBinding("Stop", proposed.StopHotkey.VirtualKeyCode),
                new HotkeyBinding("Toggle", proposed.ToggleHotkey.VirtualKeyCode));
            if (conflicts.Count > 0)
            {
                HotkeyConflict conflict = conflicts[0];
                errorMessage = $"{conflict.FirstOperation} and {conflict.SecondOperation} cannot use the same global hotkey.";
                return false;
            }

            HotkeyRegistrationSlot[] desired =
            {
                BuildHotkeySlot(Operation.Start, proposed.StartHotkey, proposed.IncludeModifiers),
                BuildHotkeySlot(Operation.Stop, proposed.StopHotkey, proposed.IncludeModifiers),
                BuildHotkeySlot(Operation.Toggle, proposed.ToggleHotkey, proposed.IncludeModifiers)
            };
            HotkeyRegistrationSlot[] current = registeredHotkeys.Values.ToArray();
            HotkeyRegistrationResult result = HotkeyRegistrationTransaction.ReplaceAll(hotkeyBackend, current, desired, Constants.MODIFIERS);
            if (!result.Success)
            {
                errorMessage = $"Windows refused the {result.FailedOperation} hotkey (error {result.NativeError}).";
                if (!result.RollbackSucceeded)
                {
                    foreach (int id in Constants.ALL_HOTKEY_IDS)
                        hotkeyBackend.Unregister(id);
                    registeredHotkeys.Clear();

                    HotkeyRegistrationSlot emergencyStop = current.FirstOrDefault(x => x.Operation == Operation.Stop.ToString());
                    if (emergencyStop != null)
                    {
                        HotkeyRegistrationResult stopRecovery = HotkeyRegistrationTransaction.TryRegisterSlot(hotkeyBackend, emergencyStop, Constants.MODIFIERS);
                        if (stopRecovery.Success)
                            registeredHotkeys[Operation.Stop] = emergencyStop;
                        else
                            errorMessage += " The previous Stop hotkey could not be restored; close AutoClicker and reopen it before starting a new run.";
                    }
                }
                return false;
            }

            registeredHotkeys.Clear();
            registeredHotkeys[Operation.Start] = desired[0];
            registeredHotkeys[Operation.Stop] = desired[1];
            registeredHotkeys[Operation.Toggle] = desired[2];
            UpdateHotkeyButtonLabels(proposed);
            return true;
        }

        private void UpdateHotkeyButtonLabels(HotkeySettings settings)
        {
            startButton.Content = $"{Constants.MAIN_WINDOW_START_BUTTON_CONTENT} ({settings.StartHotkey.DisplayName})";
            stopButton.Content = $"{Constants.MAIN_WINDOW_STOP_BUTTON_CONTENT} ({settings.StopHotkey.DisplayName})";
            toggleButton.Content = $"{Constants.MAIN_WINDOW_TOGGLE_BUTTON_CONTENT} ({settings.ToggleHotkey.DisplayName})";
        }

        #endregion Helper Methods

        #region Event Handlers

        private void ClickEngine_Faulted(object sender, Exception error)
        {
            Log.Error(error, "Click engine stopped after an input failure");
            _ = Dispatcher.InvokeAsync(() =>
                MessageBox.Show(this, error.Message, "AutoClicker stopped", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void ClickEngine_Stopped(object sender, EventArgs e)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                ResetTitle();
                Icon = _defaultIcon;
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private nint StartStopHooks(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == Constants.WM_HOTKEY && Constants.ALL_HOTKEY_IDS.Contains(wParam.ToInt32()))
            {
                int virtualKey = ((int)lParam >> 16) & 0xFFFF;
                if (virtualKey == SettingsUtils.CurrentSettings.HotkeySettings.StartHotkey.VirtualKeyCode && CanStartOperation())
                {
                    _ = StartClickingAsync();
                }
                if (virtualKey == SettingsUtils.CurrentSettings.HotkeySettings.StopHotkey.VirtualKeyCode && clickEngine.IsRunning)
                {
                    _ = StopClickingAsync();
                }
                if (virtualKey == SettingsUtils.CurrentSettings.HotkeySettings.ToggleHotkey.VirtualKeyCode && (CanStartOperation() || clickEngine.IsRunning))
                {
                    _ = ToggleClickingAsync();
                }
                handled = true;
            }
            return nint.Zero;
        }


        private void SystemTrayIcon_Click(object sender, EventArgs e)
        {
            systemTrayMenu.IsOpen = true;
            systemTrayMenu.Focus();
        }

        private void SystemTrayMenu_SystemTrayMenuActionEvent(object sender, SystemTrayMenuActionEventArgs e)
        {
            switch (e.Action)
            {
                case SystemTrayMenuAction.Show:
                    Show();
                    break;
                case SystemTrayMenuAction.Hide:
                    Hide();
                    break;
                case SystemTrayMenuAction.Exit:
                    Exit();
                    break;
                default:
                    Log.Warning("Action {Action} not supported!", e.Action);
                    throw new NotSupportedException($"Action {e.Action} not supported!");
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (aboutWindow == null)
            {
                aboutWindow = new AboutWindow();
                aboutWindow.Closed += (o, args) => aboutWindow = null;
            }

            aboutWindow.Show();
        }

        private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            systemTrayMenu.ToggleMenuItemsVisibility(true);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void RadioButtonSelectedLocationMode_CurrentLocationOnChecked(object sender, RoutedEventArgs e)
        {
            TextBoxPickedXValue.Text = string.Empty;
            TextBoxPickedYValue.Text = string.Empty;
        }

        #endregion Event Handlers

        private void TopMostCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            Topmost = checkbox.IsChecked.Value;
        }
    }
}