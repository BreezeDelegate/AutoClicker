using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using AutoClicker.Core.Hotkeys;
using AutoClicker.Models;
using AutoClicker.Utils;
using Serilog;
using CheckBox = System.Windows.Controls.CheckBox;

namespace AutoClicker.Views
{
    public partial class SettingsWindow : Window
    {
        #region Dependency Properties

        public KeyMapping SelectedStartKey
        {
            get => (KeyMapping)GetValue(SelectedStartKeyProperty);
            set => SetValue(SelectedStartKeyProperty, value);
        }

        public static readonly DependencyProperty SelectedStartKeyProperty =
            DependencyProperty.Register(nameof(SelectedStartKey), typeof(KeyMapping), typeof(SettingsWindow));

        public KeyMapping SelectedStopKey
        {
            get => (KeyMapping)GetValue(SelectedStopKeyProperty);
            set => SetValue(SelectedStopKeyProperty, value);
        }

        public static readonly DependencyProperty SelectedStopKeyProperty =
            DependencyProperty.Register(nameof(SelectedStopKey), typeof(KeyMapping), typeof(SettingsWindow));

        public KeyMapping SelectedToggleKey
        {
            get => (KeyMapping)GetValue(SelectedToggleKeyProperty);
            set => SetValue(SelectedToggleKeyProperty, value);
        }

        public static readonly DependencyProperty SelectedToggleKeyProperty =
            DependencyProperty.Register(nameof(SelectedToggleKey), typeof(KeyMapping), typeof(SettingsWindow));

        public bool IncludeModifiers
        {
            get => (bool)GetValue(IncludeModifiersProperty);
            set => SetValue(IncludeModifiersProperty, value);
        }

        public static readonly DependencyProperty IncludeModifiersProperty =
            DependencyProperty.Register(nameof(IncludeModifiers), typeof(bool), typeof(SettingsWindow));

        public List<KeyMapping> KeyMapping { get; set; }

        #endregion Dependency Properties

        #region Life Cycle

        public SettingsWindow()
        {
            DataContext = this;
            KeyMapping = KeyMappingUtils.KeyMapping;

            Title = Constants.SETTINGS_WINDOW_TITLE;
            SelectedStartKey = SettingsUtils.CurrentSettings.HotkeySettings.StartHotkey;
            SelectedStopKey = SettingsUtils.CurrentSettings.HotkeySettings.StopHotkey;
            SelectedToggleKey = SettingsUtils.CurrentSettings.HotkeySettings.ToggleHotkey;
            IncludeModifiers = SettingsUtils.CurrentSettings.HotkeySettings.IncludeModifiers;

            InitializeComponent();
        }

        #endregion Life Cycle

        #region Commands

        private void SaveCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            HotkeySettings proposed = new HotkeySettings
            {
                StartHotkey = SelectedStartKey,
                StopHotkey = SelectedStopKey,
                ToggleHotkey = SelectedToggleKey,
                IncludeModifiers = IncludeModifiers
            };

            IReadOnlyList<HotkeyConflict> conflicts = HotkeyBindingValidator.Validate(
                new HotkeyBinding("Start", proposed.StartHotkey.VirtualKeyCode),
                new HotkeyBinding("Stop", proposed.StopHotkey.VirtualKeyCode),
                new HotkeyBinding("Toggle", proposed.ToggleHotkey.VirtualKeyCode));
            if (conflicts.Count > 0)
            {
                HotkeyConflict conflict = conflicts[0];
                MessageBox.Show(this, $"{conflict.FirstOperation} and {conflict.SecondOperation} cannot use the same global hotkey.",
                    "Hotkey conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Owner is not MainWindow mainWindow)
            {
                MessageBox.Show(this, "The main AutoClicker window is unavailable; hotkeys were not changed.",
                    "Hotkey unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!mainWindow.TryApplyHotkeySettings(proposed, out string errorMessage))
            {
                MessageBox.Show(this, errorMessage, "Hotkey unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SettingsUtils.SetHotkeySettings(proposed);
            Close();
        }

        private void ResetCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedStartKey = HotkeySettings.defaultStartKeyMapping;
            SelectedStopKey = HotkeySettings.defaultStopKeyMapping;
            SelectedToggleKey = HotkeySettings.defaultToggleKeyMapping;
            IncludeModifiers = HotkeySettings.defaultIncludeModifiers;
        }

        #endregion Commands

        #region Helper Methods

        private void StartKeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            SelectedStartKey = GenericKeyDownHandler(e) ?? SelectedStartKey;
        }

        private void StopKeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            SelectedStopKey = GenericKeyDownHandler(e) ?? SelectedStopKey;
        }

        private void ToggleKeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            SelectedToggleKey = GenericKeyDownHandler(e) ?? SelectedToggleKey;
        }

        private void IncludeModifiersCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            IncludeModifiers = checkbox.IsChecked.Value;
        }

        private KeyMapping GenericKeyDownHandler(KeyEventArgs e)
        {
            KeyMapping newKeyMapping = GetNewKeyMapping(e.Key);
            if (newKeyMapping == null)
            {
                Log.Error("No Matching key for {Key}!", e.Key);
                return null;
            }

            e.Handled = true;
            return newKeyMapping;
        }

        private KeyMapping GetNewKeyMapping(Key key)
        {
            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);
            Log.Debug("GetNewKeyMapping with virtualKeyCode={VirtualKeyCode}", virtualKeyCode);
            return KeyMapping.Find(keyMapping => keyMapping.VirtualKeyCode == virtualKeyCode);
        }

        #endregion Helper Methods
    }
}
