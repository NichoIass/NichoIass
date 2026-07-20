namespace ProvisionTool.Services
{
    /// <summary>
    /// Фактория для сохранения зависимостей
    /// </summary>
    public class ServiceFactory
    {
        public static ISshService CreateSshService() => new SshService();
        public static IDeploymentService CreateDeploymentService() => new DeploymentService(CreateSshService());
        public static IStorageService CreateStorageService() => new StorageService();
        public static ICsvService CreateCsvService() => new CsvService();
    }
}
