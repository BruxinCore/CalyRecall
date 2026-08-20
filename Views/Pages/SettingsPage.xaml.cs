using System.Windows.Controls;
using CalyRecallNative.ViewModels;
using Wpf.Ui.Controls;

namespace CalyRecallNative.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }
        private readonly CalyRecallNative.Services.ISettingsService _settingsService;

        public SettingsPage(SettingsViewModel viewModel, CalyRecallNative.Services.ISettingsService settingsService)
        {
            ViewModel = viewModel;
            _settingsService = settingsService;
            DataContext = this;
            InitializeComponent();
        }

        private void OnHotkeyTextBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

            if (key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
            {
                return;
            }

            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            string shortcutText = "";

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) shortcutText += "Ctrl+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) shortcutText += "Alt+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) shortcutText += "Shift+";

            shortcutText += key.ToString();

            HotkeyTextBox.Text = shortcutText;
            ViewModel.QuickSaveHotkey = shortcutText;
            _settingsService.Config.QuickSaveHotkey = shortcutText;
            _settingsService.Save();

            RootGrid.Focus();
        }

        private void OnPagePreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!HotkeyTextBox.IsFocused) return;

            if (e.ChangedButton == System.Windows.Input.MouseButton.Left || e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                return;
            }

            e.Handled = true;

            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            string shortcutText = "";

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) shortcutText += "Ctrl+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) shortcutText += "Alt+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) shortcutText += "Shift+";

            shortcutText += e.ChangedButton.ToString();
            
            HotkeyTextBox.Text = shortcutText;
            ViewModel.QuickSaveHotkey = shortcutText;
            _settingsService.Config.QuickSaveHotkey = shortcutText;
            _settingsService.Save();

            RootGrid.Focus();
        }
    }
}
