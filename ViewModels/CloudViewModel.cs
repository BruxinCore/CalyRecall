using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CalyRecallNative.Models;
using CalyRecallNative.Services;
using Newtonsoft.Json.Linq;

namespace CalyRecallNative.ViewModels
{
    public partial class CloudViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private string _accountEmail = "";

        [ObservableProperty]
        private string _accountName = "Não conectado";

        [ObservableProperty]
        private string _accountPhotoUrl = "";

        [ObservableProperty]
        private bool _isEmailVisible = false;

        [RelayCommand]
        private void ToggleEmailVisibility()
        {
            IsEmailVisible = !IsEmailVisible;
        }

        [ObservableProperty]
        private string _cloudStorageUsage = "0 GB";

        [ObservableProperty]
        private string _cloudTotalStorage = "0 GB";

        [ObservableProperty]
        private double _cloudStoragePercentage = 0;

        public string ConnectButtonText => IsAuthenticated 
            ? (string)System.Windows.Application.Current.TryFindResource("Cloud_DisconnectBtn") ?? "Desconectar"
            : (string)System.Windows.Application.Current.TryFindResource("Cloud_ConnectBtn") ?? "Conectar Google Drive";

        [ObservableProperty]
        private ObservableCollection<BackupItem> _cloudBackups = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(CloudBackups);
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is BackupItem item)
                    {
                        return item.GameName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                               item.Nickname?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
                    }
                    return false;
                };
            }
            view.Refresh();
            OnPropertyChanged(nameof(HasCloudBackups));
            OnPropertyChanged(nameof(HasNoCloudBackups));
        }

        public bool HasCloudBackups => !System.Windows.Data.CollectionViewSource.GetDefaultView(CloudBackups).IsEmpty;
        public bool HasNoCloudBackups => System.Windows.Data.CollectionViewSource.GetDefaultView(CloudBackups).IsEmpty;

        public bool HasSelectedItems => CloudBackups.Any(b => b.IsSelected);
        public string SyncSelectedText => $"Enviar {CloudBackups.Count(b => b.IsSelected)} Selecionados";

        private void OnBackupItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BackupItem.IsSelected))
            {
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(SyncSelectedText));
            }
        }

        [ObservableProperty]
        private bool _isUploadModalOpen;

        [ObservableProperty]
        private string _uploadProgressTitle = "";

        [ObservableProperty]
        private double _uploadProgressValue;

        private CancellationTokenSource? _uploadCts;

        [RelayCommand]
        private void CancelUpload()
        {
            _uploadCts?.Cancel();
        }

        private readonly CloudDriveService _cloudService;
        private readonly BackupManager _backupManager;
        private readonly ISettingsService _settingsService;
        private readonly Wpf.Ui.ISnackbarService _snackbarService;

        public CloudViewModel(CloudDriveService cloudService, BackupManager backupManager, ISettingsService settingsService, Wpf.Ui.ISnackbarService snackbarService)
        {
            _cloudService = cloudService;
            _backupManager = backupManager;
            _settingsService = settingsService;
            _snackbarService = snackbarService;
            
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            _backupManager.BackupCompleted += (s, e) => 
            {
                dispatcher.BeginInvoke(() => LoadLocalBackups());
            };

            _backupManager.BackupDeleted += (s, e) => 
            {
                dispatcher.BeginInvoke(() => LoadLocalBackups());
            };

            LoadLocalBackups();
            _ = CheckExistingAuth();
        }

        private async Task CheckExistingAuth()
        {
            string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CalyRecall", "GoogleAuth");
            if (System.IO.Directory.Exists(credPath) && System.IO.Directory.GetFiles(credPath).Length > 0)
            {
                bool success = await _cloudService.AuthenticateAsync();
                if (success)
                {
                    IsAuthenticated = true;
                    AccountEmail = _cloudService.UserEmail;
                    AccountName = string.IsNullOrEmpty(_cloudService.UserName) ? _cloudService.UserEmail : _cloudService.UserName;
                    AccountPhotoUrl = _cloudService.UserPhotoUrl;
                    var quota = await _cloudService.GetStorageQuotaAsync();
                    if (quota.Limit > 0)
                    {
                        double usageGB = (double)quota.Usage / 1024 / 1024 / 1024;
                        double limitGB = (double)quota.Limit / 1024 / 1024 / 1024;
                        CloudStorageUsage = $"{usageGB:F2} GB";
                        CloudTotalStorage = $"{limitGB:F0} GB";
                        CloudStoragePercentage = (usageGB / limitGB) * 100;
                    }
                    OnPropertyChanged(nameof(ConnectButtonText));
                }
            }
        }

        private void LoadLocalBackups()
        {
            foreach (var b in CloudBackups)
            {
                b.PropertyChanged -= OnBackupItemPropertyChanged;
            }
            CloudBackups.Clear();
            var folder = _settingsService.Config.BackupFolder;
            if (!System.IO.Directory.Exists(folder)) return;

            var dirs = System.IO.Directory.GetDirectories(folder, "*", System.IO.SearchOption.AllDirectories);
            var tempItems = new System.Collections.Generic.List<BackupItem>();
            foreach (var dir in dirs)
            {
                var metaPath = System.IO.Path.Combine(dir, "caly_meta.json");
                if (System.IO.File.Exists(metaPath))
                {
                    try
                    {
                        var meta = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(metaPath));
                        var item = new BackupItem
                        {
                            FolderPath = dir,
                            FolderName = System.IO.Path.GetFileName(dir),
                            AppId = (int)(meta["appid"] ?? 0),
                            GameName = meta["game_name"]?.ToString(),
                            Nickname = meta["nickname"]?.ToString(),
                            Timestamp = System.DateTime.TryParseExact(meta["timestamp"]?.ToString(), "yyyy-MM-dd_HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var t) ? t : System.DateTime.MinValue,
                            CustomCoverUrl = meta["custom_cover"]?.ToString() ?? string.Empty
                        };
                        item.PropertyChanged += OnBackupItemPropertyChanged;
                        tempItems.Add(item);
                    }
                    catch { }
                }
            }
            
            foreach (var item in System.Linq.Enumerable.OrderByDescending(tempItems, x => x.Timestamp))
            {
                CloudBackups.Add(item);
            }
            ApplySearchFilter();
        }

        [RelayCommand]
        private async Task Authenticate()
        {
            if (IsAuthenticated)
            {
                _cloudService.Disconnect();
                IsAuthenticated = false;
                AccountName = "Não conectado";
                AccountEmail = "";
                AccountPhotoUrl = "";
                IsEmailVisible = false;
                CloudStorageUsage = "0 GB";
                CloudStoragePercentage = 0;
                OnPropertyChanged(nameof(ConnectButtonText));
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastDisconnectedTitle") ?? "Desconectado",
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastDisconnectedDesc") ?? "Conta do Google Drive desconectada.",
                    Wpf.Ui.Controls.SymbolRegular.Info24);
                return;
            }

            bool success = await _cloudService.AuthenticateAsync();
            if (success)
            {
                IsAuthenticated = true;
                AccountEmail = _cloudService.UserEmail;
                AccountName = string.IsNullOrEmpty(_cloudService.UserName) ? _cloudService.UserEmail : _cloudService.UserName;
                AccountPhotoUrl = _cloudService.UserPhotoUrl;
                
                var quota = await _cloudService.GetStorageQuotaAsync();
                if (quota.Limit > 0)
                {
                    double usageGB = (double)quota.Usage / 1024 / 1024 / 1024;
                    double limitGB = (double)quota.Limit / 1024 / 1024 / 1024;
                    CloudStorageUsage = $"{usageGB:F2} GB";
                    CloudTotalStorage = $"{limitGB:F0} GB";
                    CloudStoragePercentage = (usageGB / limitGB) * 100;
                }

                OnPropertyChanged(nameof(ConnectButtonText));
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastConnectedTitle") ?? "Conectado",
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastConnectedDesc") ?? "Conta conectada com sucesso!",
                    Wpf.Ui.Controls.SymbolRegular.Checkmark24);
            }
            else
            {
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast(
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastErrorTitle") ?? "Erro",
                    (string)System.Windows.Application.Current.TryFindResource("Cloud_ToastErrorDesc") ?? "Não foi possível conectar ao Google Drive.",
                    Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
            }
        }

        [RelayCommand]
        private async Task SyncAll()
        {
            var backupsToSync = CloudBackups.ToList();
            if (!backupsToSync.Any()) return;
            await UploadMultipleBackupsAsSingleZip(backupsToSync);
        }

        [RelayCommand]
        private void ClearSelection()
        {
            foreach (var b in CloudBackups)
            {
                b.IsSelected = false;
            }
        }

        [RelayCommand]
        private async Task SyncSelected()
        {
            var selectedItems = CloudBackups.Where(b => b.IsSelected).ToList();
            if (!selectedItems.Any()) return;
            await UploadMultipleBackupsAsSingleZip(selectedItems);
        }

        private async Task UploadMultipleBackupsAsSingleZip(List<BackupItem> backups)
        {
            if (!IsAuthenticated)
            {
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast("Nuvem", "Conecte sua conta do Google Drive primeiro.", Wpf.Ui.Controls.SymbolRegular.Warning24);
                return;
            }

            _uploadCts = new CancellationTokenSource();
            IsUploadModalOpen = true;
            UploadProgressTitle = "Preparando arquivo...";
            UploadProgressValue = 0;

            string tempZipPath = Path.Combine(Path.GetTempPath(), $"CalyRecall_Backups_{DateTime.Now:yyyy-MM-dd}.zip");
            
            try
            {
                await Task.Run(() => 
                {
                    var backupFolder = _settingsService.Config.BackupFolder;
                    using var stream = new FileStream(tempZipPath, FileMode.Create);
                    using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create);

                    int totalFiles = 0;
                    var allFiles = new List<(string File, string RelativePath)>();

                    foreach (var backup in backups)
                    {
                        if (Directory.Exists(backup.FolderPath))
                        {
                            var files = Directory.GetFiles(backup.FolderPath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                var relativePath = Path.GetRelativePath(backupFolder, file);
                                allFiles.Add((file, relativePath));
                            }
                        }
                    }

                    for (int i = 0; i < allFiles.Count; i++)
                    {
                        _uploadCts.Token.ThrowIfCancellationRequested();
                        var item = allFiles[i];
                        archive.CreateEntryFromFile(item.File, item.RelativePath, System.IO.Compression.CompressionLevel.Optimal);
                        
                        App.Current.Dispatcher.BeginInvoke(new Action(() => 
                        {
                            UploadProgressTitle = $"Compactando arquivos ({i + 1}/{allFiles.Count})...";
                            UploadProgressValue = (double)(i + 1) / allFiles.Count * 100;
                        }));
                    }
                }, _uploadCts.Token);

                if (_uploadCts.IsCancellationRequested) return;

                UploadProgressTitle = "Enviando para o Google Drive...";
                UploadProgressValue = 0;

                long totalBytes = new FileInfo(tempZipPath).Length;
                bool uploaded = await _cloudService.UploadBackupAsync(tempZipPath, (sent) =>
                {
                    if (totalBytes > 0)
                    {
                        App.Current.Dispatcher.BeginInvoke(new Action(() => 
                        {
                            UploadProgressValue = (double)sent / totalBytes * 100;
                        }));
                    }
                }, _uploadCts.Token);

                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                if (!uploaded && !_uploadCts.IsCancellationRequested)
                {
                    IsUploadModalOpen = false;
                    var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                    mainWindow?.ShowCustomToast("Erro", "Falha ao enviar os backups.", Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
                    return;
                }

                if (uploaded)
                {
                    foreach (var b in backups) b.IsSelected = false;
                }

                IsUploadModalOpen = false;

                if (!_uploadCts.IsCancellationRequested)
                {
                    var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                    mainWindow?.ShowCustomToast("Nuvem", "Backups sincronizados com sucesso!", Wpf.Ui.Controls.SymbolRegular.Checkmark24);
                }
            }
            catch (OperationCanceledException)
            {
                IsUploadModalOpen = false;
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }
            catch (Exception ex)
            {
                IsUploadModalOpen = false;
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast("Erro", "Falha ao processar os backups.", Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
                try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_FatalCrash.txt"), "CloudUploadError: " + ex.ToString() + "\n"); } catch { }
            }
        }

        [RelayCommand]
        private async Task UploadSingle(BackupItem backup)
        {
            if (!IsAuthenticated)
            {
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                mainWindow?.ShowCustomToast("Nuvem", "Conecte sua conta do Google Drive primeiro.", Wpf.Ui.Controls.SymbolRegular.Warning24);
                return;
            }

            string tempZipPath = Path.Combine(Path.GetTempPath(), backup.FolderName + ".zip");
            if (Directory.Exists(backup.FolderPath))
            {
                _uploadCts = new CancellationTokenSource();
                IsUploadModalOpen = true;
                UploadProgressTitle = $"Enviando {backup.GameName}...";
                UploadProgressValue = 0;

                try
                {
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                    System.IO.Compression.ZipFile.CreateFromDirectory(backup.FolderPath, tempZipPath, System.IO.Compression.CompressionLevel.Optimal, false);

                    long totalBytes = new FileInfo(tempZipPath).Length;

                    bool uploaded = await _cloudService.UploadBackupAsync(tempZipPath, (sent) =>
                    {
                        if (totalBytes > 0)
                        {
                            App.Current.Dispatcher.BeginInvoke(new Action(() => 
                            {
                                UploadProgressValue = (double)sent / totalBytes * 100;
                            }));
                        }
                    }, _uploadCts.Token);

                    IsUploadModalOpen = false;
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                    if (uploaded)
                    {
                        var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                        mainWindow?.ShowCustomToast("Sucesso", $"{backup.GameName} sincronizado com a nuvem!", Wpf.Ui.Controls.SymbolRegular.Checkmark24);
                    }
                    else if (!_uploadCts.IsCancellationRequested)
                    {
                        var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                        mainWindow?.ShowCustomToast("Erro", $"Falha ao enviar {backup.GameName}.", Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
                    }
                }
                catch
                {
                    IsUploadModalOpen = false;
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                    var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                    mainWindow?.ShowCustomToast("Erro", $"Falha ao compactar {backup.GameName}.", Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
                }
            }
        }
    }
}
