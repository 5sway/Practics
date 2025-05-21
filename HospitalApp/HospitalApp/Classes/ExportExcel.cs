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
        public static void GenerateExcelReport(List<string> selectedTables,
            Dictionary<string, List<int>> selectedRecordIds, DateTime startDate, DateTime endDate, string filePath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;

            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Add();

                using (var context = new HospitalBaseEntities())
                {
                    foreach (var table in selectedTables)
                    {
                        var tableTitle = table == "Patients" ? "Пациенты" :
                                        table == "Orders" ? "Заказы" :
                                        table == "Services" ? "Услуги" : "Пользователи";

                        // Создаём новый лист
                        Excel.Worksheet worksheet = workbook.Sheets.Add();
                        worksheet.Name = tableTitle;

                        int row = 1;

                        // Заголовок таблицы
                        worksheet.Cells[row, 1] = tableTitle;
                        worksheet.Cells[row, 1].Font.Bold = true;
                        worksheet.Cells[row, 1].Font.Size = 14;
                        row += 2;

                        var columns = GetTableColumns(table);
                        // Заголовки столбцов
                        for (int col = 0; col < columns.Count; col++)
                        {
                            worksheet.Cells[row, col + 1] = columns[col];
                            worksheet.Cells[row, col + 1].Font.Bold = true;
                        }
                        row++;

                        // Данные
                        if (table == "Patients")
                        {
                            var patients = context.Pacient.Include(p => p.Insurance_Company).AsQueryable();
                            if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                            foreach (var patient in patients.ToList())
                            {
                                worksheet.Cells[row, 1] = patient.Pacient_Id;
                                worksheet.Cells[row, 2] = patient.Full_Name;
                                worksheet.Cells[row, 3] = patient.Birth_Date.ToString("dd.MM.yyyy");
                                worksheet.Cells[row, 4] = patient.Passport;
                                worksheet.Cells[row, 5] = patient.Phone_Number;
                                worksheet.Cells[row, 6] = patient.Email;
                                worksheet.Cells[row, 7] = patient.Policy;
                                worksheet.Cells[row, 8] = patient.Policy_Type;
                                worksheet.Cells[row, 9] = patient.Insurance_Company?.Title ?? "Не указана";
                                row++;
                            }
                        }
                        else if (table == "Orders")
                        {
                            var orders = context.Order.Include(o => o.Pacient).Include(o => o.Service).AsQueryable();
                            if (selectedRecordIds.ContainsKey("Orders") && selectedRecordIds["Orders"] != null)
                                orders = orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id));
                            orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate);

                            foreach (var order in orders.ToList())
                            {
                                worksheet.Cells[row, 1] = order.Order_Id;
                                worksheet.Cells[row, 2] = order.Create_Date.ToString("dd.MM.yyyy HH:mm");
                                worksheet.Cells[row, 3] = order.Pacient.Full_Name;
                                worksheet.Cells[row, 4] = order.Service.Title;
                                worksheet.Cells[row, 5] = order.Order_Status.HasValue && order.Order_Status.Value
                                    ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})"
                                    : "В работе";
                                worksheet.Cells[row, 6] = order.BarCode?.ToString() ?? "Не указан";
                                row++;
                            }
                        }
                        else if (table == "Services")
                        {
                            var services = context.Service.AsQueryable();
                            if (selectedRecordIds.ContainsKey("Services") && selectedRecordIds["Services"] != null)
                                services = services.Where(s => selectedRecordIds["Services"].Contains(s.Service_Id));

                            foreach (var service in services.ToList())
                            {
                                worksheet.Cells[row, 1] = service.Service_Id;
                                worksheet.Cells[row, 2] = service.Title;
                                worksheet.Cells[row, 3] = service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана";
                                worksheet.Cells[row, 4] = service.Deadline;
                                worksheet.Cells[row, 5] = service.Deviation.ToString("P2");
                                row++;
                            }
                        }
                        else if (table == "Users")
                        {
                            var users = context.User.Include(u => u.Role).Include(u => u.Service).Include(u => u.Insurance_Company).AsQueryable();
                            if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"] != null)
                                users = users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id));
                            users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate);

                            foreach (var user in users.ToList())
                            {
                                worksheet.Cells[row, 1] = user.User_Id;
                                worksheet.Cells[row, 2] = user.Full_Name;
                                worksheet.Cells[row, 3] = user.Login;
                                worksheet.Cells[row, 4] = user.Password;
                                worksheet.Cells[row, 5] = user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm");
                                worksheet.Cells[row, 6] = user.Service?.Title ?? "Не указана";
                                worksheet.Cells[row, 7] = user.Insurance_Company?.Title ?? "Не указана";
                                worksheet.Cells[row, 8] = user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан";
                                worksheet.Cells[row, 9] = user.Role.Name;
                                row++;
                            }
                        }

                        // Автонастройка ширины столбцов
                        worksheet.Columns.AutoFit();
                    }

                    // Удаляем лишний лист, созданный по умолчанию
                    if (workbook.Sheets.Count > selectedTables.Count)
                    {
                        Excel.Worksheet defaultSheet = workbook.Sheets[workbook.Sheets.Count];
                        defaultSheet.Delete();
                    }
                }

                // Сохранение файла
                workbook.SaveAs(filePath);
                workbook.Close();
                excelApp.Quit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (workbook != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                if (excelApp != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            }
        }

        private static List<string> GetTableColumns(string table)
        {
            return table switch
            {
                "Patients" => new List<string> { "ID Пациента", "ФИО", "Дата рождения", "Паспорт", "Телефон", "Email", "Полис", "Тип полиса", "Страховая компания" },
                "Orders" => new List<string> { "ID Заказа", "Дата создания", "Пациент", "Услуга", "Статус", "Штрих-код" },
                "Services" => new List<string> { "ID Услуги", "Название", "Цена", "Срок (дни)", "Допуск" },
                "Users" => new List<string> { "ID Пользователя", "ФИО", "Логин", "Пароль", "Последний вход", "Услуга", "Страховая компания", "Счет", "Роль" },
                _ => new List<string>()
            };
        }
    }
}