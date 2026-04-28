using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CircleSearch.Helpers
{
    internal class BoolToStatusText : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool)value ? "● Đang hoạt động" : "● Chưa khởi động";
        }

        public object ConvertBack(object value, Type t, object? p, CultureInfo c)
        {
            throw new NotImplementedException();
        }
    }
}
