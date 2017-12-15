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

        public event EventHandler<DepthImgSrcEventArgs> DepthImageSourceArrived;
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

        public event EventHandler<InfraredImgSrcEventArgs> InfraredImageSourceArrived;
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

        public event EventHandler<BodyListArrEventArgs> BodyFrameArrived;
        public class BodyListArrEventArgs
        {
            private readonly IList<Body> kinectBodies;

            public BodyListArrEventArgs(IList<Body> bodies)
            {
                kinectBodies = bodies;
            }

            public IList<Body> GetBodyList()
            {
                return kinectBodies;
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

                if (BodyFrameArrived != null)
                {
                    using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null)
                        {
                            IList <Body> kinectBodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
                            bodyFrame.GetAndRefreshBodyData(kinectBodies);
                            BodyFrameArrived(sender, new BodyListArrEventArgs(kinectBodies));
                        }
                    }
                }
            }

        }

        //ColorFrame Stream to Image
        public BitmapSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat pixelFormat = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height *( (pixelFormat.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            return KinectExtensions.ToBitmap(pixels, width, height, pixelFormat);      
        }

        //DepthFrame Stream to Image
        public BitmapSource ToBitmap(DepthFrame frame, bool reliable)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] depthData = new ushort[width * height];          
            frame.CopyFrameDataToArray(depthData);

            return KinectExtensions.ToBitmap(depthData, width, height, reliable);
        }

        //InfraredFrame Stream to Image
        public BitmapSource ToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] frameData = new ushort[width * height];
            frame.CopyFrameDataToArray(frameData);

            return KinectExtensions.ToBitmap(frameData, width, height);
        }
    }



}
