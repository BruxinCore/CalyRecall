using System;
using System.Windows;
using CalyRecallNative.ViewModels;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace CalyRecallNative.Views
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider, Wpf.Ui.ISnackbarService snackbarService)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            snackbarService.SetSnackbarPresenter(RootSnackbar);

            SystemThemeWatcher.Watch(this);
            
            RootNavigation.SetServiceProvider(serviceProvider);
            RootNavigation.Navigated += (s, e) => 
            {
                var restoreVm = serviceProvider.GetService(typeof(RestoreViewModel)) as RestoreViewModel;
                if (restoreVm != null && restoreVm.ClearSelectionCommand.CanExecute(null))
                    restoreVm.ClearSelectionCommand.Execute(null);

                var cloudVm = serviceProvider.GetService(typeof(CloudViewModel)) as CloudViewModel;
                if (cloudVm != null && cloudVm.ClearSelectionCommand.CanExecute(null))
                    cloudVm.ClearSelectionCommand.Execute(null);
            };
            Loaded += (s, e) => RootNavigation.Navigate(typeof(Pages.DashboardPage));
        }

        public void ShowCustomToast(string title, string message, Wpf.Ui.Controls.SymbolRegular symbol)
        {
            CustomToastTitle.Text = title;
            CustomToastDesc.Text = message;
            CustomToastIcon.Symbol = symbol;
            
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromSeconds(0.3));
            var transformAnim = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromSeconds(0.4)) 
            { 
                EasingFunction = new System.Windows.Media.Animation.CircleEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } 
            };
            
            CustomToastBorder.BeginAnimation(OpacityProperty, opacityAnim);
            CustomToastTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, transformAnim);
            
            System.Threading.Tasks.Task.Delay(4000).ContinueWith(_ => Dispatcher.Invoke(() => {
                var opacityOut = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
                var transformOut = new System.Windows.Media.Animation.DoubleAnimation(20, TimeSpan.FromSeconds(0.4)) 
                { 
                    EasingFunction = new System.Windows.Media.Animation.CircleEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn } 
                };
                CustomToastBorder.BeginAnimation(OpacityProperty, opacityOut);
                CustomToastTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, transformOut);
            }));
        }
    }
}
