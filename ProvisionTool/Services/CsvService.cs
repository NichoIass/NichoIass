using ProvisionTool.Models;
using System.IO;
using System.Text;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Сервис для работы с CSV файлами
    /// </summary>
    public interface ICsvService
    {
        Task<List<DeploymentDevice>> ImportFromCsvAsync(string filePath);
        Task ExportToCsvAsync(List<DeploymentDevice> devices, string filePath);
    }

    public class CsvService : ICsvService
    {
        public async Task<List<DeploymentDevice>> ImportFromCsvAsync(string filePath)
        {
            var devices = new List<DeploymentDevice>();

            try
            {
                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string? line;
                int index = 0;
                bool isHeader = true;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        devices.Add(new DeploymentDevice
                        {
                            Index = ++index,
                            SshIp = parts[0].Trim(),
                            TargetIp = parts[1].Trim(),
                            Hostname = parts[2].Trim(),
                            Username = parts.Length > 3 ? parts[3].Trim() : string.Empty,
                            Password = parts.Length > 4 ? parts[4].Trim() : string.Empty,
                            SudoPassword = parts.Length > 5 ? parts[5].Trim() : string.Empty,
                            UseSudo = parts.Length > 6 ? bool.Parse(parts[6].Trim()) : true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CSV import error: {ex.Message}");
            }

            return devices;
        }

        public async Task ExportToCsvAsync(List<DeploymentDevice> devices, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                
                // Header
                await writer.WriteLineAsync("SSH IP,Target IP,Hostname,Username,Password,Sudo Password,Use Sudo,Status,Duration");

                // Data
                foreach (var device in devices.Where(d => d.HasData))
                {
                    var line = $"{device.SshIp},{device.TargetIp},{device.Hostname},{device.Username},{device.Password},{device.SudoPassword},{device.UseSudo},{device.StatusMessage},{device.DeployDuration}";
                    await writer.WriteLineAsync(line);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CSV export error: {ex.Message}");
            }
        }
    }
}
