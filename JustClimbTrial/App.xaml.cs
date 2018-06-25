using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace JustClimbTrial
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Wall newestValidWall = WallDataAccess.NewestValidWall;
            if (newestValidWall != null)
            {
                AppGlobal.WallID = newestValidWall.WallID;
                Debug.WriteLine("Wall Loaded -- WallID: " + AppGlobal.WallID);
                //AppGlobal.WallID = "WA201801161851jhcCKA";
            }

            //methods relating to video recording
            Directory.CreateDirectory(FileHelper.VideoBufferFolderPath());
        }
    }
}
