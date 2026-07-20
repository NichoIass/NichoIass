using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProvisionTool.Models;
using ProvisionTool.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace ProvisionTool.ViewModels
{
    /// <summary>
    /// Основная ViewModel для главного окна
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDeploymentService _deploymentService;
        private readonly IStorageService _storageService;
        private CancellationTokenSource? _deploymentCts;

        [ObservableProperty]
        private ObservableCollection<DeploymentDevice> devices = new();

        [ObservableProperty]
        private DeploymentSettings settings = new();

        [ObservableProperty]
        private int totalDevices;

        [ObservableProperty]
        private int successfulDevices;

        [ObservableProperty]
        private int failedDevices;

        [ObservableProperty]
        private int activeDeployments;

        [ObservableProperty]
        private bool isDeploying;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DeploymentResult> deploymentResults = new();

        public MainViewModel()
        {
            _deploymentService = new DeploymentService(new SshService());
            _storageService = new StorageService();

            InitializeDevices();
            LoadSessionAsync().ConfigureAwait(false);
        }

        private void InitializeDevices()
        {
            Devices.Clear();
            for (int i = 0; i < 50; i++)
            {
                Devices.Add(new DeploymentDevice { Index = i + 1 });
            }
        }

        private async Task LoadSessionAsync()
        {
            var session = await _storageService.LoadSessionAsync();
            if (session.HasValue)
            {
                var (devices, settings) = session.Value;
                Devices.Clear();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
                Settings = settings;
            }
        }

        [RelayCommand]
        public async Task StartAll()
        {
            var devicesToDeploy = Devices.Where(d => d.HasData).ToList();
            if (!devicesToDeploy.Any())
            {
                System.Windows.MessageBox.Show("Нет заполненных строк", "Ошибка");
                return;
            }

            if (System.Windows.MessageBox.Show(
                $"Запустить развёртывание на {devicesToDeploy.Count} устройствах?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
                return;

            await StartDeploymentAsync(devicesToDeploy);
        }

        [RelayCommand]
        public async Task StartSelected()
        {
            var devicesToDeploy = Devices.Where(d => d.IsSelected && d.HasData).ToList();
            if (!devicesToDeploy.Any())
            {
                System.Windows.MessageBox.Show("Нет выбранных строк", "Ошибка");
                return;
            }

            if (System.Windows.MessageBox.Show(
                $"Запустить развёртывание на {devicesToDeploy.Count} выбранных устройствах?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
                return;

            await StartDeploymentAsync(devicesToDeploy);
        }

        [RelayCommand]
        public void StopAll()
        {
            _deploymentCts?.Cancel();
        }

        [RelayCommand]
        public async Task AutofillSshIp()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.SshIp));
            if (firstDevice == null)
                return;

            var baseIp = firstDevice.SshIp.Trim();
            if (ExtractIpPattern(baseIp, out var prefix, out var startNumber, out var width))
            {
                int index = Devices.IndexOf(firstDevice);
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].SshIp = $"{prefix}{(startNumber + (i - index)).ToString().PadLeft(width, '0')}";
                }
            }
        }

        [RelayCommand]
        public async Task AutofillTargetIp()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.TargetIp));
            if (firstDevice == null)
                return;

            if (ExtractLastOctet(firstDevice.TargetIp, out var prefix, out var startNumber))
            {
                int index = Devices.IndexOf(firstDevice);
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].TargetIp = $"{prefix}{startNumber + (i - index)}";
                }
            }
        }

        [RelayCommand]
        public async Task AutofillHostname()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Hostname));
            if (firstDevice == null)
                return;

            var baseHostname = firstDevice.Hostname.Trim();
            if (ExtractIpPattern(baseHostname, out var prefix, out var startNumber, out var width))
            {
                int index = Devices.IndexOf(firstDevice);
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].Hostname = $"{prefix}{(startNumber + (i - index)).ToString().PadLeft(width, '0')}";
                }
            }
        }

        [RelayCommand]
        public async Task SaveSession()
        {
            await _storageService.SaveSessionAsync(Devices.ToList(), Settings);
            System.Windows.MessageBox.Show("Сессия сохранена", "Успех");
        }

        [RelayCommand]
        public async Task ExportReport()
        {
            if (!DeploymentResults.Any())
            {
                System.Windows.MessageBox.Show("Нет результатов для экспорта", "Ошибка");
                return;
            }

            await _storageService.SaveReportAsync(DeploymentResults.ToList());
            System.Windows.MessageBox.Show("Отчёт экспортирован", "Успех");
        }

        private async Task StartDeploymentAsync(List<DeploymentDevice> devicesToDeploy)
        {
            IsDeploying = true;
            TotalDevices = devicesToDeploy.Count;
            SuccessfulDevices = 0;
            FailedDevices = 0;
            ActiveDeployments = 0;
            DeploymentResults.Clear();

            _deploymentCts = new CancellationTokenSource();
            var semaphore = new SemaphoreSlim(Settings.MaxParallelConnections);

            var tasks = devicesToDeploy.Select(device => DeployDeviceAsync(device, semaphore));

            await Task.WhenAll(tasks);

            IsDeploying = false;
            System.Windows.MessageBox.Show(
                $"Все развёртывания завершены!\nУспех: {SuccessfulDevices}\nОшибки: {FailedDevices}",
                "Отчёт");
        }

        private async Task DeployDeviceAsync(DeploymentDevice device, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                ActiveDeployments++;
                device.IsDeploying = true;
                device.Status = DeviceStatus.Queued;
                device.StatusMessage = "Queued...";
                device.StatusColor = DeviceStatusColor.Warning;

                var result = await _deploymentService.DeployAsync(
                    device,
                    Settings,
                    msg => device.Logs.Add(msg),
                    _deploymentCts!.Token);

                device.IsDeploying = false;
                device.Status = result.IsSuccessful ? DeviceStatus.Success : DeviceStatus.Error;
                device.StatusMessage = result.IsSuccessful 
                    ? $"✅ Success ({result.Duration.TotalSeconds:F1}s)" 
                    : $"❌ Error: {result.ErrorMessage}";
                device.StatusColor = result.IsSuccessful ? DeviceStatusColor.Success : DeviceStatusColor.Danger;
                device.DeployDuration = $"{result.Duration.TotalSeconds:F1}s";

                DeploymentResults.Add(result);

                if (result.IsSuccessful)
                    SuccessfulDevices++;
                else
                    FailedDevices++;
            }
            catch (OperationCanceledException)
            {
                device.Status = DeviceStatus.Stopped;
                device.StatusMessage = "Stopped";
                device.StatusColor = DeviceStatusColor.Danger;
            }
            finally
            {
                ActiveDeployments--;
                semaphore.Release();
            }
        }

        private bool ExtractIpPattern(string value, out string prefix, out int number, out int width)
        {
            prefix = string.Empty;
            number = 0;
            width = 0;

            var match = System.Text.RegularExpressions.Regex.Match(value, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                prefix = match.Groups[1].Value;
                var numberStr = match.Groups[2].Value;
                width = numberStr.Length;
                return int.TryParse(numberStr, out number);
            }

            return false;
        }

        private bool ExtractLastOctet(string ipAddress, out string prefix, out int octet)
        {
            prefix = string.Empty;
            octet = 0;

            var parts = ipAddress.Split('.');
            if (parts.Length != 4)
                return false;

            prefix = string.Join(".", parts.Take(3)) + ".";
            return int.TryParse(parts[3], out octet);
        }
    }
}
