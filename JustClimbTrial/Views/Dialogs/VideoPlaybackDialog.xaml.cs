using JustClimbTrial.Interfaces;
using System.Windows.Controls;
using System.Windows.Navigation;
using System;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialog.xaml
    /// </summary>
    public partial class VideoPlaybackDialog : NavigationWindow, ISavingVideo
    {
        public string TmpVideoFilePath { get; set; }
        public bool IsConfirmSaveVideo { get; set; }


        public VideoPlaybackDialog(MediaElement externalMediaElement)
        {
            InitializeComponent();
        }
    }
}
