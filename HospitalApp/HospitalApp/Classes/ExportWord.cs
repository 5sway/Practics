using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data.Entity;
using System.Globalization;
using Word = Microsoft.Office.Interop.Word;

namespace HospitalApp
{
    public static class ExportWord
    {
        public static void GenerateWordReport(List<string> selectedTables, bool isTableFormat,
            Dictionary<string, List<int>> selectedRecordIds, DateTime? startDate, DateTime? endDate, bool isPdf, string filePath)
        {
            Word.Application wordApp = null;
            Word.Document doc = null;

            try
            {
                if (string.IsNullOrEmpty(filePath) || selectedTables == null)
                {
                    MessageBox.Show("Ошибка: некорректные параметры экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Удаляем существующий файл, если он есть
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                wordApp = new Word.Application();
                wordApp.Visible = false;
                wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;
                doc = wordApp.Documents.Add();

                // Настройка стилей документа
                SetDocumentStyles(doc);

                // Добавляем титульный лист
                AddTitlePage(doc, startDate, endDate);

                using (var context = new HospitalBaseEntities())
                {
                    // Фильтрация данных
                    var data = new
                    {
                        Patients = context.Pacient.Include(p => p.Insurance_Company).ToList() ?? new List<Pacient>(),
                        Orders = context.Order.Include(o => o.Pacient).Include(o => o.Service).ToList() ?? new List<Order>(),
                        Services = context.Service.ToList() ?? new List<Service>(),
                        Users = context.User.Include(u => u.Role).Include(u => u.Service).Include(u => u.Insurance_Company).ToList() ?? new List<User>()
                    };

                    // Фильтрация по выбранным записям
                    if (selectedRecordIds != null && selectedRecordIds.Any())
                    {
                        data = new
                        {
                            Patients = data.Patients.Where(p => selectedRecordIds.ContainsKey("Patients")
                                ? selectedRecordIds["Patients"].Contains(p.Pacient_Id)
                                : true).ToList(),
                            Orders = data.Orders.Where(o => selectedRecordIds.ContainsKey("Orders")
                                ? selectedRecordIds["Orders"].Contains(o.Order_Id)
                                : true).ToList(),
                            Services = data.Services.Where(s => selectedRecordIds.ContainsKey("Services")
                                ? selectedRecordIds["Services"].Contains(s.Service_Id)
                                : true).ToList(),
                            Users = data.Users.Where(u => selectedRecordIds.ContainsKey("Users")
                                ? selectedRecordIds["Users"].Contains(u.User_Id)
                                : true).ToList()
                        };
                    }

                    // Фильтрация по периоду
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        data = new
                        {
                            Patients = data.Patients,
                            Orders = data.Orders.Where(o => o.Create_Date >= startDate && o.Create_Date <= endDate).ToList(),
                            Services = data.Services,
                            Users = data.Users.Where(u => u.Last_Login_Date >= startDate && u.Last_Login_Date <= endDate).ToList()
                        };
                    }

                    // Основное содержимое отчета
                    AddReportTitle(doc, startDate, endDate);

                    bool isFirstTable = true;
                    foreach (var table in selectedTables)
                    {
                        if (!isFirstTable && doc.Paragraphs.Count > 1)
                            AddPageBreak(doc);

                        switch (table)
                        {
                            case "Patients":
                                if (data.Patients.Any())
                                {
                                    if (isTableFormat)
                                        ExportPatientsToWordTable(doc, data.Patients);
                                    else
                                        ExportPatientsToWordText(doc, data.Patients);
                                }
                                break;
                            case "Orders":
                                if (data.Orders.Any())
                                {
                                    if (isTableFormat)
                                        ExportOrdersToWordTable(doc, data.Orders);
                                    else
                                        ExportOrdersToWordText(doc, data.Orders);
                                }
                                break;
                            case "Services":
                                if (data.Services.Any())
                                {
                                    if (isTableFormat)
                                        ExportServicesToWordTable(doc, data.Services);
                                    else
                                        ExportServicesToWordText(doc, data.Services);
                                }
                                break;
                            case "Users":
                                if (data.Users.Any())
                                {
                                    if (isTableFormat)
                                        ExportUsersToWordTable(doc, data.Users);
                                    else
                                        ExportUsersToWordText(doc, data.Users);
                                }
                                break;
                        }
                        isFirstTable = false;
                    }

                    // Сохранение в выбранном формате
                    Word.WdSaveFormat saveFormat = isPdf
                        ? Word.WdSaveFormat.wdFormatPDF
                        : Word.WdSaveFormat.wdFormatDocumentDefault;

                    doc.SaveAs2(filePath, saveFormat);
                }

                // Закрытие документа
                object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                doc.Close(ref doNotSave);
                wordApp.Quit();

                ReleaseWordObjects(doc, wordApp);
                OpenExportedFile(filePath);
            }
            catch (Exception ex)
            {
                if (doc != null)
                {
                    object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref doNotSave);
                }
                if (wordApp != null) wordApp.Quit();

                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SetDocumentStyles(Word.Document doc)
        {
            doc.Content.Font.Name = "Times New Roman";
            doc.Content.Font.Size = 14;
            doc.Content.ParagraphFormat.LineSpacing = 18f;
            doc.Content.ParagraphFormat.SpaceBefore = 0;
            doc.Content.ParagraphFormat.SpaceAfter = 0;
            doc.Content.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private static void AddTitlePage(Word.Document doc, DateTime? startDate, DateTime? endDate)
        {
            // Верхний заголовок
            Word.Paragraph header = doc.Paragraphs.Add();
            Word.Range headerRange = header.Range;
            headerRange.Text = "Государственное бюджетное учреждение здравоохранения\n" +
                              "\"Поликлиника №20\"\n" +
                              "г. Санкт-Петербург\n";
            headerRange.Font.Name = "Times New Roman";
            headerRange.Font.Size = 12;
            headerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            headerRange.ParagraphFormat.SpaceAfter = 12;
            headerRange.InsertParagraphAfter();

            // Разделительная линия
            AddSeparator(doc, 24);

            // Название отчета
            Word.Paragraph title = doc.Paragraphs.Add();
            Word.Range titleRange = title.Range;
            titleRange.Text = "ОТЧЕТ\n";
            titleRange.Font.Bold = 1;
            titleRange.Font.Size = 16;
            titleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            titleRange.ParagraphFormat.SpaceAfter = 12;
            titleRange.InsertParagraphAfter();

            // Период отчета
            if (startDate.HasValue && endDate.HasValue)
            {
                Word.Paragraph period = doc.Paragraphs.Add();
                Word.Range periodRange = period.Range;
                periodRange.Text = $"За период: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}\n";
                periodRange.Font.Size = 14;
                periodRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                periodRange.ParagraphFormat.SpaceAfter = 24;
                periodRange.InsertParagraphAfter();
            }

            // Дата и город
            Word.Paragraph footer = doc.Paragraphs.Add();
            Word.Range footerRange = footer.Range;
            footerRange.Text = $"Санкт-Петербург, {DateTime.Now:yyyy}";
            footerRange.Font.Size = 14;
            footerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
            footerRange.InsertParagraphAfter();

            // Разрыв страницы
            AddPageBreak(doc);
        }

        private static void AddReportTitle(Word.Document doc, DateTime? startDate, DateTime? endDate)
        {
            Word.Paragraph title = doc.Paragraphs.Add();
            Word.Range range = title.Range;
            range.Text = startDate.HasValue
                ? $"Отчет за период {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}"
                : "Отчет";
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14;
            range.Font.Bold = 1;
            range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            range.ParagraphFormat.SpaceAfter = 12;
            range.InsertParagraphAfter();
        }

        private static void AddPageBreak(Word.Document doc)
        {
            Word.Paragraph lastParagraph = doc.Paragraphs.Add();
            lastParagraph.Range.InsertBreak(Word.WdBreakType.wdPageBreak);
        }

        private static void AddSeparator(Word.Document doc, int spaceAfter = 0)
        {
            Word.Paragraph separator = doc.Paragraphs.Add();
            Word.Range range = separator.Range;
            range.Text = string.Empty;
            range.ParagraphFormat.Borders[Word.WdBorderType.wdBorderBottom].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            range.ParagraphFormat.Borders[Word.WdBorderType.wdBorderBottom].LineWidth = Word.WdLineWidth.wdLineWidth050pt;
            range.ParagraphFormat.SpaceAfter = spaceAfter;
            range.InsertParagraphAfter();
        }

        private static void AddTableTitle(Word.Document doc, string title)
        {
            Word.Paragraph tableTitle = doc.Paragraphs.Add();
            Word.Range range = tableTitle.Range;
            range.Text = title;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14;
            range.Font.Bold = 1;
            range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            range.ParagraphFormat.SpaceBefore = 12;
            range.ParagraphFormat.SpaceAfter = 6;
            range.InsertParagraphAfter();
        }

        private static void AddSectionTitle(Word.Document doc, string title)
        {
            Word.Paragraph sectionTitle = doc.Paragraphs.Add();
            Word.Range range = sectionTitle.Range;
            range.Text = title;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14;
            range.Font.Bold = 1;
            range.Font.Underline = Word.WdUnderline.wdUnderlineSingle;
            sectionTitle.Format.SpaceBefore = 12;
            sectionTitle.Format.SpaceAfter = 6;
            sectionTitle.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            range.InsertParagraphAfter();
        }

        private static void AddTextParagraph(Word.Document doc, string text, bool bold = false)
        {
            Word.Paragraph paragraph = doc.Paragraphs.Add();
            Word.Range range = paragraph.Range;
            range.Text = text;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 12;
            range.Font.Bold = bold ? 1 : 0;
            paragraph.Format.SpaceBefore = 0;
            paragraph.Format.SpaceAfter = 0;
            paragraph.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            paragraph.Format.FirstLineIndent = 20;
            range.InsertParagraphAfter();
        }

        private static Word.Table CreateWordTable(Word.Document doc, string[] headers)
        {
            Word.Table table = doc.Tables.Add(doc.Range(doc.Content.End - 1), 1, headers.Length);
            for (int i = 0; i < headers.Length; i++)
            {
                table.Cell(1, i + 1).Range.Text = headers[i];
                table.Cell(1, i + 1).Range.Font.Bold = 1;
                table.Cell(1, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }
            return table;
        }

        private static void AddRowToWordTable(Word.Table table, string[] values)
        {
            table.Rows.Add();
            int rowIndex = table.Rows.Count;
            for (int i = 0; i < values.Length; i++)
            {
                table.Cell(rowIndex, i + 1).Range.Text = values[i] ?? "";
                table.Cell(rowIndex, i + 1).Range.Font.Bold = 0;
                table.Cell(rowIndex, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }
        }

        private static void FinalizeWordTable(Word.Table table)
        {
            table.Columns.AutoFit();
            table.Borders.Enable = 1;
            foreach (Word.Row row in table.Rows)
            {
                foreach (Word.Cell cell in row.Cells)
                {
                    cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                }
            }
        }

        private static void ExportPatientsToWordTable(Word.Document doc, List<Pacient> patients)
        {
            AddTableTitle(doc, "Пациенты");
            Word.Table table = CreateWordTable(doc, new string[] { "ID Пациента", "ФИО", "Дата рождения", "Паспорт", "Телефон", "Email", "Полис", "Тип полиса", "Страховая компания" });

            foreach (var patient in patients)
            {
                AddRowToWordTable(table, new string[] {
                    patient.Pacient_Id.ToString(),
                    patient.Full_Name ?? "",
                    patient.Birth_Date.ToString("dd.MM.yyyy"),
                    patient.Passport ?? "",
                    patient.Phone_Number ?? "",
                    patient.Email ?? "",
                    patient.Policy ?? "",
                    patient.Policy_Type ?? "",
                    patient.Insurance_Company?.Title ?? "Не указана"
                });
            }
            FinalizeWordTable(table);
        }

        private static void ExportPatientsToWordText(Word.Document doc, List<Pacient> patients)
        {
            AddSectionTitle(doc, "Пациенты");
            foreach (var patient in patients)
            {
                AddTextParagraph(doc, $"ID: {patient.Pacient_Id}", bold: true);
                AddTextParagraph(doc, $"ФИО: {patient.Full_Name ?? "Не указано"}");
                AddTextParagraph(doc, $"Дата рождения: {patient.Birth_Date:dd.MM.yyyy}");
                AddTextParagraph(doc, $"Паспорт: {patient.Passport ?? "Не указан"}");
                AddTextParagraph(doc, $"Телефон: {patient.Phone_Number ?? "Не указан"}");
                AddTextParagraph(doc, $"Email: {patient.Email ?? "Не указан"}");
                AddTextParagraph(doc, $"Полис: {patient.Policy ?? "Не указан"}");
                AddTextParagraph(doc, $"Тип полиса: {patient.Policy_Type ?? "Не указан"}");
                AddTextParagraph(doc, $"Страховая компания: {(patient.Insurance_Company != null ? patient.Insurance_Company.Title : "Не указана")}");
                AddSeparator(doc);
            }
        }

        private static void ExportOrdersToWordTable(Word.Document doc, List<Order> orders)
        {
            AddTableTitle(doc, "Заказы");
            Word.Table table = CreateWordTable(doc, new string[] { "ID Заказа", "Дата создания", "Пациент", "Услуга", "Статус", "Штрих-код" });

            foreach (var order in orders)
            {
                AddRowToWordTable(table, new string[] {
                    order.Order_Id.ToString(),
                    order.Create_Date.ToString("dd.MM.yyyy HH:mm"),
                    order.Pacient?.Full_Name ?? "Неизвестно",
                    order.Service?.Title ?? "Неизвестно",
                    order.Order_Status.HasValue && order.Order_Status.Value
                        ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})"
                        : "В работе",
                    order.BarCode?.ToString() ?? "Не указан"
                });
            }
            FinalizeWordTable(table);
        }

        private static void ExportOrdersToWordText(Word.Document doc, List<Order> orders)
        {
            AddSectionTitle(doc, "Заказы");
            foreach (var order in orders)
            {
                AddTextParagraph(doc, $"ID: {order.Order_Id}", bold: true);
                AddTextParagraph(doc, $"Дата создания: {order.Create_Date:dd.MM.yyyy HH:mm}");
                AddTextParagraph(doc, $"Пациент: {order.Pacient?.Full_Name ?? "Неизвестно"}");
                AddTextParagraph(doc, $"Услуга: {order.Service?.Title ?? "Неизвестно"}");
                AddTextParagraph(doc, $"Статус: {(order.Order_Status.HasValue && order.Order_Status.Value ? $"Проанализировано ({(order.Complete_Time.HasValue ? order.Complete_Time.Value.ToString("dd.MM.yyyy HH:mm") : "Не указано")})" : "В работе")}");
                AddTextParagraph(doc, $"Штрих-код: {(order.BarCode.HasValue ? order.BarCode.ToString() : "Не указан")}");
                AddSeparator(doc);
            }
        }

        private static void ExportServicesToWordTable(Word.Document doc, List<Service> services)
        {
            AddTableTitle(doc, "Услуги");
            Word.Table table = CreateWordTable(doc, new string[] { "ID Услуги", "Название", "Цена", "Срок (дни)", "Допуск" });

            foreach (var service in services)
            {
                AddRowToWordTable(table, new string[] {
                    service.Service_Id.ToString(),
                    service.Title ?? "",
                    service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана",
                    service.Deadline.ToString(),
                    service.Deviation.ToString("P2")
                });
            }
            FinalizeWordTable(table);
        }

        private static void ExportServicesToWordText(Word.Document doc, List<Service> services)
        {
            AddSectionTitle(doc, "Услуги");
            foreach (var service in services)
            {
                AddTextParagraph(doc, $"ID: {service.Service_Id}", bold: true);
                AddTextParagraph(doc, $"Название: {service.Title ?? "Не указано"}");
                AddTextParagraph(doc, $"Цена: {(service.Price.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", service.Price) : "Не указана")}");
                AddTextParagraph(doc, $"Срок выполнения: {service.Deadline} дней");
                AddTextParagraph(doc, $"Допуск: {service.Deviation:P2}");
                AddSeparator(doc);
            }
        }

        private static void ExportUsersToWordTable(Word.Document doc, List<User> users)
        {
            AddTableTitle(doc, "Пользователи");
            Word.Table table = CreateWordTable(doc, new string[] { "ID Пользователя", "ФИО", "Логин", "Пароль", "Последний вход", "Услуга", "Страховая компания", "Счет", "Роль" });

            foreach (var user in users)
            {
                AddRowToWordTable(table, new string[] {
                    user.User_Id.ToString(),
                    user.Full_Name ?? "",
                    user.Login ?? "",
                    user.Password ?? "",
                    user.Last_Login_Date.ToString("dd.MM.yyyy HH:mm"),
                    user.Service?.Title ?? "Не указана",
                    user.Insurance_Company?.Title ?? "Не указана",
                    user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан",
                    user.Role?.Name ?? "Неизвестно"
                });
            }
            FinalizeWordTable(table);
        }

        private static void ExportUsersToWordText(Word.Document doc, List<User> users)
        {
            AddSectionTitle(doc, "Пользователи");
            foreach (var user in users)
            {
                AddTextParagraph(doc, $"ID: {user.User_Id}", bold: true);
                AddTextParagraph(doc, $"ФИО: {user.Full_Name ?? "Не указано"}");
                AddTextParagraph(doc, $"Логин: {user.Login ?? "Не указан"}");
                AddTextParagraph(doc, $"Пароль: {user.Password ?? "Не указан"}");
                AddTextParagraph(doc, $"Последний вход: {user.Last_Login_Date:dd.MM.yyyy HH:mm}");
                AddTextParagraph(doc, $"Услуга: {(user.Service != null ? user.Service.Title : "Не указана")}");
                AddTextParagraph(doc, $"Страховая компания: {(user.Insurance_Company != null ? user.Insurance_Company.Title : "Не указана")}");
                AddTextParagraph(doc, $"Счет: {(user.Account.HasValue ? string.Format(CultureInfo.CurrentCulture, "{0:C2}", user.Account) : "Не указан")}");
                AddTextParagraph(doc, $"Роль: {user.Role?.Name ?? "Неизвестно"}");
                AddSeparator(doc);
            }
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void ReleaseWordObjects(params object[] objects)
        {
            foreach (var obj in objects)
            {
                try
                {
                    if (obj != null && System.Runtime.InteropServices.Marshal.IsComObject(obj))
                    {
                        while (System.Runtime.InteropServices.Marshal.ReleaseComObject(obj) > 0) { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при освобождении COM-объекта: {ex.Message}");
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}