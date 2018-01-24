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

        public static BitmapSource[] DefaultInitialSequence = GetBitmapSourceArray(FileHelper.BoulderButtonNormalImgSequenceDirectory());
        public static BitmapSource[] ShowSequence = GetBitmapSourceArray(FileHelper.BoulderButtonShowImgSequenceDirectory());
        public static BitmapSource[] FeedbackSequence = GetBitmapSourceArray(FileHelper.BoulderButtonFeedbackImgSequenceDirectory());
        public static BitmapSource[] ShinePopSequence = GetBitmapSourceArray(FileHelper.BoulderButtonShinePopImgSequenceDirectory());
        public static BitmapSource[] ShineLoopSequence = GetBitmapSourceArray(FileHelper.BoulderButtonShineLoopImgSequenceDirectory());
        public static BitmapSource[] ShineFeedbackPopSequence = GetBitmapSourceArray(FileHelper.BoulderButtonShineFeedbackPopImgSequenceDirectory());
        public static BitmapSource[] ShineFeedbackLoopSequence = GetBitmapSourceArray(FileHelper.BoulderButtonShineFeedbackLoopImgSequenceDirectory());

        public static BitmapSource[][] CombinedList = new BitmapSource[][]
        {
            ShowSequence,  // 1
            ShinePopSequence,  // 3
            //ShineLoopSequence  // 4
            ShineFeedbackLoopSequence
        };

        //public static BitmapSource[] CombinedList; //= ShowSequence.Concat(ShinePopSequence).Concat(ShineLoopSequence).ToArray();

        static ImageSequenceHelper()
        {
            //List<BitmapSource> list = new List<BitmapSource>();

            //foreach (BitmapSource bms in ShowSequence)
            //{
            //    list.Add(bms);
            //}

            //foreach (BitmapSource bms in ShinePopSequence)
            //{
            //    list.Add(bms);
            //}

            ////foreach (BitmapSource bms in ShineLoopSequence)
            ////{
            ////    list.Add(bms);
            ////}

            //CombinedList = list.ToArray();
        }


    #endregion


    //private static IReadOnlyDictionary<RockAnimationSeq, string>Animation
    private BitmapSource[][] sequencePlaylist;
        private bool isLastFolderToLoop = false;


        public bool isCurrentFolderToLoop = true;

        private int currentFolder;
        private int currentImgIdxInFolder;
        private Image image;

        private BitmapSource[] imagesInFolder;

        private DispatcherTimer updateImageTimer;

        private const string DefaultImgExtension = ".png";

        private event EventHandler SequenceFolderEnded;


        public ImageSequenceHelper(Image image, bool loop = false, int fps = 25)
        {
            this.image = image;
            this.updateImageTimer = new DispatcherTimer(DispatcherPriority.Render);
            this.updateImageTimer.Interval = TimeSpan.FromMilliseconds(1000/fps);
            this.updateImageTimer.Tick += new EventHandler(this.UpdateImageTimer_Tick);
            isCurrentFolderToLoop = loop;
        }

        public void SetAndPlaySequences(bool isLastFolderLoop, BitmapSource[][] imgFolders)
        {
            SetSequences(isLastFolderLoop, imgFolders);
            Play();
        }

        public void SetSequences(bool isLastFolderLoop, BitmapSource[][] imgFolders)
        {
            sequencePlaylist = imgFolders;
            isLastFolderToLoop = isLastFolderLoop;

            currentFolder = 0;
            imagesInFolder = sequencePlaylist[currentFolder++];
            SequenceFolderEnded += SequenceFolderEndedHandler;                                     
        }

        public void Load(BitmapSource[] images)
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
            if (((this.imagesInFolder != null) && (this.currentImgIdxInFolder < this.imagesInFolder.Length)) && (this.currentImgIdxInFolder >= 0))
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
            if (this.currentImgIdxInFolder == this.imagesInFolder.Length)
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
        private static BitmapSource[] GetBitmapSourceArray(string directoryPath, string imgExt = DefaultImgExtension)
        {            
            IEnumerable<FileInfo> imgFiles =
                FileHelperDLL.FileHelper.GetFilesInDirectoryByExtensions(directoryPath, imgExt);

            BitmapSource[] sequence = new BitmapSource[imgFiles.Count()];
            int sequenceCnt = 0;

            foreach (FileInfo imgFile in imgFiles)
            {
                Uri fileUri = new Uri(imgFile.FullName);
                BitmapSource frameSource = new BitmapImage(fileUri);
                sequence[sequenceCnt++] = frameSource;
                //sequence.Add(frameSource);
            }

            //return sequence.ToArray();
            return sequence;
        }
        
        #endregion
    }
}
