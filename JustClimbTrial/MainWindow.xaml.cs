using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
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

        public KinectManager KinectManagerClient;

        private Playground playgroundWindow;
        public Playground PlaygroundWindow
        {
            get { return playgroundWindow; }
        }

        private MediaElement playgroundMedia;                
        public MediaElement PlaygroundMedia
        {
            get { return playgroundMedia; }
            set { playgroundMedia = value; }
        }

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
            //by default play ScreenSaver.mp4 in Playground Window
            playgroundMedia = playgroundWindow.PlaygroundMedia;

            playgroundMedia.Source = new Uri(Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4")));


            KinectManagerClient = new KinectManager();
            //activate sensor in Main Window only once
            bool isOpenKinectSuccessful = KinectManagerClient.OpenKinect();
          
            if (isOpenKinectSuccessful)
            {
                if (debug)
                {
                    Uri wallLogImgUri = new Uri(FileHelper.WallLogImagePath(AppGlobal.WallID));
                    BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);
                    playgroundWindow.ShowImage(wallLogImg, 0.5);
                }
                else
                {
                    playgroundMedia.Source = new Uri(Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4"));
                    playgroundMedia.Play();
                }               
            }
            else
            {
                UiHelper.NotifyUser("Kinect is not available!");
            }
        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
            playgroundWindow.Close();
            KinectManagerClient.CloseKinect();
        }

        private void HandleColorImageSourceArrived(object sender, ColorBitmapSrcEventArgs e)
        {
            playgroundWindow.ShowImage(e.GetColorBitmapSrc());
        }

        public void SubscribeColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
        }
        public void UnsubColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
        }

        public void LoadAndPlayScrnSvr()
        {
            playgroundMedia.Source = new Uri(Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4"));
            playgroundWindow.LoopMedia = true;
            playgroundMedia.Play();
        }
    }
}
