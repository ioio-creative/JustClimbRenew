using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JustClimbTrial.Kinect
{
    public enum SpaceMode
    {
        Color,
        Depth,
        Infrared
    }

    public static class KinectExtensions
    {
        //Frame Dimensions of different Frame Sources
        public static Dictionary<SpaceMode, Tuple<float, float>> FrameDimensions = new Dictionary<SpaceMode, Tuple<float, float>>
        {
            //Kinect V2.0 Color Space Dimension: 1920x1080 FullHD
            { SpaceMode.Color, new Tuple<float,float>(1920f, 1080f) },
            //Depth bitmap dimension: 512×424
            { SpaceMode.Depth, new Tuple<float,float>(512f, 424f) }
        };


        //Joint pairings for drawing Lines
        private static Tuple<JointType, JointType>[] StandardJointLines = new Tuple<JointType, JointType>[]
        {
            new Tuple<JointType, JointType>( JointType.Head, JointType.Neck ),
            new Tuple<JointType, JointType>( JointType.Neck, JointType.SpineShoulder ),
            new Tuple<JointType, JointType>( JointType.SpineShoulder, JointType.ShoulderLeft ),
            new Tuple<JointType, JointType>( JointType.SpineShoulder, JointType.ShoulderRight ),
            new Tuple<JointType, JointType>( JointType.SpineShoulder, JointType.SpineMid ),
            new Tuple<JointType, JointType>( JointType.ShoulderLeft, JointType.ElbowLeft ),
            new Tuple<JointType, JointType>( JointType.ShoulderRight, JointType.ElbowRight ),
            new Tuple<JointType, JointType>( JointType.ElbowLeft, JointType.WristLeft ),
            new Tuple<JointType, JointType>( JointType.ElbowRight, JointType.WristRight ),
            new Tuple<JointType, JointType>( JointType.WristLeft, JointType.HandLeft ),
            new Tuple<JointType, JointType>( JointType.WristRight, JointType.HandRight ),
            new Tuple<JointType, JointType>( JointType.HandLeft, JointType.HandTipLeft ),
            new Tuple<JointType, JointType>( JointType.HandRight, JointType.HandTipRight ),
            new Tuple<JointType, JointType>( JointType.HandTipLeft, JointType.ThumbLeft ),
            new Tuple<JointType, JointType>( JointType.HandTipRight, JointType.ThumbRight ),
            new Tuple<JointType, JointType>( JointType.SpineMid, JointType.SpineBase ),
            new Tuple<JointType, JointType>( JointType.SpineBase, JointType.HipLeft ),
            new Tuple<JointType, JointType>( JointType.SpineBase, JointType.HipRight ),
            new Tuple<JointType, JointType>( JointType.HipLeft, JointType.KneeLeft ),
            new Tuple<JointType, JointType>( JointType.HipRight, JointType.KneeRight ),
            new Tuple<JointType, JointType>( JointType.KneeLeft, JointType.AnkleLeft ),
            new Tuple<JointType, JointType>( JointType.KneeRight, JointType.AnkleRight ),
            new Tuple<JointType, JointType>( JointType.AnkleLeft, JointType.FootLeft ),
            new Tuple<JointType, JointType>( JointType.AnkleRight, JointType.FootRight )
        };


        #region Body Draw

        
        #region Skeleton with Body.Joint<CameraSpacePoints>

        public static void DrawSkeleton(this Canvas canvas, Body body)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint);
            }

            foreach (Tuple<JointType, JointType> standardJointLine in StandardJointLines)
            {
                canvas.DrawLine(body.Joints[standardJointLine.Item1], body.Joints[standardJointLine.Item2]);
            }
        }

        public static void DrawPoint(this Canvas canvas, Joint joint)
        {
            // 1) Check whether the joint is tracked.
            if (joint.TrackingState == TrackingState.NotTracked) return;

            // 2) Map the real-world coordinates to screen pixels.
            joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            if (joint.JointType == 0)
            {
                Console.WriteLine($"Head Position in Camera Space = {joint.Position.X}, {joint.Position.Y}");
            }

            // 3) Create a WPF ellipse.
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.LightBlue)
            };

            // 4) Position the ellipse according to the joint's coordinates.       
            Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);


            // 5) Add the ellipse to the canvas.
            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Line line = new Line
            {
                X1 = first.Position.X,
                Y1 = first.Position.Y,
                X2 = second.Position.X,
                Y2 = second.Position.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.LightBlue)
            };

            canvas.Children.Add(line);
        }

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion


        #region Mapped Skeleton with <SpacePointBase> (CameraPoints Mapped to other point types)

        public static void DrawSkeleton(this Canvas canvas, Body body, CoordinateMapper mapper, SpaceMode mode)
        {
            if (body == null) return;            

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint, mapper, mode);
            }

            foreach (Tuple<JointType, JointType> standardJointLine in StandardJointLines)
            {
                canvas.DrawLine(body.Joints[standardJointLine.Item1], body.Joints[standardJointLine.Item2], mapper, mode);
            } 
        }

        public static void DrawPoint(this Canvas canvas, Joint joint, CoordinateMapper mapper, SpaceMode mode)
        {
            // 0) Check whether the joint is tracked.
            if (joint.TrackingState == TrackingState.NotTracked) return;

            SpacePointBase spPt;
            switch (mode)
            {
                case SpaceMode.Color:
                default:
                    // 1a) Convert Joint positions to Color space coordinates.
                    ColorSpacePoint colSpaceJoint = mapper.MapCameraPointToColorSpace(joint.Position);
                    spPt = new SpacePointBase(colSpaceJoint);

                    break;
                case SpaceMode.Depth:
                    // 1b) Convert Joint positions to Depth space coordinates.
                    DepthSpacePoint depSpacePoint = mapper.MapCameraPointToDepthSpace(joint.Position);
                    spPt = new SpacePointBase(depSpacePoint);
                    
                    break;
            }

            #region Joint Mapping Messages
            if (spPt.X == float.NegativeInfinity || spPt.Y == float.NegativeInfinity)
            {
                //Console.WriteLine($"Joint Mapping Error: Joint[{joint.JointType.ToString()}] ( {spPt.X} , {spPt.Y} )");
            }
            else if ((spPt.X < 0 || spPt.Y < 0 || spPt.X > FrameDimensions[mode].Item1 || spPt.Y > FrameDimensions[mode].Item2))
            {
                //Console.WriteLine($"Joint Mapping Overflow: Joint[{joint.JointType.ToString()}] ( {spPt.X} , {spPt.Y} )");
            } 
            #endregion


            //-inf meaning Joint is not detected and no corresponding mapped space point
            if (spPt.IsValid)
            {  

                // 2) Scale the mapped coordinates to window dimensions.
                spPt = spPt.ScaleTo(canvas.ActualWidth, canvas.ActualHeight, mode);
                //if (joint.JointType == 0) Console.WriteLine($"Head Position in Color Space = {spPt.X}, {spPt.Y}");

                // 3) Draw the point on Canvas
                spPt.DrawPoint(canvas); 
            }
            

        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second, CoordinateMapper mapper, SpaceMode mode)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            SpacePointBase myFirstPoint;
            SpacePointBase mySecondPoint;

            switch (mode)
            {
                case SpaceMode.Color:
                default:
                    myFirstPoint = new SpacePointBase(mapper.MapCameraPointToColorSpace(first.Position));
                    mySecondPoint = new SpacePointBase(mapper.MapCameraPointToColorSpace(second.Position));

                    break;
                case SpaceMode.Depth:
                    myFirstPoint = new SpacePointBase(mapper.MapCameraPointToDepthSpace(first.Position));
                    mySecondPoint = new SpacePointBase(mapper.MapCameraPointToDepthSpace(second.Position));

                    break;
            }

            //Both points that the line joins must be mapped correctly
            if ( 
                (   !float.IsNegativeInfinity(myFirstPoint.X) &&
                    !float.IsNegativeInfinity(myFirstPoint.Y)   )||
                (   !float.IsNegativeInfinity(mySecondPoint.X) &&
                    !float.IsNegativeInfinity(mySecondPoint.Y)  ) 
               )
            {
                myFirstPoint = myFirstPoint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight, mode);
                mySecondPoint = mySecondPoint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight, mode);

                //call static DrawLine from class SpacePointBae
                SpacePointBase.DrawLine(canvas, myFirstPoint, mySecondPoint); 
            }
        
        }

        #endregion

        
        #endregion

        #region Image Functions

        ///<summary>
        ///canvas extension function, get frame streams upon receiving frame source
        /// </summary>
        public static void Reader_MultiSourceFrameArrived(this Canvas canvas, object sender, MultiSourceFrameArrivedEventArgs e, SpaceMode _mode, Image cameraIMG, IList<Body> _bodies, CoordinateMapper coMapper, bool _displayBody)
        {
            // Get a reference to the multi-frame
            var reference = e.FrameReference.AcquireFrame();

            // Open color frame
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == SpaceMode.Color)
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

                        //pass the frame bitmap to MainWindow <Image>
                        cameraIMG.Source = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride); ;
                    }
                }
            }

            // Open depth frame
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // Do something with the frame...
                    if (_mode == SpaceMode.Depth)
                    {
                        int width = frame.FrameDescription.Width;
                        int height = frame.FrameDescription.Height;
                        PixelFormat format = PixelFormats.Bgr32;
                        ushort minDepth = 0;
                        ushort maxDepth = ushort.MaxValue;
                        bool reliable = true;

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

                        //pass the frame bitmap to MainWindow <Image>
                        //true ==> use reliable depth
                        cameraIMG.Source = BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
                    }
                }
            }

            // Open infrared frame
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // Do something with the frame...
                    if (_mode == SpaceMode.Infrared)
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

                        //pass the frame bitmap to MainWindow <Image>
                        cameraIMG.Source = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
                    }
                }
            }

            // Open body frame and draw Skeleton
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        //Do something with the body...
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                if (_displayBody)
                                {
                                    canvas.DrawSkeleton(body, coMapper, _mode);

                                }
                            }
                        }
                    }
                }
            }
        }

        #region Export Color Frame to IMG file

        public static async void colorImgSrcSavingHandler(ImageSource colorImgSrc)
        {

        }


        #endregion

        //public static byte[] ReflectImage(byte[] bitMap, int width, int height)
        //{
        //    byte[] reflection = new byte[width*height];
        //    int imagePosition = 0;
        //    // repeat for each row
        //    for (int row = 0; row < height; row++)
        //    {
        //          // read from the left edge
        //          int fromPos = imagePosition + (row * width);
        //          // write to the right edge
        //          int toPos = fromPos + width - 1;
        //          while (fromPos < width)
        //          {
        //              reflection[toPos] = bitMap[fromPos];
        //              //copy the pixel
        //              fromPos++; // move towards the middle
        //              toPos--; // move back from the right edge
        //          }
        //    }            
        //    return reflection;
        //}

        #endregion


        #region CoordinateMapper

        public static Point MapCameraSpacePointToPointOnCanvas(this CoordinateMapper coorMap, CameraSpacePoint camSP, Canvas canvas, SpaceMode spaceMode)
        {
            ColorSpacePoint colorSP = coorMap.MapCameraPointToColorSpace(camSP);
            double normedColorSPX = colorSP.X / FrameDimensions[spaceMode].Item1;
            double normedColorSPY = colorSP.Y / FrameDimensions[spaceMode].Item2;
            return new Point(normedColorSPX * canvas.Width, normedColorSPY * canvas.Height);
        }        

        #endregion
    }
}
