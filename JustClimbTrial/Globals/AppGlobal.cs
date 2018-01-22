using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Properties;
using System.IO;
using System.Reflection;

namespace JustClimbTrial.Globals
{
    public class AppGlobal
    {
        // app environment
        public static string ExeDirectory =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);        

        // data
        public static string WallID { get; set; }
        public static Wall MyWall
        {
            get
            {
                return WallDataAccess.WallById(WallID);
            }
        }        

        // config
        private static Settings settings = new Settings();
        public static string FfmpegExePath { get; }
        public static int MaxVideoRecordDurationInMinutes { get; }
        public static bool DEBUG { get; }
        public static bool WAF { get; }

        static AppGlobal()
        {
            FfmpegExePath = settings.FfmpegExePath;
            MaxVideoRecordDurationInMinutes = settings.MaxVideoRecordDurationInMinutes;
            DEBUG = settings.IsDebugMode;
            WAF = settings.WandF;
        }        
    }
}
