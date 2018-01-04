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
        public bool DebugMode = false;

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

        public VideoHelper MainVideoHelper;


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
            playgroundWindow.SetPlaygroundMediaSource(new Uri( Path.Combine(FileHelper.VideoResourcesFolderPath(),"ScreenSaver.mp4") ) );

            MainVideoHelper = new VideoHelper();

            KinectManagerClient = new KinectManager();
            //activate sensor in Main Window only once
            KinectManagerClient.OpenKinect();

            if (DebugMode)
            {
                KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
            }
            else
            {
                Uri wallLogImgUri = new Uri(FileHelper.WallLogImagePath(AppGlobal.WallID));
                BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);
                playgroundWindow.ShowImage(wallLogImg, 0.5);                
            }
        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
            playgroundWindow.Close();
            KinectManagerClient.CloseKinect();
        }

        public void HandleColorImageSourceArrived(object sender, ColorBitmapSrcEventArgs e)
        {
            playgroundWindow.ShowImage(e.GetColorBitmapSrc());
        }

        public void SetPlaygroundMediaElementSource(Uri sourceUri)
        {
            playgroundWindow.SetPlaygroundMediaSource(sourceUri);
        }
    }
}
