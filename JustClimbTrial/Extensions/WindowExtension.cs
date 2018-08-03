using System.Windows;
using System.Windows.Navigation;

namespace JustClimbTrial.Extensions
{
    public static class WindowExtension
    {
        public static void ToggleFullScreen(this NavigationWindow window,
            bool isFullScreen)
        {
            if (isFullScreen)
            {
                SetFullScreenStyle(window);
            }
            else
            {
                SetNormalStyle(window);
            }
        }

        public static void ToggleFullScreen(this Window window,
            bool isFullScreen)
        {
            if (isFullScreen)
            {
                SetFullScreenStyle(window);
            }
            else
            {
                SetNormalStyle(window);
            }
        }

        public static void SetNormalStyle(this NavigationWindow window)
        {
            window.ShowsNavigationUI = false;
            SetNormalStyle(window as Window);
        }

        public static void SetNormalStyle(this Window window)
        {
            window.WindowState = WindowState.Maximized;
            window.WindowStyle = WindowStyle.None;
            //window.ResizeMode = ResizeMode.NoResize;            
        }

        public static void SetFullScreenStyle(this NavigationWindow window)
        {
            window.ShowsNavigationUI = true;
            SetFullScreenStyle(window as Window);
        }
       
        public static void SetFullScreenStyle(this Window window)
        {
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            //window.ResizeMode = ResizeMode.CanResizeWithGrip;            
        }
    }
}
