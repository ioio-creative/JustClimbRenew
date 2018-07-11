using JustClimbTrial.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using JustClimbTrial.Enums;
using System;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for VideoPlaybackDialog.xaml
    /// </summary>
    public partial class VideoPlaybackDialog : NavigationWindow, ISavingVideo
    {
        #region ISavingVideo properties

        public string TmpVideoFilePath { get; set; }
        public bool IsConfirmSaveVideo { get; set; }

        public string RouteId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ClimbMode RouteClimbMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsRouteContainDemoVideo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion


        #region constructors

        public VideoPlaybackDialog()
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
