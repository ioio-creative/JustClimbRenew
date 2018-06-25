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
            return BoulderRouteVideoRecordedFullPath(video.RouteNo, video.VideoNo);
        }
        
        public static string BoulderRouteVideoRecordedFullPath(
            BoulderRoute route, BoulderRouteVideo video)
        {
            return BoulderRouteVideoRecordedFullPath(route.RouteNo, video.VideoNo);
        }

        public static string BoulderRouteVideoRecordedFullPath(string routeNo, string videoNo)
        {
            return Path.Combine(exeDirectory,
                string.Format(settings.BoulderRouteVideoRecordedFilePathFormat,
                    AppGlobal.MyWall.WallNo, routeNo, videoNo, settings.VideoFileExtension));
        }

        // Path: exeLocation/TrainingRouteVideoRecordedDirectory/VideoNo.extension
        public static string TrainingRouteVideoRecordedFullPath(RouteVideoViewModel video)
        {
            return TrainingRouteVideoRecordedFullPath(video.RouteNo, video.VideoNo);
        }

        public static string TrainingRouteVideoRecordedFullPath(
            TrainingRoute route, TrainingRouteVideo video)
        {
            return TrainingRouteVideoRecordedFullPath(route.RouteNo, video.VideoNo);
        }

        public static string TrainingRouteVideoRecordedFullPath(string routeNo, string videoNo)
        {
            return Path.Combine(exeDirectory,
                string.Format(settings.TrainingRouteVideoRecordedFilePathFormat,
                    AppGlobal.MyWall.WallNo, routeNo, videoNo, settings.VideoFileExtension));
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

        // Path: exeLocation/VideoDirectory/BoulderButtonNormalImgSequenceDirectory/
        public static string BoulderButtonNormalImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonNormalImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonShowImgSequenceDirectory/
        public static string BoulderButtonShowImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonShowImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonFeedbackImgSequenceDirectory/
        public static string BoulderButtonFeedbackImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonFeedbackImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonShinePopImgSequenceDirectory/
        public static string BoulderButtonShinePopImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonShinePopImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonShineLoopImgSequenceDirectory/
        public static string BoulderButtonShineLoopImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonShineLoopImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonShineFeedbackPopImgSequenceDirectory/
        public static string BoulderButtonShineFeedbackPopImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonShineFeedbackPopImgSequenceDirectory);
        }

        // Path: exeLocation/VideoDirectory/BoulderButtonShineFeedbackLoopImgSequenceDirectory/
        public static string BoulderButtonShineFeedbackLoopImgSequenceDirectory()
        {
            return Path.Combine(exeDirectory, settings.BoulderButtonShineFeedbackLoopImgSequenceDirectory);
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

        // Path: exeLocation/GameplayReadyVideoPath
        public static string GameplayReadyVideoPath()
        {
            return Path.Combine(exeDirectory, settings.GameplayReadyVideoPath);
        }

        // Path: exeLocation/GameplayStartVideoPath
        public static string GameplayStartVideoPath()
        {
            return Path.Combine(exeDirectory, settings.GameplayStartVideoPath);
        }

        // Path: exeLocation/GameplayCountdownVideoPath
        public static string GameplayCountdownVideoPath()
        {
            return Path.Combine(exeDirectory, settings.GameplayCountdownVideoPath);
        }

        // Path: exeLocation/GameplayCountdownVideoPath
        public static string GameplayFinishVideoPath()
        {
            return Path.Combine(exeDirectory, settings.GameplayFinishVideoPath);
        }

        public static string GameOverVideoPath()
        {
            return Path.Combine(exeDirectory, settings.GameOverVideoPath);
        }
    }
}
