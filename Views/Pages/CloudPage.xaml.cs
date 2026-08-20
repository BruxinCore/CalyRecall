using System.Windows.Controls;
using CalyRecallNative.ViewModels;

namespace CalyRecallNative.Views.Pages
{
    public partial class CloudPage : Page
    {
        public CloudViewModel ViewModel { get; }

        public CloudPage(CloudViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

    }
}
