using System;
using System.Collections.Generic;

namespace HospitalApp
{
    public static class UserExtensions
    {
        private static readonly Dictionary<int, DateTime?> _billDates = new Dictionary<int, DateTime?>();

        // Получает временную дату счета для пользователя
        public static DateTime? GetTempBillDate(this User user) => _billDates.ContainsKey(user.User_Id) ? _billDates[user.User_Id] : null;

        // Устанавливает временную дату счета для пользователя
        public static void SetTempBillDate(this User user, DateTime? date)
        {
            if (_billDates.ContainsKey(user.User_Id))
                _billDates[user.User_Id] = date;
            else
                _billDates.Add(user.User_Id, date);
        }
    }
}