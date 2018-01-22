using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Interfaces;
using JustClimbTrial.Kinect;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.Properties;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using JustClimbTrial.Views.Windows;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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

        // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/
        private NavigationService navSvc;

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

        private bool isRecordingDemo = false;

        private const string VideoHelperEngagedErrMsg = 
            "Recording cannot be started as previous video saving processes are not finished.";


        #region ISavingVideo properties

        public string RouteId { get { return routeId; } }

        public ClimbMode RouteClimbMode { get { return climbMode; } }

        public bool IsRouteContainDemoVideo
        {
            get
            {
                if (viewModel != null)
                {
                    return viewModel.TryGetDemoRouteVideoViewModel != null;
                }
                else
                {
                    return false;
                }
            }
        }

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

        private bool gameOverFlag = false;
        private DispatcherTimer gameOverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        private Plane wallPlane;
        private Plane floorPlane;

        #endregion


        public GameStart(string aRouteId, ClimbMode aClimbMode)
        {
            //Load Wall and Floor Planes to local variables
            float[] planeParams = Settings.Default.WallPlaneStr.Split(',').Select(x => float.Parse(x)).ToArray();
            wallPlane = new Plane(planeParams[0], planeParams[1], planeParams[2], planeParams[3]);
            planeParams = Settings.Default.FloorPlaneStr.Split(',').Select(x => float.Parse(x)).ToArray();
            floorPlane = new Plane(planeParams[0], planeParams[1], planeParams[2], planeParams[3]);

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
            return IsRouteContainDemoVideo;
        }

        private bool CanPlaySelectedVideo(object parameter = null)
        {
            return DgridRouteVideos.SelectedItem != null;
        }

        private bool CanRestartGame(object parameter = null)
        {
            return true;
        }

        private void PlayDemoVideo(object parameter = null)
        {
            RouteVideoViewModel demoVideoVM = viewModel.DemoRouteVideoViewModel;
            ShowVideoPlaybackDialog(demoVideoVM);
        }

        private void PlaySelectedVideo(object parameter = null)
        {
            RouteVideoViewModel selectedVideoVM = 
                DgridRouteVideos.SelectedItem as RouteVideoViewModel;
            ShowVideoPlaybackDialog(selectedVideoVM);
        }
        
        private async void RestartGame(object parameter = null)
        {            
            if (!gameplayVideoRecClient.IsRecording)
            {
                if (gameplayVideoRecClient.StartRecording())
                {
                    UiHelper.NotifyUser("start recording");
                }
                else
                {
                    UiHelper.NotifyUser(VideoHelperEngagedErrMsg);
                }
            }
            else
            {
                gameplayVideoRecClient.StopRecording();
                int exportVideoErrorCode = 0;
                string videoPath = @"C:\Users\IOIO\Documents\Projects\C#.NET WPF\JustClimbRenew\JustClimbTrial\bin\AppFiles\temp\untitled.mp4";
                exportVideoErrorCode = await gameplayVideoRecClient.ExportVideoAndClearBufferAsync(videoPath);
                if (exportVideoErrorCode != 0)
                {
                    UiHelper.NotifyUser("video record error!");
                }
                else
                {
                    UiHelper.NotifyUser("video exported");
                }
            }
        }

        #endregion


        #region event handlers

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/
            navSvc = this.NavigationService;
            navSvc.Navigating += NavigationService_Navigating;

            InitializeCommands();

            viewModel.LoadData();

            mainWindowClient = this.Parent as MainWindow;
            kinectManagerClient = mainWindowClient.KinectManagerClient;            

            gameplayVideoRecClient = VideoHelper.Instance(kinectManagerClient);

            if (debug)
            {
                //kinectManagerClient.ColorImageSourceArrived += mainWindowClient.HandleColorImageSourceArrived;
            }

            playgroundWindow = mainWindowClient.PlaygroundWindow;
            playgroundMedia = playgroundWindow.PlaygroundMedia;
            playgroundCanvas = playgroundWindow.PlaygroundCanvas;
            
            navHead.PropertyChanged += OnNavHeadIsRecordDemoChanged;

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

                    if (debug)
                    {
                        foreach (var rockOnTrainingRoute in rocksOnTrainingRoute)
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
                        //startRockOnRoute.DrawRockImageWrtStatus();
                        //startRockOnRoute.MyRockViewModel.BoulderButtonSequence.Play();

                        //endRockOnRoute.DrawRockImageWrtStatus();
                        //endRockOnRoute.MyRockViewModel.BoulderButtonSequence.Play();

                        startRockOnRoute.MyRockViewModel.CreateBoulderImageSequence();
                        startRockOnRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                            ImageSequenceHelper.CombinedList
                        );

                        //endRockOnRoute.MyRockViewModel.CreateBoulderImageSequence();
                        //endRockOnRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(false,
                        //    ImageSequenceHelper.ShowSequence);  // 1

                        foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                        {
                            //rockOnBoulderRoute.DrawRockImageWrtStatus();
                            //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.Play();

                            //rockOnBoulderRoute.MyRockViewModel.CreateBoulderImageSequence();
                            //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                            //    ImageSequenceHelper.ShowSequence);  // 1

                            interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                            i++;
                        }
                    }
                    break;
            }

            await gameplayVideoRecClient.WaitingForAllBufferFramesSavedAsync();

            playgroundMedia.Stop();
            playgroundMedia.Source = new Uri(FileHelper.GameplayReadyVideoPath());
            playgroundMedia.Play();

            kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/ 
            navSvc.Navigating -= NavigationService_Navigating;
            navSvc = null;

            if (gameplayVideoRecClient.IsRecording)
            {
                gameplayVideoRecClient.StopRecording();
            }

            kinectManagerClient.BodyFrameArrived -= HandleBodyListArrived;
            navHead.PropertyChanged -= OnNavHeadIsRecordDemoChanged;
            playgroundCanvas.Children.Clear();
            mainWindowClient.UnsubColorImgSrcToPlaygrd();
        }

        public void HandleBodyListArrived(object sender, BodyListArrEventArgs e)
        {
            //Microsoft.Kinect.Vector4 floorClipPlane = e.GetFloorClipPlane();

            //floorPlane = new Plane(floorClipPlane.X, floorClipPlane.Y, floorClipPlane.Z, floorClipPlane.W);
            
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

                        gameOverFlag = IsBodyGameOver(body);
                    }

                    IEnumerable<Joint> LHandJoints =
                        body.Joints.Where(x => KinectExtensions.LHandJoints.Contains(x.Value.JointType)).Select(y => y.Value);
                    IEnumerable<Joint> RHandJoints =
                        body.Joints.Where(x => KinectExtensions.RHandJoints.Contains(x.Value.JointType)).Select(y => y.Value);

                    switch (climbMode)
                    {
                        case ClimbMode.Training:
                            TrainingGameplay(body, LHandJoints, RHandJoints);
                            break;
                        case ClimbMode.Boulder:
                        default:
                            BoulderGameplay(body, LHandJoints, RHandJoints);
                            break;
                    }
                }

            }//CLOSE foreach (var body in bodies)
        }

        private void OnNavHeadIsRecordDemoChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(navHead.IsRecordDemoVideo))
            {
                isRecordingDemo = navHead.IsRecordDemoVideo;

                if (isRecordingDemo)
                {
                    LbIsRecordingDemo.Text = "Demo Recording";
                }
                else
                {
                    LbIsRecordingDemo.Text = "";
                }
            }
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (gameStarted)
            {
                UiHelper.NotifyUser("Can't navigate away before the game is finished!");
                e.Cancel = true;
            }
        }

        private bool OnGameplayStart()
        {
            // start video recording
            bool isRecordingStarted = gameplayVideoRecClient.StartRecording();

            if (isRecordingStarted)
            {
                playgroundMedia.Source = new Uri(FileHelper.GameplayStartVideoPath());
                playgroundMedia.Play();
            }

            return isRecordingStarted;
        }

        private async Task OnGameplayFinishAsync()
        {
            gameStarted = false;

            playgroundMedia.Source = new Uri(FileHelper.GameplayFinishVideoPath());
            playgroundMedia.Play();

            // stop video recording
            gameplayVideoRecClient.StopRecording();

            await SaveVideoRecordedInDbAndLocallyAsync();
        }

        #endregion


        #region game play logic

        private void TrainingGameplay(Body body, IEnumerable<Joint> LHandJoints, 
            IEnumerable<Joint> RHandJoints)
        {
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
                                    bool isRecordingStarted = OnGameplayStart();

                                    if (isRecordingStarted)
                                    {
                                        //START ROCK REACHED VERIFIED
                                        playerBodyID = body.TrackingId;
                                        //Debug.WriteLine("Player Tracking ID: "+playerBodyID);

                                        nextTrainRockIdx++;
                                        nextRockTimer.Reset();

                                        //TO DO: StartRock Feedback Animation
                                    }
                                }
                                else if (nextRockOnTrainRoute.TrainingSeq == trainingRouteLength)
                                {
                                    //END ROCK REACHED VERIFIED
                                    //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                                    playgroundMedia.Source = new Uri(FileHelper.GameplayCountdownVideoPath());

                                    if (!endRockHoldTimer.IsTickHandlerSubed)
                                    {
                                        endRockHoldTimer.Tick += async (_holdSender, _holdE) =>
                                        {
                                            endRockHoldTimer.RockTimerCountIncr();

                                            if (endRockHoldTimer.IsTimerGoalReached())
                                            {
                                                //END ROCK 3-second HOLD VERIFIED
                                                endRockHoldTimer.Stop();

                                                await OnGameplayFinishAsync();

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
        }

        private void BoulderGameplay(Body body, IEnumerable<Joint> LHandJoints,
            IEnumerable<Joint> RHandJoints)
        {
            IEnumerable<Joint> fourLimbJoints =
                        body.Joints.Where(x => KinectExtensions.LimbJoints.Contains(x.Value.JointType)).Select(y => y.Value);
            Func<RockOnRouteViewModel, bool> boulderTargetReached;
            boulderTargetReached = (x) =>
            {
                bool reached;
                switch (x.BoulderStatus)
                {
                    case RockOnBoulderStatus.Start:
                        //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel);
                        reached = IsJointGroupOnRock(fourLimbJoints, x.MyRockViewModel);
                        break;
                    case RockOnBoulderStatus.Int:
                    default:
                        reached = IsJointGroupOnRock(fourLimbJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
                        break;
                    case RockOnBoulderStatus.End:
                        //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
                        reached = IsJointGroupOnRock(fourLimbJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
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
                                //Debug.WriteLine("Player Tracking ID: " + playerBodyID);

                                startRockTimer.Stop();

                                bool isRecordingStarted = OnGameplayStart();
                                if (isRecordingStarted)
                                {
                                    gameStarted = true;
                                }
                                else
                                {
                                    UiHelper.NotifyUser(VideoHelperEngagedErrMsg);
                                }

                                //TO DO: StartRock Feedback Animation                                
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
                                    playgroundMedia.Source = new Uri(FileHelper.GameplayCountdownVideoPath());

                                    if (!endRockHoldTimer.IsTickHandlerSubed)
                                    {
                                        endRockHoldTimer.Tick += async (_holdSender, _holdE) =>
                                        {
                                            endRockHoldTimer.RockTimerCountIncr();

                                            if (endRockHoldTimer.IsTimerGoalReached())
                                            {
                                                //END ROCK 3-second HOLD VERIFIED
                                                endRockHoldTimer.Stop();

                                                await OnGameplayFinishAsync();                                                

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
        }

        #endregion


        #region determine overlap between Rock & Joint

        private bool IsJointOnRock(Joint joint, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            float distance = KinectExtensions.GetCameraSpacePointDistance(joint.Position, rockVM.MyRock.GetCameraSpacePoint());

            //CameraSpacePoint startCamSp = startRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
            //Debug.WriteLine($"{{ {startCamSp.X},{startCamSp.Y},{startCamSp.Z} }}");

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

        private bool IsBodyGameOver(Body body)
        {
            Joint bodyCenterJoint = body.Joints.Single(x => x.Value.JointType == JointType.SpineMid).Value;
            Vector3 bodyCenterVec = new Vector3(bodyCenterJoint.Position.X, bodyCenterJoint.Position.Y, bodyCenterJoint.Position.Z);
            float distanceFromWall = Plane.DotCoordinate(wallPlane, bodyCenterVec);

            Console.WriteLine("DistanceFromWall = " + distanceFromWall);

            //Joint LFootJoint = body.Joints.Single(x => x.Value.JointType == JointType.FootLeft).Value;
            //Joint RFootJoint = body.Joints.Single(x => x.Value.JointType == JointType.FootRight).Value;
            //Vector3 LFootVec = new Vector3(LFootJoint.Position.X, LFootJoint.Position.Y, LFootJoint.Position.Z);
            //Vector3 RFootVec = new Vector3(RFootJoint.Position.X, RFootJoint.Position.Y, RFootJoint.Position.Z);

            //float distanceFromFloor = Math.Min(Plane.DotCoordinate(wallPlane, LFootVec), Plane.DotCoordinate(wallPlane, RFootVec));

            Joint headJoint = body.Joints.Single(x => x.Value.JointType == JointType.Head).Value;
            Vector3 headVec = new Vector3(headJoint.Position.X, headJoint.Position.Y, headJoint.Position.Z);
            double distanceFromFloor = Plane.DotCoordinate(floorPlane, headVec);

            Console.WriteLine("DistanceFromFloor = " + distanceFromFloor);

            return true;
        }

        #endregion


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


        #region video recording

        private async Task SaveVideoRecordedInDbAndLocallyAsync()
        {
            if (!gameplayVideoRecClient.IsAllBufferFramesSaved)
            {
                await gameplayVideoRecClient.WaitingForAllBufferFramesSavedAsync();
            }

            // note: must save to db first to generate the video id and no.
            // before saving local video file

            int exportVideoErrorCode = 0;
            string routeVideoNo = null;

            switch (climbMode)
            {
                case ClimbMode.Training:
                    // save to db first
                    TrainingRouteVideo trainingRouteVideo;
                    if (isRecordingDemo)
                    {
                        trainingRouteVideo = TrainingRouteVideoDataAccess.InsertToReplacePreviousDemo(routeId, true);                        
                    }
                    else
                    {
                        trainingRouteVideo =
                            TrainingRouteVideoDataAccess.Insert(routeId, isRecordingDemo, true);
                    }
                    
                    // save local video file
                    string trainingRouteVideoLocalPath = FileHelper.TrainingRouteVideoRecordedFullPath(
                        TrainingRouteDataAccess.TrainingRouteNoById(routeId),
                        trainingRouteVideo.VideoNo);
                    exportVideoErrorCode = await gameplayVideoRecClient.ExportVideoAndClearBufferAsync(trainingRouteVideoLocalPath);
                    routeVideoNo = trainingRouteVideo.VideoNo;
                    break;
                case ClimbMode.Boulder:
                default:
                    // save to db first
                    BoulderRouteVideo boulderRouteVideo;
                    if (isRecordingDemo)
                    {
                        boulderRouteVideo =
                            BoulderRouteVideoDataAccess.InsertToReplacePreviousDemo(routeId, true);
                    }
                    else
                    {
                        boulderRouteVideo =
                            BoulderRouteVideoDataAccess.Insert(routeId, isRecordingDemo, true);
                    }

                    // save local video file
                    string boulderRouteVideoLocalPath = FileHelper.BoulderRouteVideoRecordedFullPath(
                        BoulderRouteDataAccess.BoulderRouteNoById(routeId),
                        boulderRouteVideo.VideoNo);
                    exportVideoErrorCode = await gameplayVideoRecClient.ExportVideoAndClearBufferAsync(boulderRouteVideoLocalPath);
                    routeVideoNo = boulderRouteVideo.VideoNo;
                    break;
            }

            if (exportVideoErrorCode == 0)
            {
                UiHelper.NotifyUser("Success when saving video no. " + routeVideoNo);
            }
            else
            {
                UiHelper.NotifyUser("Error when saving video no. " + routeVideoNo);
            }

            // !!! Important !!! refresh data grid to see the route video item newly inserted
            //DgridRouteVideos.Items.Refresh();
            //CollectionViewSource.GetDefaultView(DgridRouteVideos.ItemsSource).Refresh();
            viewModel.LoadRouteVideoData();

            // TODO: rather strange code
            if (isRecordingDemo)
            {
                // this would change isRecordingDemo as well
                // as isRecordingDemo is bound to navHead.IsRecordDemoVideo
                // via event handler OnNavHeadIsRecordDemoChanged
                navHead.IsRecordDemoVideo = false;                
            }
        }

        #endregion


        #region video playback

        private void ShowVideoPlaybackDialog(RouteVideoViewModel videoVM)
        {
            string videoFilePath = videoVM.VideoRecordedFullPath();            

            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playgroundMedia);
            VideoPlayback videoPlaybackPage =
                new VideoPlayback(videoFilePath, playgroundMedia);

            videoPlaybackDialog.Navigate(videoPlaybackPage);
            videoPlaybackDialog.ShowDialog();
        }

        #endregion
    }
}
