using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using JustClimbTrial.Views.Windows;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for GameStart.xaml
    /// </summary>
    public partial class GameStart : Page
    {
        private string routeId;
        private ClimbMode climbMode;
        private GameStartViewModel viewModel;

        private IEnumerable<RockOnRouteViewModel> rocksOnBoulderRoute;
        private RockOnRouteViewModel startRockOnBoulderRoute;
        private RockOnRouteViewModel endRockOnBoulderRoute;
        private CameraSpacePoint[] rocksOnRouteCamSP;

        private KinectManager kinectManagerClient;
        private Playground playgroundWindow;
        private Canvas playgroundCanvas;

        private IEnumerable<Shape> skeletonShapes;
        private VideoHelper gameplayVideoClient;
        public bool IsRecording = false;


        public GameStart(string aRouteId, ClimbMode aClimbMode)
        {

            routeId = aRouteId;
            climbMode = aClimbMode;

            InitializeComponent();

            // pass cvsBoulderRouteVideos and _routeId to the view model
            viewModel = gridContainer.DataContext as GameStartViewModel;
            if (viewModel != null)
            {
                CollectionViewSource cvsVideos = gridContainer.Resources["cvsRouteVideos"] as CollectionViewSource;
                viewModel.SetCvsVideos(cvsVideos);
                viewModel.SetRouteId(aRouteId);
                viewModel.SetClimbMode(aClimbMode);
                viewModel.SetYearListFirstItem("yyyy");
                viewModel.SetMonthListFirstItem("mm");
                viewModel.SetDayListFirstItem("dd");
                viewModel.SetHourListFirstItem(new FilterHourViewModel
                {
                    Hour = -1,
                    HourString = "time"
                });
            }

            // pass this Page to the top row user control so it can use this Page's NavigationService
            navHead.ParentPage = this;

            // set titles
            Title = "Just Climb - Game Start";
            WindowTitle = Title;
            
            
        }


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadData();

            kinectManagerClient = (this.Parent as MainWindow).KinectManagerClient;
            playgroundWindow = (this.Parent as MainWindow).PlaygroundWindow;

            if ((this.Parent as MainWindow).DebugMode)
            {
                kinectManagerClient.ColorImageSourceArrived += (this.Parent as MainWindow).HandleColorImageSourceArrived;
            }

            playgroundCanvas = playgroundWindow.PlaygroundCanvas;

            kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;

            string headerRowTitleFormat = "{0} Route {1} - Video Playback";
            switch (climbMode)
            {
                case ClimbMode.Training:
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Training", TrainingRouteDataAccess.TrainingRouteNoById(routeId));
                    break;
                case ClimbMode.Boulder:
                default:
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Bouldering", BoulderRouteDataAccess.BoulderRouteNoById(routeId));
                    rocksOnBoulderRoute = BoulderRouteAndRocksDataAccess.RocksByRouteId(routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper);
                    //startRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);
                    //endRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);
                    rocksOnRouteCamSP = new CameraSpacePoint[rocksOnBoulderRoute.Count()];

                    //for (int i = 0; i < rocksOnBoulderRoute.Count(); i++)
                    //{
                    //    RockOnRouteViewModel rockOnRouteViewModel = rocksOnBoulderRoute.ElementAt(i);
                    //    rockOnRouteViewModel.SetRockShapeWrtStatus();

                    //    rocksOnRouteCamSP[i] = rocksOnBoulderRoute.ElementAt(i).MyRockViewModel.MyRock.GetCameraSpacePoint();
                    //    rocksOnBoulderRoute.ElementAt(i).MyRockViewModel.DrawBoulder();
                    //}
                    int i = 0;
                    foreach (var rockOnBoulderRoute in rocksOnBoulderRoute)
                    {
                        rockOnBoulderRoute.SetRockShapeWrtStatus();

                        rocksOnRouteCamSP[i++] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                        rockOnBoulderRoute.MyRockViewModel.DrawBoulder();
                    }
                    break;

            }

            kinectManagerClient.ColorBitmapArrived += HandleColorBitmapArrived;
            gameplayVideoClient = (this.Parent as MainWindow).MainVideoHelper;
            Directory.CreateDirectory(FileHelper.VideoBufferFolderPath());
        }

        private void btnDemo_Click(object sender, RoutedEventArgs e)
        {
            //RouteVideoViewModel model = dgridRouteVideos.SelectedItem as RouteVideoViewModel;
            //string abx = FileHelper.VideoFullPath(model);

            //We temporary use this as video rec btn
            if (!IsRecording)
            {
                IsRecording = true;
                gameplayVideoClient.StartQueue();
                btnDemo.Content = "Stop Rec";
            }
            else
            {
                IsRecording = false;
                
                btnDemo.Content = "DEMO";
            }
        }

        private void btnPlaySelectedVideo_Click(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.ColorBitmapArrived -= HandleColorBitmapArrived;

            (this.Parent as MainWindow).KinectManagerClient.ColorImageSourceArrived -= (this.Parent as MainWindow).HandleColorImageSourceArrived;
            (this.Parent as MainWindow).PlaygroundWindow.PlaygroundCamera.Opacity = 0;

            //MediaElement playbackMonitor = (this.Parent as MainWindow).PlaygroundWindow.PlaybackMedia;
            //VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playbackMonitor);
            //videoPlaybackDialog.ShowDialog();
        }

        private void btnRestartGame_Click(object sender, RoutedEventArgs e)
        {

        }

        public void HandleBodyListArrived(object sender, BodyListArrEventArgs e)
        {
            //playgroundCanvas.Children.Clear();

            if (skeletonShapes != null)
            {
                foreach (Shape skeletonShape in skeletonShapes)
                {
                    playgroundCanvas.Children.Remove(skeletonShape);
                } 
            }

            IList<Body> bodies = e.GetBodyList();
                        
            foreach (var body in bodies)
            {
                if (body != null)
                {
                    if (body.IsTracked)
                    {
                        skeletonShapes = playgroundCanvas.DrawSkeleton(body, kinectManagerClient.ManagerCoorMapper, SpaceMode.Color);
                    }
                }

            }
        }

        public void HandleColorBitmapArrived(object sender, ColorBitmapEventArgs e)
        {
            if (IsRecording)
            {
                gameplayVideoClient.SaveImageToQueue(FileHelper.VideoBufferFolderPath(), e.GetColorBitmap());
            }
        }

        #endregion        
    }
}
