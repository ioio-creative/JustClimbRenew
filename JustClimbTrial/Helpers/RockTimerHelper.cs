using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JustClimbTrial.Helpers
{
    public class RockTimerHelper
    {
        private DispatcherTimer endTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        private const int endTimerGoal = 30;
        private int endTimerCounter = 0;
        private int endConstantTimerCounter = 0;
        private const int endTimerAllowedLag = 6;
        private bool endHeld = false;

        public RockTimerHelper()
        {

        }

    }
}
