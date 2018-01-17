﻿using JustClimbTrial.Globals;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using ProcessHelperDLL;
using JustClimbTrial.Kinect;
using JustClimbTrial.Extensions;

namespace JustClimbTrial.Helpers
{
    //Thread Saving Queue
    //http://blog.bartdemeyer.be/2012/03/creating-thread-save-queue/
    public class VideoHelper
    {
        //ffmpeg command-->
        //-framerate:   set fps of output video
        //-i:           locate folder containing img sequence as input;
        //-pix_fmt:     set pixel format;
        //-vf:          apply horizontal flip video filter
        //output file name
        private const string exportVideoCmdArgumentsFormat =
            "-y -i \"{0}\\%08d.png\" -framerate 25 -pix_fmt yuv420p -vf hflip \"{1}\"";

        private static string ffmpegExePath = AppGlobal.FfmpegExePath;
        private static string videoBufferFolderPath = FileHelper.VideoBufferFolderPath();

        private KinectManager kinectManagerClient;
        private int frameCnt = 0;

        public bool IsRecording { get; private set; }
        public bool IsAllBufferFramesSaved
        {
            get
            {
                return !(IsRecording || Queue.Count > 0);
            }
        }

        public BlockingCollection<ImageToSave> Queue { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public VideoHelper(KinectManager aKinectManagerClient)
        {
            IsRecording = false;
            Queue = new BlockingCollection<ImageToSave>();
            kinectManagerClient = aKinectManagerClient;

            if (!Directory.Exists(videoBufferFolderPath))
            {
                Directory.CreateDirectory(videoBufferFolderPath);
            }
        }


        #region private methods

        /// <summary>
        /// Start the queue handling. Will check the queue and then saves the images one by one.
        /// </summary>
        private void StartQueue()
        {
            frameCnt = 0;
            IsRecording = true;

            Task.Factory.StartNew(() =>
            {                
                //while (IsRecording)
                // Queue.Count > 0 is added to the condition here
                // to ensure that all ImageToSave added to the Queue will be saved as a file
                while (!IsAllBufferFramesSaved)
                {
                    if (Queue.Count == 0)
                    {
                        continue;
                    }

                    ImageToSave imageToSave = null;
                    if (Queue.TryTake(out imageToSave))
                    {
                        string filePath = Path.Combine(imageToSave.FolderPath, frameCnt.ToString().PadLeft(8, '0') + ".png");
                        Debug.WriteLine("1: Saving image from queue to {0}", filePath);
                        try
                        {
                            imageToSave.VBitmap.Save(filePath);
                            frameCnt++;
                            Debug.WriteLine("1: Queue is holding {0} images", Queue.Count);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("1: Error reading and executing queue", ex);
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

            // !!!Important!!! setting frameCnt here is bad! it interrupts the queue!
            //frameCnt = 0;
        }

        private void SaveImageToQueue(string filePath, Bitmap bitmap)
        {
            Queue.Add(new ImageToSave()
            {
                VBitmap = bitmap.Clone() as Bitmap,  // !!! Good !!!
                FolderPath = filePath
            });
        }        

        private async Task<int> ExportVideoAsync(string sequenceFolderPath, string outputFilePath)
        {
            if (!IsAllBufferFramesSaved)
            {
                await WaitingForAllBufferFramesSavedAsync();
            }

            string outputFileDirectory = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputFileDirectory))
            {
                Directory.CreateDirectory(outputFileDirectory);
            }

            int exitCode = 0;

            string exportVideoCmdArguments =
                string.Format(exportVideoCmdArgumentsFormat,
                    sequenceFolderPath, outputFilePath);

            ProcessStartInfo startInfo =
                ProcessUtil.CreateProcessStartInfo(
                    ffmpegExePath,
                    exportVideoCmdArguments,
                    AppGlobal.ExeDirectory);

            using (Process proc = Process.Start(startInfo))
            {
                ProcessUtil.OutputFromProcess(proc);

                //await proc.WaitForExitAsync();
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }

            return exitCode;
        }

        #endregion


        #region public methods

        public async Task WaitingForAllBufferFramesSavedAsync()
        {
            await Task.Run(() =>
            {
                while (!IsAllBufferFramesSaved)
                {
                    continue;
                }
            });
        }

        public void ClearBuffer()
        {
            // TODO: file used by others exception?
            try
            {
                FileHelperDLL.FileHelper.DeleteAllFilesInDirectorySafe(videoBufferFolderPath);
            }
            catch (Exception ex)
            {

            }
        }

        public bool StartRecording()
        {
            bool isRecordingStarted = false;

            // ensure all buffer images from previous recording processes are saved
            // before starting new recording            
            if (IsAllBufferFramesSaved)
            {
                kinectManagerClient.ColorBitmapArrived += HandleColorBitmapArrived;
                ClearBuffer();
                StartQueue();
                isRecordingStarted = true;
            }

            return isRecordingStarted;
        }

        public void StopRecording()
        {
            StopQueue();
            kinectManagerClient.ColorBitmapArrived -= HandleColorBitmapArrived;
        }

        public async Task<int> ExportVideoAndClearBufferAsync(string outputFilePath)
        {            
            int exitCode = await ExportVideoAsync(videoBufferFolderPath, outputFilePath);
            ClearBuffer();
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
