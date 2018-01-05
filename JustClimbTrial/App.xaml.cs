using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Globals;
<<<<<<< HEAD
using JustClimbTrial.Helpers;
using System.IO;
=======
using System;
>>>>>>> cd30385d8bd87e4bda491797bd32917baab0459d
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
                //AppGlobal.WallID = newestValidWall.WallID;
                //Console.WriteLine("Wall Loaded -- WallID: " + AppGlobal.WallID);
                AppGlobal.WallID = "WA2017122912011l7oBf";
            }

            //methods relating to video recording
            Directory.CreateDirectory(FileHelper.VideoBufferFolderPath());
        }
    }
}
