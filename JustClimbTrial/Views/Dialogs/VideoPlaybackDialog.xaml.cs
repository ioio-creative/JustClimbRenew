using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialog.xaml
    /// </summary>
    public partial class VideoPlaybackDialog : NavigationWindow
    {
        public VideoPlaybackDialog(MediaElement externalMediaElement)
        {
            InitializeComponent();
        }
    }
}
