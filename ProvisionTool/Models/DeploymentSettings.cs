namespace ProvisionTool.Models
{
    /// <summary>
    /// Глобальные настройки развёртывания
    /// </summary>
    public class DeploymentSettings
    {
        public string PrimaryUsername { get; set; } = "root";
        public string PrimaryPassword { get; set; } = string.Empty;
        public string BackupUsername { get; set; } = string.Empty;
        public string BackupPassword { get; set; } = string.Empty;
        public string ScriptUrl { get; set; } = "http://10.50.12.11/deploy_price.sh";
        public int MaxParallelConnections { get; set; } = 10;
        public int ConnectionRetries { get; set; } = 3;
        public int ConnectionTimeoutSeconds { get; set; } = 15;
        public int RebootTimeoutSeconds { get; set; } = 150;
        public bool WaitForReboot { get; set; } = true;
        public bool UseSshKey { get; set; } = false;
        public string SshKeyPath { get; set; } = string.Empty;
        public bool AutoVerifyAfterDeploy { get; set; } = true;
    }
}
