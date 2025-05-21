using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data.Entity;
using System.Globalization;
using word = Microsoft.Office.Interop.Word;

namespace HospitalApp
{
    public static class ExportWord
    {
        public static void GenerateWordReport(List<string> selectedTables, bool isTableFormat,
            Dictionary<string, List<int>> selectedRecordIds, DateTime startDate, DateTime endDate, bool isPdf, string filePath)
        {
            word.Application wordApp = null;
            word.Document doc = null;

            try
            {
                wordApp = new word.Application();
                doc = wordApp.Documents.Add();

                // Настройка шрифта по умолчанию (ГОСТ: Times New Roman, 14)
                doc.Styles[word.WdBuiltinStyle.wdStyleNormal].Font.Name = "Times New Roman";
                doc.Styles[word.WdBuiltinStyle.wdStyleNormal].Font.Size = 14;

                // Заголовок отчета
                word.Paragraph title = doc.Paragraphs.Add();
                title.Range.Text = "ОТЧЕТ\nпо деятельности ГБУЗ \"Поликлиника №20\"\n";
                title.Range.ParagraphFormat.Alignment = word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.Font.Size = 16;
                title.Range.Font.Bold = 1;
                title.Range.InsertParagraphAfter();

                // Введение
                word.Paragraph intro = doc.Paragraphs.Add();
                intro.Range.Text = $"Настоящий отчет содержит информацию о деятельности Государственного бюджетного учреждения здравоохранения \"Поликлиника №20\" за период с " +
                    $"{startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}.\n" +
                    $"В отчете представлены данные по следующим категориям: " +
                    $"{string.Join(", ", selectedTables.Select(t => t == "Patients" ? "Пациенты" : t == "Orders" ? "Заказы" : t == "Services" ? "Услуги" : "Пользователи"))}.\n" +
                    $"Информация представлена в {(isTableFormat ? "табличном" : "текстовом")} формате в соответствии с требованиями ГОСТ 7.32-2017.\n";
                intro.Range.ParagraphFormat.SpaceAfter = 20;
                intro.Range.InsertParagraphAfter();

                using (var context = new HospitalBaseEntities())
                {
                    int sectionNumber = 1;
                    foreach (var table in selectedTables)
                    {
                        var tableTitle = table == "Patients" ? "Пациенты" :
                                        table == "Orders" ? "Заказы" :
                                        table == "Services" ? "Услуги" : "Пользователи";

                        // Новый раздел
                        doc.Paragraphs.Add().Range.InsertBreak(word.WdBreakType.wdSectionBreakNextPage);

                        // Заголовок раздела
                        word.Paragraph sectionTitle = doc.Paragraphs.Add();
                        sectionTitle.Range.Text = $"1.{sectionNumber++} {tableTitle}\n";
                        sectionTitle.Range.Font.Size = 14;
                        sectionTitle.Range.Font.Bold = 1;
                        sectionTitle.Range.ParagraphFormat.SpaceBefore = 20;
                        sectionTitle.Range.ParagraphFormat.SpaceAfter = 10;
                        sectionTitle.Range.InsertParagraphAfter();

                        var columns = GetTableColumns(table);

                        if (!isTableFormat)
                        {
                            // Текстовый формат
                            if (table == "Patients")
                            {
                                var patients = context.Pacient.Include(p => p.Insurance_Company).AsQueryable();
                                if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                    patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                                foreach (var patient in patients.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = $"Пациент зарегистрирован в системе. " +
                                        $"ID: {patient.Pacient_Id}. " +
                                        $"ФИО: {patient.Full_Name}. " +
                                        $"Дата рождения: {patient.Birth_Date:dd.MM.yyyy}. " +
                                        $"Паспорт: {patient.Passport}. " +
                                        $"Телефон: {patient.Phone_Number}. " +
                                        $"Email: {patient.Email}. " +
                                        $"Полис: {patient.Policy}. " +
                                        $"Тип полиса: {patient.Policy_Type}. " +
                                        $"Страховая компания: {(patient.Insurance_Company != null ? patient.Insurance_Company.Title : "Не указана")}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
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
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = $"Заказ зарегистрирован в системе. " +
                                        $"ID: {order.Order_Id}. " +
                                        $"Дата создания: {order.Create_Date:dd.MM.yyyy HH:mm}. " +
                                        $"Пациент: {order.Pacient.Full_Name}. " +
                                        $"Услуга: {order.Service.Title}. " +
                                        $"Статус: {(order.Order_Status.HasValue && order.Order_Status.Value ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})" : "В работе")}. " +
                                        $"Штрих-код: {(order.BarCode.HasValue ? order.BarCode.ToString() : "Не указан")}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Services")
                            {
                                var services = context.Service.AsQueryable();
                                if (selectedRecordIds.ContainsKey("Services") && selectedRecordIds["Services"] != null)
                                    services = services.Where(s => selectedRecordIds["Services"].Contains(s.Service_Id));

                                foreach (var service in services.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = $"Услуга зарегистрирована в системе. " +
                                        $"ID: {service.Service_Id}. " +
                                        $"Название: {service.Title}. " +
                                        $"Цена: {(service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана")}. " +
                                        $"Срок выполнения: {service.Deadline} дней. " +
                                        $"Допуск: {service.Deviation:P2}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
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
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = $"Пользователь зарегистрирован в системе. " +
                                        $"ID: {user.User_Id}. " +
                                        $"ФИО: {user.Full_Name}. " +
                                        $"Логин: {user.Login}. " +
                                        $"Пароль: {user.Password}. " +
                                        $"Последний вход: {user.Last_Login_Date:dd.MM.yyyy HH:mm}. " +
                                        $"Услуга: {(user.Service != null ? user.Service.Title : "Не указана")}. " +
                                        $"Страховая компания: {(user.Insurance_Company != null ? user.Insurance_Company.Title : "Не указана")}. " +
                                        $"Счет: {(user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан")}. " +
                                        $"Роль: {user.Role.Name}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
                                }
                            }
                        }
                        else
                        {
                            // Табличный формат
                            if (table == "Patients")
                            {
                                var patients = context.Pacient.Include(p => p.Insurance_Company).AsQueryable();
                                if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                    patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                                var tableData = patients.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, columns.Count);
                                    SetTableBorders(wordTable);

                                    // Заголовки таблицы
                                    for (int col = 0; col < columns.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = columns[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var patient = tableData[row];
                                        wordTable.Cell(row + 2, 1).Range.Text = patient.Pacient_Id.ToString();
                                        wordTable.Cell(row + 2, 2).Range.Text = patient.Full_Name;
                                        wordTable.Cell(row + 2, 3).Range.Text = patient.Birth_Date.ToString("dd.MM.yyyy");
                                        wordTable.Cell(row + 2, 4).Range.Text = patient.Passport;
                                        wordTable.Cell(row + 2, 5).Range.Text = patient.Phone_Number;
                                        wordTable.Cell(row + 2, 6).Range.Text = patient.Email;
                                        wordTable.Cell(row + 2, 7).Range.Text = patient.Policy;
                                        wordTable.Cell(row + 2, 8).Range.Text = patient.Policy_Type;
                                        wordTable.Cell(row + 2, 9).Range.Text = patient.Insurance_Company?.Title ?? "Не указана";
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Orders")
                            {
                                var orders = context.Order.Include(o => o.Pacient).Include(o => o.Service).AsQueryable();
                                if (selectedRecordIds.ContainsKey("Orders") && selectedRecordIds["Orders"] != null)
                                    orders = orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id));
                                orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate);

                                var tableData = orders.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, columns.Count);
                                    SetTableBorders(wordTable);

