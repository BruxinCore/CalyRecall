using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CalyRecallNative.Services;
using Microsoft.Win32;
using System.IO;

namespace CalyRecallNative.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly GameSavePathResolver _pathResolver;
        private readonly Wpf.Ui.ISnackbarService _snackbarService;

        [ObservableProperty]
        private bool _isUpdatingDatabase;

        [ObservableProperty]
        private string _backupFolder;

        [ObservableProperty]
        private string _selectedMode;

        [ObservableProperty]
        private string _quickSaveHotkey;

        [ObservableProperty]
        private string _selectedLanguage;

        public string[] AvailableModes { get; } = new[] { "Auto", "SemiAuto", "Manual" };
        
        public string[] AvailableLanguages { get; } = new[] { "pt-BR", "en-US", "es-ES" };

        public SettingsViewModel(ISettingsService settingsService, GameSavePathResolver pathResolver, Wpf.Ui.ISnackbarService snackbarService)
        {
            _settingsService = settingsService;
            _pathResolver = pathResolver;
            _snackbarService = snackbarService;
            
            _backupFolder = _settingsService.Config.BackupFolder;
            _selectedMode = _settingsService.Config.Mode;
            _quickSaveHotkey = _settingsService.Config.QuickSaveHotkey;
            _selectedLanguage = _settingsService.Config.Language;
        }

        [RelayCommand]
        private void ChangeFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = System.Windows.Application.Current.TryFindResource("Settings_SelectFolderDialog") as string ?? "Selecione a pasta de Backups",
                InitialDirectory = BackupFolder
            };

            if (dialog.ShowDialog() == true)
            {
                BackupFolder = dialog.FolderName;
                SaveSettings();
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            System.IO.File.AppendAllText("save_log.txt", "SettingsViewModel.SaveSettings called with: " + (QuickSaveHotkey ?? "null") + "\n");
            _settingsService.Config.BackupFolder = BackupFolder;
            _settingsService.Config.Mode = SelectedMode;
            _settingsService.Config.QuickSaveHotkey = QuickSaveHotkey;
            _settingsService.Config.Language = SelectedLanguage;
            _settingsService.Save();
        }
        
        partial void OnSelectedModeChanged(string value) => SaveSettings();
        partial void OnQuickSaveHotkeyChanged(string value) => SaveSettings();
        partial void OnSelectedLanguageChanged(string value)
        {
            SaveSettings();
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task UpdateLudusavi()
        {
            if (IsUpdatingDatabase) return;
            IsUpdatingDatabase = true;

            var result = await _pathResolver.UpdateManifestAsync();

            IsUpdatingDatabase = false;

            var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();

            if (result == true)
            {
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastSuccessTitle") ?? "Sucesso",
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastSuccessDesc") ?? "Banco de dados atualizado com sucesso!",
                    Wpf.Ui.Controls.SymbolRegular.Checkmark24
                );
            }
            else if (result == false)
            {
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastInfoTitle") ?? "Atualizado",
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastInfoDesc") ?? "O banco de dados já está na versão mais recente!",
                    Wpf.Ui.Controls.SymbolRegular.Info24
                );
            }
            else
            {
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastErrorTitle") ?? "Erro",
                    (string)System.Windows.Application.Current.TryFindResource("Ludusavi_ToastErrorDesc") ?? "Falha ao conectar. Verifique sua internet.",
                    Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                );
            }
        }
    }
}
