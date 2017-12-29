using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
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

        private ulong playerBodyID;

        private MainWindow mainWindowClient;
        private KinectManager kinectManagerClient;
        private Playground playgroundWindow;
        private Canvas playgroundCanvas;
        private MediaElement playgroundMedia;

        //private IEnumerable<Shape> skeletonShapes;
        private IList<IEnumerable<Shape>> skeletonBodies = new List<IEnumerable<Shape>>();
        private VideoHelper gameplayVideoRecClient;
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

            mainWindowClient = this.Parent as MainWindow;
            kinectManagerClient = mainWindowClient.KinectManagerClient;
            playgroundWindow = mainWindowClient.PlaygroundWindow;
            playgroundWindow.LoopSrcnSvr = false;
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundMedia.Stop();
            playgroundWindow.SetPlaygroundMediaSource(new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(),"Countdown.mp4")));

            if (mainWindowClient.DebugMode)
            {
                kinectManagerClient.ColorImageSourceArrived += mainWindowClient.HandleColorImageSourceArrived;
            }

            playgroundCanvas = playgroundWindow.PlaygroundCanvas;

            kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;

            string headerRowTitleFormat = "{0} Route {1} - Video Playback";

            //methods to access rocks on route from data base
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
                    startRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);

                    CameraSpacePoint startCamSp = startRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                    Console.WriteLine($"{{ {startCamSp.X},{startCamSp.Y},{startCamSp.Z} }}");
                    
                    endRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);
                    rocksOnRouteCamSP = new CameraSpacePoint[rocksOnBoulderRoute.Count()];

                    int i = 0;
                    foreach (var rockOnBoulderRoute in rocksOnBoulderRoute)
                    {
                        rockOnBoulderRoute.DrawRockShapeWrtStatus();
                        rocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                        i++;
                    }
                    break;


            }

            //methods relating to video recording
            kinectManagerClient.ColorBitmapArrived += HandleColorBitmapArrived;
            gameplayVideoRecClient = mainWindowClient.MainVideoHelper;
            Directory.CreateDirectory(FileHelper.VideoBufferFolderPath());
        }

        private void BtnDemo_Click(object sender, RoutedEventArgs e)
        {
            //RouteVideoViewModel model = dgridRouteVideos.SelectedItem as RouteVideoViewModel;
            //string abx = FileHelper.VideoFullPath(model);

            //We temporary use this as video rec btn
            if (!IsRecording)
            {
                IsRecording = true;
                gameplayVideoRecClient.StartQueue();
                BtnDemo.Content = "Stop Rec";
            }
            else
            {
                IsRecording = false;
                
                BtnDemo.Content = "DEMO";
            }
        }

        private void BtnPlaySelectedVideo_Click(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.ColorBitmapArrived -= HandleColorBitmapArrived;

            if(mainWindowClient.DebugMode) kinectManagerClient.ColorImageSourceArrived -= mainWindowClient.HandleColorImageSourceArrived;
            //mainWindowClient.PlaygroundWindow.PlaygroundCamera.Opacity = 0;

            MediaElement playbackMonitor = mainWindowClient.PlaygroundWindow.PlaybackMedia;
            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playbackMonitor);
            videoPlaybackDialog.ShowDialog();
        }

        private void BtnRestartGame_Click(object sender, RoutedEventArgs e)
        {

        }

        public void HandleBodyListArrived(object sender, BodyListArrEventArgs e)
        {
            //playgroundCanvas.Children.Clear();

            foreach (IEnumerable<Shape> skeletonShapes in skeletonBodies)
            {
                foreach (Shape skeletonShape in skeletonShapes)
                {
                    playgroundCanvas.RemoveChild(skeletonShape);
                }

            }
            skeletonBodies = new List<IEnumerable<Shape>>();
                       

            IList<Body> bodies = e.GetBodyList();
            
                        
            foreach (var body in bodies)
            {
                if (body != null)
                {
                    if (body.IsTracked)
                    {
                        IEnumerable<Shape> skeletonShapes = playgroundCanvas.DrawSkeleton(body, kinectManagerClient.ManagerCoorMapper, SpaceMode.Color);
                        skeletonBodies.Add(skeletonShapes);

                        List<Joint> relevantJoints = new List<Joint>();
                        foreach (Joint bodyJoint in body.Joints.Values)
                        {
                            foreach (JointType limbJointType in KinectExtensions.LimbJoints)
                            {
                                if (limbJointType.Equals(bodyJoint.JointType))
                                {
                                    relevantJoints.Add(bodyJoint);
                                    break;
                                } 
                            }                
                        }

                        foreach (Joint relevantJoint in relevantJoints)
                        {
                            float distance = KinectExtensions.GetCameraSpacePointDistance(relevantJoint.Position, startRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint());
                            float distanceEnd = KinectExtensions.GetCameraSpacePointDistance(relevantJoint.Position, endRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint());

                            //Console.WriteLine("Distance = "+distance);
                            if (distance < 0.1 || distanceEnd < 0.1)
                            {
                                ///DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT
                                playerBodyID = body.TrackingId;
                                //Console.WriteLine("Player Tracking ID: "+playerBodyID);
                                playgroundMedia.Play();
                            }


                        }
                    }
                }

            }
        }

        public void HandleColorBitmapArrived(object sender, ColorBitmapEventArgs e)
        {
            if (IsRecording)
            {
                gameplayVideoRecClient.SaveImageToQueue(FileHelper.VideoBufferFolderPath(), e.GetColorBitmap());
            }
        }

        #endregion        
    }
}
