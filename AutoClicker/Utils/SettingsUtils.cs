using System;
using System.IO;
using AutoClicker.Core.Storage;
using AutoClicker.Models;
using Serilog;

namespace AutoClicker.Utils
{
    public static class SettingsUtils
    {
        private static readonly string dataDirectory = DataDirectoryResolver.Resolve(
            AppContext.BaseDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoClicker"));
        private static readonly string settingsFilePath = Path.Combine(dataDirectory, Constants.SETTINGS_FILE_PATH);
        private static readonly string logFilePath = Path.Combine(dataDirectory, Constants.LOG_FILE_PATH);

        public static ApplicationSettings CurrentSettings { get; set; }

        public static string DataDirectory => dataDirectory;

        static SettingsUtils()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logFilePath)
                .CreateLogger();
            Log.Debug("==================================================");
            Log.Information("Logger initialized successfully");

            LoadSettingsFromFile();
        }


        private static void SaveSettingsToFile()
        {
            try
            {
                JsonUtils.WriteJson(settingsFilePath, CurrentSettings);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Could not save settings to {SettingsFile}", settingsFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "Could not save settings to {SettingsFile}", settingsFilePath);
            }
        }

        public static void LoadSettingsFromFile()
        {
            ApplicationSettings applicationSettings = JsonUtils.ReadJson<ApplicationSettings>(settingsFilePath);
            if (applicationSettings == null)
            {
                CurrentSettings = new ApplicationSettings();
            }
            else
            {
                CurrentSettings = applicationSettings;
            }
        }

        public static void SetHotkeySettings(HotkeySettings settings)
        {
            CurrentSettings.HotkeySettings.StartHotkey = settings.StartHotkey;
            CurrentSettings.HotkeySettings.StopHotkey = settings.StopHotkey;
            CurrentSettings.HotkeySettings.ToggleHotkey = settings.ToggleHotkey;
            CurrentSettings.HotkeySettings.IncludeModifiers = settings.IncludeModifiers;
            SaveSettingsToFile();
        }

        public static void SetApplicationSettings(AutoClickerSettings settings)
        {
            CurrentSettings.AutoClickerSettings.Milliseconds = settings.Milliseconds;
            CurrentSettings.AutoClickerSettings.Seconds = settings.Seconds;
            CurrentSettings.AutoClickerSettings.Minutes = settings.Minutes;
            CurrentSettings.AutoClickerSettings.Hours = settings.Hours;
            CurrentSettings.AutoClickerSettings.VarianceMilliseconds = settings.VarianceMilliseconds;

            CurrentSettings.AutoClickerSettings.PickedXValue = settings.PickedXValue;
            CurrentSettings.AutoClickerSettings.PickedYValue = settings.PickedYValue;

            CurrentSettings.AutoClickerSettings.SelectedLocationMode = settings.SelectedLocationMode;
            CurrentSettings.AutoClickerSettings.SelectedMouseAction = settings.SelectedMouseAction;
            CurrentSettings.AutoClickerSettings.SelectedMouseButton = settings.SelectedMouseButton;
            CurrentSettings.AutoClickerSettings.SelectedRepeatMode = settings.SelectedRepeatMode;
            CurrentSettings.AutoClickerSettings.SelectedTimesToRepeat = settings.SelectedTimesToRepeat;

            SaveSettingsToFile();
        }
    }
}
