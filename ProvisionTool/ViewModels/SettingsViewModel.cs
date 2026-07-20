using CommunityToolkit.Mvvm.ComponentModel;
using ProvisionTool.Models;

namespace ProvisionTool.ViewModels
{
    /// <summary>
    /// ViewModel для Settings
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private DeploymentSettings settings;

        public SettingsViewModel(DeploymentSettings settings)
        {
            Settings = settings;
        }
    }
}
