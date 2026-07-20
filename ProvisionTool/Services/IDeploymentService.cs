using ProvisionTool.Models;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Интерфейс для сервиса развёртывания
    /// </summary>
    public interface IDeploymentService
    {
        Task<DeploymentResult> DeployAsync(
            DeploymentDevice device,
            DeploymentSettings settings,
            Action<string> onLogMessage,
            CancellationToken cancellationToken);

        Task<bool> VerifyDeviceAfterDeployAsync(
            string targetIp,
            string expectedHostname,
            DeploymentSettings settings,
            Action<string> onLogMessage,
            CancellationToken cancellationToken);
    }
}
