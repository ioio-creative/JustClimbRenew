using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JustClimbTrial.Helpers
{
    public class RockTimerHelper : DispatcherTimer
    {
        private readonly int RockTimerGoal;
        private int rockTimerCounter = 0;
        private int rockConstantTimerCounter = 0;
        private readonly int RockTimerAllowedLag;
        public bool IsTickHandlerSubed = false;

        public RockTimerHelper(int goal = 7, int lag = 3, int msInterval = 100) : base()
        {
            Interval = TimeSpan.FromMilliseconds(msInterval); 
            RockTimerGoal = goal;
            RockTimerAllowedLag = lag;

            Tick += (sender, e) =>
            {
                rockConstantTimerCounter++;
            };
        }

        public RockTimerHelper(int manualInterval) : this(msInterval: manualInterval) { }

        public void RockTimerCountIncr()
        {
            rockTimerCounter++;
        }

        public void Reset()
        {
            Stop();
            rockConstantTimerCounter = 0;
            rockTimerCounter = 0;
        }

        public bool IsTimerGoalReached()
        {
            return (rockTimerCounter == RockTimerGoal);
        }

        public bool IsLagThresholdExceeded()
        {
            return (rockConstantTimerCounter - rockTimerCounter >= RockTimerAllowedLag);
        }
    }
}
