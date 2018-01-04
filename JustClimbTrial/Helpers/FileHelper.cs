using JustClimbTrial.DataAccess;
using JustClimbTrial.Globals;
using JustClimbTrial.Properties;
using JustClimbTrial.ViewModels;
using System.IO;

namespace JustClimbTrial.Helpers
{
    public class FileHelper
    {
        private static string exeDirectory = AppGlobal.ExeDirectory;
        private static Settings settings = new Settings();

        // Path: exeLocation/VideoTempFileDirectory/RandomString.extension
        public static string VideoTempFullPath(EntityType entityType)
        {
            string tmpVideoKey = KeyGenerator.GenerateNewKey(entityType);
            return Path.Combine(settings.VideoTempFileDirectory, 
                tmpVideoKey + settings.VideoFileExtension);
        }

        // Path: exeLocation/VideoFileDirectory/RouteNo/VideoNo.extension
        public static string VideoRecordedFullPath(RouteVideoViewModel video)
        {
            return Path.Combine(exeDirectory, settings.VideoRecordedFileDirectory,
                video.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }       

        // Path: exeLocation/VideoResourcesDirectory/
        public static string VideoResourcesFolderPath()
        {
            return Path.Combine(exeDirectory, settings.VideoResourcesDirectory);
        }

        // Path: exeLocation/VideoBufferDirectory/
        public static string VideoBufferFolderPath()
        {    
            //Console.WriteLine(Path.Combine(exeDirectory, settings.VideoBufferDirectory));
            return Path.Combine(exeDirectory, settings.VideoBufferDirectoryUnderAppVideo);
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
