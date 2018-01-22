using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using JustClimbTrial.Extensions;
using System.Numerics;
using JustClimbTrial.Properties;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for WallAndFloor.xaml
    /// </summary>
    public partial class WallAndFloor : Page
    {
        private MainWindow myMainWindowParent;
        // declare Kinect object and frame reader
        private KinectManager kinectManagerClient;

        private float colorWidth, colorHeight, depthWidth, depthHeight;
        /// <summary>
        /// In NewWall mode, depthFrame must be used;
        /// Intermediate storage for the colorpoints to be mapped to depthframe
        /// </summary>
        private DepthSpacePoint[] colorMappedToDepthSpace;
        private ushort[] kinectDepthData;

        private Plane wallPlane;
        private Plane floorPlane;
        private Vector3[] threePoints = new Vector3[3];
        private Ellipse[] threePtEllipses = new Ellipse[3];
        private int threePtIdxCnt = 0;

        public WallAndFloor()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            myMainWindowParent = Parent as MainWindow;

            kinectManagerClient = myMainWindowParent.KinectManagerClient;
            if (kinectManagerClient.kinectSensor != null)
            {
                kinectManagerClient.multiSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                colorWidth = KinectExtensions.FrameDimensions[SpaceMode.Color].Item1;
                colorHeight = KinectExtensions.FrameDimensions[SpaceMode.Color].Item2;
                depthWidth = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1;
                depthHeight = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item2;

                colorMappedToDepthSpace = new DepthSpacePoint[(int)(colorWidth * colorHeight)];
                kinectDepthData =  new ushort[(int)depthWidth * (int)depthHeight];
            }
            else
            {
                //Debug.WriteLine("Kinect not available!");
                UiHelper.NotifyUser("Kinect not available!");
            }

            //Settings settings = new Settings();
            //UiHelper.NotifyUser(settings.WallPlaneStr);

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.multiSourceReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;

            if (kinectManagerClient.multiSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                kinectManagerClient.multiSourceReader.Dispose();
                kinectManagerClient.multiSourceReader = null;
            }
        }

        private void ConfigWallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!threePoints.Where(x => x == default(Vector3)).Any())
            {
                wallPlane = Plane.CreateFromVertices(threePoints[0], threePoints[1], threePoints[2]);
                threePoints = new Vector3[3];
                canvas.Children.Clear();
            }
        }

        private void ConfigFloorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!threePoints.Where(x => x == default(Vector3)).Any())
            {
                floorPlane = Plane.CreateFromVertices(threePoints[0], threePoints[1], threePoints[2]);
                threePoints = new Vector3[3];
                canvas.Children.Clear();
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            //Removed last entered point
            threePtIdxCnt = ( (threePtIdxCnt - 1) + 3 ) % 3;
            if (threePoints[threePtIdxCnt] != default(Vector3))
            {
                if (threePtEllipses[threePtIdxCnt] != null)
                {
                    canvas.RemoveChild(threePtEllipses[threePtIdxCnt]);
                }
                threePoints[threePtIdxCnt] = default(Vector3); 
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if ( wallPlane != default(System.Numerics.Plane) && floorPlane != default(System.Numerics.Plane))
            {
                Settings settings = new Settings();

                settings.WallPlaneStr = string.Format("{0},{1},{2},{3}",
                    wallPlane.Normal.X,
                    wallPlane.Normal.Y,
                    wallPlane.Normal.Z,
                    wallPlane.D);

                settings.FloorPlaneStr = string.Format("{0},{1},{2},{3}",
                    floorPlane.Normal.X,
                    floorPlane.Normal.Y,
                    floorPlane.Normal.Z,
                    floorPlane.D);

                settings.Save();

                UiHelper.NotifyUser("Wall and Floor Saved as Planes");
            }
            else if( wallPlane != default(System.Numerics.Plane) )
            {
                UiHelper.NotifyUser("Please Configure Points on Wall");
            }
            else if( floorPlane != default(System.Numerics.Plane))
            {
                UiHelper.NotifyUser("Please Configure Points on Floor");
            }
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {          
            Point mouseClickPt = e.GetPosition(cameraIMG);
            CameraSpacePoint mouseCamSP = GetCamSpacePtFromMouseClick(mouseClickPt, SpaceMode.Color);
            if (!mouseCamSP.Equals(default(CameraSpacePoint)))
            {
                if (threePtEllipses[threePtIdxCnt] != null)
                {
                    canvas.RemoveChild(threePtEllipses[threePtIdxCnt]);
                }

                threePoints[threePtIdxCnt] = new Vector3(mouseCamSP.X, mouseCamSP.Y, mouseCamSP.Z);
                threePtEllipses[threePtIdxCnt] = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = Brushes.Red
                };
                canvas.SetLeftAndTopForShapeWrtCentre(threePtEllipses[threePtIdxCnt], mouseClickPt);
                canvas.AddChild(threePtEllipses[threePtIdxCnt]);

                threePtIdxCnt++;
                threePtIdxCnt %= 3;
            }
            else
            {
                UiHelper.NotifyUser("No depth info is captured for this point!");
            }


        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var mSourceFrame = kinectManagerClient.multiSourceFrame;
            // If the Frame has expired by the time we process this event, return.
            if (mSourceFrame == null)
            {
                return;
            }

            using (ColorFrame colorFrame = mSourceFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    BitmapSource newWallColorBitmapSrc = kinectManagerClient.ToBitmapSrc(colorFrame);
                    cameraIMG.Source = newWallColorBitmapSrc;     
                }
            }

            using (DepthFrame depthFrame = mSourceFrame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // Access the depth frame data directly via LockImageBuffer to avoid making a copy
                    using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                    {
                        kinectManagerClient.kinectSensor.CoordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                            depthFrameData.UnderlyingBuffer,
                            depthFrameData.Size,
                            colorMappedToDepthSpace);

                        depthFrame.CopyFrameDataToArray(kinectDepthData);
                    }
                }
            }
        }

        private CameraSpacePoint GetCamSpacePtFromMouseClick(Point mousePt, SpaceMode spMode)
        {
            Tuple<float, float> dimensions = KinectExtensions.FrameDimensions[spMode];
            float x_temp = (float)(mousePt.X * dimensions.Item1 / canvas.ActualWidth);
            float y_temp = (float)(mousePt.Y * dimensions.Item2 / canvas.ActualHeight);

            DepthSpacePoint depPtFromMousePt = colorMappedToDepthSpace[(int)(x_temp + 0.5f) + (int)(y_temp + 0.5f) * (int)dimensions.Item1];
            if (depPtFromMousePt.X == float.NegativeInfinity || depPtFromMousePt.Y == float.NegativeInfinity)
            {
                return default(CameraSpacePoint);
            }

            ushort depth = kinectDepthData[(int)depPtFromMousePt.X + (int)(depPtFromMousePt.Y) * (int)KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1];
            
            return kinectManagerClient.kinectSensor.CoordinateMapper.MapDepthPointToCameraSpace(depPtFromMousePt, depth);
        }
    }
}
