using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Globals;
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
                //AppGlobal.WallID = "WA2017121416029xmq7H";
            }
        }
    }
}
