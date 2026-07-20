using Renci.SshNet;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Интерфейс для SSH операций
    /// </summary>
    public interface ISshService
    {
        Task<bool> ConnectAsync(string host, string username, string password, string? keyPath = null, int timeoutSeconds = 15);
        Task DisconnectAsync();
        Task<(bool success, string output)> ExecuteCommandAsync(string command);
        Task<bool> ExecuteCommandWithPasswordPromptAsync(string command, string sudoPassword);
        Task<bool> IsConnectedAsync();
        string? GetLastError();
    }
}
