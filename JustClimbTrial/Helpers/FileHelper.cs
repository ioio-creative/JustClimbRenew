using JustClimbTrial.Properties;
using JustClimbTrial.ViewModels;
using System.IO;
using System.Reflection;

namespace JustClimbTrial.Helpers
{
    public class FileHelper
    {       
        private static string exeDirectory = 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static Settings settings = new Settings();

        // Path: exeLocation/videoFileDirectory/RouteNo/VideoNo.extension
        public static string VideoFullPath(RouteVideoViewModel video)
        {
            return Path.Combine(exeDirectory, settings.VideoFileDirectory,
                video.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }
    }
}
