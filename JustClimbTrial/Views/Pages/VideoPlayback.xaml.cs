using JustClimbTrial.Kinect;
using JustClimbTrial.Views.Windows;
using JustClimbTrial;
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
using System.Windows.Threading;
using JustClimbTrial.Views.Dialogs;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoPlayback.xaml
    /// </summary>
    public partial class VideoPlayback : Page
    {
        private DispatcherTimer timer = new DispatcherTimer();
        private const double defaultTick = 500;
        private bool supressNavTick = false;
        private MediaElement externalPlaybackMonitor;

        private readonly string videoFilePath;
        private readonly bool isShowSaveVideoPanel;


        #region constructors

        public VideoPlayback(string aVideoFilePath, 
            MediaElement anExternalPlaybackMonitor,
            bool isToShowSaveVideoPanel)
        {
            videoFilePath = aVideoFilePath;
            externalPlaybackMonitor = anExternalPlaybackMonitor;
            isShowSaveVideoPanel = isToShowSaveVideoPanel;

            InitializeComponent();
            //add some handlers manually because slider IsMoveToPointEnabled absorbs MouseButtonEvent
            //reference-- https://social.msdn.microsoft.com/Forums/vstudio/en-US/e1318ef4-c76c-4267-9031-3bb7a0db502b/sliderismovetopointenabled-aborts-mouse-click-events?forum=wpf
            navigationSlider.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(NavSlider_MouseDown), true);
            navigationSlider.AddHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(NavSlider_MouseUp), true);            
        }

        #endregion


        private void InitializePropertyValues()
        {
            // Set the media's starting SpeedRatio to the current value of the
            // their respective slider controls.
            mediaPlayback.SpeedRatio = speedRatioSlider.Value * 0.01;
            externalPlaybackMonitor.SpeedRatio = speedRatioSlider.Value * 0.01;
        }

        private void ShowMediaInformation()
        {
            var duration = mediaPlayback.NaturalDuration.HasTimeSpan
                ? mediaPlayback.NaturalDuration.TimeSpan.TotalMilliseconds.ToString("#ms")
                : "No duration";

            Console.WriteLine(duration);
        }


        #region event handlers

        private void VideoPlaybackLoaded(object sender, RoutedEventArgs e)
        {
            //VideoPlaybackDialog pageParent = this.Parent as VideoPlaybackDialog;
            NavigationWindow pageParent = this.Parent as NavigationWindow;
            pageParent.Width = 900;
            pageParent.MinWidth = 600;
            pageParent.Height = 600;
            pageParent.MinHeight = 500;

            mediaPlayback.Source = new Uri(videoFilePath);
            externalPlaybackMonitor.Source = mediaPlayback.Source;

            panelSaveVideo.Visibility =
                isShowSaveVideoPanel ? Visibility.Visible : Visibility.Collapsed;
        }

        private void mediaPlayback_Loaded(object sender, RoutedEventArgs e)
        {
            // show first frame of video in WPF MediaElement
            // https://stackoverflow.com/questions/1346886/show-first-frame-of-video-in-wpf-mediaelement
            mediaPlayback.Play();
            mediaPlayback.Pause();
        }

        private void mediaPlayback_MediaOpended(object sender, RoutedEventArgs e)
        {
            ShowMediaInformation();
            if (mediaPlayback.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = new TimeSpan();
                ts = mediaPlayback.NaturalDuration.TimeSpan;
                navigationSlider.Maximum = ts.TotalSeconds;

                timer.Interval = TimeSpan.FromMilliseconds(defaultTick);
                //timelineSlider.SmallChange = 5;
            }
            timer.Tick += TimerTickHandler;
            
        }

        private void mediaPlayback_MediaClosed(object sender, RoutedEventArgs e)
        {
            mediaPlayback.Stop();
        }

        private void PlayMediaBtnClicked(object sender, RoutedEventArgs e)
        {
            // The Play method will begin the media if it is not currently active or 
            // resume media if it is paused. This has no effect if the media is
            // already running.
            timer.Start();
            mediaPlayback.Play();
            externalPlaybackMonitor.Play();

            // Initialize the MediaElement property values.
            InitializePropertyValues();
        }

        private void PauseMediaBtnClicked(object sender, RoutedEventArgs e)
        {
            // The Pause method pauses the media if it is currently running.
            // The Play method can be used to resume.
            mediaPlayback.Pause();
            externalPlaybackMonitor.Pause();
        }

        private void StopMediaBtnClicked(object sender, RoutedEventArgs e)
        {
            // The Stop method stops and resets the media to be played from
            // the beginning.   
            mediaPlayback.Position = TimeSpan.FromMilliseconds(0);
            externalPlaybackMonitor.Position = TimeSpan.FromMilliseconds(0);

            navigationSlider.Value = navigationSlider.Minimum;
            timer.Stop();

            mediaPlayback.Stop();
            externalPlaybackMonitor.Stop();
        }

        private void ChangeMediaSpeedRatio(object sender, MouseButtonEventArgs e)
        {
            mediaPlayback.SpeedRatio = speedRatioSlider.Value * 0.01;
            externalPlaybackMonitor.SpeedRatio = speedRatioSlider.Value * 0.01;

            timer.Interval = TimeSpan.FromMilliseconds((int)(defaultTick / speedRatioSlider.Value));
        }
        
        private void NavValueChangedHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void NavSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (supressNavTick)
            {
                supressNavTick = false;
                timer.Start();
            }
            mediaPlayback.Position = TimeSpan.FromSeconds(navigationSlider.Value);
            externalPlaybackMonitor.Position = TimeSpan.FromSeconds(navigationSlider.Value);

            Console.WriteLine("Slider Value = " + navigationSlider.Value);
            Console.WriteLine("Current Position: " + mediaPlayback.Position.TotalSeconds);

        }

        private void TimerTickHandler(object sender, EventArgs e)
        {
            if (!supressNavTick)
            {
                navigationSlider.Value = mediaPlayback.Position.TotalSeconds;
            }
            
        }

        private void NavSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            supressNavTick = true;
            timer.Stop();
            //Point mouseOnNav = e.GetPosition(navigationSlider);
            //navigationSlider.Value = navigationSlider.Maximum * mouseOnNav.X / navigationSlider.ActualWidth;
        }

        private void SpeedResetBtn_Click(object sender, RoutedEventArgs e)
        {
            speedRatioSlider.Value = 100;
            mediaPlayback.SpeedRatio = 1;
            externalPlaybackMonitor.SpeedRatio = 1;
        }

        private void saveVideoBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cancelSaveVideoBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion


    }
}
