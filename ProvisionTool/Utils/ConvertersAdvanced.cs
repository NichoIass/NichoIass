using System.Windows;
using System.Windows.Data;

namespace ProvisionTool.Utils
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DeviceStatusColor statusColor)
            {
                return statusColor switch
                {
                    DeviceStatusColor.Success => Application.Current.Resources["SuccessBrush"],
                    DeviceStatusColor.Danger => Application.Current.Resources["DangerBrush"],
                    DeviceStatusColor.Warning => Application.Current.Resources["WarningBrush"],
                    DeviceStatusColor.Info => Application.Current.Resources["InfoBrush"],
                    _ => Application.Current.Resources["TextDimBrush"]
                };
            }
            return Application.Current.Resources["TextDimBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
