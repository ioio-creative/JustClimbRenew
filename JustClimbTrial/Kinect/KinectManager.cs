using JustClimbTrial.Helpers;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Kinect
{
    //Windows.Media.Imaging.BitmapSource
    public class ColorBitmapSrcEventArgs : EventArgs
    {
        private readonly BitmapSource colorBitmapSrc;

        public ColorBitmapSrcEventArgs(BitmapSource ColorBitmapSrc)
        {
            colorBitmapSrc = ColorBitmapSrc;
        }

        public BitmapSource GetColorBitmapSrc()
        {
            return colorBitmapSrc;
        }
    }
    //Drawing.Bitmap
    public class ColorBitmapEventArgs
    {
        private readonly Bitmap colorBitmap;

        public ColorBitmapEventArgs(Bitmap ColorBitmap)
        {
            colorBitmap = ColorBitmap;
        }

        public Bitmap GetColorBitmap()
        {
            return colorBitmap;
        }
    }
    public class DepthImgSrcEventArgs : EventArgs
    {
        private readonly BitmapSource depthBitmapSrc;

        public DepthImgSrcEventArgs(BitmapSource DepthBitmapSrc)
        {
            depthBitmapSrc = DepthBitmapSrc;
        }

        public BitmapSource GetDepthBitmapSrc()
        {
            return depthBitmapSrc;
        }
    }
    public class InfraredImgSrcEventArgs : EventArgs
    {
        private readonly BitmapSource infraredBitmapSrc;

        public InfraredImgSrcEventArgs(BitmapSource InfraredBitmapSrc)
        {
            infraredBitmapSrc = InfraredBitmapSrc;
        }

        public BitmapSource GetInfraredBitmapSrc()
        {
            return infraredBitmapSrc;
        }
    }
    public class BodyListArrEventArgs
    {
        private readonly IList<Body> kinectBodies;
        private readonly Vector4 floorClipPlane;

        public BodyListArrEventArgs(IList<Body> bodies, Vector4 aFloorClipPlane)
        {
            kinectBodies = bodies;
            floorClipPlane = aFloorClipPlane;
        }

        public IList<Body> GetBodyList()
        {
            return kinectBodies;
        }

        public Vector4 GetFloorClipPlane()
        {
            return floorClipPlane;
        }
    }

    public class KinectManager
    {
        public KinectSensor kinectSensor;
        public MultiSourceFrameReader multiSourceReader;
        public MultiSourceFrame multiSourceFrame;
        public CoordinateMapper ManagerCoorMapper
        {
            get
            {
               return kinectSensor.CoordinateMapper;
            }
        }

        #region BitmapSrc EventHandlers

        public event EventHandler<ColorBitmapSrcEventArgs> ColorImageSourceArrived;          
        //!!! we create another event specifically to handle color bitmap as System.Drawing.Bitmap
        //This is only for saving img and video files
        public event EventHandler<ColorBitmapEventArgs> ColorBitmapArrived;
        public event EventHandler<DepthImgSrcEventArgs> DepthImageSourceArrived;    
        public event EventHandler<InfraredImgSrcEventArgs> InfraredImageSourceArrived;
        public event EventHandler<BodyListArrEventArgs> BodyFrameArrived;
        

        #endregion

        public KinectManager()
        {          
            if (!IsKinectConnected())
            {
                UiHelper.NotifyUser("Kinect Unfound" + Environment.NewLine + "Please connect Kinect device and restart application.");
                Application.Current.Shutdown();                
            }

            // initialize Kinect object
            kinectSensor = KinectSensor.GetDefault();
        }

        //https://stackoverflow.com/a/28213157
        //Kinect 2 For Windows
        private const string HardwareId = @"VID_045E&PID_02D9";
        private const string WmiQuery = @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%{0}%'";
        
        public static bool IsKinectConnected()
        {
            // Use WMI to find devices with the proper hardware id for the Kinect2
            // note that one Kinect2 is listed as three harwdare devices    
            string query = String.Format(WmiQuery, HardwareId);
            using (var searcher = new ManagementObjectSearcher(query))
            {
                using (var collection = searcher.Get())
                {
                    return collection.Count > 0;
                }
            }
        }

        public bool OpenKinect()
        {
            bool isSuccess = false;
           
            if (!kinectSensor.IsOpen)
            {
                // activate sensor
                kinectSensor.Open();
            }

            if (kinectSensor.IsOpen && multiSourceReader == null)
            {
                multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                //multiSourceReader = mSrcReader;

                multiSourceReader.MultiSourceFrameArrived += Manager_MultiSourceFrameArrived;
            }

            isSuccess = kinectSensor.IsOpen && multiSourceReader != null;

            if (isSuccess)
            {
                Debug.WriteLine("Kinect activated!");
            }
            else
            {
                Debug.WriteLine("Kinect activation ERROR.");
            }
            return isSuccess;
        }

        public void CloseKinect()
        {
            if (multiSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                multiSourceReader.Dispose();
                multiSourceReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }

        public void PauseMultiSrcReader()
        {
            multiSourceReader.IsPaused = true;
        }

        public void UnpauseMultiSrcReader()
        {
            multiSourceReader.IsPaused = false;
        }

        public void Manager_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                //Fire Img Srcs upon subscription
                if (ColorBitmapArrived != null || ColorImageSourceArrived != null)
                {
                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            if (ColorImageSourceArrived != null)
                            {
                                BitmapSource colorImgSrc = ToBitmapSrc(colorFrame);
                                ColorImageSourceArrived(sender, new ColorBitmapSrcEventArgs(colorImgSrc));
                            }

                            if (ColorBitmapArrived != null)
                            {
                                using (Bitmap colorBitmap = ToBitmap(colorFrame))
                                {
                                    //Recording Frame Dimensions Hard-Coded!!!!
                                    using (Bitmap resizedBitmap = ResizeBitmap(colorBitmap, 640, 360))
                                    {
                                        ColorBitmapArrived(sender, new ColorBitmapEventArgs(resizedBitmap));
                                    }
                                }
                            }
                        }
                    }
                }

                if (DepthImageSourceArrived != null)
                {
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                        {
                            BitmapSource depthImgSrc = ToBitmapSrc(depthFrame, true);
                            DepthImageSourceArrived(sender, new DepthImgSrcEventArgs(depthImgSrc));
                        }
                    }
                }
                if (InfraredImageSourceArrived != null)
                {
                    using (InfraredFrame infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame())
                    {
                        if (infraredFrame != null)
                        {
                            BitmapSource irImgSrc = ToBitmapSrc(infraredFrame);
                            InfraredImageSourceArrived(sender, new InfraredImgSrcEventArgs(irImgSrc));
                        }
                    }
                }

                if (BodyFrameArrived != null)
                {
                    using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                            IList <Body> kinectBodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
                            bodyFrame.GetAndRefreshBodyData(kinectBodies);
                            BodyFrameArrived(sender, new BodyListArrEventArgs(kinectBodies, bodyFrame.FloorClipPlane));
                        }
                    }
                }
            }
        }

        //ColorFrame Stream to Image
        public BitmapSource ToBitmapSrc(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            System.Windows.Media.PixelFormat pixelFormat = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height *( (pixelFormat.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }
            return KinectExtensions.ToBitmapSrc(pixels, width, height, pixelFormat);      
        }

        //ColorFrane Stream to System.Drawing.Bitmap
        //this method uses InPtr so no method is called from KinectExtension
        public Bitmap ToBitmap(ColorFrame frame)
        {
            System.Drawing.Imaging.PixelFormat bitmapPixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;

            Bitmap bitmap = new Bitmap((int)KinectExtensions.FrameDimensions[SpaceMode.Color].Item1, (int)KinectExtensions.FrameDimensions[SpaceMode.Color].Item2, bitmapPixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr bitmapPtr = bitmapData.Scan0;
            // verify data and write the new color frame data to the display bitmap
            if ((frame.FrameDescription.Width == bitmap.Width) && (frame.FrameDescription.Height == bitmap.Height))
            {
                frame.CopyConvertedFrameDataToIntPtr(
                    bitmapPtr,
                    (uint)(frame.FrameDescription.Width * frame.FrameDescription.Height * 4),
                    ColorImageFormat.Bgra);
            }
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        //DepthFrame Stream to Image
        public BitmapSource ToBitmapSrc(DepthFrame frame, bool reliable)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] depthData = new ushort[width * height];          
            frame.CopyFrameDataToArray(depthData);

            return KinectExtensions.ToBitmapSrc(depthData, width, height, reliable);
        }

        //InfraredFrame Stream to Image
        public BitmapSource ToBitmapSrc(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] frameData = new ushort[width * height];
            frame.CopyFrameDataToArray(frameData);

            return KinectExtensions.ToBitmapSrc(frameData, width, height);
        }

        public static Bitmap ResizeBitmap(Bitmap srcBitmap, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.DrawImage(srcBitmap, 0, 0, width, height);
            }
            return result;
        }
    }
}
