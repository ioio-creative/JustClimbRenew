using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Views.Pages;
using JustClimbTrial.Views.Windows;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace JustClimbTrial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        //in Debug Mode we display the live camera image from Kinect at all times
        private readonly bool debug = AppGlobal.DEBUG;
        private readonly bool wallAndFloor = AppGlobal.WAF;

        public KinectManager KinectManagerClient;

        private Playground playgroundWindow;
        public Playground PlaygroundWindow
        {
            get { return playgroundWindow; }
        }

        //Monitors projected to Wall (i.e. Playground)
        //bottom layer for [Count Down]/[Start]/[Game Over],etc videos
        private MediaElement playgroundMedia;                
        public MediaElement PlaygroundMedia
        {
            get { return playgroundMedia; }
            set { playgroundMedia = value; }
        }
        //top layer for game recording playback
        private MediaElement playbackMedia;
        public MediaElement PlaybackMedia
        {
            get { return playbackMedia; }
            set { playbackMedia = value; }
        }



        public MainWindow()
        {
            InitializeComponent();

            playgroundWindow = new Playground();
            playgroundWindow.Show();
        }

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Get registered wall from File
            Uri wallLogImgUri = new Uri(FileHelper.WallLogImagePath(AppGlobal.WallID));
            BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);

            //activate sensor in Main Window only once
            KinectManagerClient = new KinectManager();
            bool isOpenKinectSuccessful = KinectManagerClient.OpenKinect();

            //assign MediaElement ref to MainWindow member var
            PlaygroundMedia = playgroundWindow.PlaygroundMedia;
            //MUST LOAD AN IMAGE TO PLAYGROUND CANVAS TO GIVE DIMENSION
            playgroundWindow.ShowImage(wallLogImg, 0);
            //play ScreenSaver.mp4 in Playground Window
            CheckAndLoadAndPlayScrnSvr();

            if (isOpenKinectSuccessful)
            {                             
                if (debug)
                {                   
                    playgroundWindow.ShowImage(wallLogImg, 0.5);
                }
                UiHelper.NotifyUser("Kinect connected!");
            }
            else
            {
                if (UiHelper.NotifyUserResult("Kinect is not available!" + Environment.NewLine + "Please Check Kinect Connection and Restart Programme.") == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }

            //Wall & Floor Calibration
            if (wallAndFloor)
            {
                WallAndFloor wAndF = new WallAndFloor();
                Navigate(wAndF);
            }
        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;          
            playgroundWindow.Close();
            if (KinectManagerClient.multiSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                KinectManagerClient.multiSourceReader.Dispose();
                KinectManagerClient.multiSourceReader = null;
            }
            KinectManagerClient.CloseKinect();
        }

        private void HandleColorImageSourceArrived(object sender, ColorBitmapSrcEventArgs e)
        {
            playgroundWindow.ShowImage(e.GetColorBitmapSrc());
        }


        //This is only called by several Pages when debug mode in On
        public void SubscribeColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
        }
        public void UnsubColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
        }

        public void CheckAndLoadAndPlayScrnSvr()
        {
            Uri scrnSvrUri = new Uri(Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4"));
            if ( playgroundMedia.Source == null || !playgroundMedia.Source.Equals(scrnSvrUri))
            {
                playgroundMedia.Stop();
                playgroundMedia.Source = scrnSvrUri;
                playgroundWindow.LoopMedia = true;
                playgroundMedia.Play();
            }
        }

        #region playground Media Elements ctrl



        #endregion
    }
}
