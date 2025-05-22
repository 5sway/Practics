using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data.Entity;
using System.Globalization;
using Excel = Microsoft.Office.Interop.Excel;

namespace HospitalApp
{
    public static class ExportExcel
    {
        public static void GenerateExcelReport(List<string> selectedTables, Dictionary<string, List<int>> selectedRecordIds,
            DateTime? startDate, DateTime? endDate, string filePath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;

            try
            {
                if (string.IsNullOrEmpty(filePath) || selectedTables == null)
                {
                    MessageBox.Show("Ошибка: некорректные параметры экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                excelApp = new Excel.Application();
                excelApp.Visible = false;
                workbook = excelApp.Workbooks.Add();

                while (workbook.Sheets.Count > 1)
                    ((Excel.Worksheet)workbook.Sheets[workbook.Sheets.Count]).Delete();

                using (var context = new HospitalBaseEntities())
                {
                    var patients = context.Pacient.Include(p => p.Insurance_Company).ToList() ?? new List<Pacient>();
                    var orders = context.Order.Include(o => o.Pacient).Include(o => o.Service).ToList() ?? new List<Order>();
                    var services = context.Service.ToList() ?? new List<Service>();
                    var users = context.User.Include(u => u.Role).Include(u => u.Service).Include(u => u.Insurance_Company).ToList() ?? new List<User>();
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate).ToList();
                        users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate).ToList();
                    }
                    if (selectedRecordIds.Any())
                    {
                        patients = selectedRecordIds.ContainsKey("Patients")
                            ? patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id)).ToList()
                            : new List<Pacient>();
                        orders = selectedRecordIds.ContainsKey("Orders")
                            ? orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id)).ToList()
                            : new List<Order>();
                        services = selectedRecordIds.ContainsKey("Services")
                            ? services.Where(s => selectedRecordIds["Services"].Contains(s.Service_Id)).ToList()
                            : new List<Service>();
                        users = selectedRecordIds.ContainsKey("Users")
                            ? users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id)).ToList()
                            : new List<User>();
                    }

                    if (!selectedTables.Any())
                    {
                        MessageBox.Show("Нет данных для экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int sheetIndex = 1;
                    foreach (var table in selectedTables)
                    {
                        Excel.Worksheet sheet;
                        if (sheetIndex == 1)
                            sheet = (Excel.Worksheet)workbook.Sheets[1];
                        else
                            sheet = (Excel.Worksheet)workbook.Sheets.Add(After: workbook.Sheets[workbook.Sheets.Count]);

                        switch (table)
                        {
                            case "Patients":
                                if (patients.Any())
                                {
                                    sheet.Name = "Пациенты";
                                    ExportPatientsToExcel(sheet, patients);
                                }
                                break;
                            case "Orders":
                                if (orders.Any())
                                {
                                    sheet.Name = "Заказы";
                                    ExportOrdersToExcel(sheet, orders);
                                }
                                break;
                            case "Services":
                                if (services.Any())
                                {
                                    sheet.Name = "Услуги";
                                    ExportServicesToExcel(sheet, services);
                                }
                                break;
                            case "Users":
                                if (users.Any())
                                {
                                    sheet.Name = "Пользователи";
                                    ExportUsersToExcel(sheet, users);
                                }
                                break;
                            default:
                                MessageBox.Show($"Неизвестная таблица: {table}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                break;
                        }
                        sheetIndex++;
                    }

                    for (int i = workbook.Sheets.Count; i > selectedTables.Count; i--)
                        ((Excel.Worksheet)workbook.Sheets[i]).Delete();

                    workbook.SaveAs(filePath);
                    workbook.Close();
                    excelApp.Quit();

                    ReleaseExcelObjects(workbook, excelApp);
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ExportPatientsToExcel(Excel.Worksheet sheet, List<Pacient> patients)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID Пациента";
            sheet.Cells[1, 2] = "ФИО";
            sheet.Cells[1, 3] = "Дата рождения";
            sheet.Cells[1, 4] = "Паспорт";
            sheet.Cells[1, 5] = "Телефон";
            sheet.Cells[1, 6] = "Email";
            sheet.Cells[1, 7] = "Полис";
            sheet.Cells[1, 8] = "Тип полиса";
            sheet.Cells[1, 9] = "Страховая компания";

            for (int i = 0; i < patients.Count; i++)
            {
                var patient = patients[i];
                sheet.Cells[i + 2, 1] = patient.Pacient_Id;
                sheet.Cells[i + 2, 2] = patient.Full_Name ?? "";
                sheet.Cells[i + 2, 3] = patient.Birth_Date.ToString("dd.MM.yyyy");
                sheet.Cells[i + 2, 4] = patient.Passport ?? "";
                sheet.Cells[i + 2, 5] = patient.Phone_Number ?? "";
                sheet.Cells[i + 2, 6] = patient.Email ?? "";
                sheet.Cells[i + 2, 7] = patient.Policy ?? "";
                sheet.Cells[i + 2, 8] = patient.Policy_Type ?? "";
                sheet.Cells[i + 2, 9] = patient.Insurance_Company?.Title ?? "Не указана";
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportOrdersToExcel(Excel.Worksheet sheet, List<Order> orders)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID Заказа";
            sheet.Cells[1, 2] = "Дата создания";
            sheet.Cells[1, 3] = "Пациент";
            sheet.Cells[1, 4] = "Услуга";
            sheet.Cells[1, 5] = "Статус";
            sheet.Cells[1, 6] = "Штрих-код";

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                sheet.Cells[i + 2, 1] = order.Order_Id;
                sheet.Cells[i + 2, 2] = order.Create_Date.ToString("dd.MM.yyyy HH:mm");
                sheet.Cells[i + 2, 3] = order.Pacient?.Full_Name ?? "Неизвестно";
                sheet.Cells[i + 2, 4] = order.Service?.Title ?? "Неизвестно";
                sheet.Cells[i + 2, 5] = order.Order_Status.HasValue && order.Order_Status.Value
                    ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})"
                    : "В работе";
                sheet.Cells[i + 2, 6] = order.BarCode?.ToString() ?? "Не указан";
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportServicesToExcel(Excel.Worksheet sheet, List<Service> services)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID Услуги";
            sheet.Cells[1, 2] = "Название";
            sheet.Cells[1, 3] = "Цена";
            sheet.Cells[1, 4] = "Срок (дни)";
            sheet.Cells[1, 5] = "Допуск";

            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                sheet.Cells[i + 2, 1] = service.Service_Id;
                sheet.Cells[i + 2, 2] = service.Title ?? "";
                sheet.Cells[i + 2, 3] = service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана";
                sheet.Cells[i + 2, 4] = service.Deadline;
                sheet.Cells[i + 2, 5] = service.Deviation.ToString("P2");
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportUsersToExcel(Excel.Worksheet sheet, List<User> users)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID Пользователя";
            sheet.Cells[1, 2] = "ФИО";
            sheet.Cells[1, 3] = "Логин";
            sheet.Cells[1, 4] = "Пароль";
            sheet.Cells[1, 5] = "Последний вход";
            sheet.Cells[1, 6] = "Услуга";
            sheet.Cells[1, 7] = "Страховая компания";
            sheet.Cells[1, 8] = "Счет";
            sheet.Cells[1, 9] = "Роль";

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                sheet.Cells[i + 2, 1] = user.User_Id;
                sheet.Cells[i + 2, 2] = user.Full_Name ?? "";
                sheet.Cells[i + 2, 3] = user.Login ?? "";
                sheet.Cells[i + 2, 4] = user.Password ?? "";
                sheet.Cells[i + 2, 5] = user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm");
                sheet.Cells[i + 2, 6] = user.Service?.Title ?? "Не указана";
                sheet.Cells[i + 2, 7] = user.Insurance_Company?.Title ?? "Не указана";
                sheet.Cells[i + 2, 8] = user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан";
                sheet.Cells[i + 2, 9] = user.Role?.Name ?? "Неизвестно";
            }

            FormatExcelSheet(sheet);
        }

        private static void FormatExcelSheet(Excel.Worksheet sheet)
        {
            sheet.Columns.AutoFit();
            Excel.Range headerRange = sheet.Range["A1", GetExcelColumnName(sheet.UsedRange.Columns.Count) + "1"];
            headerRange.Font.Bold = true;
            headerRange.Font.Name = "Times New Roman";
            headerRange.Font.Size = 12;
            headerRange.Interior.Color = Excel.XlRgbColor.rgbLightGray;
            Excel.Range allCells = sheet.UsedRange;
            allCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            allCells.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show("Файл отчета не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void ReleaseExcelObjects(params object[] objects)
        {
            foreach (var obj in objects)
            {
                try
                {
                    if (obj != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
                catch { }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}