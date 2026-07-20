using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ProvisionTool.Models
{
    /// <summary>
    /// Модель устройства для прошивки
    /// </summary>
    public partial class DeploymentDevice : ObservableObject
    {
        [ObservableProperty]
        private int index;

        [ObservableProperty]
        private string sshIp = string.Empty;

        [ObservableProperty]
        private string targetIp = string.Empty;

        [ObservableProperty]
        private string hostname = string.Empty;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string sudoPassword = string.Empty;

        [ObservableProperty]
        private bool useSudo = true;

        [ObservableProperty]
        private DeviceStatus status = DeviceStatus.Ready;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private DeviceStatusColor statusColor = DeviceStatusColor.Idle;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private bool isDeploying;

        [ObservableProperty]
        private double deployProgress;

        [ObservableProperty]
        private string deployDuration = string.Empty;

        [ObservableProperty]
        private DateTime? startTime;

        [ObservableProperty]
        private ObservableCollection<string> logs = new();

        public bool HasData => !string.IsNullOrWhiteSpace(SshIp);

        public string FullSshIp
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SshIp))
                    return string.Empty;

                // Если уже полный IP (содержит точки)
                if (SshIp.Contains("."))
                    return SshIp;

                // Иначе добавляем префикс
                return $"10.90.27.{SshIp}";
            }
        }
    }

    public enum DeviceStatus
    {
        Ready,
        Queued,
        Connecting,
        Executing,
        Rebooting,
        Verifying,
        Success,
        Error,
        Stopped
    }

    public enum DeviceStatusColor
    {
        Idle,
        Warning,
        Info,
        Success,
        Danger
    }
}