                                    // Заголовки таблицы
                                    for (int col = 0; col < columns.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = columns[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var order = tableData[row];
                                        wordTable.Cell(row + 2, 1).Range.Text = order.Order_Id.ToString();
                                        wordTable.Cell(row + 2, 2).Range.Text = order.Create_Date.ToString("dd.MM.yyyy HH:mm");
                                        wordTable.Cell(row + 2, 3).Range.Text = order.Pacient.Full_Name;
                                        wordTable.Cell(row + 2, 4).Range.Text = order.Service.Title;
                                        wordTable.Cell(row + 2, 5).Range.Text = order.Order_Status.HasValue && order.Order_Status.Value
                                            ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})"
                                            : "В работе";
                                        wordTable.Cell(row + 2, 6).Range.Text = order.BarCode?.ToString() ?? "Не указан";
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Services")
                            {
                                var services = context.Service.AsQueryable();
                                if (selectedRecordIds.ContainsKey("Services") && selectedRecordIds["Services"] != null)
                                    services = services.Where(s => selectedRecordIds["Services"].Contains(s.Service_Id));

                                var tableData = services.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, columns.Count);
                                    SetTableBorders(wordTable);

                                    // Заголовки таблицы
                                    for (int col = 0; col < columns.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = columns[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var service = tableData[row];
                                        wordTable.Cell(row + 2, 1).Range.Text = service.Service_Id.ToString();
                                        wordTable.Cell(row + 2, 2).Range.Text = service.Title;
                                        wordTable.Cell(row + 2, 3).Range.Text = service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана";
                                        wordTable.Cell(row + 2, 4).Range.Text = service.Deadline.ToString();
                                        wordTable.Cell(row + 2, 5).Range.Text = service.Deviation.ToString("P2");
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Users")
                            {
                                var users = context.User.Include(u => u.Role).Include(u => u.Service).Include(u => u.Insurance_Company).AsQueryable();
                                if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"] != null)
                                    users = users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id));
                                users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate);

