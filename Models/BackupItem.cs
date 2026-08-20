using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CalyRecallNative.Models
{
    public partial class BackupItem : ObservableObject
    {
        public string FolderPath { get; set; }
        public string FolderName { get; set; }
        public int AppId { get; set; }
        public string GameName { get; set; }
        public string Nickname { get; set; }
        public DateTime Timestamp { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeaderImageUrl))]
        private string _customCoverUrl = string.Empty;

        public string HeaderImageUrl => !string.IsNullOrEmpty(CustomCoverUrl) ? CustomCoverUrl : (AppId > 0 ? $"https://steamcdn-a.akamaihd.net/steam/apps/{AppId}/header.jpg" : "pack://application:,,,/Assets/default_cover.png");
        public string DisplayName => string.IsNullOrEmpty(Nickname) ? GameName : Nickname;
        public string FormattedDate => Timestamp.ToString("dd/MM/yyyy • HH:mm:ss");
    }
}
