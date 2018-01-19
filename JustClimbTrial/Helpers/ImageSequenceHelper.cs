using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JustClimbTrial.Helpers
{
    public enum RockAnimationSeq
    {

    }


    /// <summary>
    /// WPF Image Sequencer. Adapted from:
    /// http://blogarchive.claritycon.com/blog/2009/04/wpf-image-sequencer-for-animations/
    /// </summary>
    public class ImageSequenceHelper
    {
        public static List<BitmapSource> DefaultInitializeSequence = GetDefaultInitializeSequence();


        //private static IReadOnlyDictionary<RockAnimationSeq, string>Animation

        public bool ToLoop = true;

        private int currentIndex;
        private Image image;

        private List<BitmapSource> images;
        //private List<ImageSource> images;


        private DispatcherTimer updateImageTimer;

        private string imgExtension = ".png";


        public ImageSequenceHelper(Image image, bool loop = false, int fps = 25)
        {
            this.image = image;
            this.updateImageTimer = new DispatcherTimer(DispatcherPriority.Render);
            this.updateImageTimer.Interval = TimeSpan.FromMilliseconds(1000/fps);
            this.updateImageTimer.Tick += new EventHandler(this.updateImageTimer_Tick);
            ToLoop = loop;
        }


        public void Load(List<BitmapSource> images)
        {
            this.updateImageTimer.Stop();

            this.images = images;

            this.currentIndex = 0;
            this.LoadCurrentIndex();
        }

        public void Load()
        {
            Load(DefaultInitializeSequence);
        }

        private void LoadCurrentIndex()
        {
            if (((this.images != null) && (this.currentIndex < this.images.Count)) && (this.currentIndex >= 0))
            {
                image.Source = this.images[this.currentIndex];
                currentIndex++;
            }
        }

        public void Play()
        {
            this.currentIndex = 0;
            this.updateImageTimer.Start();
        }

        public void Stop()
        {
            this.updateImageTimer.Stop();
        }

        private void updateImageTimer_Tick(object sender, EventArgs e)
        {
            if (this.currentIndex == this.images.Count)
            {
                if (ToLoop)
                {
                    this.currentIndex = 0;                    
                }
                else
                {
                    Stop();
                }                
            }

            if (this.images != null)
            {
                this.LoadCurrentIndex();
            }
        }


        #region Sequence BitmapSource List

        public static List<BitmapSource> GetDefaultInitializeSequence()
        {
            List<BitmapSource> sequence = new List<BitmapSource>();

            IEnumerable<FileInfo> imgFiles =
                FileHelperDLL.FileHelper.GetFilesInDirectoryByExtensions(FileHelper.BoulderButtonNormalImgSequenceDirectory(), ".png");

            foreach (FileInfo imgFile in imgFiles)
            {
                Uri fileUri = new Uri(imgFile.FullName);
                BitmapSource frameSource = new BitmapImage(fileUri);
                sequence.Add(frameSource);
            }

            return sequence;
        }

        //private List<ImageSource> GetDefaultInitializeSequence()
        //{
        //    List<ImageSource> sequence = new List<ImageSource>();

        //    IEnumerable<FileInfo> imgFiles =
        //        FileHelperDLL.FileHelper.GetFilesInDirectoryByExtensions(FileHelper.BoulderButtonNormalImgSequenceDirectory(), ".png");

        //    foreach (FileInfo imgFile in imgFiles)
        //    {                
        //        sequence.Add(ImageSourceHelper.GetImageSource(imgFile.FullName));
        //    }

        //    return sequence;
        //}

        #endregion
    }
}
