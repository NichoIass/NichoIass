namespace ProvisionTool.Utils
{
    /// <summary>
    /// Конвертер для инвертирования булевых значений
    /// </summary>
    public class InvertBoolConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}
