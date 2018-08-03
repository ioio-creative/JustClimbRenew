using JustClimbTrial.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace JustClimbTrial.Mvvm.Commands
{
    public class ToggleFullScreenCommandHelper
    {
        private bool isFullScreen = false;

        public RoutedCommand ToggleFullScreenCmd { get; set; }


        public ToggleFullScreenCommandHelper(bool initialIsFullScreen)
        {
            isFullScreen = initialIsFullScreen;
            ToggleFullScreenCmd = new RoutedCommand();
        }
       

        public void ToggleFullScreenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        public void ToggleFullScreenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            isFullScreen = !isFullScreen;

            NavigationWindow navWindow = sender as NavigationWindow;
            if (navWindow != null)
            {
                navWindow.ToggleFullScreen(isFullScreen);                
            }
            else
            {
                Window window = sender as Window;
                if (window != null)
                {
                    window.ToggleFullScreen(isFullScreen);
                }
            }     
        }
    }
}
