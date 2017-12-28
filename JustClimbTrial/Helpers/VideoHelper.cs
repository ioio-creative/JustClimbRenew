using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace JustClimbTrial.Helpers
{
    //Thread Saving Queue
    //http://blog.bartdemeyer.be/2012/03/creating-thread-save-queue/
    public class VideoHelper
    {
        private string folderPath;
        private int frameCnt = 0;
        private bool recOn = false;

        public BlockingCollection<ImageToSave> Queue { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public VideoHelper()
        {
            Queue = new BlockingCollection<ImageToSave>();            
        }

        /// <summary>
        /// Start the queue handling. Will check the queue and then saves the images one by one.
        /// </summary>
        public void StartQueue()
        {
            Task.Factory.StartNew(() =>
            {
                recOn = true;
                while (true)
                {
                    if (!recOn)
                    {
                        break;
                    }

                    ImageToSave imageToSave = null;     
                    if (Queue.TryTake(out imageToSave))
                    {
                        string filePath = Path.Combine(imageToSave.FolderPath, frameCnt.ToString().PadLeft(8, '0') + ".png");
                        Console.WriteLine("1: Saving image from queue to {0}", filePath);
                        try
                        {
                            imageToSave.VBitmap.Save(filePath);
                            frameCnt++;
                            imageToSave.Dispose();
                            Console.WriteLine("1: Queue is holding {0} images", Queue.Count);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("1: Error reading and executing queue", ex);
                        }
                    }
                }
            });
        }
        public void StopQueue()
        {
            recOn = false;
            frameCnt = 0;
        }

        public void SaveImageToQueue(string filePath, Bitmap bitmap)
        {
            this.Queue.Add(new ImageToSave() { VBitmap = (Bitmap)bitmap.Clone(), FolderPath = filePath });
            bitmap.Dispose();
        }

        public static bool ExportVideo(string sequenceFolderPath, string outputFolderPath)
        {
            ProcessStartInfo procStart = new ProcessStartInfo();
            Process proc = new Process();
            procStart.FileName = "ffmpeg.exe";
            procStart.UseShellExecute = true;

            //ffmpeg command-->
            //-framerate:   set fps of output video
            //-i:           locate folder containing img sequence as input;
            //-pix_fmt:     set pixel format;
            //-vf:          apply horizontal flip video filter
            //output file name
            string arg = "-i " + sequenceFolderPath + "%08d.png -framerate 25 -pix_fmt yuv420p -vf hflip " + outputFolderPath + ".mp4";

            procStart.Arguments = arg;
            procStart.WindowStyle = ProcessWindowStyle.Normal;
            proc.StartInfo = procStart;
            proc.Start();
            //proc.StandardInput.WriteLine("ffmpeg");
            return true;
        }
    }

    public class ImageToSave : IDisposable
    {
        public string FolderPath { get; set; }
        public Bitmap VBitmap { get; set; }

        /// <summary>
        /// Disposes the Bitmap in the helper class
        /// </summary>
        public void Dispose()
        {
            VBitmap.Dispose();
        }
    }
}
