using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CalyRecallNative.Services;
using CalyRecallNative.ViewModels;
using CalyRecallNative.Views;
using CalyRecallNative.Views.Pages;

namespace CalyRecallNative
{
    public partial class App : System.Windows.Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<SteamService>();
                services.AddSingleton<BackupManager>();
                services.AddSingleton<GameSavePathResolver>();
                services.AddSingleton<TrayIconService>();
                services.AddSingleton<Wpf.Ui.ISnackbarService, Wpf.Ui.SnackbarService>();
                services.AddHostedService<SteamMonitorService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();

                services.AddSingleton<RestorePage>();
                services.AddSingleton<RestoreViewModel>();

                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<CloudDriveService>();
                services.AddSingleton<CloudPage>();
                services.AddSingleton<CloudViewModel>();
            }).Build();

        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica);
            Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(System.Windows.Media.Color.FromRgb(178, 141, 249), Wpf.Ui.Appearance.ApplicationTheme.Dark);
            
            await _host.StartAsync();

            var pathResolver = GetService<GameSavePathResolver>();
            _ = Task.Run(() => pathResolver.InitializeAsync());

            var settingsService = GetService<ISettingsService>();
            ApplyLanguage(settingsService.Config.Language);
            settingsService.SettingsChanged += (s, ev) => ApplyLanguage(settingsService.Config.Language);

            var trayIconService = GetService<TrayIconService>();
            trayIconService.Initialize();

            var mainWindow = GetService<MainWindow>();
            mainWindow.Show();
        }

        private void ApplyLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) languageCode = "pt-BR";

            try
            {
                var dict = new System.Windows.ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/dictionaries/{languageCode.ToLowerInvariant()}.xaml", UriKind.Absolute)
                };

                var existingDict = Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("dictionaries/"));

                if (existingDict != null)
                {
                    Current.Resources.MergedDictionaries.Remove(existingDict);
                }

                Current.Resources.MergedDictionaries.Add(dict);

                try
                {
                    File.AppendAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_LangDebug.txt"),
                        $"[{DateTime.Now}] Language={languageCode}, Keys={dict.Count}, Total MergedDicts={Current.Resources.MergedDictionaries.Count}\n");
                }
                catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_LangDebug.txt"),
                        $"[{DateTime.Now}] ERROR loading {languageCode}: {ex.Message}\n");
                }
                catch { }
            }
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
