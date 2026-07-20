using ProvisionTool.Models;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Интерфейс для сохранения/загрузки данных
    /// </summary>
    public interface IStorageService
    {
        Task SaveSessionAsync(List<DeploymentDevice> devices, DeploymentSettings settings);
        Task<(List<DeploymentDevice> devices, DeploymentSettings settings)?> LoadSessionAsync();
        Task SaveReportAsync(List<DeploymentResult> results);
    }
}
