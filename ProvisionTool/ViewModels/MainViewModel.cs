using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProvisionTool.Models;
using ProvisionTool.Services;
using ProvisionTool.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace ProvisionTool.ViewModels
{
    /// <summary>
    /// Основная ViewModel для главного окна
    /// Управляет развёртыванием устройств, сохранением и загрузкой данных
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDeploymentService _deploymentService;
        private readonly IStorageService _storageService;
        private readonly ICsvService _csvService;
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

        [ObservableProperty]
        private ObservableCollection<DeploymentDevice> filteredDevices = new();

        public MainViewModel()
        {
            _deploymentService = ServiceFactory.CreateDeploymentService();
            _storageService = ServiceFactory.CreateStorageService();
            _csvService = ServiceFactory.CreateCsvService();

            InitializeDevices();
            LoadSessionAsync().ConfigureAwait(false);
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        private void InitializeDevices()
        {
            Devices.Clear();
            for (int i = 0; i < 50; i++)
            {
                Devices.Add(new DeploymentDevice { Index = i + 1 });
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchQuery?.ToLower() ?? string.Empty;
            var filtered = Devices.Where(d =>
                string.IsNullOrEmpty(query) ||
                d.SshIp.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.TargetIp.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Hostname.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Username.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            FilteredDevices.ClearAndAddRange(filtered);
        }

        private async Task LoadSessionAsync()
        {
            try
            {
                var session = await _storageService.LoadSessionAsync();
                if (session.HasValue)
                {
                    var (devices, settings) = session.Value;
                    if (devices.Any())
                    {
                        Devices.ClearAndAddRange(devices);
                        ApplyFilter();
                    }
                    Settings = settings;
                    Logger.LogInfo("Session loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load session", ex);
            }
        }

        [RelayCommand]
        public async Task StartAll()
        {
            var devicesToDeploy = Devices.Where(d => d.HasData).ToList();
            if (!devicesToDeploy.Any())
            {
                MessageBox.Show("Нет заполненных строк для развёртывания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Запустить развёртывание на {devicesToDeploy.Count} устройствах?\n\nЭто может занять несколько минут.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await StartDeploymentAsync(devicesToDeploy);
        }

        [RelayCommand]
        public async Task StartSelected()
        {
            var devicesToDeploy = Devices.Where(d => d.IsSelected && d.HasData).ToList();
            if (!devicesToDeploy.Any())
            {
                MessageBox.Show("Не выбрано ни одного устройства", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                $"Запустить развёртывание на {devicesToDeploy.Count} выбранных устройствах?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await StartDeploymentAsync(devicesToDeploy);
        }

        [RelayCommand]
        public void StopAll()
        {
            _deploymentCts?.Cancel();
            Logger.LogInfo("Deployment stopped by user");
        }

        [RelayCommand]
        public async Task AutofillSshIp()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.SshIp));
            if (firstDevice == null)
            {
                MessageBox.Show("Сначала заполните первую строку SSH IP", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var baseIp = firstDevice.SshIp.Trim();
            int index = Devices.IndexOf(firstDevice);

            if (ExtractIpPattern(baseIp, out var prefix, out var startNumber, out var width))
            {
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].SshIp = $"{prefix}{(startNumber + (i - index)).ToString().PadLeft(width, '0')}";
                }
                Logger.LogInfo($"SSH IP auto-filled from {baseIp}");
            }
            else
            {
                MessageBox.Show("Невозможно определить шаблон заполнения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task AutofillTargetIp()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.TargetIp));
            if (firstDevice == null)
            {
                MessageBox.Show("Сначала заполните первую строку Target IP", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ExtractLastOctet(firstDevice.TargetIp, out var prefix, out var startNumber))
            {
                int index = Devices.IndexOf(firstDevice);
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].TargetIp = $"{prefix}{startNumber + (i - index)}";
                }
                Logger.LogInfo($"Target IP auto-filled from {firstDevice.TargetIp}");
            }
            else
            {
                MessageBox.Show("Некорректный IP адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task AutofillHostname()
        {
            var firstDevice = Devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Hostname));
            if (firstDevice == null)
            {
                MessageBox.Show("Сначала заполните первую строку Hostname", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var baseHostname = firstDevice.Hostname.Trim();
            int index = Devices.IndexOf(firstDevice);

            if (ExtractIpPattern(baseHostname, out var prefix, out var startNumber, out var width))
            {
                for (int i = index + 1; i < Devices.Count; i++)
                {
                    Devices[i].Hostname = $"{prefix}{(startNumber + (i - index)).ToString().PadLeft(width, '0')}";
                }
                Logger.LogInfo($"Hostname auto-filled from {baseHostname}");
            }
        }

        [RelayCommand]
        public async Task SaveSession()
        {
            try
            {
                await _storageService.SaveSessionAsync(Devices.ToList(), Settings);
                MessageBox.Show("Сессия успешно сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Logger.LogInfo("Session saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("Failed to save session", ex);
            }
        }

        [RelayCommand]
        public async Task ExportReport()
        {
            if (!DeploymentResults.Any())
            {
                MessageBox.Show("Нет результатов для экспорта", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await _storageService.SaveReportAsync(DeploymentResults.ToList());
                MessageBox.Show("Отчёт успешно экспортирован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Logger.LogInfo("Report exported successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("Failed to export report", ex);
            }
        }

        [RelayCommand]
        public async Task ImportCsv()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "Import Devices from CSV"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var importedDevices = await _csvService.ImportFromCsvAsync(dialog.FileName);
                if (importedDevices.Any())
                {
                    Devices.ClearAndAddRange(importedDevices);
                    ApplyFilter();
                    MessageBox.Show($"Импортировано {importedDevices.Count} устройств", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logger.LogInfo($"Imported {importedDevices.Count} devices from CSV");
                }
                else
                {
                    MessageBox.Show("Файл не содержит данных", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("Failed to import CSV", ex);
            }
        }

        [RelayCommand]
        public async Task ExportCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"devices_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Devices to CSV"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var devicesToExport = Devices.Where(d => d.HasData).ToList();
                await _csvService.ExportToCsvAsync(devicesToExport, dialog.FileName);
                MessageBox.Show($"Экспортировано {devicesToExport.Count} устройств", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Logger.LogInfo($"Exported {devicesToExport.Count} devices to CSV");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("Failed to export CSV", ex);
            }
        }

        [RelayCommand]
        public void ClearAll()
        {
            if (MessageBox.Show(
                "Вы уверены? Все данные будут удалены.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var device in Devices)
            {
                device.SshIp = string.Empty;
                device.TargetIp = string.Empty;
                device.Hostname = string.Empty;
                device.Username = string.Empty;
                device.Password = string.Empty;
                device.SudoPassword = string.Empty;
                device.IsSelected = false;
                device.Status = DeviceStatus.Ready;
                device.StatusMessage = "Ready";
                device.Logs.Clear();
            }
            Logger.LogInfo("All data cleared");
        }

        private async Task StartDeploymentAsync(List<DeploymentDevice> devicesToDeploy)
        {
            IsDeploying = true;
            TotalDevices = devicesToDeploy.Count;
            SuccessfulDevices = 0;
            FailedDevices = 0;
            DeploymentResults.Clear();

            _deploymentCts = new CancellationTokenSource();
            var semaphore = new SemaphoreSlim(Settings.MaxParallelConnections);

            Logger.LogInfo($"Starting deployment on {devicesToDeploy.Count} devices");

            var tasks = devicesToDeploy.Select(device => DeployDeviceAsync(device, semaphore));

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Deployment cancelled by user");
            }
            finally
            {
                IsDeploying = false;
                Logger.LogInfo($"Deployment completed: {SuccessfulDevices} success, {FailedDevices} failed");
                MessageBox.Show(
                    $"Развёртывание завершено!\n\n✓ Успешно: {SuccessfulDevices}\n✗ Ошибок: {FailedDevices}\n" +
                    $"Всего: {TotalDevices}",
                    "Отчёт о развёртывании",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async Task DeployDeviceAsync(DeploymentDevice device, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                ActiveDeployments++;
                device.IsDeploying = true;
                device.Status = DeviceStatus.Queued;
                device.StatusMessage = "⏳ Queued...";
                device.StatusColor = DeviceStatusColor.Warning;
                device.StartTime = DateTime.Now;
                device.Logs.Clear();

                var result = await _deploymentService.DeployAsync(
                    device,
                    Settings,
                    msg => device.Logs.Add(msg),
                    _deploymentCts!.Token);

                device.IsDeploying = false;
                device.Status = result.IsSuccessful ? DeviceStatus.Success : DeviceStatus.Error;
                device.StatusMessage = result.IsSuccessful
                    ? $"✅ Success ({result.Duration.TotalSeconds:F1}s)"
                    : $"❌ Error: {result.ErrorMessage.TruncateForDisplay(30)}";
                device.StatusColor = result.IsSuccessful ? DeviceStatusColor.Success : DeviceStatusColor.Danger;
                device.DeployDuration = $"{result.Duration.TotalSeconds:F1}s";

                DeploymentResults.Add(result);

                if (result.IsSuccessful)
                {
                    SuccessfulDevices++;
                    Logger.LogInfo($"Device {device.Hostname} deployed successfully");
                }
                else
                {
                    FailedDevices++;
                    Logger.LogError($"Device {device.Hostname} deployment failed: {result.ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                device.Status = DeviceStatus.Stopped;
                device.StatusMessage = "⊘ Stopped";
                device.StatusColor = DeviceStatusColor.Danger;
            }
            catch (Exception ex)
            {
                device.Status = DeviceStatus.Error;
                device.StatusMessage = $"❌ Error: {ex.Message.TruncateForDisplay(20)}";
                device.StatusColor = DeviceStatusColor.Danger;
                FailedDevices++;
                Logger.LogError($"Unexpected error for device {device.Hostname}", ex);
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
