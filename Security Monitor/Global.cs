using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Security_Monitor
{
    public static class Global
    {
        public static BitmapImage ConvertBase64ToImage(string base64)

        {
            if (string.IsNullOrEmpty(base64))
                return null;

            try
            {
                // Убираем префикс вроде "data:image/jpeg;base64,"
                var cleanBase64 = base64;
                var commaIndex = base64.IndexOf(",");
                if (commaIndex != -1)
                    cleanBase64 = base64.Substring(commaIndex + 1);

                byte[] bytes = Convert.FromBase64String(cleanBase64);
                using var ms = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze(); // важно для WPF
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
