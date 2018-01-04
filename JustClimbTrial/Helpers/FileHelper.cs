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
            return Path.Combine(exeDirectory, settings.VideoDirectory,
                video.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }

        // Path: exeLocation/VideoDirectory/VideoResourcesDirectory/
        public static string VideoResourcesFolderPath()
        {
            return Path.Combine(exeDirectory, settings.VideoDirectory, settings.VideoResourcesDirectory);
        }

        // Path: exeLocation/VideoDirectory/VideoBufferDirectory/
        public static string VideoBufferFolderPath()
        {    
            return Path.Combine(exeDirectory, settings.VideoDirectory, settings.VideoBufferDirectory);
        }

        //Path: exeLocation/VideoDirectory/PngSequenceDirectory/
        public static string PngSequencesFolderPath()
        {
            return Path.Combine(exeDirectory, settings.VideoDirectory, settings.PngSequenceDirectory);
        }

        // Path: exeLocation/WallLogDirectory/
        public static string WallLogFolderPath()
        {
            return Path.Combine(exeDirectory, settings.WallLogDirectory);
        }

        public static string WallLogImagePath(string wallId)
        {
            return Path.Combine(WallLogFolderPath(), wallId, wallId + ".png");
        }
    }
}
