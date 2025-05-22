using System;
using System.Windows.Data;
using System.Globalization;

namespace HospitalApp
{
    public class StatusToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null)
                return "Неизвестно";

            bool orderStatus;
            // Проверяем тип values[0] и пытаемся преобразовать
            if (values[0] is bool boolValue)
            {
                orderStatus = boolValue;
            }
            else if (values[0] is int intValue)
            {
                // Если Order_Status — это int, преобразуем 1 в true, 0 в false
                orderStatus = intValue == 1;
            }
            else if (values[0] is string stringValue)
            {
                // Если Order_Status — строка, например, "True" или "1"
                orderStatus = stringValue == "True" || stringValue == "1" || stringValue == "Выполнен";
            }
            else
            {
                return "Неизвестно"; // Некорректный тип
            }

            DateTime? completeTime = values[1] as DateTime?;
            return orderStatus && completeTime.HasValue
                ? $"Проанализировано ({completeTime.Value:dd.MM.yyyy HH:mm})"
                : "В работе";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}