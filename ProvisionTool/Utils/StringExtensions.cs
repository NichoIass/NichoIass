namespace ProvisionTool.Utils
{
    /// <summary>
    /// Расширения для работы со строками
    /// </summary>
    public static class StringExtensions
    {
        public static bool TryParseIpOctet(this string value, out int octet)
        {
            octet = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (int.TryParse(value.Trim(), out var result))
            {
                if (result >= 0 && result <= 255)
                {
                    octet = result;
                    return true;
                }
            }

            return false;
        }

        public static string TruncateForDisplay(this string value, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
        }
    }
}
