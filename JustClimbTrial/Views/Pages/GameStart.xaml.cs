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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for GameStart.xaml
    /// </summary>
    public partial class GameStart : Page
    {
        private const float DefaultDistanceThreshold = 0.1f;

        private string routeId;
        private ClimbMode climbMode;
        private GameStartViewModel viewModel;

        private IEnumerable<RockOnRouteViewModel> interRocksOnBoulderRoute;
        private RockOnRouteViewModel startRockOnBoulderRoute;
        private RockOnRouteViewModel endRockOnBoulderRoute;
        private CameraSpacePoint[] interRocksOnRouteCamSP;

        private ulong playerBodyID;

        private MainWindow mainWindowClient;
        private KinectManager kinectManagerClient;
        private Playground playgroundWindow;
        private Canvas playgroundCanvas;
        private MediaElement playgroundMedia;

        //private IEnumerable<Shape> skeletonShapes;
        private IList<IEnumerable<Shape>> skeletonBodies = new List<IEnumerable<Shape>>();
        private VideoHelper gameplayVideoRecClient;

        private bool isRecording = false;


        #region Gameplay Flags/Timers

        private bool gameStarted = false;

        

        private RockTimerHelper endRockHoldTimer = new RockTimerHelper(goal:24, lag:6);

        private bool endHeld = false;

        private DispatcherTimer gameOverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };

        #endregion

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
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundMedia.Stop();
            playgroundWindow.SetPlaygroundMediaSource(new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(),"Ready.mp4")));
            playgroundMedia.Play();

            gameplayVideoRecClient = new VideoHelper(kinectManagerClient);

            if (mainWindowClient.DebugMode)
            {
                kinectManagerClient.ColorImageSourceArrived += mainWindowClient.HandleColorImageSourceArrived;
            }

            playgroundCanvas = playgroundWindow.PlaygroundCanvas;

            kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;

            string headerRowTitleFormat = "{0} Route {1} - Video Playback";

            //methods to access rocks on route from database
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

                    IEnumerable<RockOnRouteViewModel> allRocksOnBoulderRoute = BoulderRouteAndRocksDataAccess.RocksByRouteId(routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper);
                    interRocksOnBoulderRoute = allRocksOnBoulderRoute.Where(x => x.BoulderStatus == RockOnBoulderStatus.Int);
                    startRockOnBoulderRoute = allRocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);
                    endRockOnBoulderRoute = allRocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);
                    
                    interRocksOnRouteCamSP = new CameraSpacePoint[interRocksOnBoulderRoute.Count()];

                    int i = 0;
                    foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                    {
                        rockOnBoulderRoute.DrawRockShapeWrtStatus();
                        interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                        //TO BE CHANGED ---- ANIMATION
                        rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.Play();
                        i++;
                    }
                    break;
            }         
        }

        private void BtnDemo_Click(object sender, RoutedEventArgs e)
        {
            //RouteVideoViewModel model = dgridRouteVideos.SelectedItem as RouteVideoViewModel;
            //string abx = FileHelper.VideoFullPath(model);

            //We temporary use this as video rec btn
            if (!isRecording)
            {                                
                BtnDemo.Content = "Stop Rec";
            }
            else
            {                
                BtnDemo.Content = "DEMO";
            }

            isRecording = !isRecording;
        }

        private void BtnPlaySelectedVideo_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindowClient.DebugMode)
            {
                kinectManagerClient.ColorImageSourceArrived -= mainWindowClient.HandleColorImageSourceArrived;
            }
            
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

            if (mainWindowClient.DebugMode)
            {
                foreach (IEnumerable<Shape> skeletonShapes in skeletonBodies)
                {
                    foreach (Shape skeletonShape in skeletonShapes)
                    {
                        playgroundCanvas.RemoveChild(skeletonShape);
                    }
                }
                skeletonBodies = new List<IEnumerable<Shape>>(); 
            }
                       

            IList<Body> bodies = e.GetBodyList();
            
                        
            foreach (var body in bodies)
            {
                if (body != null && body.IsTracked)
                {


                    if (mainWindowClient.DebugMode)
                    {
                        IEnumerable<Shape> skeletonShapes = playgroundCanvas.DrawSkeleton(body, kinectManagerClient.ManagerCoorMapper, SpaceMode.Color);
                        skeletonBodies.Add(skeletonShapes);  
                    }
           

                    //List<Joint> relevantJoints = new List<Joint>();
                    //foreach (Joint bodyJoint in body.Joints.Values)
                    //{
                    //    foreach (JointType limbJointType in KinectExtensions.LimbJoints)
                    //    {
                    //        if (limbJointType.Equals(bodyJoint.JointType))
                    //        {
                    //            relevantJoints.Add(bodyJoint);
                    //            break;
                    //        } 
                    //    }                
                    //}

                    //List<Joint> relevantJoints = new List<Joint>();
                    //foreach (Joint bodyJoint in body.Joints.Values)
                    //{
                    //    if (KinectExtensions.LimbJoints.Contains(bodyJoint.JointType))
                    //    {
                    //        relevantJoints.Add(bodyJoint);
                    //    }
                    //}

                    IEnumerable<Joint> LHandJoints =
                        body.Joints.Where(x => KinectExtensions.LHandJoints.Contains(x.Value.JointType)).Select(y => y.Value);
                    IEnumerable<Joint> RHandJoints =
                        body.Joints.Where(x => KinectExtensions.RHandJoints.Contains(x.Value.JointType)).Select(y => y.Value);

                    switch (climbMode)
                    {
                        case ClimbMode.Training:
                            break;
                        case ClimbMode.Boulder:
                        default:
#if climbMode == ClimbMode.Boulder
                            if (!gameStarted) //Progress: Game not yet started, waiting player to reach Starting Point
                            {
                                if (AreBothJointGroupsOnRock(LHandJoints, RHandJoints, startRockOnBoulderRoute.MyRockViewModel))
                                {
                                    ///DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT
                                    playerBodyID = body.TrackingId;
                                    //Console.WriteLine("Player Tracking ID: "+playerBodyID);

                                    playgroundWindow.LoopSrcnSvr = false;
                                    playgroundMedia.Stop();

                                    RockTimerHelper startRockTimer = startRockOnBoulderRoute.MyRockTimerHelper;
                                    startRockTimer.Tick += (_sender, _e) =>
                                    {

                                        if (AreBothJointGroupsOnRock(LHandJoints, RHandJoints, startRockOnBoulderRoute.MyRockViewModel))
                                        {
                                            //START ROCK REACHED VERIFIED
                                            startRockTimer.RockTimerCountIncr();

                                            if (startRockTimer.IsTimerGoalReached())
                                            {
                                                startRockTimer.Reset();
                                                gameStarted = true;
                                                //TO DO: StartRock Feedback Animation
                                                playgroundMedia.Source = (new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Start.mp4")));
                                            }
                                        }

                                        if (startRockTimer.IsLagThresholdExceeded())
                                        {
                                            startRockTimer.Reset();
                                        }
                                    };

                                    if (!startRockTimer.IsEnabled)
                                    {
                                        startRockTimer.Reset();
                                        startRockTimer.Start();
                                    }
                                }
                            }
                            else //gameStarted = true
                            {
                                //Progress: Game Started, waiting player to reach End Point
                                if (AreBothJointGroupsOnRock(LHandJoints, RHandJoints, endRockOnBoulderRoute.MyRockViewModel)
                                    && body.TrackingId == playerBodyID)
                                {
                                    RockTimerHelper endRockTimer = endRockOnBoulderRoute.MyRockTimerHelper;
                                    endRockTimer.Tick += (_sender, _e) =>
                                    {

                                        if (AreBothJointGroupsOnRock(LHandJoints, RHandJoints, endRockOnBoulderRoute.MyRockViewModel))
                                        {
                                            endRockTimer.RockTimerCountIncr();

                                            if (endRockTimer.IsTimerGoalReached())
                                            {
                                                //END ROCK REACHED VERIFIED
                                                endRockTimer.Reset();

                                                //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                                                playgroundMedia.Source = (new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Countdown.mp4")));

                                                endRockHoldTimer.Tick += (_holdSender, _holdE) =>
                                                {
                                                    if (endRockHoldTimer.IsTimerGoalReached())
                                                    {
                                                        //END ROCK 3-second HOLD VERIFIED
                                                        playgroundMedia.Source = (new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Finish.mp4")));
                                                        playgroundMedia.Play();
                                                        //TO DO: animation Feedback for that rock
                                                    }

                                                    if (endRockHoldTimer.IsLagThresholdExceeded())
                                                    {
                                                        playgroundMedia.Stop();
                                                        endRockHoldTimer.Reset();
                                                    }
                                                };

                                                if (!endRockHoldTimer.IsEnabled)
                                                {
                                                    endRockHoldTimer.Reset();
                                                    playgroundMedia.Play();
                                                    endRockHoldTimer.Start();
                                                }
                                            }
                                        }

                                        if (endRockTimer.IsLagThresholdExceeded())
                                        {
                                            endRockTimer.Reset();
                                        }
                                    };

                                    if (!endRockTimer.IsEnabled)
                                    {
                                        endRockTimer.Reset();
                                        endRockTimer.Start();
                                    }

                                }
                                else //Intermediate Rocks verification
                                {
                                    IEnumerable<Joint> relevantJoints =
                                        body.Joints.Where(x => KinectExtensions.LimbJoints.Contains(x.Value.JointType)).Select(y => y.Value);

                                    foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute)
                                    {
                                        foreach (Joint relevantJoint in relevantJoints)
                                        {

                                            if (IsJointOnRock(relevantJoint, rockOnRoute.MyRockViewModel)
                                                && body.TrackingId == playerBodyID)
                                            {
                                                RockTimerHelper anyRockTimer = rockOnRoute.MyRockTimerHelper;
                                                if (anyRockTimer.IsTimerGoalReached())
                                                {
                                                    //TO DO: animation Feedback for that rock

                                                    break;
                                                }
                                                if (anyRockTimer.IsLagThresholdExceeded())
                                                {
                                                    anyRockTimer.Reset();
                                                }

                                                if (!anyRockTimer.IsEnabled)
                                                {
                                                    anyRockTimer.Reset();
                                                    anyRockTimer.Start();
                                                }
                                            }
                                        }//CLOSE foreach (Joint relevantJoint in relevantJoints)

                                    }//CLOSE foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute)

                                }
                            } 
#endif
                            break;
                    }
                }                

            }//CLOSE foreach (var body in bodies)
        }

#endregion

        private bool IsJointOnRock(Joint joint, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {

            float distance = KinectExtensions.GetCameraSpacePointDistance(joint.Position, rockVM.MyRock.GetCameraSpacePoint());

            //CameraSpacePoint startCamSp = startRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
            //Console.WriteLine($"{{ {startCamSp.X},{startCamSp.Y},{startCamSp.Z} }}");

            return (distance < threshold);
        }

        private bool IsJointGroupOnROck(IEnumerable<Joint> handJoints, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            bool isHandOnRock = false;
            foreach (Joint handJoint in handJoints)
            {
                if (IsJointOnRock(handJoint, rockVM, threshold))
                {
                    isHandOnRock = true;
                    break;
                }
            }
            return isHandOnRock;
        }

        private bool AreBothJointGroupsOnRock(IEnumerable<Joint> groupA, IEnumerable<Joint> groupB, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            return (IsJointGroupOnROck(groupA, rockVM, threshold) && IsJointGroupOnROck(groupB, rockVM, threshold) );
        }

    }
}
