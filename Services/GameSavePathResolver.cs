using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using YamlDotNet.Serialization;

namespace CalyRecallNative.Services
{
    public class GameManifestData
    {
        [YamlMember(Alias = "files")]
        public Dictionary<string, object> Files { get; set; }

        [YamlMember(Alias = "steam")]
        public SteamData Steam { get; set; }
    }

    public class SteamData
    {
        [YamlMember(Alias = "id")]
        public int? Id { get; set; }
    }

    public class GameSavePathResolver
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/mtkennerly/ludusavi-manifest/master/data/manifest.yaml";
        private readonly string _manifestPath;
        private Dictionary<string, GameManifestData> _manifestCache;

        public GameSavePathResolver()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var recallDir = Path.Combine(appData, "CalyRecall");
            Directory.CreateDirectory(recallDir);
            _manifestPath = Path.Combine(recallDir, "manifest.yaml");
        }

        public async Task InitializeAsync()
        {
            if (!File.Exists(_manifestPath) || (DateTime.Now - File.GetLastWriteTime(_manifestPath)).TotalDays > 7)
            {
                try
                {
                    using var client = new HttpClient();
                    var content = await client.GetStringAsync(ManifestUrl);
                    File.WriteAllText(_manifestPath, content);
                }
                catch
                {
                }
            }

            if (File.Exists(_manifestPath))
            {
                try
                {
                    var yaml = File.ReadAllText(_manifestPath);
                    var deserializer = new DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .Build();
                    _manifestCache = deserializer.Deserialize<Dictionary<string, GameManifestData>>(yaml);
                }
                catch
                {
                    _manifestCache = new Dictionary<string, GameManifestData>();
                }
            }
            else
            {
                _manifestCache = new Dictionary<string, GameManifestData>();
            }
        }

        public async Task<bool?> UpdateManifestAsync()
        {
            try
            {
                using var client = new HttpClient();
                var content = await client.GetStringAsync(ManifestUrl);

                if (File.Exists(_manifestPath))
                {
                    var current = File.ReadAllText(_manifestPath);
                    if (current == content)
                    {
                        return false;
                    }
                }

                File.WriteAllText(_manifestPath, content);
                
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                _manifestCache = deserializer.Deserialize<Dictionary<string, GameManifestData>>(content);
                
                return true;
            }
            catch
            {
                return null;
            }
        }

        public List<string> GetSavePathsForGame(string gameName, int appId)
        {
            var results = new List<string>();
            if (_manifestCache == null || _manifestCache.Count == 0) return results;

            var match = _manifestCache.FirstOrDefault(x => (x.Value.Steam?.Id == appId && appId > 0) || x.Key.Equals(gameName, StringComparison.OrdinalIgnoreCase));

            if (match.Value?.Files != null)
            {
                foreach (var pathKey in match.Value.Files.Keys)
                {
                    var resolved = ExpandPath(pathKey);
                    if (!string.IsNullOrEmpty(resolved))
                    {
                        results.Add(resolved);
                    }
                }
            }

            return results.Distinct().ToList();
        }

        private string ExpandPath(string path)
        {
            var p = path;

            p = p.Replace("<home>", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            p = p.Replace("<winAppData>", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            p = p.Replace("<winLocalAppData>", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            p = p.Replace("<winDocuments>", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            p = p.Replace("<winPublic>", Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
            p = p.Replace("<winProgramData>", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
            p = p.Replace("<winDir>", Environment.GetEnvironmentVariable("windir") ?? "C:\\Windows");
            p = p.Replace("<osUserName>", Environment.UserName);

            if (p.Contains("<user-id>"))
            {
                p = p.Substring(0, p.IndexOf("<user-id>"));
            }
            if (p.Contains("<storeUserId>"))
            {
                p = p.Substring(0, p.IndexOf("<storeUserId>"));
            }

            p = p.Replace("/", "\\");
            p = p.TrimEnd('\\');

            return p;
        }
    }
}
