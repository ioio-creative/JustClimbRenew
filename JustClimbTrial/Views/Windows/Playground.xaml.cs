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
using System.Windows.Shapes;

namespace JustClimbTrial.Views.Windows
{
    /// <summary>
    /// Interaction logic for Playground.xaml
    /// </summary>
    public partial class Playground : Window
    {
        public Playground()
        {
            InitializeComponent();
        }

        public void ShowImage(BitmapSource source)
        {
            PlaygroundCamera.Source = source;
        }

        public void SetPlaygroundMediaSource(Uri sourceUri)
        {
            PlaygroundMedia.Source = sourceUri;
        }
        private void PlaygroundMedia_Loaded(object sender, RoutedEventArgs e)
        {
            PlaygroundMedia.Play();
        }
        private void PlaygroundMedia_MediaOpened(object sender, RoutedEventArgs e)
        {

        }
        private void PlaygroundMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            PlaygroundMedia.Position = TimeSpan.FromSeconds(0);
        }


        public void SetPlaybackMediaSource(Uri sourceUri)
        {
            PlaybackMedia.Source = sourceUri;
        }
        private void PlaybackMedia_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
        private void PlaybackMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
           
        }


    }
}
