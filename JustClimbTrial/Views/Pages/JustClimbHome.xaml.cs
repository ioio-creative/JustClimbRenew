using JustClimbTrial.Kinect;
using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JustClimbTrial.Globals;
using System.IO;
using JustClimbTrial.Helpers;
using JustClimbTrial.Properties;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for JustClimbHome.xaml
    /// </summary>
    public partial class JustClimbHome : Page
    {
        private readonly bool debug = AppGlobal.DEBUG;

        private MainWindow mainWindowClient;

        public JustClimbHome()
        {

            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            ModeSelect modeSelectPage = new ModeSelect();
            this.NavigationService.Navigate(modeSelectPage);
           
            //(this.Parent as MainWindow).KinectManagerClient.ColorImageSourceArrived -= (this.Parent as MainWindow).HandleColorImageSourceArrived;
        }

        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowTitle = AppGlobal.WallID;
            mainWindowClient = this.Parent as MainWindow;
            mainWindowClient.CheckAndLoadAndPlayScrnSvr();

            if (debug)
            {
                mainWindowClient.SubscribeColorImgSrcToPlaygrd();
            }
            
        }


        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {
            if (debug)
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd();
            }
        }
    }
}
