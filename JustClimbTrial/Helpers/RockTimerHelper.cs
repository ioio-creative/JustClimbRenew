﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace JustClimbTrial.Helpers
{
    public class RockTimerHelper
    {
        private readonly int RockTimerGoal;
        private int rockTimerCounter = 0;
        private int rockConstantTimerCounter = 0;
        private readonly int RockTimerAllowedLag;
        public bool IsTickHandlerSubed
        {
            get
            {
                return tickHandlers.Count > 0;
            }
        }
        public bool IsEnabled
        {
            get
            {
                return timer.IsEnabled;
            }

        }

        private IList<EventHandler> tickHandlers = new List<EventHandler>();
        private DispatcherTimer timer;

        public RockTimerHelper(int goal = 5, int lag = 3, int msInterval = 100) : base()
        {
            RockTimerGoal = goal;
            RockTimerAllowedLag = lag;

            TimeSpan interval = TimeSpan.FromMilliseconds(msInterval);                                   
            timer = new DispatcherTimer()
            {
                Interval = interval

            };

            // internal timer
            // no need to change to use AddTickEventHandler()
            timer.Tick += (sender, e) =>
            {
                this.rockConstantTimerCounter++;
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
            Debug.WriteLine("Rock Timer Threshold: " + rockConstantTimerCounter + " - " + rockTimerCounter);
            return (rockConstantTimerCounter - rockTimerCounter >= RockTimerAllowedLag);
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }


        public void AddTickEventHandler(EventHandler handler)
        {
            timer.Tick += handler;
            tickHandlers.Add(handler);
        }

        public void RemoveTickEventHandler(EventHandler handler)
        {
            timer.Tick -= handler;
            tickHandlers.Remove(handler);
        }

        public void ClearTickEventHandlers()
        {
            foreach (EventHandler handler in tickHandlers)
            {
                timer.Tick -= handler;
            }
            tickHandlers.Clear();
        }
    }
}
