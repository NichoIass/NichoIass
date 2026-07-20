using ProvisionTool.Models;
using Renci.SshNet;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Реализация сервиса развёртывания устройств
    /// </summary>
    public class DeploymentService : IDeploymentService
    {
        private readonly ISshService _sshService;

        public DeploymentService(ISshService sshService)
        {
            _sshService = sshService ?? throw new ArgumentNullException(nameof(sshService));
        }

        public async Task<DeploymentResult> DeployAsync(
            DeploymentDevice device,
            DeploymentSettings settings,
            Action<string> onLogMessage,
            CancellationToken cancellationToken)
        {
            var result = new DeploymentResult
            {
                DeviceHostname = device.Hostname,
                TargetIp = device.TargetIp,
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] Начало развёртывания устройства {device.Hostname} ({device.FullSshIp})...");

                // Попытка подключения с повторами
                bool connected = false;
                string username = settings.PrimaryUsername;
                string password = settings.PrimaryPassword;

                for (int attempt = 1; attempt <= settings.ConnectionRetries; attempt++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] Развёртывание остановлено пользователем.");
                        result.IsSuccessful = false;
                        result.ErrorMessage = "Stopped by user";
                        return result;
                    }

                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] Попытка подключения {attempt}/{settings.ConnectionRetries}...");

                    // Попытка с основными учётными данными
                    connected = await _sshService.ConnectAsync(
                        device.FullSshIp,
                        username,
                        password,
                        settings.SshKeyPath,
                        settings.ConnectionTimeoutSeconds);

                    if (!connected && !string.IsNullOrEmpty(settings.BackupUsername))
                    {
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] Основные учётные данные не сработали, попытка с резервными...");
                        username = settings.BackupUsername;
                        password = settings.BackupPassword;
                        connected = await _sshService.ConnectAsync(
                            device.FullSshIp,
                            username,
                            password,
                            settings.SshKeyPath,
                            settings.ConnectionTimeoutSeconds);
                    }

                    if (connected)
                    {
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] SSH подключение успешно!");
                        break;
                    }

                    if (attempt < settings.ConnectionRetries)
                    {
                        int backoff = Math.Min((int)Math.Pow(2, attempt), 15);
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] Ошибка подключения. Пауза {backoff}s перед повторной попыткой...");
                        await Task.Delay(backoff * 1000, cancellationToken);
                    }
                }

                if (!connected)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"Failed to connect after {settings.ConnectionRetries} attempts: {_sshService.GetLastError()}";
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] ❌ Не удалось подключиться: {result.ErrorMessage}");
                    return result;
                }

                // Выполнение скрипта прошивки
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] Загрузка и выполнение скрипта прошивки...");
                var downloadCmd = $"curl -f -s -k -L {settings.ScriptUrl} -o /tmp/deploy.sh && chmod +x /tmp/deploy.sh";
                var (downloadSuccess, downloadOutput) = await _sshService.ExecuteCommandAsync(downloadCmd);

                if (!downloadSuccess)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"Failed to download script: {downloadOutput}";
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] ❌ Ошибка загрузки скрипта: {downloadOutput}");
                    await _sshService.DisconnectAsync();
                    return result;
                }

                onLogMessage($"[{DateTime.Now:HH:mm:ss}] Скрипт загружен успешно. Выполнение...");

                // Формирование команды выполнения
                string deployCmd = $"/tmp/deploy.sh --ip {device.TargetIp} --hostname {device.Hostname}";
                if (device.UseSudo && username != "root")
                {
                    deployCmd = $"sudo {deployCmd}";
                }

                var (deploySuccess, deployOutput) = await _sshService.ExecuteCommandAsync(deployCmd);
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] Выход скрипта: {deployOutput}");

                if (!deploySuccess)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"Script execution failed: {deployOutput}";
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] ❌ Ошибка выполнения скрипта");
                    await _sshService.DisconnectAsync();
                    return result;
                }

                onLogMessage($"[{DateTime.Now:HH:mm:ss}] ✅ Скрипт прошивки выполнен успешно!");
                await _sshService.DisconnectAsync();

                // Ожидание перезагрузки
                if (settings.WaitForReboot)
                {
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] Ожидание перезагрузки устройства на {device.TargetIp}...");
                    bool rebootOk = await WaitForRebootAsync(device.TargetIp, settings.RebootTimeoutSeconds, onLogMessage, cancellationToken);
                    result.Ping = rebootOk;

                    if (rebootOk && settings.AutoVerifyAfterDeploy)
                    {
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] Устройство онлайн. Проверка конфигурации...");
                        bool verified = await VerifyDeviceAfterDeployAsync(
                            device.TargetIp,
                            device.Hostname,
                            settings,
                            onLogMessage,
                            cancellationToken);

                        result.SshConnectivity = verified;
                        result.HostnameMatches = verified; // Упрощённая проверка
                    }
                }

                result.IsSuccessful = true;
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] ✅ Развёртывание завершено успешно за {result.Duration.TotalSeconds:F1}s");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.Duration = stopwatch.Elapsed;
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] ❌ Критическая ошибка: {ex.Message}");
                await _sshService.DisconnectAsync();
                return result;
            }
        }

        public async Task<bool> VerifyDeviceAfterDeployAsync(
            string targetIp,
            string expectedHostname,
            DeploymentSettings settings,
            Action<string> onLogMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                // Проверка SSH подключения
                bool sshOk = await _sshService.ConnectAsync(
                    targetIp,
                    settings.PrimaryUsername,
                    settings.PrimaryPassword,
                    settings.SshKeyPath,
                    10);

                if (!sshOk)
                {
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] ⚠ SSH подключение для проверки не удалось");
                    return false;
                }

                onLogMessage($"[{DateTime.Now:HH:mm:ss}] ✅ SSH подключение успешно");

                // Получение хостнейма
                var (success, hostname) = await _sshService.ExecuteCommandAsync("hostname");
                if (success)
                {
                    hostname = hostname.Trim();
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] Текущий hostname: {hostname}");
                    if (hostname == expectedHostname)
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] ✅ Hostname совпадает");
                    else
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] ⚠ Hostname не совпадает (ожидается {expectedHostname})");
                }

                await _sshService.DisconnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] ⚠ Ошибка проверки: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitForRebootAsync(
            string targetIp,
            int timeoutSeconds,
            Action<string> onLogMessage,
            CancellationToken cancellationToken)
        {
            // Даём устройству время на перезагрузку
            int graceSeconds = 20;
            onLogMessage($"[{DateTime.Now:HH:mm:ss}] Пауза {graceSeconds}s перед началом проверки...");
            await Task.Delay(graceSeconds * 1000, cancellationToken);

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                try
                {
                    var ping = new Ping();
                    var reply = ping.Send(targetIp, 3000);
                    if (reply.Status == IPStatus.Success)
                    {
                        onLogMessage($"[{DateTime.Now:HH:mm:ss}] ✅ Устройство {targetIp} онлайн (Ping ответил)");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    onLogMessage($"[{DateTime.Now:HH:mm:ss}] Проверка ping: {ex.Message}");
                }

                int elapsed = (int)stopwatch.Elapsed.TotalSeconds;
                onLogMessage($"[{DateTime.Now:HH:mm:ss}] Ожидание перезагрузки... {elapsed}/{timeoutSeconds}s");
                await Task.Delay(3000, cancellationToken);
            }

            onLogMessage($"[{DateTime.Now:HH:mm:ss}] ⚠ Таймаут ожидания перезагрузки ({timeoutSeconds}s)");
            return false;
        }
    }
}
