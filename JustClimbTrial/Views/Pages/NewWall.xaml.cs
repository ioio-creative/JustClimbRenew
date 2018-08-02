using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using JustClimbTrial.Views.UserControls;
using Microsoft.Kinect;
using System;
using System.Diagnostics;
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
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }


        #region constants

        private const string RockOverlapsWarningMsg = 
            "Rocks can't overlap each other!";

        private const string DepthInfoMissingWarningMsg = 
            "No depth info is captured for this point!";

        private const DragDropEffects supportedDragDropEffects = DragDropEffects.Move;

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


        private KinectWall jcWall;

        private RocksOnWallViewModel rocksOnWallViewModel;

        private int newWallNo;

        private bool isSnapShotTaken = false;

        private MainWindow mainWindowClient;

        #endregion


        #region constructors 

        public NewWall()
        {           
            InitializeComponent();
            
            newWallNo = WallDataAccess.LargestWallNo + 1;            
        }

        #endregion


        #region initialization

        private void InitializeCommands()
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

        private void InitializeNavHead()
        {
            HeaderRowNavigation navHead = master.NavHead;
            navHead.HeaderRowTitle = string.Format("Scan KinectWall - {0}", newWallNo);
            navHead.ParentPage = this;
        }

        #endregion


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
            InitializeNavHead();

            mainWindowClient = this.Parent as MainWindow;

            kinectManagerClient = mainWindowClient.KinectManagerClient;
            if (kinectManagerClient.kinectSensor != null)
            {                
                kinectManagerClient.multiSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                colorWidth = KinectExtensions.FrameDimensions[SpaceMode.Color].Item1;
                colorHeight = KinectExtensions.FrameDimensions[SpaceMode.Color].Item2;
                depthWidth = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1;
                depthHeight = KinectExtensions.FrameDimensions[SpaceMode.Depth].Item2;

                colorMappedToDepthSpace = new DepthSpacePoint[(int)(colorWidth * colorHeight)];
                lastNotNullDepthData = new ushort[(int)depthWidth * (int)depthHeight];
                lastNotNullColorData = new byte[(int)colorWidth * (int)colorHeight * PixelFormats.Bgr32.BitsPerPixel / 8];
            }
            else
            {
                UiHelper.NotifyUser("Kinect not available!");
            }

            //kinectSensor.Open();
            //jcWall = new KinectWall(canvas, kinectSensor.CoordinateMapper);
            //rocksOnWallViewModel = new RocksOnWallViewModel(canvas, kinectSensor.CoordinateMapper);

            jcWall = new KinectWall(canvas, kinectManagerClient.kinectSensor.CoordinateMapper);
            rocksOnWallViewModel = new RocksOnWallViewModel(canvas, kinectManagerClient.kinectSensor.CoordinateMapper);

            InitializeCommands();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.multiSourceReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;

            kinectManagerClient.multiSourceFrame = null;

            colorMappedToDepthSpace = null;
            lastNotNullColorData = null;
            lastNotNullDepthData = null;
            jcWall = null;
            GC.Collect();
        }        
       
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {            
            if (isSnapShotTaken)
            {
                //Debug.WriteLine("canvas_MouseDown");

                Point mouseClickPt = e.GetPosition(cameraIMG);

                RockViewModel rockCorrespondsToCanvasPt =
                    rocksOnWallViewModel.GetRockInListByCanvasPoint(mouseClickPt);

                if (rockCorrespondsToCanvasPt == null)  // new rock
                {
                    Size newBoulderSizeOnCanvas = GetBoulderSizeOnCanvasFromSliders();
                    
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
                            UiHelper.NotifyUser(DepthInfoMissingWarningMsg);
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
                    boulderWidthSlider.Value = rockCorrespondsToCanvasPt.RockShapeContainer.GetWidth();
                    boulderHeightSlider.Value = rockCorrespondsToCanvasPt.RockShapeContainer.GetHeight();

                    // if rock already in list, enable drag drop!
                    rockCorrespondsToCanvasPt.RockShapeContainer.DoDragDrop();
                }
            }
            else
            {
                UiHelper.NotifyUser("Please take snap shot first.");
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-enabling-drag-and-drop-on-a-user-control
        private void canvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(MyRockShape.RockViewModelDataFormatName))
            {
                //Debug.WriteLine("canvas_DragOver");

                bool isAllowedDragMove = false;

                // Canvas inherits Panel
                Panel _canvas = sender as Panel;
                RockViewModel selectedRock = rocksOnWallViewModel.SelectedRock;

                if (_canvas != null && selectedRock != null)
                {
                    Point mousePt = e.GetPosition(cameraIMG);

                    // check rock overlaps
                    if (rocksOnWallViewModel.IsOverlapWithRocksOnWallOtherThanSelectedRock(
                            mousePt, selectedRock.SizeOnCanvas)
                        == false)
                    {
                        CameraSpacePoint csp = jcWall.GetCamSpacePointFromMousePoint(mousePt, _mode);
                        if (!csp.Equals(default(CameraSpacePoint)))
                        {
                            // note: 
                            // use RocksOnWallViewModel.MoveSelectedRock() 
                            // instead of RockViewModel.MoveBoulder()
                            // because RocksOnWallViewModel.MoveSelectedRock()
                            // will set the selected rock indicator as well
                            isAllowedDragMove = rocksOnWallViewModel.MoveSelectedRock(csp);                            
                        }
                    }
                }

                // These Effects values are used in the drag source's
                // GiveFeedback event handler to determine which cursor to display.
                if (isAllowedDragMove)
                {                    
                    e.Effects = supportedDragDropEffects;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-enabling-drag-and-drop-on-a-user-control
        private void canvas_Drop(object sender, DragEventArgs e)
        {
            // If an element in the panel has already handled in the drop,
            // the panel should not also handle it.
            if (!e.Handled && e.Data.GetDataPresent(MyRockShape.RockViewModelDataFormatName))
            {
                bool isAllowedDragMove = false;

                // Canvas inherits Panel
                Panel _canvas = sender as Panel;
                RockViewModel selectedRock = rocksOnWallViewModel.SelectedRock;

                if (_canvas != null && selectedRock != null)
                {
                    Point mousePt = e.GetPosition(cameraIMG);                    

                    // check rock overlaps
                    if (rocksOnWallViewModel.IsOverlapWithRocksOnWallOtherThanSelectedRock(
                            mousePt, selectedRock.SizeOnCanvas)
                        == false)
                    {
                        CameraSpacePoint csp = jcWall.GetCamSpacePointFromMousePoint(mousePt, _mode);
                        if (!csp.Equals(default(CameraSpacePoint)))
                        {
                            // note: 
                            // use RocksOnWallViewModel.MoveSelectedRock() 
                            // instead of RockViewModel.MoveBoulder()
                            // because RocksOnWallViewModel.MoveSelectedRock()
                            // will set the selected rock indicator as well
                            isAllowedDragMove = rocksOnWallViewModel.MoveSelectedRock(csp);

                            if (!isAllowedDragMove)
                            {
                                UiHelper.NotifyUser(DepthInfoMissingWarningMsg);
                            }
                        }
                        else
                        {
                            UiHelper.NotifyUser(DepthInfoMissingWarningMsg);
                        }
                    }
                    else
                    {
                        UiHelper.NotifyUser(RockOverlapsWarningMsg);
                    }
                }

                if (isAllowedDragMove)
                {
                    e.Effects = supportedDragDropEffects;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
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
                Size newBoulderSizeOnCanvas = GetBoulderSizeOnCanvasFromSliders();

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

            if (lastNotNullColorData != null)
            {
                using (ColorFrame colorFrame = mSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (colorFrame != null)
                    {
                        BitmapSource newWallColorBitmapSrc = kinectManagerClient.ToBitmapSrc(colorFrame);
                        cameraIMG.Source = newWallColorBitmapSrc;
                        colorFrame.CopyConvertedFrameDataToArray(lastNotNullColorData, ColorImageFormat.Bgra);
                        mainWindowClient.ShowImageInPlaygroundCanvas(newWallColorBitmapSrc);
                    }
                } 
            }

            if (colorMappedToDepthSpace != null && lastNotNullDepthData != null)
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

                            depthFrame.CopyFrameDataToArray(lastNotNullDepthData);
                        }
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

        private Size GetBoulderSizeOnCanvasFromSliders()
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
