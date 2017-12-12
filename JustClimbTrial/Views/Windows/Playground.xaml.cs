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
            playgroundCamera.Source = source;
        }

        public void ShowMediaSource(Uri sourceUri)
        {
            playgroundPlayback.Source = sourceUri;
        }
    }
}
