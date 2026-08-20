using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using CalyRecallNative.Models;
using CalyRecallNative.Services;

namespace CalyRecallNative.ViewModels
{
    public partial class RestoreViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly BackupManager _backupManager;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasBackups))]
        [NotifyPropertyChangedFor(nameof(HasNoBackups))]
        private ObservableCollection<BackupItem> _backups = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Backups);
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
                               item.DisplayName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
                    }
                    return false;
                };
            }
            view.Refresh();
            OnPropertyChanged(nameof(HasBackups));
            OnPropertyChanged(nameof(HasNoBackups));
        }

        public bool HasBackups => !System.Windows.Data.CollectionViewSource.GetDefaultView(Backups).IsEmpty;
        public bool HasNoBackups => System.Windows.Data.CollectionViewSource.GetDefaultView(Backups).IsEmpty;

        public bool HasSelectedItems => Backups.Any(b => b.IsSelected);
        public string DeleteSelectedText => $"Apagar {Backups.Count(b => b.IsSelected)} Selecionados";

        public string DeleteModalMessage => SelectedDeleteItem != null ? SelectedDeleteItem.GameName : $"{Backups.Count(b => b.IsSelected)} backups selecionados";

        private void OnBackupItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BackupItem.IsSelected))
            {
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(DeleteSelectedText));
            }
        }

        public RestoreViewModel(ISettingsService settingsService, BackupManager backupManager)
        {
            _settingsService = settingsService;
            _backupManager = backupManager;
            
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            _backupManager.BackupCompleted += (s, e) => 
            {
                dispatcher.BeginInvoke(() => LoadBackups());
            };

            LoadBackups();
        }

        [ObservableProperty]
        private bool _isLoading;

        private bool _isLoadingInternal;

        [RelayCommand]
        public async Task LoadBackups()
        {
            if (_isLoadingInternal) return;
            _isLoadingInternal = true;
            IsLoading = true;
            
            try
            {
                foreach (var b in Backups)
                {
                    b.PropertyChanged -= OnBackupItemPropertyChanged;
                }
                Backups.Clear();
                await Task.Delay(400);
                var folder = _settingsService.Config.BackupFolder;
                if (!Directory.Exists(folder)) return;

                var dirs = Directory.GetDirectories(folder);
                var tempItems = new System.Collections.Generic.List<BackupItem>();
                foreach (var dir in dirs)
                {
                    var metaPath = Path.Combine(dir, "caly_meta.json");
                    if (File.Exists(metaPath))
                    {
                        try
                        {
                            var json = JObject.Parse(File.ReadAllText(metaPath));
                            var timestampStr = json["timestamp"]?.ToString();
                            DateTime.TryParseExact(timestampStr, "yyyy-MM-dd_HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var timestamp);

                            var item = new BackupItem
                            {
                                FolderPath = dir,
                                FolderName = Path.GetFileName(dir),
                                AppId = json["appid"]?.Value<int>() ?? 0,
                                GameName = json["game_name"]?.ToString(),
                                Nickname = json["nickname"]?.ToString(),
                                Timestamp = timestamp,
                                CustomCoverUrl = json["custom_cover"]?.ToString() ?? string.Empty
                            };
                            item.PropertyChanged += OnBackupItemPropertyChanged;
                            tempItems.Add(item);
                        }
                        catch { }
                    }
                }
                foreach (var item in tempItems.OrderByDescending(x => x.Timestamp))
                {
                    Backups.Add(item);
                }
                ApplySearchFilter();
            }
            finally
            {
                IsLoading = false;
                _isLoadingInternal = false;
            }
        }

        [ObservableProperty]
        private bool _isRestoreModalOpen;

        [ObservableProperty]
        private bool _isConfirmState;

        [ObservableProperty]
        private bool _isProgressState;

        [ObservableProperty]
        private bool _isSuccessState;

        [ObservableProperty]
        private BackupItem? _selectedRestoreItem;

        [RelayCommand]
        private void RestoreBackup(BackupItem item)
        {
            if (item == null) return;
            SelectedRestoreItem = item;
            
            IsConfirmState = true;
            IsProgressState = false;
            IsSuccessState = false;
            IsRestoreModalOpen = true;
        }

        [RelayCommand]
        private async Task ConfirmRestore()
        {
            if (SelectedRestoreItem == null) return;

            IsConfirmState = false;
            IsProgressState = true;

            try
            {
                await _backupManager.RestoreBackupAsync(SelectedRestoreItem.FolderPath);
                
                await Task.Delay(1500);

                IsProgressState = false;
                IsSuccessState = true;
            }
            catch (Exception)
            {
                IsRestoreModalOpen = false;
            }
        }

        [RelayCommand]
        private void CancelRestore()
        {
            IsRestoreModalOpen = false;
            SelectedRestoreItem = null;
        }

        [ObservableProperty]
        private bool _isDeleteModalOpen;

        [ObservableProperty]
        private BackupItem? _selectedDeleteItem;

        [RelayCommand]
        private void DeleteBackup(BackupItem item)
        {
            if (item == null) return;
            SelectedDeleteItem = item;
            OnPropertyChanged(nameof(DeleteModalMessage));
            IsDeleteModalOpen = true;
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (!HasSelectedItems) return;
            SelectedDeleteItem = null;
            OnPropertyChanged(nameof(DeleteModalMessage));
            IsDeleteModalOpen = true;
        }

        [RelayCommand]
        private void ClearSelection()
        {
            foreach (var b in Backups)
            {
                b.IsSelected = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmDelete()
        {
            try
            {
                var itemsToDelete = SelectedDeleteItem != null 
                    ? new System.Collections.Generic.List<BackupItem> { SelectedDeleteItem } 
                    : Backups.Where(b => b.IsSelected).ToList();

                if (!itemsToDelete.Any()) return;

                await Task.Run(() => 
                {
                    foreach (var item in itemsToDelete)
                    {
                        var folder = item.FolderPath;
                        if (Directory.Exists(folder))
                        {
                            var di = new DirectoryInfo(folder);
                            foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                            {
                                file.Attributes &= ~FileAttributes.ReadOnly;
                            }
                            di.Delete(true);
                        }
                    }
                });
                
                foreach (var item in itemsToDelete)
                {
                    item.PropertyChanged -= OnBackupItemPropertyChanged;
                    Backups.Remove(item);
                }
                
                OnPropertyChanged(nameof(HasBackups));
                OnPropertyChanged(nameof(HasNoBackups));
                OnPropertyChanged(nameof(HasSelectedItems));
                _backupManager.NotifyBackupDeleted();
                
                var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                var message = itemsToDelete.Count > 1 ? $"{itemsToDelete.Count} backups apagados com sucesso!" : "Backup apagado com sucesso!";
                mainWindow?.ShowCustomToast("Sucesso", message, Wpf.Ui.Controls.SymbolRegular.Delete24);
            }
            catch (Exception ex)
            {
                try
                {
                    var mainWindow = App.GetService<CalyRecallNative.Views.MainWindow>();
                    mainWindow?.ShowCustomToast("Erro", "NÃ£o foi possÃ­vel apagar.", Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
                }
                catch { }
            }
            finally
            {
                IsDeleteModalOpen = false;
                SelectedDeleteItem = null;
            }
        }

        [RelayCommand]
        private void CancelDelete()
        {
            IsDeleteModalOpen = false;
            SelectedDeleteItem = null;
        }

        [ObservableProperty]
        private bool _isEditModalOpen;

        [ObservableProperty]
        private BackupItem _selectedEditItem;

        [ObservableProperty]
        private string _editDisplayName;

        [ObservableProperty]
        private string _editFolderName;

        [ObservableProperty]
        private string _editCustomCoverUrl;

        [ObservableProperty]
        private bool _isSearchingCover;

        [RelayCommand]
        public async Task AutoSearchCover()
        {
            if (string.IsNullOrWhiteSpace(EditDisplayName)) return;
            IsSearchingCover = true;
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(EditDisplayName)}&l=english&cc=US";
                var response = await client.GetStringAsync(url);
                var json = Newtonsoft.Json.Linq.JObject.Parse(response);
                var items = json["items"] as Newtonsoft.Json.Linq.JArray;
                
                if (items != null && items.Count > 0)
                {
                    var appId = items[0]["id"]?.ToString();
                    if (!string.IsNullOrEmpty(appId))
                    {
                        EditCustomCoverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg";
                    }
                }
            }
            catch { }
            finally
            {
                IsSearchingCover = false;
            }
        }

        [RelayCommand]
        public void OpenEditModal(BackupItem item)
        {
            SelectedEditItem = item;
            EditDisplayName = item.DisplayName;
            EditFolderName = item.FolderName;
            EditCustomCoverUrl = item.CustomCoverUrl;
            IsEditModalOpen = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditModalOpen = false;
            SelectedEditItem = null;
        }

        [RelayCommand]
        public async Task ConfirmEdit()
        {
            if (SelectedEditItem == null) return;

            try
            {
                var oldPath = SelectedEditItem.FolderPath;
                var parentDir = Path.GetDirectoryName(oldPath);
                var newPath = Path.Combine(parentDir, EditFolderName);

                if (oldPath != newPath)
                {
                    if (Directory.Exists(newPath))
                    {
                        var trayService = App.GetService<TrayIconService>();
                        trayService?.ShowNotification("Erro", "JÃ¡ existe uma pasta com este nome.");
                        return;
                    }
                    Directory.Move(oldPath, newPath);
                }

                var metaPath = Path.Combine(newPath, "caly_meta.json");
                if (File.Exists(metaPath))
                {
                    var json = JObject.Parse(File.ReadAllText(metaPath));
                    json["game_name"] = EditDisplayName;
                    json["custom_cover"] = EditCustomCoverUrl;
                    File.WriteAllText(metaPath, json.ToString());
                }

                IsEditModalOpen = false;
                SelectedEditItem = null;

                _backupManager.NotifyBackupsChanged();
            }
            catch (Exception ex)
            {
                var trayService = App.GetService<TrayIconService>();
                trayService?.ShowNotification("Erro ao Editar", "A pasta pode estar aberta ou em uso.");
                try
                {
                    File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_FatalCrash.txt"), "EditError: " + ex.ToString() + "\n");
                }
                catch { }
            }
        }

        [ObservableProperty]
        private bool _isZipProgressModalOpen;

        [ObservableProperty]
        private string _zipProgressTitle;

        [ObservableProperty]
        private int _zipProgressValue;

        private CancellationTokenSource _zipCts;

        [RelayCommand]
        private void CancelZipProgress()
        {
            _zipCts?.Cancel();
        }

        [RelayCommand]
        public async Task ExportAllBackups()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP Archive (*.zip)|*.zip",
                FileName = $"CalyRecall_Backups_{DateTime.Now:yyyy-MM-dd}.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                IsZipProgressModalOpen = true;
                ZipProgressTitle = (string)System.Windows.Application.Current.TryFindResource("Zip_ProgressTitle_Export") ?? "Exportando...";
                ZipProgressValue = 0;
                _zipCts = new CancellationTokenSource();

                try
                {
                    var progress = new Progress<int>(value => ZipProgressValue = value);
                    await _backupManager.ExportAllBackupsAsync(dialog.FileName, progress, _zipCts.Token);
                    
                    IsZipProgressModalOpen = false;
                    var successMsg = (string)System.Windows.Application.Current.TryFindResource("Zip_ExportSuccess") ?? "Sucesso";
                    App.GetService<TrayIconService>()?.ShowNotification("ExportaÃ§Ã£o", successMsg);
                }
                catch (OperationCanceledException)
                {
                    IsZipProgressModalOpen = false;
                }
                catch (Exception ex)
                {
                    IsZipProgressModalOpen = false;
                    App.GetService<TrayIconService>()?.ShowNotification("Erro", "Falha ao exportar.");
                    try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_FatalCrash.txt"), "ExportError: " + ex.ToString() + "\n"); } catch { }
                }
            }
        }

        [RelayCommand]
        public async Task ImportBackup()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP Archive (*.zip)|*.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                IsZipProgressModalOpen = true;
                ZipProgressTitle = (string)System.Windows.Application.Current.TryFindResource("Zip_ProgressTitle_Import") ?? "Importando...";
                ZipProgressValue = 0;
                _zipCts = new CancellationTokenSource();

                try
                {
                    var progress = new Progress<int>(value => ZipProgressValue = value);
                    await _backupManager.ImportBackupAsync(dialog.FileName, progress, _zipCts.Token);
                    
                    IsZipProgressModalOpen = false;
                    var successMsg = (string)System.Windows.Application.Current.TryFindResource("Zip_ImportSuccess") ?? "Sucesso";
                    App.GetService<TrayIconService>()?.ShowNotification("ImportaÃ§Ã£o", successMsg);
                }
                catch (OperationCanceledException)
                {
                    IsZipProgressModalOpen = false;
                }
                catch (Exception ex)
                {
                    IsZipProgressModalOpen = false;
                    App.GetService<TrayIconService>()?.ShowNotification("Erro", "Falha ao importar.");
                    try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_FatalCrash.txt"), "ImportError: " + ex.ToString() + "\n"); } catch { }
                }
            }
        }
    }
}
