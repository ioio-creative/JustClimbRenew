using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
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

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoRecord.xaml
    /// </summary>
    public partial class VideoRecord : Page
    {
        private readonly ClimbMode climbMode;
        private readonly VideoRecordType videoRecordType;
        private readonly VideoHelper videoHelper;


        #region constructors

        public VideoRecord(ClimbMode aClimbMode, 
            VideoRecordType aVideoRecordType, KinectManager kinectManagerClient)
        {
            InitializeComponent();
            
            climbMode = aClimbMode;
            videoRecordType = aVideoRecordType;
            videoHelper = new VideoHelper(kinectManagerClient);
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitialiseCommands();
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

            // export video
            string tmpVideoFilePath =
                FileHelper.VideoTempFullPath(videoEntityType);
            int exportVideoExitCode = videoHelper.ExportVideo(tmpVideoFilePath);

            // TODO: deal with fail case
            if (exportVideoExitCode == 0)
            {
                // navigate to VideoPlayback page
                VideoPlayback videoPlayBack = new VideoPlayback();
                this.NavigationService.Navigate(videoPlayBack);
            }                       
        }

        #endregion
    }
}
