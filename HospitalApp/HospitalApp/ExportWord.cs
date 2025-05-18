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
            Dictionary<string, List<int>> selectedRecordIds, Dictionary<string, List<string>> selectedColumns,
            DateTime startDate, DateTime endDate, bool isPdf)
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

                using (var context = HospitalBaseEntities.GetContext())
                {
                    int sectionNumber = 1;
                    foreach (var table in selectedTables)
                    {
                        var tableTitle = table == "Patients" ? "Пациенты" :
                                        table == "Orders" ? "Заказы" :
                                        table == "Services" ? "Услуги" : "Пользователи";

                        // Заголовок раздела
                        word.Paragraph sectionTitle = doc.Paragraphs.Add();
                        sectionTitle.Range.Text = $"1.{sectionNumber++} {tableTitle}\n";
                        sectionTitle.Range.Font.Size = 14;
                        sectionTitle.Range.Font.Bold = 1;
                        sectionTitle.Range.ParagraphFormat.SpaceBefore = 20;
                        sectionTitle.Range.ParagraphFormat.SpaceAfter = 10;
                        sectionTitle.Range.InsertParagraphAfter();

                        if (!isTableFormat)
                        {
                            // Текстовое описание от третьего лица
                            if (table == "Patients")
                            {
                                var patients = context.Pacient.Include("Insurance_Company").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                    patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                                var selectedCols = selectedColumns.ContainsKey("Пациенты") ? selectedColumns["Пациенты"] : new List<string> { "ФИО", "Дата рождения", "Телефон", "Полис", "Тип полиса", "Страховая компания" };
                                foreach (var patient in patients.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = "Пациент зарегистрирован в системе. ";
                                    if (selectedCols.Contains("ФИО"))
                                        p.Range.Text += $"ФИО: {patient.Full_Name}. ";
                                    if (selectedCols.Contains("Дата рождения"))
                                        p.Range.Text += $"Дата рождения: {patient.Birth_Date:dd.MM.yyyy}. ";
                                    if (selectedCols.Contains("Телефон"))
                                        p.Range.Text += $"Номер телефона: {patient.Phone_Number}. ";
                                    if (selectedCols.Contains("Полис"))
                                        p.Range.Text += $"Номер полиса: {patient.Policy}. ";
                                    if (selectedCols.Contains("Тип полиса"))
                                        p.Range.Text += $"Тип полиса: {patient.Policy_Type}. ";
                                    if (selectedCols.Contains("Страховая компания"))
                                        p.Range.Text += $"Страховая компания: {(patient.Insurance_Company != null ? patient.Insurance_Company.Title : "Не указана")}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Orders")
                            {
                                var orders = context.Order.Include("Pacient").Include("Service").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Orders") && selectedRecordIds["Orders"] != null)
                                    orders = orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id));
                                orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate);

                                var selectedCols = selectedColumns.ContainsKey("Заказы") ? selectedColumns["Заказы"] : new List<string> { "Штрих-код", "Пациент", "Услуга", "Дата создания", "Статус" };
                                foreach (var currentOrder in orders.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = "Заказ зарегистрирован в системе. ";
                                    if (selectedCols.Contains("Штрих-код"))
                                        p.Range.Text += $"Штрих-код: {currentOrder.BarCode}. ";
                                    if (selectedCols.Contains("Пациент"))
                                        p.Range.Text += $"Пациент: {currentOrder.Pacient.Full_Name}. ";
                                    if (selectedCols.Contains("Услуга"))
                                        p.Range.Text += $"Услуга: {currentOrder.Service.Title}. ";
                                    if (selectedCols.Contains("Дата создания"))
                                        p.Range.Text += $"Дата создания: {currentOrder.Create_Date:dd.MM.yyyy HH:mm}. ";
                                    if (selectedCols.Contains("Статус"))
                                        p.Range.Text += $"Статус: {(currentOrder.Order_Status.HasValue && currentOrder.Order_Status.Value ? $"Проанализировано ({(currentOrder.Complete_Time.HasValue ? currentOrder.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})" : "В работе")}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Services")
                            {
                                var services = context.Service.AsQueryable();
                                if (selectedRecordIds.ContainsKey("Services") && selectedRecordIds["Services"] != null)
                                    services = services.Where(s => selectedRecordIds["Services"].Contains(s.Service_Id));

                                var selectedCols = selectedColumns.ContainsKey("Услуги") ? selectedColumns["Услуги"] : new List<string> { "Название", "Цена", "Срок (дни)", "Допуск" };
                                foreach (var service in services.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = "Услуга зарегистрирована в системе. ";
                                    if (selectedCols.Contains("Название"))
                                        p.Range.Text += $"Название: {service.Title}. ";
                                    if (selectedCols.Contains("Цена"))
                                        p.Range.Text += $"Цена: {string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price)}. ";
                                    if (selectedCols.Contains("Срок (дни)"))
                                        p.Range.Text += $"Срок выполнения: {service.Deadline} дней. ";
                                    if (selectedCols.Contains("Допуск"))
                                        p.Range.Text += $"Допуск: {service.Deviation:P2}. ";
                                    p.Range.ParagraphFormat.SpaceAfter = 10;
                                    p.Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Users")
                            {
                                var users = context.User.Include("Role").Include("Service").Include("Insurance_Company").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"] != null)
                                    users = users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id));
                                users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate);

                                var selectedCols = selectedColumns.ContainsKey("Пользователи") ? selectedColumns["Пользователи"] : new List<string> { "ФИО", "Роль", "Логин", "Услуга", "Страховая компания", "Последний вход" };
                                foreach (var user in users.ToList())
                                {
                                    word.Paragraph p = doc.Paragraphs.Add();
                                    p.Range.Text = "Пользователь зарегистрирован в системе. ";
                                    if (selectedCols.Contains("ФИО"))
                                        p.Range.Text += $"ФИО: {user.Full_Name}. ";
                                    if (selectedCols.Contains("Роль"))
                                        p.Range.Text += $"Роль: {user.Role.Name}. ";
                                    if (selectedCols.Contains("Логин"))
                                        p.Range.Text += $"Логин: {user.Login}. ";
                                    if (selectedCols.Contains("Услуга"))
                                        p.Range.Text += $"Услуга: {(user.Service != null ? user.Service.Title : "Не указана")}. ";
                                    if (selectedCols.Contains("Страховая компания"))
                                        p.Range.Text += $"Страховая компания: {(user.Insurance_Company != null ? user.Insurance_Company.Title : "Не указана")}. ";
                                    if (selectedCols.Contains("Последний вход"))
                                        p.Range.Text += $"Последний вход: {user.Last_Login_Date:dd.MM.yyyy HH:mm}. ";
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
                                var patients = context.Pacient.Include("Insurance_Company").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Patients") && selectedRecordIds["Patients"] != null)
                                    patients = patients.Where(p => selectedRecordIds["Patients"].Contains(p.Pacient_Id));

                                var selectedCols = selectedColumns.ContainsKey("Пациенты") ? selectedColumns["Пациенты"] : new List<string> { "ФИО", "Дата рождения", "Телефон", "Полис", "Тип полиса", "Страховая компания" };
                                var tableData = patients.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, selectedCols.Count);
                                    wordTable.Borders[word.WdBorderType.wdBorderTop].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderBottom].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderLeft].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderRight].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderHorizontal].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderVertical].LineStyle = word.WdLineStyle.wdLineStyleSingle;

                                    // Заголовки таблицы
                                    for (int col = 0; col < selectedCols.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = selectedCols[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var patient = tableData[row];
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
                                            wordTable.Cell(row + 2, colIndex++).Range.Text = cellValue;
                                        }
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Orders")
                            {
                                var orders = context.Order.Include("Pacient").Include("Service").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Orders") && selectedRecordIds["Orders"] != null)
                                    orders = orders.Where(o => selectedRecordIds["Orders"].Contains(o.Order_Id));
                                orders = orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate);

                                var selectedCols = selectedColumns.ContainsKey("Заказы") ? selectedColumns["Заказы"] : new List<string> { "Штрих-код", "Пациент", "Услуга", "Дата создания", "Статус" };
                                var tableData = orders.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, selectedCols.Count);
                                    wordTable.Borders[word.WdBorderType.wdBorderTop].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderBottom].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderLeft].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderRight].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderHorizontal].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderVertical].LineStyle = word.WdLineStyle.wdLineStyleSingle;

                                    // Заголовки таблицы
                                    for (int col = 0; col < selectedCols.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = selectedCols[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var order = tableData[row];
                                        int colIndex = 1;
                                        foreach (var col in selectedCols)
                                        {
                                            string cellValue = col switch
                                            {
                                                "Штрих-код" => order.BarCode.ToString(),
                                                "Пациент" => order.Pacient.Full_Name,
                                                "Услуга" => order.Service.Title,
                                                "Дата создания" => order.Create_Date.ToString("dd.MM.yyyy HH:mm"),
                                                "Статус" => order.Order_Status.HasValue && order.Order_Status.Value
                                                    ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})"
                                                    : "В работе",
                                                _ => ""
                                            };
                                            wordTable.Cell(row + 2, colIndex++).Range.Text = cellValue;
                                        }
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

                                var selectedCols = selectedColumns.ContainsKey("Услуги") ? selectedColumns["Услуги"] : new List<string> { "Название", "Цена", "Срок (дни)", "Допуск" };
                                var tableData = services.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, selectedCols.Count);
                                    wordTable.Borders[word.WdBorderType.wdBorderTop].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderBottom].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderLeft].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderRight].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderHorizontal].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderVertical].LineStyle = word.WdLineStyle.wdLineStyleSingle;

                                    // Заголовки таблицы
                                    for (int col = 0; col < selectedCols.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = selectedCols[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var service = tableData[row];
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
                                            wordTable.Cell(row + 2, colIndex++).Range.Text = cellValue;
                                        }
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                            else if (table == "Users")
                            {
                                var users = context.User.Include("Role").Include("Service").Include("Insurance_Company").AsQueryable();
                                if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"] != null)
                                    users = users.Where(u => selectedRecordIds["Users"].Contains(u.User_Id));
                                users = users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate);

                                var selectedCols = selectedColumns.ContainsKey("Пользователи") ? selectedColumns["Пользователи"] : new List<string> { "ФИО", "Роль", "Логин", "Услуга", "Страховая компания", "Последний вход" };
                                var tableData = users.ToList();
                                if (tableData.Any())
                                {
                                    word.Table wordTable = doc.Tables.Add(doc.Paragraphs[doc.Paragraphs.Count].Range, tableData.Count + 1, selectedCols.Count);
                                    wordTable.Borders[word.WdBorderType.wdBorderTop].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderBottom].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderLeft].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderRight].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderHorizontal].LineStyle = word.WdLineStyle.wdLineStyleSingle;
                                    wordTable.Borders[word.WdBorderType.wdBorderVertical].LineStyle = word.WdLineStyle.wdLineStyleSingle;

                                    // Заголовки таблицы
                                    for (int col = 0; col < selectedCols.Count; col++)
                                    {
                                        wordTable.Cell(1, col + 1).Range.Text = selectedCols[col];
                                        wordTable.Cell(1, col + 1).Range.Font.Bold = 1;
                                    }

                                    // Данные таблицы
                                    for (int row = 0; row < tableData.Count; row++)
                                    {
                                        var user = tableData[row];
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
                                            wordTable.Cell(row + 2, colIndex++).Range.Text = cellValue;
                                        }
                                    }
                                    wordTable.Rows[1].Range.Font.Bold = 1;
                                    doc.Paragraphs.Add().Range.InsertParagraphAfter();
                                }
                            }
                        }
                    }
                }

                // Сохранение документа
                string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\Report_{DateTime.Now:yyyyMMdd_HHmmss}.{(isPdf ? "pdf" : "docx")}";
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

                System.Diagnostics.Process.Start(filePath);
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
    }
}