using JustClimbTrial.Extensions;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Views.Windows
{
    /// <summary>
    /// Interaction logic for Playground.xaml
    /// </summary>
    public partial class Playground : Window
    {
        /* commands associated with keyboard short cuts */

        public static RoutedCommand ToggleFullScreenCmd = new RoutedCommand();

        /* end of commands associated with keyboard short cuts */


        public bool LoopMedia = true;
        public bool PlaygroundMediaPlaying;
        public bool PlaybackMediaPlaying;

        private bool isFullScreen = false;


        public Playground()
        {
            InitializeComponent();            
        }


        #region key gesture commands

        private void ToggleFullScreenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            isFullScreen = !isFullScreen;
            this.ToggleFullScreen(isFullScreen);
        }

        #endregion


        public void LoadImage(string sourcePath)
        {
            PlaygroundCamera.SetSourceByPath(sourcePath);
        }

        public void LoadImage(BitmapSource source)
        {
            PlaygroundCamera.Source = source;
        }

        public void LoadAndShowImage(string sourcePath, double opacity = 0.5)
        {
            LoadImage(sourcePath);
            SetImageOpacity(opacity);
        }

        public void LoadAndShowImage(BitmapSource source, double opacity = 0.5)
        {            
            LoadImage(source);
            SetImageOpacity(opacity);
        }

        public void HideImage()
        {
            SetImageOpacity(0);
        }

        public void SetImageOpacity(double opacity)
        {
            PlaygroundCamera.Opacity = opacity;
        }

        public void SetImageDimensions(BitmapSource source)
        {
            SetImageDimensions(source.Width, source.Height);
        }

        public void SetImageDimensions(double width, double height)
        {
            PlaygroundCamera.Width = width;
            PlaygroundCamera.Height = height;
        }


        #region event handlers

        private void PlaygroundMedia_Loaded(object sender, RoutedEventArgs e)
        {
            if (LoopMedia)
            {
                PlaygroundMedia.Play();
            }
        }

        private void PlaygroundMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            //triggered when Meida Element is Played
        }

        private void PlaygroundMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (LoopMedia)
            {
                PlaygroundMedia.Position = TimeSpan.FromSeconds(0);
            }
            else
            {
                PlaygroundMedia.Stop();
            }
        }

        private void PlaybackMedia_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void PlaybackMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
           
        }        

        #endregion
    }
}
