using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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

        public static List<BitmapSource> DefaultInitialSequence = GetBitmapSourceList(FileHelper.BoulderButtonNormalImgSequenceDirectory());
        public static List<BitmapSource> ShowSequence = GetBitmapSourceList(FileHelper.BoulderButtonShowImgSequenceDirectory());
        public static List<BitmapSource> FeedbackSequence = GetBitmapSourceList(FileHelper.BoulderButtonFeedbackImgSequenceDirectory());
        public static List<BitmapSource> ShinePopSequence = GetBitmapSourceList(FileHelper.BoulderButtonShinePopImgSequenceDirectory());
        public static List<BitmapSource> ShineLoopSequence = GetBitmapSourceList(FileHelper.BoulderButtonShineLoopImgSequenceDirectory());
        public static List<BitmapSource> ShineFeedbackPopSequence = GetBitmapSourceList(FileHelper.BoulderButtonShineFeedbackPopImgSequenceDirectory());
        public static List<BitmapSource> ShineFeedbackLoopSequence = GetBitmapSourceList(FileHelper.BoulderButtonShineFeedbackLoopImgSequenceDirectory());

        public static List<BitmapSource> CombinedList = ShowSequence.Concat(ShinePopSequence).Concat(ShineLoopSequence).ToList();

        #endregion


        //private static IReadOnlyDictionary<RockAnimationSeq, string>Animation
        private List<BitmapSource>[] sequencePlaylist;
        private bool isLastFolderToLoop = false;


        public bool isCurrentFolderToLoop = true;

        private int currentFolder;
        private int currentImgIdxInFolder;
        private Image image;

        private List<BitmapSource> imagesInFolder;

        private DispatcherTimer updateImageTimer;

        private const string DefaultImgExtension = ".png";

        private event EventHandler SequenceFolderEnded;
        //private class SeqFolderEndedEventArgs : EventArgs
        //{
        //    public bool loopFolder;

        //    public SeqFolderEndedEventArgs(bool _loopFolder)
        //    {
        //        loopFolder = _loopFolder;
        //    }
        //}

        public ImageSequenceHelper(Image image, bool loop = false, int fps = 25)
        {
            this.image = image;
            this.updateImageTimer = new DispatcherTimer(DispatcherPriority.Render);
            this.updateImageTimer.Interval = TimeSpan.FromMilliseconds(1000/fps);
            this.updateImageTimer.Tick += new EventHandler(this.UpdateImageTimer_Tick);
            isCurrentFolderToLoop = loop;
        }

        public void SetAndPlaySequences(bool isLastFolderLoop, params List<BitmapSource>[] imgFolders)
        {
            SetSequences(isLastFolderLoop, imgFolders);
            Play();
        }

        public void SetSequences(bool isLastFolderLoop, params List<BitmapSource>[] imgFolders)
        {
            sequencePlaylist = imgFolders;
            isLastFolderToLoop = isLastFolderLoop;

            currentFolder = 0;
            imagesInFolder = sequencePlaylist[currentFolder++];
            SequenceFolderEnded += SequenceFolderEndedHandler;                                     
        }

        public void Load(List<BitmapSource> images)
        {
            this.updateImageTimer.Stop();

            this.imagesInFolder = images;

            this.currentImgIdxInFolder = 0;
            this.LoadCurrentIndex();
          
        }

        public void Load()
        {
            Load(DefaultInitialSequence);
        }

        private void LoadCurrentIndex()
        {
            if (((this.imagesInFolder != null) && (this.currentImgIdxInFolder < this.imagesInFolder.Count)) && (this.currentImgIdxInFolder >= 0))
            {
                image.Source = this.imagesInFolder[this.currentImgIdxInFolder];
                currentImgIdxInFolder++;
            }
        }

        public void Play()
        {
            this.currentImgIdxInFolder = 0;
            this.updateImageTimer.Start();
        }

        public void Stop()
        {
            this.updateImageTimer.Stop();
        }


        #region event handlers

        private void UpdateImageTimer_Tick(object sender, EventArgs e)
        {
            if (this.currentImgIdxInFolder == this.imagesInFolder.Count)
            {
                if (isCurrentFolderToLoop)
                {
                    this.currentImgIdxInFolder = 0;                    
                }
                else
                {
                    Stop();
                }
                SequenceFolderEnded?.Invoke(sender, e);
            }

            if (this.imagesInFolder != null)
            {
                this.LoadCurrentIndex();
            }
        }

        private void SequenceFolderEndedHandler(object sender, EventArgs e)
        {
            Stop();

            if (currentFolder == sequencePlaylist.Length)  // last folder
            {               
                // unsubscribe from sequence folder ended event
                SequenceFolderEnded -= SequenceFolderEndedHandler;

                if (isLastFolderToLoop)
                {
                    isCurrentFolderToLoop = true;
                    Play();
                }
            }
            else  // not last folder
            {
                imagesInFolder = sequencePlaylist[currentFolder++];

                isCurrentFolderToLoop = false;
                Play();
            }            
        }

        #endregion


        #region Sequence BitmapSource List

        // imgExt e.g. ".mp4"
        private static List<BitmapSource> GetBitmapSourceList(string directoryPath, string imgExt = DefaultImgExtension)
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
        
        #endregion
    }
}
