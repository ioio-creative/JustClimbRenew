using JustClimbTrial.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialog.xaml
    /// </summary>
    public partial class VideoPlaybackDialog : NavigationWindow, ISavingVideo
    {
        #region properties

        public string TmpVideoFilePath { get; set; }
        public bool IsConfirmSaveVideo { get; set; }

        #endregion


        #region constructors

        public VideoPlaybackDialog(MediaElement externalMediaElement)
        {
            InitializeComponent();
        }

        #endregion


        #region ISavingVideoInterfaces

        public void DeleteTmpVideoFileSafe()
        {
            if (!IsConfirmSaveVideo && !string.IsNullOrEmpty(TmpVideoFilePath))
            {
                FileHelperDLL.FileHelper.DeleteFileSafe(TmpVideoFilePath);
            }
        }

        public void ResetSavingVideoProperties()
        {
            TmpVideoFilePath = null;
            IsConfirmSaveVideo = false;
        }

        #endregion


        #region event handlers

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ResetSavingVideoProperties();
        }

        private void NavigationWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            DeleteTmpVideoFileSafe();
        }

        #endregion        
    }
}
