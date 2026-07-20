using System.Text.RegularExpressions;

namespace ProvisionTool.Utils
{
    /// <summary>
    /// Поможные функции для проверки нормализации данных
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex IpOctetRegex = new Regex(@"^\d{1,3}$");
        private static readonly Regex FullIpRegex = new Regex(@"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$");

        public static bool IsValidIpOctetOrFull(string value)
        {
            value = value.Trim();
            if (string.IsNullOrEmpty(value))
                return true;

            if (IpOctetRegex.IsMatch(value))
                return 0 <= int.Parse(value) && int.Parse(value) <= 255;

            var match = FullIpRegex.Match(value);
            if (match.Success)
                return match.Groups.Cast<Group>().Skip(1).All(g => 
                {
                    var num = int.Parse(g.Value);
                    return num >= 0 && num <= 255;
                });

            return false;
        }

        public static bool IsValidIpAddress(string ipAddress)
        {
            return FullIpRegex.IsMatch(ipAddress) && ipAddress.Split('.')
                .All(octet => int.TryParse(octet, out var num) && num >= 0 && num <= 255);
        }
    }
}
