using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using System.Windows;
using System.Windows.Controls;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoRecord.xaml
    /// </summary>
    public partial class VideoRecord : Page
    {
        #region private members

        private readonly ClimbMode climbMode;
        private readonly VideoRecordType videoRecordType;
        private readonly VideoHelper videoHelper;

        // need to pass externalPlaybackMonitor to another view
        private MediaElement externalPlaybackMonitor;

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
            InitialiseCommands();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            videoHelper.ClearBuffer();
        }

        #endregion


        #region command methods

        private void InitialiseCommands()
        {
            btnStartRecordVideo.Command = new RelayCommand(
                StartRecordVideo, CanStartRecordVideo);
            btnStopRecordVideo.Command = new RelayCommand(
                StopRecordVideo, CanStopRecordVideo);
            btnViewVideo.Command = new RelayCommand(
                ViewVideo, CanViewVideo);
        }

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
