using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoRecord.xaml
    /// </summary>
    public partial class VideoRecord : Page
    {
        private readonly bool debug = AppGlobal.DEBUG;

        #region private members

        private readonly ClimbMode climbMode;
        private readonly VideoRecordType videoRecordType;
        private readonly VideoHelper videoHelper;

        // need to pass externalPlaybackMonitor to another view
        private MediaElement externalPlaybackMonitor;

        // timer to show time elapsed since recording started
        private DispatcherTimer timerToShowRecordTime;
        private DateTime recordStartTime;
        private const int timerIntervalInMillis = 1000;
        private readonly int maxRecordDurationInMinutes = AppGlobal.MaxVideoRecordDurationInMinutes;

        #endregion


        #region constructors

        public VideoRecord(ClimbMode aClimbMode, 
            VideoRecordType aVideoRecordType, 
            KinectManager kinectManagerClient,
            MediaElement anExternalPlaybackMonitor)
        {
            InitializeComponent();
            
            climbMode = aClimbMode;
            videoRecordType = aVideoRecordType;
            videoHelper = new VideoHelper(kinectManagerClient);

            externalPlaybackMonitor = anExternalPlaybackMonitor;
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCommands();
            InitializeTimerToShowRecordTime();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            timerToShowRecordTime.Tick -= timerToShowRecordTime_Tick;
            videoHelper.ClearBuffer();
        }

        private void timerToShowRecordTime_Tick(object sender, EventArgs e)
        {
            TimeSpan timeElapsed = DateTime.Now - recordStartTime;

            lbStopwatch.Text = string.Format("{0:00}:{1:00}",
                timeElapsed.Minutes, timeElapsed.Seconds);

            if (timeElapsed.TotalMinutes > maxRecordDurationInMinutes)
            {
                StopRecordVideo();
            }            
        }

        #endregion


        #region initialization

        private void InitializeTimerToShowRecordTime()
        {
            timerToShowRecordTime = new DispatcherTimer();
            timerToShowRecordTime.Tick += timerToShowRecordTime_Tick;
            timerToShowRecordTime.Interval = TimeSpan.FromMilliseconds(timerIntervalInMillis);
        }

        private void InitializeCommands()
        {
            btnStartRecordVideo.Command = new RelayCommand(
                StartRecordVideo, CanStartRecordVideo);
            btnStopRecordVideo.Command = new RelayCommand(
                StopRecordVideo, CanStopRecordVideo);
            btnViewVideo.Command = new RelayCommand(
                ViewVideo, CanViewVideo);
        }

        #endregion


        #region command methods        

        private bool CanStartRecordVideo(object parameter = null)
        {
            return !videoHelper.IsRecording;
        }

        private bool CanStopRecordVideo(object parameter = null)
        {
            return videoHelper.IsRecording;
        }

        private bool CanViewVideo(object parameter = null)
        {
            return !videoHelper.IsRecording && videoHelper.IsFirstRecordingDone;
        }

        private void StartRecordVideo(object parameter = null)
        {
            videoHelper.StartRecording();
            timerToShowRecordTime.Start();
            recordStartTime = DateTime.Now;
        }

        private void StopRecordVideo(object parameter = null)
        {
            videoHelper.StopRecording();
        }

        private void ViewVideo(object parameter = null)
        {
            EntityType videoEntityType;

            switch (climbMode)
            {
                case ClimbMode.Training:
                    videoEntityType = EntityType.TV;
                    break;
                case ClimbMode.Boulder:
                default:
                    videoEntityType = EntityType.BV;
                    break;
            }

            string tmpVideoFilePath =
                    FileHelper.VideoTempFileFullPath(videoEntityType);

            // export video                
            int exportVideoExitCode = videoHelper.ExportVideoAndClearBuffer(tmpVideoFilePath);

            // TODO: deal with fail case
            if (exportVideoExitCode == 0)
            {
                // navigate to VideoPlayback page
                bool isToShowSaveVideoPanel = true;
                VideoPlayback videoPlayBack = 
                    new VideoPlayback(tmpVideoFilePath, externalPlaybackMonitor,
                        isToShowSaveVideoPanel);
                this.NavigationService.Navigate(videoPlayBack);                
            }         
        }

        #endregion     
    }
}
