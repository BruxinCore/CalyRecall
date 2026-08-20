using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CalyRecallNative.Services
{
    public class CloudDriveService
    {
        private static string[] Scopes = { DriveService.Scope.DriveFile };
        private static string ApplicationName = "CalyRecall";
        private DriveService? _driveService;
        private string _userEmail = string.Empty;
        private string _userName = string.Empty;
        private string _userPhotoUrl = string.Empty;

        public bool IsAuthenticated => _driveService != null;
        public string UserEmail => _userEmail;
        public string UserName => _userName;
        public string UserPhotoUrl => _userPhotoUrl;

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                UserCredential credential;

                using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
                {
                    string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CalyRecall", "GoogleAuth");

                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "user, storageQuota";
                var aboutResponse = await aboutRequest.ExecuteAsync();

                _userEmail = aboutResponse.User.EmailAddress;
                _userName = aboutResponse.User.DisplayName;
                _userPhotoUrl = aboutResponse.User.PhotoLink;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error authenticating with Google Drive: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _driveService = null;
            _userEmail = "NÃ£o conectado";
            _userName = string.Empty;
            _userPhotoUrl = string.Empty;
            string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CalyRecall", "GoogleAuth");
            if (System.IO.Directory.Exists(credPath))
            {
                try { System.IO.Directory.Delete(credPath, true); } catch { }
            }
        }

        public async Task<(long Usage, long Limit)> GetStorageQuotaAsync()
        {
            if (_driveService == null) return (0, 0);

            try
            {
                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "storageQuota";
                var aboutResponse = await aboutRequest.ExecuteAsync();

                return (aboutResponse.StorageQuota.Usage ?? 0, aboutResponse.StorageQuota.Limit ?? 0);
            }
            catch
            {
                return (0, 0);
            }
        }

        private async Task<string> GetOrCreateBackupFolderAsync()
        {
            if (_driveService == null) throw new InvalidOperationException("Not authenticated");

            var listRequest = _driveService.Files.List();
            listRequest.Q = "mimeType='application/vnd.google-apps.folder' and name='CalyRecall Backups' and trashed=false";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";
            var result = await listRequest.ExecuteAsync();

            if (result.Files != null && result.Files.Count > 0)
            {
                return result.Files.First().Id;
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = "CalyRecall Backups",
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = _driveService.Files.Create(fileMetadata);
            createRequest.Fields = "id";
            var folder = await createRequest.ExecuteAsync();

            return folder.Id;
        }

        public async Task<bool> UploadBackupAsync(string filePath, Action<long>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (_driveService == null) return false;

            try
            {
                string folderId = await GetOrCreateBackupFolderAsync();

                var listRequest = _driveService.Files.List();
                listRequest.Q = $"'{folderId}' in parents and name='{Path.GetFileName(filePath)}' and trashed=false";
                var searchResult = await listRequest.ExecuteAsync(cancellationToken);
                if (searchResult.Files != null && searchResult.Files.Count > 0)
                {
                    await _driveService.Files.Delete(searchResult.Files.First().Id).ExecuteAsync(cancellationToken);
                }

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new List<string> { folderId }
                };

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    request = _driveService.Files.Create(fileMetadata, stream, "application/zip");
                    request.Fields = "id";

                    if (progressCallback != null)
                    {
                        request.ProgressChanged += (progress) =>
                        {
                            progressCallback(progress.BytesSent);
                        };
                    }

                    var response = await request.UploadAsync(cancellationToken);
                    return response.Status == Google.Apis.Upload.UploadStatus.Completed;
                }
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
                return false;
            }
        }
    }
}
