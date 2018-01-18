using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace JustClimbTrial.Helpers
{
    // https://coderelief.net/2009/05/21/frame-based-animation-in-wpf/
    public static class ImageSourceHelper
    {
        private static ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();

        public static ImageSource GetImageSource(string path)
        {
            return (ImageSource)_imageSourceConverter.ConvertFromString(path);               
        }
    }
}
