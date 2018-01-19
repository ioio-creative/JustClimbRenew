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
        #region preloaded sequences

        public static List<BitmapSource> DefaultInitializeSequence = GetDefaultInitializeSequence();

        #endregion


        //private static IReadOnlyDictionary<RockAnimationSeq, string>Animation
        private List<BitmapSource>[] sequencePlaylist;
        private bool isLastFolderToLoop = false;


        public bool ToLoop = true;

        private int currentFolder;
        private int currentIndex;
        private Image image;

        private List<BitmapSource> images;

        private DispatcherTimer updateImageTimer;

        private const string ImgExtenstion = ".png";

        private event EventHandler<SeqFolderEndedEventArgs> SequenceFolderEnded;
        private class SeqFolderEndedEventArgs : EventArgs
        {
            public bool loopFolder;

            public SeqFolderEndedEventArgs(bool _loopFolder)
            {
                loopFolder = _loopFolder;
            }
        }

        public ImageSequenceHelper(Image image, bool loop = false, int fps = 25)
        {
            this.image = image;
            this.updateImageTimer = new DispatcherTimer(DispatcherPriority.Render);
            this.updateImageTimer.Interval = TimeSpan.FromMilliseconds(1000/fps);
            this.updateImageTimer.Tick += new EventHandler(this.UpdateImageTimer_Tick);
            ToLoop = loop;
        }

        public void SetSequences(bool loop, params List<BitmapSource>[] imgFolders)
        {
            sequencePlaylist = imgFolders;
            isLastFolderToLoop = loop;

            currentFolder = 0;
            images = sequencePlaylist[currentFolder++];
            SequenceFolderEnded += SequenceFolderEndedHandler;                          
           
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

        private void UpdateImageTimer_Tick(object sender, EventArgs e)
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
                SequenceFolderEnded?.Invoke(sender, new SeqFolderEndedEventArgs(isLastFolderToLoop) );
            }

            if (this.images != null)
            {
                this.LoadCurrentIndex();
            }
        }

        private void SequenceFolderEndedHandler(object sender, SeqFolderEndedEventArgs e)
        {
            Stop();

            ToLoop = false;
            images = sequencePlaylist[currentFolder++];
            if (currentFolder == sequencePlaylist.Length)
            {
                ToLoop = e.loopFolder;
            }
            Play();
        }


        #region Sequence BitmapSource List

        // imgExt e.g. ".mp4"
        private static List<BitmapSource> GetBitmapSourceList(string directoryPath, string imgExt = ImgExtenstion)
        {
            List<BitmapSource> sequence = new List<BitmapSource>();

            IEnumerable<FileInfo> imgFiles =
                FileHelperDLL.FileHelper.GetFilesInDirectoryByExtensions(directoryPath, imgExt);

            foreach (FileInfo imgFile in imgFiles)
            {
                Uri fileUri = new Uri(imgFile.FullName);
                BitmapSource frameSource = new BitmapImage(fileUri);
                sequence.Add(frameSource);
            }

            return sequence;
        }

        private static List<BitmapSource> GetDefaultInitializeSequence()
        {            
            return GetBitmapSourceList(FileHelper.BoulderButtonNormalImgSequenceDirectory());
        }
 
        #endregion
    }
}
