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

        // config
        private static Settings settings = new Settings();
        public static string FfmpegExePath { get; }


        static AppGlobal()
        {
            FfmpegExePath = settings.FfmpegExePath;
        }

        public static bool DEBUG { get; set; }
    }
}
