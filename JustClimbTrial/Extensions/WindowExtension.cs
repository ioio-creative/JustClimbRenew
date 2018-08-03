using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace JustClimbTrial.Extensions
{
    public static class WindowExtension
    {
        public static void ShowInScreen(this Window window, Screen screen)
        {            
            Rectangle rect = screen.WorkingArea;
            window.Top = rect.Top;
            window.Left = rect.Left;

            window.Show();
        }

        public static void ToggleFullScreen(this NavigationWindow window,
            bool isFullScreen = true)
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
            bool isFullScreen = true)
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
            window.ShowsNavigationUI = true;
            SetNormalStyle(window as Window);
        }

        public static void SetNormalStyle(this Window window)
        {
            window.WindowState = WindowState.Normal;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            //window.ResizeMode = ResizeMode.CanResizeWithGrip;            
        }

        public static void SetFullScreenStyle(this NavigationWindow window)
        {
            window.ShowsNavigationUI = false;
            SetFullScreenStyle(window as Window);
        }
       
        public static void SetFullScreenStyle(this Window window)
        {
            window.WindowState = WindowState.Maximized;
            window.WindowStyle = WindowStyle.None;
            //window.ResizeMode = ResizeMode.NoResize;
        }
    }
}
