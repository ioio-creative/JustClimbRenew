﻿using JustClimbTrial.Globals;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using ProcessHelperDLL;
using JustClimbTrial.Kinect;

namespace JustClimbTrial.Helpers
{
    //Thread Saving Queue
    //http://blog.bartdemeyer.be/2012/03/creating-thread-save-queue/
    public class VideoHelper
    {
        private static string ffmpegExePath = AppGlobal.FfmpegExePath;
        private static string videoBufferFolderPath = FileHelper.VideoBufferFolderPath();

        private KinectManager kinectManagerClient;
        private int frameCnt = 0;

        public bool IsFirstRecordingDone { get; private set; }
        public bool IsRecording { get; private set; }

        public BlockingCollection<ImageToSave> Queue { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public VideoHelper(KinectManager aKinectManagerClient)
        {
            IsFirstRecordingDone = false;
            IsRecording = false;
            Queue = new BlockingCollection<ImageToSave>();
            kinectManagerClient = aKinectManagerClient;
        }


        #region private methods

        /// <summary>
        /// Start the queue handling. Will check the queue and then saves the images one by one.
        /// </summary>
        private void StartQueue()
        {
            Task.Factory.StartNew(() =>
            {                
                while (IsRecording)
                {
                    ImageToSave imageToSave = null;     
                    if (Queue.TryTake(out imageToSave))
                    {
                        string filePath = Path.Combine(imageToSave.FolderPath, frameCnt.ToString().PadLeft(8, '0') + ".png");
                        Console.WriteLine("1: Saving image from queue to {0}", filePath);
                        try
                        {
                            imageToSave.VBitmap.Save(filePath);
                            frameCnt++;
                            Console.WriteLine("1: Queue is holding {0} images", Queue.Count);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("1: Error reading and executing queue", ex);
                        }
                        finally
                        {
                            imageToSave.Dispose();
                        }
                    }
                }
            });
        }

        private void StopQueue()
        {
            IsRecording = false;
            frameCnt = 0;
        }

        private void SaveImageToQueue(string filePath, Bitmap bitmap)
        {
            Queue.Add(new ImageToSave()
            {
                VBitmap = bitmap.Clone() as Bitmap,  // !!! Good !!!
                FolderPath = filePath
            });
        }

        #endregion


        #region public methods

        public void StartRecording()
        {
            IsRecording = true;
            kinectManagerClient.ColorBitmapArrived += HandleColorBitmapArrived;
            StartQueue();
        }

        public void StopRecording()
        {
            IsFirstRecordingDone = true;
            StopQueue();
            kinectManagerClient.ColorBitmapArrived -= HandleColorBitmapArrived;
        }

        public int ExportVideo(string outputFilePath)
        {
            return ExportVideo(videoBufferFolderPath, outputFilePath);
        }

        public static int ExportVideo(string sequenceFolderPath, string outputFilePath)
        {
            int exitCode = 0;

            //ffmpeg command-->
            //-framerate:   set fps of output video
            //-i:           locate folder containing img sequence as input;
            //-pix_fmt:     set pixel format;
            //-vf:          apply horizontal flip video filter
            //output file name
            string exportVideoCmdArgumentsFormat =
                "-i \"{0}\" %08d.png -framerate 25 -pix_fmt yuv420p -vf hflip \"{1}\"";

            string exportVideoCmdArguments =
                string.Format(exportVideoCmdArgumentsFormat,
                    sequenceFolderPath, outputFilePath);

            ProcessStartInfo startInfo =
                ProcessUtil.CreateProcessStartInfo(
                    ffmpegExePath,
                    exportVideoCmdArgumentsFormat,
                    AppGlobal.ExeDirectory);

            using (Process proc = Process.Start(startInfo))
            {
                ProcessUtil.OutputFromProcess(proc);

                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            
            return exitCode;
        }

        #endregion


        #region event handlers

        private void HandleColorBitmapArrived(object sender, ColorBitmapEventArgs e)
        {
            if (IsRecording)
            {
                SaveImageToQueue(videoBufferFolderPath, e.GetColorBitmap());
            }
        }

        #endregion
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
