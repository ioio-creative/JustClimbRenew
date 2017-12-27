using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialog.xaml
    /// </summary>
    public partial class VideoPlaybackDialog : NavigationWindow
    {
        public MediaElement PlaybackMonitor { get; set; }

        public VideoPlaybackDialog(MediaElement externalMediaElement)
        {
            InitializeComponent();

            PlaybackMonitor = externalMediaElement;
        }
    }
}
