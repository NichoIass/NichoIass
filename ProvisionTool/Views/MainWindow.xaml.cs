using System.Windows;
using ProvisionTool.ViewModels;

namespace ProvisionTool.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
