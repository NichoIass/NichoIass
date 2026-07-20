using System.Windows;
using ProvisionTool.ViewModels;

namespace ProvisionTool.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel?.IsDeploying ?? false)
            {
                if (MessageBox.Show(
                    "Развёртывание ещё выполняется. Вы уверены, что хотите выйти?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _viewModel?.SaveSessionCommand.Execute(null);
        }
    }
}
