using System;
using System.IO;

namespace CalyRecallNative.Models
{
    public class AppConfig
    {
        public string BackupFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CalyRecall_Backups");
        public string Mode { get; set; } = "Auto";
        public string QuickSaveHotkey { get; set; } = "Ctrl+Shift+S";
        public string Language { get; set; } = "pt-BR";
    }
}
