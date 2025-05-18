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
            Dictionary<string, List<int>> selectedRecordIds, Dictionary<string, List<string>> selectedColumns,
            DateTime startDate, DateTime endDate)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Add();
                worksheet = workbook.ActiveSheet;

                int row = 1;
                using (var context = HospitalBaseEntities.GetContext())
                {
                    foreach (var table in selectedTables)
                    {
                        var tableTitle = table == "Patients" ? "Пациенты" :
                                        table == "Orders" ? "Заказы" :
                                        table == "Services" ? "Услуги" : "Пользователи";

                        // Заголовок таблицы
                        worksheet.Cells[row, 1] = tableTitle;
                        worksheet.Cells[row, 1].Font.Bold = true;
                        worksheet.Cells[row, 1].Font.Size = 14;
                        row += 2;

                        var selectedCols = selectedColumns.ContainsKey("Пациенты") ? selectedColumns["Пациенты"] : new List<string> { "ФИО", "Дата рождения", "Телефон", "Полис", "Тип полиса", "Страховая компания" };
                        // Заголовки столбцов
                        for (int col = 0; col < selectedCols.Count; col++)
                        {
                            worksheet.Cells[row, col + 1] = selectedCols[col];
                            worksheet.Cells[row, col + 1].Font.Bold = true;
                        }
                        row++;

                        // Данные
                        if (table == "Patients")
                        {
                            var patients = context.Pacient.Include("Insurance_Company").AsQueryable();
                            if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                            foreach (var patient in patients.ToList())
                            {
                                int colIndex = 1;
                                foreach (var col in selectedCols)
                                {
                                    string cellValue = col switch
                                    {
                                        "ФИО" => patient.Full_Name,
                                        "Дата рождения" => patient.Birth_Date.ToString("dd.MM.yyyy"),
                                        "Телефон" => patient.Phone_Number,
                                        "Полис" => patient.Policy,
                                        "Тип полиса" => patient.Policy_Type,
                                        "Страховая компания" => patient.Insurance_Company?.Title ?? "Не указана",
                                        _ => ""
                                    };
                                    worksheet.Cells[row, colIndex++] = cellValue;
                                }
                                row++;
                            }
                        }
                        else if (table == "Orders")
                        {
                            var orders = context.Order.Include("Pacient").Include("Service").AsQueryable();
                            if (selectedRecordIds.ContainsKey("Orders") && selectedRecordIds["Orders"] != null)
                                orders = orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id));
                            orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate);

                            foreach (var order in orders.ToList())
                            {
                                int colIndex = 1;
                                foreach (var col in selectedCols)
                                {
                                    string cellValue = col switch
                                    {
                                        "Штрих-код" => order.BarCode.ToString(),
                                        "Пациент" => order.Pacient.Full_Name,
                                        "Услуга" => order.Service.Title,
                                        "Дата создания" => order.Create_Date.ToString("dd.MM.yyyy HH:mm"),
                                        "Статус" => order.Order_Status.HasValue && order.Order_Status.Value ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})" : "В работе",
                                        _ => ""
                                    };
                                    worksheet.Cells[row, colIndex++] = cellValue;
                                }
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
                                int colIndex = 1;
                                foreach (var col in selectedCols)
                                {
                                    string cellValue = col switch
                                    {
                                        "Название" => service.Title,
                                        "Цена" => string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price),
                                        "Срок (дни)" => service.Deadline.ToString(),
                                        "Допуск" => service.Deviation.ToString("P2"),
                                        _ => ""
                                    };
                                    worksheet.Cells[row, colIndex++] = cellValue;
                                }
                                row++;
                            }
                        }
                        else if (table == "Users")
                        {
                            var users = context.User.Include("Role").Include("Service").Include("Insurance_Company").AsQueryable();
                            if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"] != null)
                                users = users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id));
                            users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate);

                            foreach (var user in users.ToList())
                            {
                                int colIndex = 1;
                                foreach (var col in selectedCols)
                                {
                                    string cellValue = col switch
                                    {
                                        "ФИО" => user.Full_Name,
                                        "Роль" => user.Role.Name,
                                        "Логин" => user.Login,
                                        "Услуга" => user.Service?.Title ?? "Не указана",
                                        "Страховая компания" => user.Insurance_Company?.Title ?? "Не указана",
                                        "Последний вход" => user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm"),
                                        _ => ""
                                    };
                                    worksheet.Cells[row, colIndex++] = cellValue;
                                }
                                row++;
                            }
                        }
                        row++;
                    }
                }

                // Автонастройка ширины столбцов
                worksheet.Columns.AutoFit();

                // Сохранение файла
                string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                workbook.SaveAs(filePath);
                workbook.Close();
                excelApp.Quit();

                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (worksheet != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                if (workbook != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                if (excelApp != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            }
        }

        private static List<string> GetDefaultColumns(string table)
        {
            return table switch
            {
                "Patients" => new List<string> { "ФИО", "Дата рождения", "Телефон", "Полис", "Тип полиса", "Страховая компания" },
                "Orders" => new List<string> { "Штрих-код", "Пациент", "Услуга", "Дата создания", "Статус" },
                "Services" => new List<string> { "Название", "Цена", "Срок (дни)", "Допуск" },
                "Users" => new List<string> { "ФИО", "Роль", "Логин", "Услуга", "Страховая компания", "Последний вход" },
                _ => new List<string>()
            };
        }
    }
}