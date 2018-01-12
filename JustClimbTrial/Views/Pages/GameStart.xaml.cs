using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Interfaces;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
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
    public partial class GameStart : Page, ISavingVideo
    {
        private readonly bool debug = AppGlobal.DEBUG;

        private const float DefaultDistanceThreshold = 0.1f;

        private string routeId;
        private ClimbMode climbMode;
        private GameStartViewModel viewModel;

        private IEnumerable<RockOnRouteViewModel> interRocksOnBoulderRoute;
        private IEnumerable<RockOnRouteViewModel> rocksOnTrainingRoute;
        private int trainingRouteLength;

        private RockOnRouteViewModel nextRockOnTrainRoute;

        private RockOnRouteViewModel startRockOnRoute;
        private RockOnRouteViewModel endRockOnRoute;
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


        #region ISavingVideo properties

        public string TmpVideoFilePath
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsConfirmSaveVideo
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion


        #region Gameplay Flags/Timers

        private int nextTrainRockIdx = 0;

        private bool gameStarted = false;

        private RockTimerHelper endRockHoldTimer = new RockTimerHelper(goal: 24, lag: 6);
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


        #region initializtion

        private void InitializeCommands()
        {
            BtnDemo.Command = new RelayCommand(PlayDemoVideo, CanPlayDemoVideo);
            BtnPlaySelectedVideo.Command = new RelayCommand(PlaySelectedVideo, CanPlaySelectedVideo);
            BtnRestartGame.Command = new RelayCommand(RestartGame, CanRestartGame);
        }

        #endregion

        #region command methods

        private bool CanPlayDemoVideo(object paramter = null)
        {
            return true;
        }

        private bool CanPlaySelectedVideo(object parameter = null)
        {
            return true;
        }

        private bool CanRestartGame(object parameter = null)
        {
            return true;
        }

        private void PlayDemoVideo(object parameter = null)
        {
            RouteVideoViewModel demoVideoVM = viewModel.RouteVideoViewModels.Single(x => x.IsDemo);

            string demoVideoFilePath;
            switch (climbMode)
            {
                case ClimbMode.Training:
                    demoVideoFilePath = 
                        FileHelper.TrainingRouteVideoRecordedFullPath(demoVideoVM);
                    break;
                case ClimbMode.Boulder:
                default:
                    demoVideoFilePath =
                        FileHelper.BoulderRouteVideoRecordedFullPath(demoVideoVM);
                    break;
            }

            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playgroundMedia);
            VideoPlayback videoPlaybackPage =
                new VideoPlayback(demoVideoFilePath, playgroundMedia);

            videoPlaybackDialog.Navigate(videoPlaybackPage);
            videoPlaybackDialog.ShowDialog();
        }

        private void PlaySelectedVideo(object parameter = null)
        {
            if (debug)
            {
                //kinectManagerClient.ColorImageSourceArrived -= mainWindowClient.HandleColorImageSourceArrived;
            }

            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playgroundMedia);
            videoPlaybackDialog.ShowDialog();
        }

        private void RestartGame(object parameter = null)
        {

        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCommands();

            viewModel.LoadData();

            mainWindowClient = this.Parent as MainWindow;
            kinectManagerClient = mainWindowClient.KinectManagerClient;
            playgroundWindow = mainWindowClient.PlaygroundWindow;
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundMedia.Stop();
            playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Ready.mp4"));
            playgroundMedia.Play();

            gameplayVideoRecClient = new VideoHelper(kinectManagerClient);

            if (debug)
            {
                //kinectManagerClient.ColorImageSourceArrived += mainWindowClient.HandleColorImageSourceArrived;
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

                    rocksOnTrainingRoute = TrainingRouteAndRocksDataAccess.OrderedRocksByRouteId(routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper).ToArray();
                    endRockOnRoute = rocksOnTrainingRoute.Last();
                    trainingRouteLength = rocksOnTrainingRoute.Count();

                    foreach (var rockOnTrainingRoute in rocksOnTrainingRoute)
                    {
                        if (debug)
                        {
                            rockOnTrainingRoute.DrawRockShapeWrtTrainSeq(trainingRouteLength);
                        }
                    }

                    break;
                case ClimbMode.Boulder:
                default:
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Bouldering", BoulderRouteDataAccess.BoulderRouteNoById(routeId));

                    IEnumerable<RockOnRouteViewModel> allRocksOnBoulderRoute = BoulderRouteAndRocksDataAccess.RocksByRouteId(routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper);
                    interRocksOnBoulderRoute = allRocksOnBoulderRoute.Where(x => x.BoulderStatus == RockOnBoulderStatus.Int).ToArray();
                    startRockOnRoute = allRocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);
                    endRockOnRoute = allRocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);

                    interRocksOnRouteCamSP = new CameraSpacePoint[interRocksOnBoulderRoute.Count()];

                    int i = 0;
                    if (debug)
                    {
                        startRockOnRoute.DrawRockShapeWrtStatus();
                        endRockOnRoute.DrawRockShapeWrtStatus();
                        foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                        {
                            rockOnBoulderRoute.DrawRockShapeWrtStatus();
                            interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                            i++;
                        }

                    }
                    else
                    {
                        startRockOnRoute.DrawRockImageWrtStatus();
                        endRockOnRoute.DrawRockImageWrtStatus();
                        foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                        {

                            rockOnBoulderRoute.DrawRockImageWrtStatus();
                            rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.Play();

                            interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                            i++;
                            //TO BE CHANGED ---- ANIMATION

                        }
                        startRockOnRoute.MyRockViewModel.BoulderButtonSequence.Play();
                        endRockOnRoute.MyRockViewModel.BoulderButtonSequence.Play();

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
            if (debug)
            {
                //kinectManagerClient.ColorImageSourceArrived -= mainWindowClient.HandleColorImageSourceArrived;
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
            //remove skeleton shape when DEBUG
            if (debug)
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

                    //draw skeleton shape when DEBUG
                    if (debug)
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
                            #region Training Gameplay
                            IEnumerable<Joint> handJoints =
                                        body.Joints.Where(x => KinectExtensions.HandJoints.Contains(x.Value.JointType)).Select(y => y.Value);
                            nextRockOnTrainRoute = rocksOnTrainingRoute.ElementAt(nextTrainRockIdx);
                            RockTimerHelper nextRockTimer = nextRockOnTrainRoute.MyRockTimerHelper;
                            Func<bool> trainingTargetReached;
                            if (nextRockOnTrainRoute.TrainingSeq == 1)
                            {
                                trainingTargetReached = () =>
                                    AreBothJointGroupsOnRock(LHandJoints, RHandJoints, nextRockOnTrainRoute.MyRockViewModel);
                            }
                            else if (nextRockOnTrainRoute.TrainingSeq == trainingRouteLength)
                            {
                                trainingTargetReached = () =>
                                    AreBothJointGroupsOnRock(LHandJoints, RHandJoints, nextRockOnTrainRoute.MyRockViewModel) && body.TrackingId == playerBodyID;
                            }
                            else
                            {
                                trainingTargetReached = () =>
                                    IsJointGroupOnRock(handJoints, nextRockOnTrainRoute.MyRockViewModel) && body.TrackingId == playerBodyID;
                            }


                            if (trainingTargetReached())
                            {
                                //DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT

                                playgroundWindow.LoopMedia = false;
                                playgroundMedia.Stop();

                                if (!nextRockTimer.IsTickHandlerSubed)
                                {
                                    nextRockTimer.Tick += (_sender, _e) =>
                                    {

                                        if (trainingTargetReached())
                                        {
                                            nextRockTimer.RockTimerCountIncr();

                                            if (nextRockTimer.IsTimerGoalReached())
                                            {
                                                if (nextRockOnTrainRoute.TrainingSeq == 1)
                                                {
                                                    //START ROCK REACHED VERIFIED
                                                    playerBodyID = body.TrackingId;
                                                    //Console.WriteLine("Player Tracking ID: "+playerBodyID);

                                                    nextTrainRockIdx++;
                                                    nextRockTimer.Reset();
                                                    //TO DO: StartRock Feedback Animation
                                                    playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Start.mp4"));
                                                }
                                                else if (nextRockOnTrainRoute.TrainingSeq == trainingRouteLength)
                                                {
                                                    //END ROCK REACHED VERIFIED
                                                    //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                                                    playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Countdown.mp4"));

                                                    if (!endRockHoldTimer.IsTickHandlerSubed)
                                                    {
                                                        endRockHoldTimer.Tick += (_holdSender, _holdE) =>
                                                        {
                                                            endRockHoldTimer.RockTimerCountIncr();

                                                            if (endRockHoldTimer.IsTimerGoalReached())
                                                            {
                                                                //END ROCK 3-second HOLD VERIFIED
                                                                endRockHoldTimer.Stop();

                                                                playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Finish.mp4"));
                                                                playgroundMedia.Play();
                                                                //TO DO: animation Feedback for that rock
                                                            }

                                                            if (endRockHoldTimer.IsLagThresholdExceeded())
                                                            {
                                                                playgroundMedia.Stop();
                                                                endRockHoldTimer.Reset();
                                                            }
                                                        };

                                                        endRockHoldTimer.IsTickHandlerSubed = true;
                                                    }

                                                    if (!endRockHoldTimer.IsEnabled)
                                                    {
                                                        endRockHoldTimer.Reset();
                                                        playgroundMedia.Play();
                                                        endRockHoldTimer.Start();
                                                    }
                                                }
                                                else
                                                {
                                                    //INTER ROCK REACHED VERIFIED
                                                    nextRockTimer.Reset();
                                                    nextTrainRockIdx++;
                                                }
                                            }
                                        }

                                        if (nextRockTimer.IsLagThresholdExceeded())
                                        {
                                            nextRockTimer.Reset();
                                        }


                                    };
                                    nextRockTimer.IsTickHandlerSubed = true;
                                }

                                if (!nextRockTimer.IsEnabled)
                                {
                                    nextRockTimer.Reset();
                                    nextRockTimer.Start();
                                }
                            }
  
                            break;
                        #endregion

                        case ClimbMode.Boulder:
                        default:
                            #region Boulder Gameplay
                            IEnumerable<Joint> fourLimbJoints =
                                        body.Joints.Where(x => KinectExtensions.LimbJoints.Contains(x.Value.JointType)).Select(y => y.Value);
                            Func<RockOnRouteViewModel,bool> boulderTargetReached;
                            boulderTargetReached = (x) =>
                            {
                                bool reached;
                                switch (x.BoulderStatus)
                                {
                                    case RockOnBoulderStatus.Start:
                                        reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel);
                                        //reached = IsJointGroupOnRock(fourLimbJoints, x.MyRockViewModel);
                                        break;
                                    case RockOnBoulderStatus.Int:
                                    default:
                                        reached = IsJointGroupOnRock(fourLimbJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
                                        break;
                                    case RockOnBoulderStatus.End:
                                        reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
                                        break;
                                }
                                return reached;
                            };
                            
                            if (!gameStarted) //Progress: Game not yet started, waiting player to reach Starting Point
                            {

                                if (boulderTargetReached(startRockOnRoute))
                                {
                                    //DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT

                                    playgroundWindow.LoopMedia = false;
                                    playgroundMedia.Stop();
                                    playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Start.mp4"));

                                    RockTimerHelper startRockTimer = startRockOnRoute.MyRockTimerHelper;
                                    startRockTimer.Tick += (_sender, _e) =>
                                    {

                                        if (boulderTargetReached(startRockOnRoute))
                                        {

                                            startRockTimer.RockTimerCountIncr();

                                            if (startRockTimer.IsTimerGoalReached())
                                            {
                                                //START ROCK REACHED VERIFIED
                                                playerBodyID = body.TrackingId;
                                                //Console.WriteLine("Player Tracking ID: "+playerBodyID);

                                                startRockTimer.Stop();
                                                gameStarted = true;
                                                //TO DO: StartRock Feedback Animation

                                                //playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Start.mp4"));
                                                playgroundMedia.Play();
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
                                //CHECK END ROCK REACHED ALL THE TIME
                                if (boulderTargetReached(endRockOnRoute))
                                {
                                    RockTimerHelper endRockTimer = endRockOnRoute.MyRockTimerHelper;

                                    if (!endRockTimer.IsTickHandlerSubed)
                                    {
                                        endRockTimer.Tick += (_sender, _e) =>
                                        {

                                        if (boulderTargetReached(endRockOnRoute))
                                        {
                                            endRockTimer.RockTimerCountIncr();

                                            if (endRockTimer.IsTimerGoalReached())
                                            {
                                                //END ROCK REACHED VERIFIED
                                                endRockTimer.Stop();

                                                //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                                                playgroundMedia.Source = (new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Countdown.mp4")));

                                                if (!endRockHoldTimer.IsTickHandlerSubed)
                                                {
                                                    endRockHoldTimer.Tick += (_holdSender, _holdE) =>
                                                    {
                                                        endRockHoldTimer.RockTimerCountIncr();

                                                        if (endRockHoldTimer.IsTimerGoalReached())
                                                        {
                                                            //END ROCK 3-second HOLD VERIFIED
                                                            endRockHoldTimer.Stop();

                                                            playgroundMedia.Source = new Uri(System.IO.Path.Combine(FileHelper.VideoResourcesFolderPath(), "Finish.mp4"));
                                                            playgroundMedia.Play();
                                                            //TO DO: animation Feedback for that rock
                                                        }

                                                        if (endRockHoldTimer.IsLagThresholdExceeded())
                                                        {
                                                            playgroundMedia.Stop();
                                                            endRockHoldTimer.Reset();
                                                        }
                                                    };

                                                    endRockHoldTimer.IsTickHandlerSubed = true;
                                                }

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

                                        endRockTimer.IsTickHandlerSubed = true;
                                    }

                                    if (!endRockTimer.IsEnabled)
                                    {
                                        endRockTimer.Reset();
                                        endRockTimer.Start();
                                    }

                                }
                                
                                
                                foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute)
                                {
                                    RockTimerHelper anyRockTimer = rockOnRoute.MyRockTimerHelper;

                                    if (boulderTargetReached(rockOnRoute))
                                    {
                                        if (!anyRockTimer.IsTickHandlerSubed)
                                        {
                                            anyRockTimer.Tick += (_sender, _e) =>
                                            {
                                                if (boulderTargetReached(rockOnRoute))
                                                {
                                                    anyRockTimer.RockTimerCountIncr();

                                                    if (anyRockTimer.IsTimerGoalReached())
                                                    {
                                                        anyRockTimer.Stop();
                                                        //TO DO: animation Feedback for that rock

                                                    }
                                                    if (anyRockTimer.IsLagThresholdExceeded())
                                                    {
                                                        anyRockTimer.Reset();
                                                    }

                                                }
                                            };

                                            anyRockTimer.IsTickHandlerSubed = true;
                                        }

                                    }

                                    if (!anyRockTimer.IsEnabled)
                                    {
                                        anyRockTimer.Reset();
                                        anyRockTimer.Start();
                                    }
                                } //CLOSE foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute)

                            }
                            break;
                            #endregion
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

        private bool IsJointGroupOnRock(IEnumerable<Joint> joints, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            bool onRock = false;
            foreach (Joint joint in joints)
            {
                if (IsJointOnRock(joint, rockVM, threshold))
                {
                    onRock = true;
                    break;
                }
            }
            return onRock;
        }

        private bool AreBothJointGroupsOnRock(IEnumerable<Joint> groupA, IEnumerable<Joint> groupB, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            return (IsJointGroupOnRock(groupA, rockVM, threshold) && IsJointGroupOnRock(groupB, rockVM, threshold));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            kinectManagerClient.BodyFrameArrived -= HandleBodyListArrived;
            playgroundCanvas.Children.Clear();
            mainWindowClient.UnsubColorImgSrcToPlaygrd();
        }


        #region ISavingVideo methods

        public void DeleteTmpVideoFileSafe()
        {
            throw new NotImplementedException();
        }

        public void ResetSavingVideoProperties()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
