using System;
using System.IO;
using Newtonsoft.Json;
using CalyRecallNative.Models;

namespace CalyRecallNative.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _configPath;
        public AppConfig Config { get; private set; }

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "CalyRecall");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _configPath = Path.Combine(appFolder, "config.json");
            Load();
        }

        public void Load()
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                Config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                Config = new AppConfig();
                Save();
            }
        }

        public event EventHandler SettingsChanged;

        public void Save()
        {
            var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
