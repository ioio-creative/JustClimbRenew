using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Kinect
{
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
            set { }
        }

        #region ImageSrc Events and Handlers

        public event EventHandler<ColorImgSrcEventArgs> ColorImageSourceArrived;
        public event EventHandler<DepthImgSrcEventArgs> DepthImageSourceArrived;
        public event EventHandler<InfraredImgSrcEventArgs> InfraredImageSourceArrived;
        public class ColorImgSrcEventArgs : EventArgs
        {
            private readonly BitmapSource colorBitmapSrc;

            public ColorImgSrcEventArgs(BitmapSource ColorBitmapSrc)
            {
                colorBitmapSrc = ColorBitmapSrc;
            }

            public BitmapSource GetColorBitmapSrc()
            {
                return colorBitmapSrc;
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

        #endregion

        public KinectManager()
        {
            // initialize Kinect object
            kinectSensor = KinectSensor.GetDefault();
        }

        public bool OpenKinect()
        {
            bool isSuccess = false;

            // activate sensor
            if (kinectSensor != null)
            {
                kinectSensor.Open();

                multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                Console.WriteLine("Kinect activated!");
                //multiSourceReader = mSrcReader;

                multiSourceReader.MultiSourceFrameArrived += Manager_MultiSourceFrameArrived;

                isSuccess = true;
            }
            else
            {
                Console.WriteLine("Kinect not available!");
            }

            return isSuccess;
        }

        public void CloseKinect()
        {
            //if (multiSourceReader != null)
            //{
            //    // MultiSourceFrameReder is IDisposable
            //    multiSourceReader.Dispose();
            //    multiSourceReader = null;
            //}

            //if (kinectSensor != null)
            //{
            //    kinectSensor.Close();
            //    kinectSensor = null;
            //}
        }

        public void Manager_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                //Fire Img Srcs upon subscription
                if (ColorImageSourceArrived != null)
                {
                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            BitmapSource colorImgSrc = ToBitmap(colorFrame);
                            ColorImageSourceArrived(sender, new ColorImgSrcEventArgs(colorImgSrc) ); 
                        }
                    }
                }
                if (DepthImageSourceArrived != null)
                {
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                        {
                            BitmapSource depthImgSrc = ToBitmap(depthFrame, true);
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
                            BitmapSource irImgSrc = ToBitmap(infraredFrame);
                            InfraredImageSourceArrived(sender, new InfraredImgSrcEventArgs(irImgSrc));
                        }
                    }
                }
            }

        }

        //ColorFrame Stream to Image
        public static BitmapSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }


            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        //DepthFrame Stream to Image
        public static BitmapSource ToBitmap(DepthFrame frame, bool reliable)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;
            ushort minDepth = 0;
            ushort maxDepth = ushort.MaxValue;

            if (reliable)
            {
                //minDepth = frame.DepthMinReliableDistance;
                minDepth = 2000;//frame.DepthMinReliableDistance;
                maxDepth = 5000;//frame.DepthMaxReliableDistance;
                //Console.WriteLine($"Use Reliable Depth: {minDepth}.min, {maxDepth}.max");
            }

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                //byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? 255 - ((depth - minDepth) * 256 / (maxDepth - minDepth)) : 0);
                ushort intensity = (ushort)(depth >= minDepth && depth <= maxDepth ? depth % 256 : 0);

                //pixelData[colorIndex++] = intensity; // Blue
                //pixelData[colorIndex++] = intensity; // Green
                //pixelData[colorIndex++] = intensity; // Red

                pixelData[colorIndex++] = (byte)(intensity <= 127 ? intensity * 2 : 255 - intensity * 2); // Blue
                pixelData[colorIndex++] = (byte)(intensity <= 127 ? 255 - intensity * 2 : intensity * 2); // Green
                pixelData[colorIndex++] = (byte)(intensity <= 127 ? 255 - intensity * 2 : intensity * 2); // Red



                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
        }

        //InfraredFrame Stream to Image
        public static BitmapSource ToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }
    }



}
