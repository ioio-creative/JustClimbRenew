using JustClimbTrial.Globals;
using JustClimbTrial.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialogConfirm.xaml
    /// </summary>
    public partial class VideoPlaybackDialogConfirm : Page
    {
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }

        // need to pass externalPlaybackMonitor to another view
        private MediaElement externalPlaybackMonitor;


        public VideoPlaybackDialogConfirm(MediaElement anExternalPlaybackMonitor)
        {
            InitializeComponent();

            externalPlaybackMonitor = anExternalPlaybackMonitor;
        }


        #region event handler

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {            
            //VideoPlayback videoPlayback = new VideoPlayback(externalPlaybackMonitor);
            //this.NavigationService.Navigate(videoPlayback);
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            (this.Parent as VideoPlaybackDialog).Close();
        }

        #endregion
    }
}
