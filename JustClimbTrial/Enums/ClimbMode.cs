using System;
using System.Collections.Generic;
using JustClimbTrial.DataAccess.Entities;

namespace JustClimbTrial.Enums
{
    public enum ClimbMode
    {
        Boulder,
        Training
    }

    public class ClimbModeGlobals
    {
        public static Dictionary<ClimbMode, string> StringDict = 
            new Dictionary<ClimbMode, string>
            {
                { ClimbMode.Boulder, "Boulder" },
                { ClimbMode.Training, "Training" }
            };

        public static Dictionary<ClimbMode, Func<string, int>> LargestRouteNoByWallFuncDict =
            new Dictionary<ClimbMode, Func<string, int>>
            {
                { ClimbMode.Boulder, BoulderRouteDataAccess.LargestBoulderRouteNoByWall },
                { ClimbMode.Training, TrainingRouteDataAccess.LargestTrainingRouteNoByWall }
            };
    }
}
