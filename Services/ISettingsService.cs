using CalyRecallNative.Models;

namespace CalyRecallNative.Services
{
    public interface ISettingsService
    {
        AppConfig Config { get; }
        event System.EventHandler SettingsChanged;
        void Save();
        void Load();
    }
}
