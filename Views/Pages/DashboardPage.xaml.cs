using System.Windows.Controls;
using CalyRecallNative.ViewModels;
using Wpf.Ui.Controls;

namespace CalyRecallNative.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
