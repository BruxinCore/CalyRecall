using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace CalyRecallNative.Services
{
    public class SteamMonitorService : BackgroundService
    {
        private readonly SteamService _steamService;
        private readonly BackupManager _backupManager;
        private readonly ISettingsService _settingsService;

        private int _lastAppId = 0;
        private bool _wasRunning = false;

        public SteamMonitorService(SteamService steamService, BackupManager backupManager, ISettingsService settingsService)
        {
            _steamService = steamService;
            _backupManager = backupManager;
            _settingsService = settingsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var currentAppId = _steamService.GetRunningAppId();

                if (_wasRunning && currentAppId == 0)
                {
                    await Task.Delay(5000, stoppingToken);

                    if (_settingsService.Config.Mode == "Auto")
                    {
                        await _backupManager.DoBackupAsync(_lastAppId);
                    }
                    else if (_settingsService.Config.Mode == "SemiAuto")
                    {
                        _backupManager.RequestSemiAutoBackup(_lastAppId);
                    }

                    _wasRunning = false;
                }
                else if (currentAppId > 0)
                {
                    _wasRunning = true;
                    _lastAppId = currentAppId;
                }

                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
