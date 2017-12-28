using System;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Extensions
{
    public static class ImageControlExtension
    {
        public static void SetSourceByPath(this System.Windows.Controls.Image img, string path)
        {
            Uri wallLogImgUri = new Uri(path);
            BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);
            img.Source = wallLogImg; 
        }
    }
}
