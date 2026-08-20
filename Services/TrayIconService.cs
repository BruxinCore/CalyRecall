using System;
using System.Drawing;
using System.Windows;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.Input;

namespace CalyRecallNative.Services
{
    public partial class TrayIconService : IDisposable
    {
        private TaskbarIcon _taskbarIcon;

        public void Initialize()
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "CalyRecall",
                Visibility = Visibility.Visible,
                ContextMenu = new System.Windows.Controls.ContextMenu()
            };

            var streamInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/CalyRecall-Icon.ico"));
            if (streamInfo != null)
            {
                _taskbarIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
            }

            var openItem = new System.Windows.Controls.MenuItem { Header = "Abrir CalyRecall" };
            openItem.Click += (s, e) => ShowMainWindow();

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Sair" };
            exitItem.Click += (s, e) => ExitApplication();

            _taskbarIcon.ContextMenu.Items.Add(openItem);
            _taskbarIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
            _taskbarIcon.ContextMenu.Items.Add(exitItem);

            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
        }

        [RelayCommand]
        private void ShowMainWindow()
        {
            System.Windows.Application.Current.MainWindow?.Show();
            if (System.Windows.Application.Current.MainWindow != null)
            {
                if (System.Windows.Application.Current.MainWindow.WindowState == WindowState.Minimized)
                    System.Windows.Application.Current.MainWindow.WindowState = WindowState.Normal;
                    
                System.Windows.Application.Current.MainWindow.Activate();
            }
        }

        [RelayCommand]
        private void ExitApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                var builder = new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                    .AddText(title)
                    .AddText(message);

                builder.Show();
            }
            catch { }
        }

        public void Dispose()
        {
            _taskbarIcon?.Dispose();
        }
    }
}
