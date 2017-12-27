using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Views.Windows;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        private Playground playgroundWindow;
        public Playground PlaygroundWindow
        {
            get { return playgroundWindow; }            
        }

        public KinectManager KinectManagerClient;

        public VideoHelper MainVideoHelper;
        private FileHelper mainFileHelper;
        private MediaElement playgroundMedia;
                
        public MediaElement PlaygroundMedia
        {
            get { return playgroundMedia; }
            set { playgroundMedia = value; }
        }


        public MainWindow()
        {
            InitializeComponent();

            playgroundWindow = new Playground();
            playgroundWindow.Show();
        }

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainVideoHelper = new VideoHelper();
            mainFileHelper = new FileHelper();
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundWindow.SetPlaygroundMediaSource(new Uri( Path.Combine(FileHelper.VideoResourcesFolderPath(),"ScreenSaver.mp4") ) );

            KinectManagerClient = new KinectManager();
            //activate sensor in Main Window only once
            KinectManagerClient.OpenKinect();
            //KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;

        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
            playgroundWindow.Close();
            KinectManagerClient.CloseKinect();
        }

        public void HandleColorImageSourceArrived(object sender, ColorBitmapSrcEventArgs e)
        {
            playgroundWindow.ShowImage( e.GetColorBitmapSrc() );
        }

        public void SetPlaygroundMediaElementSource(Uri sourceUri)
        {
            playgroundWindow.SetPlaygroundMediaSource(sourceUri);
        }
    }
}
