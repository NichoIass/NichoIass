using System.Diagnostics;
using System.IO;
using System.Text;

namespace ProvisionTool.Utils
{
    /// <summary>
    /// Логирование операций приложения
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProvisionTool",
            "Logs");

        static Logger()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}\n{ex}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public static void LogDebug(string message)
        {
            if (Debugger.IsAttached)
                Debug.WriteLine($"[DEBUG] {message}");
            WriteLog("DEBUG", message);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                var logFile = Path.Combine(LogDirectory, $"app_{DateTime.Now:yyyy-MM-dd}.log");
                var logMessage = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}\n";
                File.AppendAllText(logFile, logMessage, Encoding.UTF8);
            }
            catch
            {
                // Если не можем записать логи, молча игнорируем
            }
        }
    }
}
