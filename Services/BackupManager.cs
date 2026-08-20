using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CalyRecallNative.Services
{
    public class BackupManager
    {
        private readonly ISettingsService _settingsService;
        private readonly SteamService _steamService;
        private static readonly HttpClient _httpClient = new HttpClient();

        public event EventHandler BackupCompleted;
        public event EventHandler BackupDeleted;
        public event EventHandler<int> SemiAutoBackupRequested;

        public void NotifyBackupDeleted()
        {
            BackupDeleted?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyBackupsChanged()
        {
            BackupCompleted?.Invoke(this, EventArgs.Empty);
        }

        public BackupManager(ISettingsService settingsService, SteamService steamService)
        {
            _settingsService = settingsService;
            _steamService = steamService;
        }

        public async Task<string> GetGameNameAsync(int appId)
        {
            if (appId <= 0) return "Steam Session";
            try
            {
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=basic";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);
                
                if (data[appId.ToString()]?["success"]?.Value<bool>() == true)
                {
                    return data[appId.ToString()]["data"]["name"].ToString();
                }
            }
            catch { }
            return $"AppID {appId}";
        }

        public async void RequestSemiAutoBackup(int appId)
        {
            var gameName = await GetGameNameAsync(appId);
            var trayService = App.GetService<TrayIconService>();
            trayService?.ShowNotification("AÃ§Ã£o NecessÃ¡ria", $"VocÃª deseja salvar o backup de {gameName}?");
            
            SemiAutoBackupRequested?.Invoke(this, appId);
        }

        public async Task DoBackupAsync(int appId, string gameName = null)
        {
            try
            {
                gameName ??= await GetGameNameAsync(appId);
                var sanitizedGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var folderName = $"{sanitizedGameName}-{timestamp}";
                var destFolder = Path.Combine(_settingsService.Config.BackupFolder, folderName);

                Directory.CreateDirectory(destFolder);
                
                var trayService = App.GetService<TrayIconService>();
                trayService?.ShowNotification("Salvando Backup", $"Registrando progresso de {gameName}...");

            var resolver = App.GetService<GameSavePathResolver>();
            var customPaths = resolver.GetSavePathsForGame(gameName, appId);

            var metaJson = new Newtonsoft.Json.Linq.JObject
            {
                ["appid"] = appId,
                ["game_name"] = gameName,
                ["timestamp"] = timestamp
            };
            
            if (appId == 0 && !string.IsNullOrWhiteSpace(gameName))
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(gameName)}&l=english&cc=US";
                    var response = await client.GetStringAsync(url);
                    var json = Newtonsoft.Json.Linq.JObject.Parse(response);
                    var items = json["items"] as Newtonsoft.Json.Linq.JArray;
                    
                    if (items != null && items.Count > 0)
                    {
                        var foundAppId = items[0]["id"]?.ToString();
                        if (!string.IsNullOrEmpty(foundAppId))
                        {
                            metaJson["custom_cover"] = $"https://steamcdn-a.akamaihd.net/steam/apps/{foundAppId}/header.jpg";
                        }
                    }
                }
                catch { }
            }
            
            var customPathsJson = new JObject();

            if (customPaths.Any())
            {
                int idx = 0;
                foreach (var srcPath in customPaths)
                {
                    if (Directory.Exists(srcPath))
                    {
                        var zipName = $"custom_{idx}.zip";
                        var zipPath = Path.Combine(destFolder, zipName);
                        ZipFile.CreateFromDirectory(srcPath, zipPath, CompressionLevel.Fastest, false);
                        customPathsJson[zipName] = srcPath;
                        idx++;
                    }
                    else if (File.Exists(srcPath))
                    {
                        var destFile = Path.Combine(destFolder, Path.GetFileName(srcPath));
                        File.Copy(srcPath, destFile, true);
                        customPathsJson[Path.GetFileName(srcPath)] = srcPath;
                    }
                }
                metaJson["custom_paths"] = customPathsJson;
            }
            else
            {
                var steamPath = _steamService.GetSteamPath();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    var targets = new[]
                    {
                        new { Src = Path.Combine(steamPath, "userdata"), Name = "userdata" },
                        new { Src = Path.Combine(steamPath, "appcache", "stats"), Name = "appcache_stats" },
                        new { Src = Path.Combine(steamPath, "depotcache"), Name = "depotcache" },
                        new { Src = Path.Combine(steamPath, "config", "stplug-in"), Name = "stplug-in" }
                    };

                    foreach (var target in targets)
                    {
                        if (Directory.Exists(target.Src))
                        {
                            var zipPath = Path.Combine(destFolder, $"{target.Name}.zip");
                            ZipFile.CreateFromDirectory(target.Src, zipPath, CompressionLevel.Fastest, false);
                        }
                        else if (File.Exists(target.Src))
                        {
                            var destFile = Path.Combine(destFolder, Path.GetFileName(target.Src));
                            File.Copy(target.Src, destFile, true);
                        }
                    }
                }
            }

            var metaPath = Path.Combine(destFolder, "caly_meta.json");
            File.WriteAllText(metaPath, metaJson.ToString());

            trayService?.ShowNotification("Backup Finalizado", $"{gameName} salvo com sucesso!");
            BackupCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                try
                {
                    var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    File.AppendAllText(Path.Combine(docFolder, "CalyRecall_FatalCrash.txt"), ex.ToString() + "\n");
                }
                catch { }
            }
        }
        public async Task RestoreBackupAsync(string folderPath)
        {
            await Task.Run(() =>
            {
                var metaPath = Path.Combine(folderPath, "caly_meta.json");
                JObject meta = null;
                if (File.Exists(metaPath))
                {
                    meta = JObject.Parse(File.ReadAllText(metaPath));
                }

                if (meta != null && meta["custom_paths"] != null)
                {
                    var customPaths = meta["custom_paths"] as JObject;
                    foreach (var prop in customPaths.Properties())
                    {
                        var itemName = prop.Name;
                        var originalPath = prop.Value.ToString();
                        
                        var srcItem = Path.Combine(folderPath, itemName);
                        if (File.Exists(srcItem))
                        {
                            if (itemName.EndsWith(".zip"))
                            {
                                Directory.CreateDirectory(originalPath);
                                ZipFile.ExtractToDirectory(srcItem, originalPath, overwriteFiles: true);
                            }
                            else
                            {
                                var dir = Path.GetDirectoryName(originalPath);
                                if (dir != null) Directory.CreateDirectory(dir);
                                File.Copy(srcItem, originalPath, overwrite: true);
                            }
                        }
                    }
                }
                else
                {
                    var steamPath = _steamService.GetSteamPath();
                    if (string.IsNullOrEmpty(steamPath)) throw new Exception("Steam nÃ£o encontrada.");

                    var targets = new[]
                    {
                        new { Src = Path.Combine(steamPath, "userdata"), Name = "userdata" },
                        new { Src = Path.Combine(steamPath, "appcache", "stats"), Name = "appcache_stats" },
                        new { Src = Path.Combine(steamPath, "depotcache"), Name = "depotcache" },
                        new { Src = Path.Combine(steamPath, "config", "stplug-in"), Name = "stplug-in" }
                    };

                    foreach (var target in targets)
                    {
                        var zipPath = Path.Combine(folderPath, $"{target.Name}.zip");
                        if (File.Exists(zipPath))
                        {
                            Directory.CreateDirectory(target.Src);
                            ZipFile.ExtractToDirectory(zipPath, target.Src, overwriteFiles: true);
                        }
                        else
                        {
                            var srcFile = Path.Combine(folderPath, Path.GetFileName(target.Src));
                            if (File.Exists(srcFile))
                            {
                                var targetDir = Path.GetDirectoryName(target.Src);
                                if (targetDir != null) Directory.CreateDirectory(targetDir);
                                File.Copy(srcFile, target.Src, overwrite: true);
                            }
                        }
                    }
                }
            });
        }

        public async Task ExportAllBackupsAsync(string destinationZipPath, IProgress<int> progress, CancellationToken ct)
        {
            try
            {
                await Task.Run(() =>
                {
                    var backupFolder = _settingsService.Config.BackupFolder;
                    if (!Directory.Exists(backupFolder)) return;

                    var files = Directory.GetFiles(backupFolder, "*", SearchOption.AllDirectories);
                    if (files.Length == 0) return;

                    using var stream = new FileStream(destinationZipPath, FileMode.Create);
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

                    for (int i = 0; i < files.Length; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var file = files[i];
                        var relativePath = Path.GetRelativePath(backupFolder, file);
                        archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                        
                        progress?.Report((int)((i + 1) * 100.0 / files.Length));
                    }
                }, ct);
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(destinationZipPath))
                {
                    try { File.Delete(destinationZipPath); } catch { }
                }
                throw;
            }
        }

        public async Task ImportBackupAsync(string zipPath, IProgress<int> progress, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                var backupFolder = _settingsService.Config.BackupFolder;
                Directory.CreateDirectory(backupFolder);

                using var archive = ZipFile.OpenRead(zipPath);
                var entries = archive.Entries;
                if (entries.Count == 0) return;

                for (int i = 0; i < entries.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var entry = entries[i];
                    var destinationPath = Path.GetFullPath(Path.Combine(backupFolder, entry.FullName));

                    if (!destinationPath.StartsWith(backupFolder, StringComparison.OrdinalIgnoreCase)) continue;

                    if (string.IsNullOrEmpty(entry.Name)) 
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else 
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                    
                    progress?.Report((int)((i + 1) * 100.0 / entries.Count));
                }
                
                BackupCompleted?.Invoke(this, EventArgs.Empty);
            }, ct);
        }
    }
}
