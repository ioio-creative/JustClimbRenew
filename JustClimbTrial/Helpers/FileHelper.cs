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

        // Path: exeLocation/VideoTempDirectory/RandomString.extension
        public static string VideoTempFileFullPath(EntityType entityType)
        {
            string tmpVideoKey = KeyGenerator.GenerateNewKey(entityType);
            return Path.Combine(exeDirectory, settings.VideoTempDirectory, 
                tmpVideoKey + settings.VideoFileExtension);
        }

        // Path: exeLocation/BoulderRouteVideoRecordedDirectory/RouteNo/VideoNo.extension
        public static string BoulderRouteVideoRecordedFullPath(RouteVideoViewModel video)
        {
            return Path.Combine(exeDirectory, settings.BoulderRouteVideoRecordedDirectory,
                video.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }
        
        public static string BoulderRouteVideoRecordedFullPath(
            BoulderRoute route, BoulderRouteVideo video)
        {
            return Path.Combine(exeDirectory, settings.BoulderRouteVideoRecordedDirectory,
                route.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }

        // Path: exeLocation/TrainingRouteVideoRecordedDirectory/RouteNo/VideoNo.extension
        public static string TrainingRouteVideoRecordedFullPath(RouteVideoViewModel video)
        {
            return Path.Combine(exeDirectory, settings.TrainingRouteVideoRecordedDirectory,
                video.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }

        public static string TrainingRouteVideoRecordedFullPath(
            TrainingRoute route, TrainingRouteVideo video)
        {
            return Path.Combine(exeDirectory, settings.BoulderRouteVideoRecordedDirectory,
                route.RouteNo, video.VideoNo + settings.VideoFileExtension);
        }

        // Path: exeLocation/VideoResourcesDirectory/
        public static string VideoResourcesFolderPath()
        {
            return Path.Combine(exeDirectory, settings.VideoResourcesDirectory);
        }

        // Path: exeLocation/VideoBufferDirectory/
        public static string VideoBufferFolderPath()
        {                
            return Path.Combine(exeDirectory, settings.VideoBufferDirectory);
        }

        // Path: exeLocation/VideoDirectory/ImgSequenceDirectory/
        public static string ImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.ImgSequenceDirectory);
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
