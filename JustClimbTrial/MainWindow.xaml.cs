﻿using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Views.Pages;
using JustClimbTrial.Views.Windows;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JustClimbTrial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        //in Debug Mode we display the live camera image from Kinect at all times        

        public KinectManager KinectManagerClient;


        /* commands associated with keyboard short cuts */
        
        //reference: https://stackoverflow.com/questions/1361350/keyboard-shortcuts-in-wpf
        public static RoutedCommand DebugModeToggleCommand = new RoutedCommand();
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }
        
        public static RoutedCommand WallCalibrationCommand = new RoutedCommand();        
        public static RoutedCommand IsFullScreenToggleCommand = new RoutedCommand();
        public static RoutedCommand CloseAppCommand = new RoutedCommand();

        /* end of commands associated with keyboard short cuts */


        private Playground playgroundWindow;

        //Monitors projected to Wall (i.e. Playground)
        //bottom layer for [Count Down]/[Start]/[Game Over],etc videos
        private MediaElement playgroundMedia;
        private Canvas playgroundCanvas;
        //top layer for game recording playback
        private MediaElement playbackMedia;


        public MainWindow()
        {
            InitializeComponent();

            this.ToggleFullScreen(AppGlobal.IsFullScreen);
            
            // show playground window

            playgroundWindow = new Playground();

            System.Windows.Forms.Screen[] allScreens = System.Windows.Forms.Screen.AllScreens;
            System.Windows.Forms.Screen screenToShowPlaygroundWindow = allScreens[0];

            if (allScreens.Length > 1)
            {
                screenToShowPlaygroundWindow = allScreens[1];
                playgroundWindow.ToggleFullScreen();
            }

            playgroundWindow.ShowInScreen(
                screenToShowPlaygroundWindow);
        }


        #region event handlers

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppGlobal.DebugModeChanged += HandleDebugModeChanged;
            AppGlobal.IsFullScreenChanged += HandleIsFullScreenChanged;

            

            //activate sensor in Main Window only once
            KinectManagerClient = new KinectManager();
            bool isOpenKinectSuccessful = KinectManagerClient.OpenKinect();

            if (isOpenKinectSuccessful)
            {                            
                //Get registered wall from File
                Uri wallLogImgUri = new Uri(FileHelper.WallLogImagePath(AppGlobal.WallID));
                BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);
                SetDebugWallLogImg();
                playgroundWindow.LoadImage(wallLogImg);

                //assign MediaElement ref to MainWindow member var
                playgroundMedia = playgroundWindow.PlaygroundMedia;
                playgroundCanvas = playgroundWindow.PlaygroundCanvas;
                playbackMedia = playgroundWindow.PlaybackMedia;
             
                //play ScreenSaver.mp4 in Playground Window
                CheckAndLoadAndPlayScrnSvr();

                UiHelper.NotifyUser("Kinect connected!");
            }
            else
            {
                UiHelper.NotifyUser("Kinect connection ERROR" + Environment.NewLine + "Please try reconnect Kinect device and restart application.");
                Application.Current.Shutdown();
            }
                     
        }

        private void NavigationWindow_Closed(object sender, EventArgs e)
        {
            AppGlobal.DebugModeChanged -= HandleDebugModeChanged;
            AppGlobal.IsFullScreenChanged -= HandleIsFullScreenChanged;

            UnsubColorImgSrcToPlaygrd();
            playgroundWindow.Close();
            if (KinectManagerClient.multiSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                KinectManagerClient.multiSourceReader.Dispose();
                KinectManagerClient.multiSourceReader = null;
            }
            KinectManagerClient.CloseKinect();
        }

        private void HandleColorImageSourceArrived(object sender, ColorBitmapSrcEventArgs e)
        {
            playgroundWindow.LoadAndShowImage(e.GetColorBitmapSrc());
        }

        #endregion


        #region debug commands and func

        private void CloseAppCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (UiHelper.NotifyUserYesNo("Close Application?") == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void DebugModeToggleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AppGlobal.DEBUG = !AppGlobal.DEBUG;           
        }

        private void WallCalibrationCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(Content is WallCalibration))
            {
                UnsubColorImgSrcToPlaygrd();
                WallCalibration WallCal = new WallCalibration();
                Navigate(WallCal); 
            }
            else
            {
                GoBack();
                RemoveBackEntry();
                GC.Collect();
            }
        }

        private void HandleDebugModeChanged(bool _debug)
        {
            UiHelper.NotifyUser("Debug Mode: " + (_debug ? "On" : "Off"));
            SetDebugWallLogImg();
        }

        private void SetDebugWallLogImg()
        {
            if (debug)
            {
                playgroundWindow.SetImageOpacity(0.5);
            }
            else
            {
                playgroundWindow.HideImage();
            }
        }

        private void IsFullScreenToggleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AppGlobal.IsFullScreen = !AppGlobal.IsFullScreen;     
        }

        private void HandleIsFullScreenChanged(bool _isFullScreenChanged)
        {
            this.ToggleFullScreen(_isFullScreenChanged);
        }

        #endregion


        //This is only called by several Pages when debug mode in On
        public void SubscribeColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
            playgroundWindow.PlaygroundCamera.Opacity = 1;
        }

        public void UnsubColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
            playgroundWindow.PlaygroundCamera.Opacity = 0;

        }

        public void CheckAndLoadAndPlayScrnSvr()
        {
            if (playgroundMedia != null)
            {
                Uri scrnSvrUri = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4"));
                if (playgroundMedia.Source == null || !playgroundMedia.Source.Equals(scrnSvrUri))
                {
                    playgroundMedia.Stop();
                    playgroundMedia.Source = scrnSvrUri;
                    playgroundWindow.LoopMedia = true;
                    playgroundMedia.Play();
                } 
            }
        }

        
        #region Playground Canvas ctrl

        public Canvas GetPlaygroundCanvas()
        {
            return playgroundCanvas;
        }

        public void ShowPlaygroundCanvas()
        {
            playgroundCanvas.Opacity = 1;
        }

        public void ShowImageInPlaygroundCanvas(BitmapSource bitmapSrc)
        {
            playgroundWindow.LoadAndShowImage(bitmapSrc);
        }

        public IEnumerable<Shape> DrawSkeletonInPlaygroundCanvas(Body body, CoordinateMapper coorMapper, SpaceMode spaceMode)
        {
            return playgroundCanvas.DrawSkeleton(body, coorMapper, spaceMode);
        }

        public void RemoveChildFromPlaygroundCanvas(UIElement uiElement)
        {
            playgroundCanvas.RemoveChild(uiElement);
        }

        public void HidePlaygroundCanvas()
        {
            playgroundCanvas.Opacity = 0;
        }

        // use with care
        public void ClearPlaygroundCanvas()
        {
            playgroundCanvas.Children.Clear();
        }

        #endregion


        #region Playground Media ctrl

        public void ChangeSrcAndPlayInPlaygroundMedia(string uriString, bool loop = false)
        {
            StopPlaygroundMedia();
            ChangeSrcInPlaygroundMedia(uriString);
            PlayPlaygroundMedia(loop);        
        }

        public void ChangeSrcInPlaygroundMedia(string uriString)
        {
            playgroundMedia.Source = new Uri(uriString);
        }

        public void PlayPlaygroundMedia(bool loop = false)
        {
            playgroundWindow.LoopMedia = loop;
            playgroundMedia.Visibility = Visibility.Visible;
            playgroundMedia.Play();
        }

        public void StopPlaygroundMedia()
        {
            playgroundMedia.Visibility = Visibility.Hidden;
            playgroundMedia.Stop();
        }

        public void PausePlaygroundMedia()
        {
            playgroundMedia.Pause();
        }

        public void AddPlaygrounMediaEndedEventHandler(RoutedEventHandler eventHandler)
        {
            playgroundMedia.MediaEnded += eventHandler;
        }

        public void RemovePlaygrounMediaEndedEventHandler(RoutedEventHandler eventHandler)
        {
            playgroundMedia.MediaEnded -= eventHandler;
        }
        
        #endregion

        
        #region Playback Media ctrl

        public void ShowPlaybackMedia()
        {
            playbackMedia.Opacity = 1;
        }

        public void HidePlaybackMedia()
        {
            playbackMedia.Opacity = 0;
        }

        public void ChangeSrcAndPlayInPlaygbackMedia(string uriString, bool loop = false)
        {
            StopPlaygbackMedia();
            ChangeSrcInPlaygbackMedia(uriString);
            PlayPlaybackMedia(loop);
        }

        public void ChangeSrcInPlaygbackMedia(string uriString)
        {
            playbackMedia.Source = new Uri(uriString);
        }

        public void PlayPlaybackMedia(bool loop = false)
        {
            playgroundWindow.LoopMedia = loop;
            playbackMedia.Visibility = Visibility.Visible;
            playbackMedia.Play();
        }

        public void StopPlaygbackMedia()
        {
            playbackMedia.Visibility = Visibility.Hidden;
            playbackMedia.Stop();
        }

        public void PausePlaybackMedia()
        {
            playbackMedia.Pause();
        }
        
        public void SetPositionOfPlaybackMedia(TimeSpan timeSpan)
        {
            playbackMedia.Position = timeSpan;
        }

        public void SetSpeedRatioOfPlaybackMedia(double speedRatio)
        {
            playbackMedia.SpeedRatio = speedRatio;
        }

        #endregion
    }
}
