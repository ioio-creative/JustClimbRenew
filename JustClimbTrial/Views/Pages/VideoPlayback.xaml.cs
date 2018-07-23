using JustClimbTrial.Globals;
using JustClimbTrial.Interfaces;
using JustClimbTrial.Mvvm.Infrastructure;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoPlayback.xaml
    /// </summary>
    public partial class VideoPlayback : Page
    {
        private readonly bool debug = AppGlobal.DEBUG;

        #region private members
        private MainWindow mainWindowClient;

        private DispatcherTimer timer = new DispatcherTimer();
        private const double defaultTick = 500;
        private bool supressNavTick = false;

        private readonly string videoFilePath;

        #endregion
        

        #region constructors

        public VideoPlayback(string aVideoFilePath, MainWindow mainWindow)
        {
            mainWindowClient = mainWindow;
            videoFilePath = aVideoFilePath;

            InitializeComponent();
            //add some handlers manually because slider IsMoveToPointEnabled absorbs MouseButtonEvent
            //reference-- https://social.msdn.microsoft.com/Forums/vstudio/en-US/e1318ef4-c76c-4267-9031-3bb7a0db502b/sliderismovetopointenabled-aborts-mouse-click-events?forum=wpf
            navigationSlider.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(NavSlider_MouseDown), true);
            navigationSlider.AddHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(NavSlider_MouseUp), true);            
        }

        #endregion


        private void InitializeCommands()
        {
            playBtn.Command = new RelayCommand(PlayMedia, CanPlayMedia);
            pauseBtn.Command = new RelayCommand(PauseMedia, CanPauseMedia);
            backBtn.Command = new RelayCommand(StopAndClosePlaybackDialog, CanStopMedia);
            speedResetBtn.Command = new RelayCommand(ResetPlayMediaSpeed, CanResetPlayMediaSpeed);          
        }

        private void InitializePropertyValues()
        {
            // Set the media's starting SpeedRatio to the current value of the
            // their respective slider controls.
            double speedRatio = speedRatioSlider.Value * 0.01;
            mediaPlayback.SpeedRatio = speedRatio;
            mainWindowClient.SetSpeedRatioOfPlaybackMedia(speedRatio);         
        }

        private void ShowMediaInformation()
        {
            var duration = mediaPlayback.NaturalDuration.HasTimeSpan
                ? mediaPlayback.NaturalDuration.TimeSpan.TotalMilliseconds.ToString("#ms")
                : "No duration";

            Debug.WriteLine(duration);
        }


        #region CanExecute command methods

        private bool CanPlayMedia(object parameter = null)
        {
            return true;
        }

        private bool CanPauseMedia(object parameter = null)
        {
            return true;
        }

        private bool CanStopMedia(object parameter = null)
        {
            return true;
        }

        private bool CanResetPlayMediaSpeed(object parameter = null)
        {
            return true;
        }

        #endregion


        #region command methods

        private void PlayMedia(object parameter = null)
        {
            // The Play method will begin the media if it is not currently active or 
            // resume media if it is paused. This has no effect if the media is
            // already running.
            timer.Start();
            mediaPlayback.Play();
            mainWindowClient.PlayPlaygbackMedia();

            // Initialize the MediaElement property values.
            InitializePropertyValues();
        }

        private void PauseMedia(object parameter = null)
        {
            // The Pause method pauses the media if it is currently running.
            // The Play method can be used to resume.
            mediaPlayback.Pause();
            mainWindowClient.PausePlaygbackMedia();
        }

        private void StopMedia(object parameter = null)
        {
            // The Stop method stops and resets the media to be played from
            // the beginning. 
            TimeSpan beginning = TimeSpan.FromMilliseconds(0);
            mediaPlayback.Position = beginning;
            mainWindowClient.SetPositionOfPlaybackMedia(beginning);

            navigationSlider.Value = navigationSlider.Minimum;
            timer.Stop();

            mediaPlayback.Stop();
            mainWindowClient.StopPlaygbackMedia();
        }

        private void ResetPlayMediaSpeed(object parameter = null)
        {
            speedRatioSlider.Value = 100;
            mediaPlayback.SpeedRatio = 1;
            mainWindowClient.SetSpeedRatioOfPlaybackMedia(1);
        }

        private void StopAndClosePlaybackDialog(object parameter = null)
        {
            StopMedia();
            (this.Parent as NavigationWindow).Close();
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //VideoPlaybackDialog pageParent = this.Parent as VideoPlaybackDialog;
            NavigationWindow pageParent = this.Parent as NavigationWindow;
            pageParent.Width = 900;
            pageParent.MinWidth = 600;
            pageParent.Height = 600;
            pageParent.MinHeight = 500;

            mediaPlayback.Source = new Uri(videoFilePath);
            mainWindowClient.ChangeSrcInPlaygbackMedia(videoFilePath);

            InitializeCommands();
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

        private void mediaPlayback_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayback.Stop();
        }

        private void ChangeMediaSpeedRatio(object sender, MouseButtonEventArgs e)
        {
            double speedRatio = speedRatioSlider.Value * 0.01;
            mediaPlayback.SpeedRatio = speedRatio;
            mainWindowClient.SetSpeedRatioOfPlaybackMedia(speedRatio);

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
            TimeSpan sliderTimePosition = TimeSpan.FromSeconds(navigationSlider.Value);
            mediaPlayback.Position = sliderTimePosition;
            mainWindowClient.SetPositionOfPlaybackMedia(sliderTimePosition);

            Debug.WriteLine("Slider Value = " + navigationSlider.Value);
            Debug.WriteLine("Current Position: " + mediaPlayback.Position.TotalSeconds);

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

        #endregion        
    }
}
