using System;
using System.Windows.Data;
using System.Globalization;

namespace HospitalApp
{
    public class StatusToTextConverter : IMultiValueConverter
    {
        // Преобразует статус заказа и дату завершения в текстовое представление
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null)
                return "Неизвестно";

            bool orderStatus = (bool)values[0];
            DateTime? completeTime = values[1] as DateTime?;
            return orderStatus && completeTime.HasValue
                ? $"Проанализировано ({completeTime.Value:dd.MM.yyyy HH:mm})"
                : "В работе";
        }

        // Не реализовано, так как обратное преобразование не требуется
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}