using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for NewWall.xaml
    /// </summary>
    public partial class NewWall : Page
    {
        #region constants

        private const string RockOverlapsWarningMsg = 
            "Please set a smaller rock size to avoid overlaps among rocks!";

        #endregion


        #region global objects & variables

        private SpaceMode _mode = SpaceMode.Color;

        // declare Kinect object and frame reader
        private KinectManager kinectManagerClient;

        private float colorWidth, colorHeight, depthWidth, depthHeight;

        /// <summary>
        /// In NewWall mode, depthFrame must be used;
        /// Intermediate storage for the colorpoints to be mapped to depthframe
        /// </summary>
        private DepthSpacePoint[] colorMappedToDepthSpace;

        /// <summary>
        /// Instantaneous storage of frame data
        /// </summary>
        private ushort[] lastNotNullDepthData;
        private byte[] lastNotNullColorData;

        /// <summary>
        ///Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// The size in bytes of the bitmap back buffer
        /// </summary>
        private uint bitmapBackBufferSize = 0;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private IList<Body> _bodies;

        private bool _displayBody = false;
        //private bool _mirror = false;

        private KinectWall jcWall;

        private RocksOnWallViewModel rocksOnWallViewModel;

        private int newWallNo;

        private bool isSnapShotTaken = false;

        private MainWindow myMainWindowParent;

        #endregion


        public NewWall()
        {
           
            InitializeComponent();

            // set navHead
            newWallNo = WallDataAccess.LargestWallNo + 1;
            navHead.HeaderRowTitle = string.Format("Scan KinectWall - {0}", newWallNo);
            navHead.ParentPage = this;
        }

        private void InitialiseCommands()
        {
            snapshotWallBtn.Command = new RelayCommand(
                SnapShotWall, CanSnapShotWall);
            deselectRockBtn.Command = new RelayCommand(
                DeselectRock, CanDeselectRock);
            removeRockBtn.Command = new RelayCommand(
                RemoveRock, CanRemoveRock);
            removeAllRocksBtn.Command = new RelayCommand(
                RemoveAllRocks, CanRemoveAllRocks);            
            saveWallBtn.Command = new RelayCommand(
                SaveWall, CanSaveWall);
        }


        #region command methods executed when button clicked

        private bool CanSnapShotWall(object parameter = null)
        {
            return true;
        }

        private bool CanDeselectRock(object parameter = null)
        {
            return rocksOnWallViewModel.SelectedRock != null;
        }

        private bool CanRemoveRock(object parameter = null)
        {
            return rocksOnWallViewModel.SelectedRock != null;
        }

        private bool CanRemoveAllRocks(object parameter = null)
        {
            return rocksOnWallViewModel.AnyRocksInList();
        }

        private bool CanSaveWall(object parameter = null)
        {
            return rocksOnWallViewModel.AnyRocksInList();
        }

        private void SnapShotWall(object parameter = null)
        {
            if (rocksOnWallViewModel.AnyRocksInList())
            {
                rocksOnWallViewModel.RemoveAllRocks();
            }

            isSnapShotTaken = jcWall.SnapShotWallData(
                colorMappedToDepthSpace, lastNotNullDepthData, lastNotNullColorData);

            if (isSnapShotTaken)
            {
                UiHelper.NotifyUser("Snap shot taken.");
            }
        }

        private void DeselectRock(object parameter = null)
        {
            rocksOnWallViewModel.DeselectRock();
        }

        private void RemoveRock(object parameter = null)
        {
            rocksOnWallViewModel.RemoveSelectedRock();
        }

        private void RemoveAllRocks(object parameter = null)
        {
            // Display message box
            string messageBoxText = "Do you want to remove all rocks?";
            string caption = "Remove All Rocks Confirmation";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    rocksOnWallViewModel.RemoveAllRocks();
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

        private void SaveWall(object parameter = null)
        {
            if (isSnapShotTaken)
            {
                string newWallKey = rocksOnWallViewModel.SaveRocksOnWall(newWallNo.ToString());
                jcWall.WallID = newWallKey;
                jcWall.SaveWallData();
                AppGlobal.WallID = newWallKey;

                RouteSetModeSelectDialog routeSetModeSelect = new RouteSetModeSelectDialog();
                bool dialogResult = routeSetModeSelect.ShowDialog().GetValueOrDefault(false);

                if (dialogResult)
                {
                    RouteSet routeSetPage = new RouteSet(routeSetModeSelect.ClimbModeSelected);
                    this.NavigationService.Navigate(routeSetPage);
                }
            }
            else
            {
                UiHelper.NotifyUser("Please Take a Snapshot of the Wall Before Saving");
            }
        }

        #endregion


        #region event handlers

        public void Page_Loaded(object sender, RoutedEventArgs e)
        {
            myMainWindowParent = this.Parent as MainWindow;

            kinectManagerClient = myMainWindowParent.KinectManagerClient;
            if (kinectManagerClient.kinectSensor != null)
            {
                //Unsubsricbe playground handler to in Mainwindow class and use local handler instead
                kinectManagerClient.ColorImageSourceArrived -= myMainWindowParent.HandleColorImageSourceArrived;

                kinectManagerClient.multiSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                colorWidth = KinectExtensions.FrameDimensions[SpaceMode.Color].Item1;
                colorHeight = KinectExtensions.FrameDimensions[SpaceMode.Color].Item2;
                depthWidth = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1;
                depthHeight = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item2;

                colorMappedToDepthSpace = new DepthSpacePoint[(int)(colorWidth * colorHeight)];
                lastNotNullDepthData = new ushort[(int)depthWidth * (int)depthHeight];
                lastNotNullColorData = new byte[(int)colorWidth * (int)colorHeight * PixelFormats.Bgr32.BitsPerPixel / 8];

                bitmap = new WriteableBitmap((int)depthWidth, (int)depthHeight, 96.0, 96.0, PixelFormats.Bgra32, null);

                // Calculate the WriteableBitmap back buffer size
                bitmapBackBufferSize = (uint)((bitmap.BackBufferStride * (bitmap.PixelHeight - 1)) + (bitmap.PixelWidth * bytesPerPixel));
            }
            else
            {
                Console.WriteLine("Kinect not available!");
            }

            //kinectSensor.Open();
            //jcWall = new KinectWall(canvas, kinectSensor.CoordinateMapper);
            //rocksOnWallViewModel = new RocksOnWallViewModel(canvas, kinectSensor.CoordinateMapper);


            jcWall = new KinectWall(canvas, kinectManagerClient.kinectSensor.CoordinateMapper);
            rocksOnWallViewModel = new RocksOnWallViewModel(canvas, kinectManagerClient.kinectSensor.CoordinateMapper);

            InitialiseCommands();
        }

        private void Page_Unloaded(object sender, EventArgs e)
        {
            kinectManagerClient.multiSourceReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;
    
            if (kinectManagerClient.multiSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                kinectManagerClient.multiSourceReader.Dispose();
                kinectManagerClient.multiSourceReader = null;
            }
        }        
       
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isSnapShotTaken)
            {
                Point mouseClickPt = e.GetPosition(cameraIMG);

                RockViewModel rockCorrespondsToCanvasPt =
                    rocksOnWallViewModel.GetRockInListByCanvasPoint(mouseClickPt);

                if (rockCorrespondsToCanvasPt == null)  // new rock
                {
                    Size newBoulderSizeOnCanvas = GetNewBoulderSizeOnCanvasFromSliders();
                    
                    // check rock overlaps
                    if (rocksOnWallViewModel.IsOverlapWithRocksOnWall(
                            mouseClickPt, newBoulderSizeOnCanvas)
                        == false)
                    {
                        CameraSpacePoint csp = jcWall.GetCamSpacePointFromMousePoint(mouseClickPt, _mode);
                        if (!csp.Equals(default(CameraSpacePoint)))
                        {
                            rocksOnWallViewModel.AddRock(csp, newBoulderSizeOnCanvas);
                        }
                        else
                        {
                            UiHelper.NotifyUser("No depth info is captured for this point!");
                        }
                    }
                    else
                    {
                        UiHelper.NotifyUser(RockOverlapsWarningMsg);
                    }
                }
                else  // rock already in list
                {
                    rocksOnWallViewModel.SelectedRock = rockCorrespondsToCanvasPt;
                    boulderWidthSlider.Value = rockCorrespondsToCanvasPt.BoulderShape.Width;
                    boulderHeightSlider.Value = rockCorrespondsToCanvasPt.BoulderShape.Height;
                }
            }
            else
            {
                UiHelper.NotifyUser("Please take snap shot first.");
            }
        }

        private void boulderSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider boulderSizeSlider = sender as Slider;

            if (boulderSizeSlider.Value == 0)
            {
                UiHelper.NotifyUser("Zero size is not allowed.");
                boulderSizeSlider.Value = boulderSizeSlider.Minimum + boulderSizeSlider.TickFrequency;
            }

            if (rocksOnWallViewModel != null && rocksOnWallViewModel.SelectedRock != null)
            {
                Size newBoulderSizeOnCanvas = GetNewBoulderSizeOnCanvasFromSliders();

                // check rock overlaps
                if (rocksOnWallViewModel.IsOverlapWithRocksOnWallOtherThanSelectedRock(
                        rocksOnWallViewModel.SelectedRock.BCanvasPoint, newBoulderSizeOnCanvas)
                    == false)
                {
                    string boulderSizeSliderName = boulderSizeSlider.Name;
                    switch (boulderSizeSliderName)
                    {
                        case "boulderHeightSlider":
                            rocksOnWallViewModel.ChangeHeightOfSelectedRock(newBoulderSizeOnCanvas.Height);
                            break;
                        case "boulderWidthSlider":
                        default:
                            rocksOnWallViewModel.ChangeWidthOfSelectedRock(newBoulderSizeOnCanvas.Width);
                            break;
                    }
                }
                else
                {
                    UiHelper.NotifyUser(RockOverlapsWarningMsg);

                    // restore original value
                    boulderSizeSlider.Value -= boulderSizeSlider.TickFrequency;
                }
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
                    colorFrame.CopyConvertedFrameDataToArray(lastNotNullColorData, ColorImageFormat.Bgra);
                    myMainWindowParent.PlaygroundWindow.ShowImage(newWallColorBitmapSrc);
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

                        depthFrame.CopyFrameDataToArray(lastNotNullDepthData);
                    }   
                }
            }
        }
        #endregion

        #region slider value converters

        private static double ConvertSliderValueToSizeOnCanvas(double sliderValue)
        {
            double multiplicationFactor = 1;
            return multiplicationFactor * sliderValue;
        }

        private Size GetNewBoulderSizeOnCanvasFromSliders()
        {
            return new Size
            (
                ConvertSliderValueToSizeOnCanvas(boulderWidthSlider.Value),
                ConvertSliderValueToSizeOnCanvas(boulderHeightSlider.Value)
            );
        }

        #endregion
    }
}
