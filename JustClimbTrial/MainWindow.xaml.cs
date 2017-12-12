using JustClimbTrial.Kinect;
using JustClimbTrial.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

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

        public MainWindow()
        {
            InitializeComponent();

            playgroundWindow = new Playground();
            playgroundWindow.Show();
        }

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KinectManagerClient = new KinectManager();
            //activate sensor in Main Window only once
            KinectManagerClient.OpenKinect();
            KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
            playgroundWindow.Close();
            KinectManagerClient.CloseKinect();
        }

        public void HandleColorImageSourceArrived(object sender, ColorImgSrcEventArgs e)
        {
            playgroundWindow.ShowImage( e.GetColorBitmapSrc() );
        }
    }
}
