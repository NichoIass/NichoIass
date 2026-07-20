namespace ProvisionTool.Models
{
    /// <summary>
    /// Результат развёртывания устройства
    /// </summary>
    public class DeploymentResult
    {
        public string DeviceHostname { get; set; } = string.Empty;
        public string TargetIp { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public List<string> Logs { get; set; } = new();
        public bool Ping { get; set; }
        public bool SshConnectivity { get; set; }
        public bool HostnameMatches { get; set; }
    }
}
