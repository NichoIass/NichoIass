using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProvisionTool.Models;
using ProvisionTool.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace ProvisionTool.ViewModels
{
    /// <summary>
    /// ViewModel для LogViewer
    /// </summary>
    public partial class LogViewerViewModel : ObservableObject
    {
        [ObservableProperty]
        private DeploymentDevice? currentDevice;

        [ObservableProperty]
        private string logContent = string.Empty;

        public LogViewerViewModel(DeploymentDevice device)
        {
            CurrentDevice = device;
            UpdateLogContent();
        }

        private void UpdateLogContent()
        {
            if (CurrentDevice != null)
            {
                LogContent = string.Join("\n", CurrentDevice.Logs);
            }
        }
    }
}
