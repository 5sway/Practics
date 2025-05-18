using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;

namespace HospitalApp
{
    public static class BarcodeGenerator
    {
        public static void GenerateBarcodePdf(string barcodeValue, string outputPath)
        {
            try
            {
                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    // Размер страницы в pt (100 мм x 50 мм)
                    Rectangle pageSize = new iTextSharp.text.Rectangle(100f * 2.83465f, 50f * 2.83465f);
                    Document doc = new Document(pageSize);
                    PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    PdfContentByte cb = writer.DirectContent;
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    cb.SetFontAndSize(baseFont, 2.75f);

                    // Параметры штрих-кода (в pt, 1 мм = 2.83465 pt)
                    float barHeight = 22.85f * 2.83465f; // Высота штриха
                    float extendedBarHeight = (22.85f + 1.65f) * 2.83465f; // Высота ограничивающих штрихов
                    float leftQuietZone = 3.63f * 2.83465f; // Левая тихая зона
                    float rightQuietZone = 2.31f * 2.83465f; // Правая тихая зона
                    float x = leftQuietZone;
                    float y = 10f;

                    // Цвета
                    BaseColor barColor = BaseColor.BLACK;
                    BaseColor spaceColor = BaseColor.WHITE;

                    // Фон (белый)
                    cb.SetColorFill(spaceColor);
                    cb.Rectangle(0, 0, pageSize.Width, pageSize.Height);
                    cb.Fill();

                    // Ограничивающий левый штрих
                    cb.SetColorFill(barColor);
                    cb.Rectangle(x, y, 0.15f * 2.83465f, extendedBarHeight);
                    cb.Fill();
                    x += (0.15f + 0.2f) * 2.83465f;

                    // Штрихи для цифр
                    foreach (char digit in barcodeValue)
                    {
                        if (digit == '0')
                        {
                            x += 1.35f * 2.83465f; // Пропуск для 0 (белый штрих)
                        }
                        else
                        {
                            float width = (0.15f * (digit - '0')) * 2.83465f;
                            cb.Rectangle(x, y, width, barHeight);
                            cb.Fill();
                            x += width + (0.2f * 2.83465f);
                        }
                    }

                    // Центральный и правый ограничивающие штрихи
                    cb.Rectangle(x, y, 0.15f * 2.83465f, extendedBarHeight);
                    cb.Fill();
                    x += (0.15f + 0.2f) * 2.83465f;
                    cb.Rectangle(x, y, 0.15f * 2.83465f, extendedBarHeight);
                    cb.Fill();

                    // Проверка правой тихой зоны
                    float totalWidth = x + rightQuietZone;
                    if (totalWidth > pageSize.Width)
                    {
                        throw new Exception("Штрих-код превышает ширину страницы!");
                    }

                    // Числовое представление
                    cb.BeginText();
                    cb.SetTextMatrix(leftQuietZone, y - 3f);
                    cb.ShowText(barcodeValue);
                    cb.EndText();

                    doc.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка генерации штрих-кода: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}