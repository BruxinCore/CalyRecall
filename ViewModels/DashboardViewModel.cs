using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CalyRecallNative.Models;
using CalyRecallNative.Services;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CalyRecallNative.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly BackupManager _backupManager;

        [ObservableProperty]
        private string _totalBackups = "0";

        [ObservableProperty]
        private string _totalSize = "0 MB";

        [ObservableProperty]
        private string _freeSpace = "0 GB";

        [ObservableProperty]
        private double _usedSpacePercentage = 0;

        [ObservableProperty]
        private string _totalDriveSize = "0 GB";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRecentBackups))]
        [NotifyPropertyChangedFor(nameof(HasNoBackups))]
        private ObservableCollection<BackupItem> _recentBackups = new();

        public bool HasRecentBackups => RecentBackups.Count > 0;
        public bool HasNoBackups => RecentBackups.Count == 0;

        public DashboardViewModel(ISettingsService settingsService, BackupManager backupManager)
        {
            _settingsService = settingsService;
            _backupManager = backupManager;
            
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            _backupManager.BackupCompleted += (s, e) => 
            {
                dispatcher.BeginInvoke(() => UpdateStats());
            };

            _backupManager.BackupDeleted += (s, e) => 
            {
                dispatcher.BeginInvoke(() => UpdateStats());
            };

            UpdateStats();
        }

        [RelayCommand]
        private void NavigateToRestore()
        {
            if (System.Windows.Application.Current.MainWindow is CalyRecallNative.Views.MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(CalyRecallNative.Views.Pages.RestorePage));
            }
        }

        public void UpdateStats()
        {
            var folder = _settingsService.Config.BackupFolder;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var dirs = System.IO.Directory.GetDirectories(folder);
            TotalBackups = dirs.Length.ToString();

            long sizeBytes = 0;
            foreach (var dir in dirs)
            {
                sizeBytes += GetDirectorySize(new DirectoryInfo(dir));
            }
            TotalSize = $"{(sizeBytes / 1024 / 1024.0):F2} MB";

            var drive = new DriveInfo(Path.GetPathRoot(folder));
            double freeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            double totalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            FreeSpace = $"{freeGb:F2} GB";
            TotalDriveSize = $"{totalGb:F0} GB";
            UsedSpacePercentage = ((totalGb - freeGb) / totalGb) * 100.0;

            RecentBackups.Clear();
            var sortedDirs = System.Linq.Enumerable.OrderByDescending(dirs, d => d);
            foreach (var dir in sortedDirs)
            {
                var metaPath = Path.Combine(dir, "caly_meta.json");
                if (File.Exists(metaPath))
                {
                    try
                    {
                        var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(metaPath));
                        System.DateTime.TryParseExact(json["timestamp"]?.ToString(), "yyyy-MM-dd_HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var timestamp);
                        RecentBackups.Add(new CalyRecallNative.Models.BackupItem
                        {
                            FolderPath = dir,
                            FolderName = Path.GetFileName(dir),
                            AppId = json["appid"]?.Value<int>() ?? 0,
                            GameName = json["game_name"]?.ToString(),
                            Nickname = json["nickname"]?.ToString(),
                            Timestamp = timestamp,
                            CustomCoverUrl = json["custom_cover"]?.ToString() ?? string.Empty
                        });
                    }
                    catch { }
                }
            }
            OnPropertyChanged(nameof(HasRecentBackups));
            OnPropertyChanged(nameof(HasNoBackups));
        }

        private long GetDirectorySize(DirectoryInfo d)
        {
            long size = 0;
            var fis = d.GetFiles();
            foreach (FileInfo fi in fis) size += fi.Length;
            var dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis) size += GetDirectorySize(di);
            return size;
        }
    }
}
