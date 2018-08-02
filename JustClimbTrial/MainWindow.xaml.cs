using JustClimbTrial.Extensions;
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

        private KeyGesture debugModeToggleCommandKey = new KeyGesture(Key.D, ModifierKeys.Alt | ModifierKeys.Control);
        //reference: https://stackoverflow.com/questions/1361350/keyboard-shortcuts-in-wpf
        public static RoutedCommand DebugModeToggleCommand = new RoutedCommand();
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }

        private KeyGesture wallCalibrationCommandKey = new KeyGesture(Key.W, ModifierKeys.Alt | ModifierKeys.Control);
        public static RoutedCommand WallCalibrationCommand = new RoutedCommand();

        private KeyGesture isFullScreenToggleCommandKey = new KeyGesture(Key.F, ModifierKeys.Alt | ModifierKeys.Control);
        public static RoutedCommand IsFullScreenToggleCommand = new RoutedCommand();

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

            playgroundWindow = new Playground();
            playgroundWindow.Show();

            InitializeDebugModeToggleCommand();
            InitializeWallCalibrationCommand();
            InitializeIsFullScreenToggleCommand();

            ToggleFullScreen(AppGlobal.IsFullScreen);
        }


        #region event handlers

        private void NavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppGlobal.DebugModeChanged += HandleDebugModeChanged;
            AppGlobal.IsFullScreenChanged += HandleIsFullScreenChanged;

            //Get registered wall from File
            Uri wallLogImgUri = new Uri(FileHelper.WallLogImagePath(AppGlobal.WallID));
            BitmapImage wallLogImg = new BitmapImage(wallLogImgUri);

            //activate sensor in Main Window only once
            KinectManagerClient = new KinectManager();
            bool isOpenKinectSuccessful = KinectManagerClient.OpenKinect();

            //assign MediaElement ref to MainWindow member var
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundCanvas = playgroundWindow.PlaygroundCanvas;
            playbackMedia = playgroundWindow.PlaybackMedia;

            playgroundWindow.LoadImage(wallLogImg);

            //play ScreenSaver.mp4 in Playground Window
            CheckAndLoadAndPlayScrnSvr();

            if (isOpenKinectSuccessful)
            {
                ChangeDebugWallLogImg();

                UiHelper.NotifyUser("Kinect connected!");
            }
            else
            {
                if (UiHelper.NotifyUserResult("Kinect is not available!" + Environment.NewLine + "Please Check Kinect Connection and Restart Programme.") == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
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

        private void InitializeDebugModeToggleCommand()
        {
            DebugModeToggleCommand.InputGestures.Add(debugModeToggleCommandKey);
        }

        private void DebugModeToggleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AppGlobal.DEBUG = !AppGlobal.DEBUG;           
        }

        private void InitializeWallCalibrationCommand()
        {
            WallCalibrationCommand.InputGestures.Add(wallCalibrationCommandKey);
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
            ChangeDebugWallLogImg();
        }

        private void ChangeDebugWallLogImg()
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

        private void InitializeIsFullScreenToggleCommand()
        {
            IsFullScreenToggleCommand.InputGestures.Add(isFullScreenToggleCommandKey);
        }

        private void IsFullScreenToggleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AppGlobal.IsFullScreen = !AppGlobal.IsFullScreen;     
        }

        private void HandleIsFullScreenChanged(bool _isFullScreenChanged)
        {
            ToggleFullScreen(_isFullScreenChanged);
        }

        private void ToggleFullScreen(bool isFullScreen)
        {
            if (isFullScreen)
            {
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
                //ResizeMode = ResizeMode.NoResize;
                ShowsNavigationUI = false;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                //ResizeMode = ResizeMode.CanResizeWithGrip;
                ShowsNavigationUI = true;
            }
        }

        #endregion


        //This is only called by several Pages when debug mode in On
        public void SubscribeColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived += HandleColorImageSourceArrived;
        }        

        public void UnsubColorImgSrcToPlaygrd()
        {
            KinectManagerClient.ColorImageSourceArrived -= HandleColorImageSourceArrived;
        }

        public void CheckAndLoadAndPlayScrnSvr()
        {
            Uri scrnSvrUri = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "ScreenSaver.mp4"));
            if ( playgroundMedia.Source == null || !playgroundMedia.Source.Equals(scrnSvrUri))
            {
                playgroundMedia.Stop();
                playgroundMedia.Source = scrnSvrUri;
                playgroundWindow.LoopMedia = true;
                playgroundMedia.Play();
            }
        }

        
        #region Playground Canvas ctrl

        public Canvas GetPlaygroundCanvas()
        {
            return playgroundCanvas;
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

        public void ChangeSrcAndPlayInPlaygbackMedia(string uriString, bool loop = false)
        {
            StopPlaygbackMedia();
            ChangeSrcInPlaygbackMedia(uriString);
            PlayPlaygbackMedia(loop);
        }

        public void ChangeSrcInPlaygbackMedia(string uriString)
        {
            playbackMedia.Source = new Uri(uriString);
        }

        public void PlayPlaygbackMedia(bool loop = false)
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

        public void PausePlaygbackMedia()
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
