using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Globalization;

namespace HospitalApp
{
    public static class CaptchaGenerator
    {
        private static Random _random = new Random();

        // Генерация случайного текста CAPTCHA
        // length - длина генерируемой строки (по умолчанию 5 символов)
        public static string GenerateCaptchaText(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789abcdefghjklmnpqrstuvwxyz!@#$%^&*()-_=+";
            char[] captcha = new char[length];
            for (int i = 0; i < length; i++)
                captcha[i] = chars[_random.Next(chars.Length)];
            return new string(captcha);
        }

        // Генерация изображения CAPTCHA на основе текста
        // captchaText - текст для отображения на CAPTCHA
        public static BitmapImage GenerateCaptchaImage(string captchaText)
        {
            int width = 150, height = 50;
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                Typeface typeface = new Typeface("Arial"); 
                FormattedText formattedText = new FormattedText( 
                    captchaText,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    30,
                    Brushes.Black,
                    1.0);
                dc.DrawText(formattedText, new Point(15, 5));

                for (int i = 0; i < 20; i++)
                {
                    double x = _random.Next(width);
                    double y = _random.Next(height);
                    dc.DrawRectangle(Brushes.Gray, null, new Rect(x, y, 2, 2));
                }
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                width, height,                      
                96, 96,                             
                PixelFormats.Pbgra32);              
            bitmap.Render(visual);                  

            return ConvertBitmapToBitmapImage(bitmap);
        }

        // Конвертация BitmapSource в BitmapImage (для совместимости с WPF)
        // bitmap - исходное изображение в формате BitmapSource
        private static BitmapImage ConvertBitmapToBitmapImage(BitmapSource bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memory);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memory.ToArray());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}