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
        private DispatcherTimer rockTimer;
        public TimeSpan Interval
        {
            get { return rockTimer.Interval; }
            set { rockTimer.Interval = value; }
        }
        public bool IsEnabled
        {
            get { return rockTimer.IsEnabled; }
            set { rockTimer.IsEnabled = value; }
        }

        private readonly int RockTimerGoal;
        private int rockTimerCounter = 0;
        private int rockConstantTimerCounter = 0;
        private readonly int RockTimerAllowedLag;

        public event EventHandler Tick;

        public RockTimerHelper(int goal, int lag, int msInterval = 100)
        {
            Interval = TimeSpan.FromMilliseconds(msInterval);
            rockTimer = new DispatcherTimer { Interval = Interval };
            RockTimerGoal = goal;
            RockTimerAllowedLag = lag;

            rockTimer.Tick += (sender, e) =>
            {
                Tick?.Invoke(sender, e);
            };
        }

        public void AddRockTimerTickHandler(EventHandler anEventHandler)
        {
            rockTimer.Tick += anEventHandler;
        }

        public RockTimerHelper() : this(7, 3) { }

        public RockTimerHelper(int msInterval) : this(7, 3, msInterval) { }

        public void Start()
        {
            rockTimer.Start();
        }

        public void Stop()
        {
            rockTimer.Stop();
        }

        
    }
}
