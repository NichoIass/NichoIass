using ProvisionTool.Models;
using System.Text.Json;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Реализация сервиса для сохранения и загрузки данных
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly string _sessionFile;
        private readonly string _reportsDir;

        public StorageService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProvisionTool");
            
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            _sessionFile = Path.Combine(appDataPath, "session.json");
            _reportsDir = Path.Combine(appDataPath, "reports");

            if (!Directory.Exists(_reportsDir))
                Directory.CreateDirectory(_reportsDir);
        }

        public async Task SaveSessionAsync(List<DeploymentDevice> devices, DeploymentSettings settings)
        {
            try
            {
                var sessionData = new
                {
                    timestamp = DateTime.Now,
                    devices = devices.Select(d => new
                    {
                        d.Index,
                        d.SshIp,
                        d.TargetIp,
                        d.Hostname,
                        d.Username,
                        d.Password,
                        d.SudoPassword,
                        d.UseSudo
                    }).ToList(),
                    settings = new
                    {
                        settings.PrimaryUsername,
                        settings.PrimaryPassword,
                        settings.BackupUsername,
                        settings.BackupPassword,
                        settings.ScriptUrl,
                        settings.MaxParallelConnections,
                        settings.ConnectionRetries,
                        settings.ConnectionTimeoutSeconds,
                        settings.RebootTimeoutSeconds,
                        settings.WaitForReboot,
                        settings.UseSshKey,
                        settings.SshKeyPath,
                        settings.AutoVerifyAfterDeploy
                    }
                };

                var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_sessionFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public async Task<(List<DeploymentDevice> devices, DeploymentSettings settings)?> LoadSessionAsync()
        {
            try
            {
                if (!File.Exists(_sessionFile))
                    return null;

                var json = await File.ReadAllTextAsync(_sessionFile);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var devices = new List<DeploymentDevice>();
                if (root.TryGetProperty("devices", out var devicesElement))
                {
                    foreach (var devElement in devicesElement.EnumerateArray())
                    {
                        devices.Add(new DeploymentDevice
                        {
                            Index = devElement.GetProperty("index").GetInt32(),
                            SshIp = devElement.GetProperty("sshIp").GetString() ?? string.Empty,
                            TargetIp = devElement.GetProperty("targetIp").GetString() ?? string.Empty,
                            Hostname = devElement.GetProperty("hostname").GetString() ?? string.Empty,
                            Username = devElement.GetProperty("username").GetString() ?? string.Empty,
                            Password = devElement.GetProperty("password").GetString() ?? string.Empty,
                            SudoPassword = devElement.GetProperty("sudoPassword").GetString() ?? string.Empty,
                            UseSudo = devElement.GetProperty("useSudo").GetBoolean()
                        });
                    }
                }

                var settings = new DeploymentSettings();
                if (root.TryGetProperty("settings", out var settingsElement))
                {
                    settings = new DeploymentSettings
                    {
                        PrimaryUsername = settingsElement.GetProperty("primaryUsername").GetString() ?? "root",
                        PrimaryPassword = settingsElement.GetProperty("primaryPassword").GetString() ?? string.Empty,
                        BackupUsername = settingsElement.GetProperty("backupUsername").GetString() ?? string.Empty,
                        BackupPassword = settingsElement.GetProperty("backupPassword").GetString() ?? string.Empty,
                        ScriptUrl = settingsElement.GetProperty("scriptUrl").GetString() ?? string.Empty,
                        MaxParallelConnections = settingsElement.GetProperty("maxParallelConnections").GetInt32(),
                        ConnectionRetries = settingsElement.GetProperty("connectionRetries").GetInt32(),
                        ConnectionTimeoutSeconds = settingsElement.GetProperty("connectionTimeoutSeconds").GetInt32(),
                        RebootTimeoutSeconds = settingsElement.GetProperty("rebootTimeoutSeconds").GetInt32(),
                        WaitForReboot = settingsElement.GetProperty("waitForReboot").GetBoolean(),
                        UseSshKey = settingsElement.GetProperty("useSshKey").GetBoolean(),
                        SshKeyPath = settingsElement.GetProperty("sshKeyPath").GetString() ?? string.Empty,
                        AutoVerifyAfterDeploy = settingsElement.GetProperty("autoVerifyAfterDeploy").GetBoolean()
                    };
                }

                return (devices, settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
                return null;
            }
        }

        public async Task SaveReportAsync(List<DeploymentResult> results)
        {
            try
            {
                var filename = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filepath = Path.Combine(_reportsDir, filename);

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Hostname,Target IP,Status,Duration,Error,Timestamp");

                foreach (var result in results)
                {
                    csv.AppendLine($"{result.DeviceHostname},{result.TargetIp},{(result.IsSuccessful ? "Success" : "Failed")},{result.Duration.TotalSeconds:F1},{result.ErrorMessage},{result.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }

                await File.WriteAllTextAsync(filepath, csv.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save report: {ex.Message}");
            }
        }
    }
}
