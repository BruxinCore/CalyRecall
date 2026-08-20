using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;
using CalyRecallNative.Services;
using Gma.System.MouseKeyHook;

namespace CalyRecallNative.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly SteamService _steamService;
        private readonly ISettingsService _settingsService;
        private readonly BackupManager _backupManager;
        private readonly IKeyboardMouseEvents _globalHook;

        public RestoreViewModel RestoreVM { get; }
        public CloudViewModel CloudVM { get; }

        public MainWindowViewModel(SteamService steamService, ISettingsService settingsService, BackupManager backupManager, RestoreViewModel restoreVM, CloudViewModel cloudVM)
        {
            _steamService = steamService;
            _settingsService = settingsService;
            _backupManager = backupManager;
            RestoreVM = restoreVM;
            CloudVM = cloudVM;
            
            CheckConnectionStatus();

            _backupManager.SemiAutoBackupRequested += OnSemiAutoBackupRequested;

            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnGlobalKeyDown;
            _globalHook.MouseDownExt += OnGlobalMouseDown;
        }

        private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.ShiftKey || e.KeyCode == System.Windows.Forms.Keys.LShiftKey || e.KeyCode == System.Windows.Forms.Keys.RShiftKey ||
                e.KeyCode == System.Windows.Forms.Keys.ControlKey || e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey ||
                e.KeyCode == System.Windows.Forms.Keys.Menu || e.KeyCode == System.Windows.Forms.Keys.LMenu || e.KeyCode == System.Windows.Forms.Keys.RMenu ||
                e.KeyCode == System.Windows.Forms.Keys.LWin || e.KeyCode == System.Windows.Forms.Keys.RWin)
            {
                return;
            }

            CheckAndExecuteHotkey(e.KeyCode.ToString());
        }

        private void OnGlobalMouseDown(object sender, MouseEventExtArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left || e.Button == System.Windows.Forms.MouseButtons.Right)
                return;

            CheckAndExecuteHotkey(e.Button.ToString());
        }

        private async void CheckAndExecuteHotkey(string keyOrButton)
        {
            var hotkeyStr = _settingsService.Config.QuickSaveHotkey;
            if (string.IsNullOrEmpty(hotkeyStr)) return;

            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            string currentInput = "";

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) currentInput += "Ctrl+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) currentInput += "Alt+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) currentInput += "Shift+";

            currentInput += keyOrButton;

            if (currentInput.Equals(hotkeyStr, StringComparison.OrdinalIgnoreCase))
            {
                var appId = _steamService.GetRunningAppId();
                if (appId > 0)
                {
                    await _backupManager.DoBackupAsync(appId);
                }
            }
        }

        private async void OnSemiAutoBackupRequested(object sender, int appId)
        {
            PendingSemiAutoAppId = appId;
            PendingSemiAutoGameName = await _backupManager.GetGameNameAsync(appId);
            PendingSemiAutoGameCover = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg";
            IsSemiAutoModalOpen = true;
        }

        [RelayCommand]
        private async Task ConfirmSemiAutoBackup()
        {
            IsSemiAutoModalOpen = false;
            if (PendingSemiAutoAppId > 0)
            {
                await _backupManager.DoBackupAsync(PendingSemiAutoAppId);
            }
        }

        [RelayCommand]
        private void DiscardSemiAutoBackup()
        {
            IsSemiAutoModalOpen = false;
            PendingSemiAutoAppId = 0;
            PendingSemiAutoGameName = string.Empty;
        }

        [ObservableProperty]
        private bool _isSemiAutoModalOpen;

        [ObservableProperty]
        private int _pendingSemiAutoAppId;

        [ObservableProperty]
        private string _pendingSemiAutoGameName;

        [ObservableProperty]
        private string _pendingSemiAutoGameCover;

        private void CheckConnectionStatus()
        {
            var steamPath = _steamService.GetSteamPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                ConnectionStatusText = System.Windows.Application.Current.TryFindResource("MainWindow_StatusConnected") as string ?? "v3.0 - Conectado";
                ConnectionStatusColor = "#2ecc71";
            }
            else
            {
                ConnectionStatusText = System.Windows.Application.Current.TryFindResource("MainWindow_StatusDisconnected") as string ?? "v3.0 - Desconectado";
                ConnectionStatusColor = "#e74c3c";
            }
        }

        [ObservableProperty]
        private string _connectionStatusText = "v3.0";

        [ObservableProperty]
        private string _connectionStatusColor = "#e74c3c";

        [ObservableProperty]
        private string _applicationTitle = "CalyRecall";

        [ObservableProperty]
        private ObservableCollection<object> _navigationItems = new()
        {
            new NavigationViewItem()
            {
                Content = System.Windows.Application.Current.TryFindResource("Nav_Dashboard") as string ?? "InÃ­cio",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = System.Windows.Application.Current.TryFindResource("Notification_BackupsTitle") as string ?? "Backups",
                Icon = new SymbolIcon { Symbol = SymbolRegular.History24 },
                TargetPageType = typeof(Views.Pages.RestorePage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _navigationFooter = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };
    }
}
