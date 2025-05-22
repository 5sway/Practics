using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HospitalApp
{
    public class DeviationToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal deviation)
            {
                return deviation * 100; // Переводим в проценты для отображения
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && decimal.TryParse(strValue, out decimal percent))
            {
                return percent / 100; // Переводим обратно в десятичную дробь для хранения
            }
            return value;
        }
    }
}