                                var tableData = users.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, columns.Count);
                                    SetTableBorders(wordTable);

                                    // Заголовки таблицы
                                    for (int col = 0; col < columns.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = columns[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var user = tableData[row];
                                        wordTable.Cell(row + 2, 1).Range.Text = user.User_Id.ToString();
                                        wordTable.Cell(row + 2, 2).Range.Text = user.Full_Name;
                                        wordTable.Cell(row + 2, 3).Range.Text = user.Login;
                                        wordTable.Cell(row + 2, 4).Range.Text = user.Password;
                                        wordTable.Cell(row + 2, 5).Range.Text = user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm");
                                        wordTable.Cell(row + 2, 6).Range.Text = user.Service?.Title ?? "Не указана";
                                        wordTable.Cell(row + 2, 7).Range.Text = user.Insurance_Company?.Title ?? "Не указана";
                                        wordTable.Cell(row + 2, 8).Range.Text = user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан";
                                        wordTable.Cell(row + 2, 9).Range.Text = user.Role.Name;
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                        }
                    }
                }

                // Сохранение документа
                if (isPdf)
                {
                    doc.SaveAs2(filePath, word.WdSaveFormat.wdFormatPDF);
                }
                else
                {
                    doc.SaveAs2(filePath);
                }
                doc.Close();
                wordApp.Quit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (doc != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                if (wordApp != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
            }
        }

        private static void SetTableBorders(word.Table wordTable)
        {
            wordTable.Borders[word.WdBorderType.wdBorderTop].LineStyle = word.WdLineStyle.wdLineStyleSingle;
            wordTable.Borders[word.WdBorderType.wdBorderBottom].LineStyle = word.WdLineStyle.wdLineStyleSingle;
            wordTable.Borders[word.WdBorderType.wdBorderLeft].LineStyle = word.WdLineStyle.wdLineStyleSingle;
            wordTable.Borders[word.WdBorderType.wdBorderRight].LineStyle = word.WdLineStyle.wdLineStyleSingle;
            wordTable.Borders[word.WdBorderType.wdBorderHorizontal].LineStyle = word.WdLineStyle.wdLineStyleSingle;
            wordTable.Borders[word.WdBorderType.wdBorderVertical].LineStyle = word.WdLineStyle.wdLineStyleSingle;
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