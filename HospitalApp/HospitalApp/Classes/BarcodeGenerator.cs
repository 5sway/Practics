using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Zen.Barcode;

namespace HospitalApp
{
    public static class BarcodeGenerator
    {
        public static void GenerateBarcodePng(string barcodeValue, string outputPath)
        {
            try
            {
                Code128BarcodeDraw barcode = BarcodeDrawFactory.Code128WithChecksum;
                Image barcodeImage = barcode.Draw(barcodeValue, 50);

                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    barcodeImage.Save(fs, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка генерации штрих-кода: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}