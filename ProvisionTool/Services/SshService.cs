using Renci.SshNet;
using System.Diagnostics;

namespace ProvisionTool.Services
{
    /// <summary>
    /// Реализация SSH сервиса для подключения и выполнения команд
    /// </summary>
    public class SshService : ISshService, IDisposable
    {
        private SshClient? _client;
        private string? _lastError;

        public async Task<bool> ConnectAsync(string host, string username, string password, string? keyPath = null, int timeoutSeconds = 15)
        {
            try
            {
                _client = new SshClient(host, username, password)
                {
                    ConnectionInfo = new PasswordConnectionInfo(host, 22, username, password)
                    {
                        Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                    }
                };

                if (!string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath))
                {
                    var connectionInfo = new PrivateKeyConnectionInfo(host, 22, username, new PrivateKeyFile(keyPath))
                    {
                        Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                    };
                    _client = new SshClient(connectionInfo);
                }

                _client.Connect();
                return true;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _client?.Disconnect();
                _client?.Dispose();
                _client = null;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        public async Task<(bool success, string output)> ExecuteCommandAsync(string command)
        {
            try
            {
                if (_client == null || !_client.IsConnected)
                    return (false, "Not connected");

                using var cmd = _client.CreateCommand(command);
                var result = cmd.Execute();
                return (cmd.ExitStatus == 0, result);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return (false, ex.Message);
            }
        }

        public async Task<bool> ExecuteCommandWithPasswordPromptAsync(string command, string sudoPassword)
        {
            try
            {
                if (_client == null || !_client.IsConnected)
                    return false;

                using var stream = _client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
                stream.WriteLine(command);
                stream.WriteLine(sudoPassword);
                
                await Task.Delay(500);
                var result = stream.ReadLine();
                
                return !string.IsNullOrEmpty(result);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return false;
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            return _client?.IsConnected ?? false;
        }

        public string? GetLastError() => _lastError;

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
