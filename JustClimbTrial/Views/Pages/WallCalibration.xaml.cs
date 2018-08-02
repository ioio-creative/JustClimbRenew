using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Properties;
using Microsoft.Kinect;
using System;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for WallAndFloor.xaml
    /// </summary>
    /// 
    //Remarks: If calibration not accurate in practice, should use sampling and averaging to calibrate planes
    public partial class WallCalibration : Page
    {
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }

        private MainWindow mainWindowClient;
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
        private Vector3[] threePoints = new Vector3[3];
        private Ellipse[] threePtEllipses = new Ellipse[3];
        private int threePtIdxCnt = 0;


        #region constructors

        public WallCalibration()
        {
            InitializeComponent();
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowClient = Parent as MainWindow;

            kinectManagerClient = mainWindowClient.KinectManagerClient;
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

            // pass this Page to the top row user control so it can use this Page's NavigationService
            //navHead.ParentPage = this;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.multiSourceReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;
            kinectManagerClient.multiSourceFrame = null;
            colorMappedToDepthSpace = null;
            kinectDepthData = null;

            GC.Collect();
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

                settings.Save();

                if (UiHelper.NotifyUserResult("Wall and Floor Saved as Planes. Click \"OK\" to quit Application.") == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }
            else if (wallPlane == default(System.Numerics.Plane) && floorPlane == default(System.Numerics.Plane))
            {
                UiHelper.NotifyUser("Please Configure Points on Wall and Floor");

            }
            else if( wallPlane == default(System.Numerics.Plane) )
            {
                UiHelper.NotifyUser("Please Configure Points on Wall");
            }

        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "Do you want to cancel and quit the application?";
            string caption = "Exit";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Application.Current.Shutdown();
                    break;
                case MessageBoxResult.No:
                    break;
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

            if (colorMappedToDepthSpace != null)
            {
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
        }

        #endregion


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
