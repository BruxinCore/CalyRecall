using System.Windows.Controls;
using CalyRecallNative.ViewModels;
using Wpf.Ui.Controls;

namespace CalyRecallNative.Views.Pages
{
    public partial class RestorePage : Page
    {
        public RestoreViewModel ViewModel { get; }

        public RestorePage(RestoreViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

    }
}
