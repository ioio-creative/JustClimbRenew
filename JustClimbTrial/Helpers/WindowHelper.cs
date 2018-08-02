using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;

namespace JustClimbTrial.Helpers
{
    public class WindowHelper
    {
        public static void SetWindowNormal(NavigationWindow window)
        {
            window.WindowState = WindowState.Maximized;
            window.WindowStyle = WindowStyle.None;
            //window.ResizeMode = ResizeMode.NoResize;            
            window.ShowsNavigationUI = false;
        }

        public static void SetWindowFullScreen(NavigationWindow window)
        {
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            //window.ResizeMode = ResizeMode.CanResizeWithGrip;
            window.ShowsNavigationUI = true;
        }
    }
}
