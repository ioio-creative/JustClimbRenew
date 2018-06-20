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
             
        //Both modes use the same 
        private RockOnRouteViewModel startRockOnRoute;
        private RockOnRouteViewModel endRockOnRoute;
        //Training mode use only
        private RockOnRouteViewModel nextRockOnTrainRoute;
        private IEnumerable<RockOnRouteViewModel> rocksOnTrainingRoute;
        //We save this as global to avoid repeat contructor calling by LINQ.Count();
        private int trainingRouteLength;

        //Boulder mode use only
        private IEnumerable<RockOnRouteViewModel> interRocksOnBoulderRoute;
        private CameraSpacePoint[] interRocksOnRouteCamSP;
        

        private ulong playerBodyID;

        #region Manager vars

        private MainWindow mainWindowClient;
        private KinectManager kinectManagerClient;

        #endregion

        #region Playground variables

        private Playground playgroundWindow;
        private Canvas playgroundCanvas;
        private MediaElement playgroundMedia;

        #endregion


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

        private DispatcherTimer gameOverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
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
            viewModel = v_gridContainer.DataContext as GameStartViewModel;
            if (viewModel != null)
            {
                CollectionViewSource cvsVideos = v_gridContainer.Resources["cvsRouteVideos"] as CollectionViewSource;
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
                await gameplayVideoRecClient.StartRecordingAsync();                
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

            if (kinectManagerClient.OpenKinect())
            {

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
                        #region Training Mode Setup
                        navHead.HeaderRowTitle =
                            string.Format(headerRowTitleFormat, "Training", TrainingRouteDataAccess.TrainingRouteNoById(routeId));

                        rocksOnTrainingRoute = TrainingRouteAndRocksDataAccess.OrderedRocksByRouteId(routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper).ToArray();
                        startRockOnRoute = rocksOnTrainingRoute.First();
                        endRockOnRoute = rocksOnTrainingRoute.Last();
                        trainingRouteLength = rocksOnTrainingRoute.Count();

                        if (debug)
                        {
                            foreach (var rockOnTrainingRoute in rocksOnTrainingRoute)
                            {
                                rockOnTrainingRoute.DrawRockShapeWrtTrainSeq(trainingRouteLength);
                            }
                        }
                        #endregion
                        break;
                    

                    case ClimbMode.Boulder:
                    default:
                        #region Boulder Mode Setup
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
                                new BitmapSource[][]
                                {
                                ImageSequenceHelper.ShowSequence,  // 1
                                ImageSequenceHelper.ShinePopSequence,  // 3
                                //ImageSequenceHelper.ShineLoopSequence  // 4
                                ImageSequenceHelper.ShineFeedbackLoopSequence
                                }
                            );

                            endRockOnRoute.MyRockViewModel.CreateBoulderImageSequence();
                            endRockOnRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                                new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1

                            foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                            {
                                //rockOnBoulderRoute.DrawRockImageWrtStatus();
                                //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.Play();

                                //rockOnBoulderRoute.MyRockViewModel.CreateBoulderImageSequence();
                                //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                                //    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1

                                interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                                i++;
                            }
                        } 
                        #endregion
                        break;
                }

                await gameplayVideoRecClient.WaitingForAllBufferFramesSavedAsync();

                playgroundMedia.Stop();
                playgroundMedia.Source = new Uri(FileHelper.GameplayReadyVideoPath());
                playgroundWindow.LoopMedia = true;
                playgroundMedia.Play();

                kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;
            }
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
            playgroundMedia.MediaEnded -= PlaygroundVideoEndedHandler;
            playgroundCanvas.Children.Clear();
            mainWindowClient.UnsubColorImgSrcToPlaygrd();
        }

        public void HandleBodyListArrived(object sender, BodyListArrEventArgs e)
        {
            //Microsoft.Kinect.Vector4 floorClipPlane = e.GetFloorClipPlane();

            //floorPlane = new Plane(floorClipPlane.X, floorClipPlane.Y, floorClipPlane.Z, floorClipPlane.W);
            
            //remove skeleton shape and empty List<>when DEBUG
            if (debug)
            {
                foreach (Shape skeletonShape in skeletonBodies.SelectMany(shapes => shapes))
                {
                    playgroundCanvas.RemoveChild(skeletonShape);
                }

                //foreach (IEnumerable<Shape> skeletonShapes in skeletonBodies)
                //{
                //    foreach (Shape skeletonShape in skeletonShapes)
                //    {
                //        playgroundCanvas.RemoveChild(skeletonShape);
                //    }
                //}
                //skeletonBodies = new List<IEnumerable<Shape>>();
                skeletonBodies.Clear();
            }

            IList<Body> bodies = e.GetBodyList();

            // if game started and player (previously tracked) is no longer tracked, then game over
            if (gameStarted && !bodies.Where(x => x.TrackingId == playerBodyID).Any())
            {
                OnGameOver();
            }
            else
            {            
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

                        if (!gameStarted)
                        {
                            GameplayMainSwitch(body);
                        }
                        else
                        {
                            if (body.TrackingId == playerBodyID)
                            {
                                GameplayMainSwitch(body);
                            }
                        }
                    }

                }//CLOSE foreach (var body in bodies) 
            }
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
                UiHelper.NotifyUser("Can't leave page before the game is finished!");
                e.Cancel = true;
            }
        }

        private async Task OnGameplayStartAsync()
        {
            // start video recording
            await gameplayVideoRecClient.StartRecordingAsync();

            //Play "Start" video
            playgroundMedia.Source = new Uri(FileHelper.GameplayStartVideoPath());
            playgroundMedia.Play();

            gameStarted = true;
        }

        private async Task OnGameplayFinishAsync()
        {
            gameStarted = false;
            //Play "Finish" video
            playgroundMedia.Source = new Uri(FileHelper.GameplayFinishVideoPath());
            playgroundMedia.MediaEnded += PlaygroundVideoEndedHandler;
            playgroundMedia.Play();

            await SaveVideoRecordedInDbAndLocallyAsync();


        }

        private void CheckGameOverWithTimer(Body body)
        {        
            if (IsBodyGameOver(body))
            {
                //We have to declare null to EventHandler before we can unsubcribe itself inside lambda expression
                ////https://stackoverflow.com/questions/3082143/can-an-anonymous-delegate-unsubscribe-itself-from-an-event-once-it-has-been-fire

                EventHandler _tickHandler = null;
                _tickHandler = (_sender, _e) =>
                {
                    if (IsBodyGameOver(body))
                    {                       
                        OnGameOver();                       
                    }
                    gameOverTimer.Stop();
                    gameOverTimer.Tick -= _tickHandler;
                };

                if (!gameOverTimer.IsEnabled)
                {
                    gameOverTimer.Tick += _tickHandler;
                    gameOverTimer.Stop();
                    gameOverTimer.Start();
                }
            }
        }

        private void OnGameOver()
        {
            gameStarted = false;
            playgroundCanvas.Children.Clear();

            playgroundMedia.Source = new Uri(FileHelper.GameOverVideoPath());
            playgroundMedia.MediaEnded += PlaygroundVideoEndedHandler;

            playgroundMedia.Play();
            
        }

        private void PlaygroundVideoEndedHandler(object sender, RoutedEventArgs e)
        {
            //This Function should only be subscribed when Gameover or Finish videos are loaded to playground
            if (gameplayVideoRecClient.IsRecording)
            {
                // stop video recording
                gameplayVideoRecClient.StopRecording(); 
            }

            NavigationService.Navigate(new Routes(climbMode));
        }

        #endregion


        #region game play logic

        private void GameplayMainSwitch(Body body)
        {
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

        private bool IsTrainingTargetReached(RockOnRouteViewModel rockOnRouteVM, Body body,
            IEnumerable<Joint> LHandJoints, IEnumerable<Joint> RHandJoints)
        {
            bool reached = false;

            //Both hands need to be on starting rock to start training mode
            if (rockOnRouteVM == startRockOnRoute)
            {
                reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel);
            }
            //Both hands need to be on final rock to end game
            else if (rockOnRouteVM == endRockOnRoute)
            {
                reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
            }
            //Single hand can validate for all the other rocks in between
            else
            {
                IEnumerable<Joint> handJoints =
                    LHandJoints.Union(RHandJoints);  //.Where(x => KinectExtensions.HandJoints.Contains(x.JointType));
                reached = IsJointGroupOnRock(handJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
            }

            return reached;
        }

        private void TrainingGameplay(Body body, IEnumerable<Joint> LHandJoints, 
            IEnumerable<Joint> RHandJoints)
        {
            nextRockOnTrainRoute = rocksOnTrainingRoute.ElementAt(nextTrainRockIdx);
            RockTimerHelper nextRockTimer = nextRockOnTrainRoute.MyRockTimerHelper;

            if (gameStarted)
            {
                //CHECK GAME OVER AT ALL TIMES AFTER GAME STARTED              
                CheckGameOverWithTimer(body);
            }

            Func<RockOnRouteViewModel, bool> isTrainingTargetReached = (rockOnRouteVM) =>
            {
                return IsTrainingTargetReached(nextRockOnTrainRoute, body, LHandJoints, RHandJoints);
            };

            if (isTrainingTargetReached(nextRockOnTrainRoute))
            {
                //DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT
                //Stop "Ready" video
                playgroundWindow.LoopMedia = false;
                playgroundMedia.Stop();

                if (!nextRockTimer.IsTickHandlerSubed)
                {
                    SetNextTrainingRockTimerTickEventHandler(body, nextRockTimer, isTrainingTargetReached);
                    nextRockTimer.IsTickHandlerSubed = true;
                }

                if (!nextRockTimer.IsEnabled)
                {
                    nextRockTimer.Reset();
                    nextRockTimer.Start();
                }
            }            
        }
       
        private void SetNextTrainingRockTimerTickEventHandler(Body body, RockTimerHelper nextRockTimer, Func<RockOnRouteViewModel, bool> isTrainingTargetReached)
        {
            EventHandler trainingRockTimerTickEventHandler = null;
            trainingRockTimerTickEventHandler = async (_sender, _e) =>
            {
                if (isTrainingTargetReached(nextRockOnTrainRoute))
                {
                    nextRockTimer.RockTimerCountIncr();

                    if (nextRockTimer.IsTimerGoalReached())
                    {
                        //Starting Rock
                        if (nextRockOnTrainRoute == startRockOnRoute)
                        {
                            await OnGameplayStartAsync();


                            //START ROCK REACHED VERIFIED
                            playerBodyID = body.TrackingId;
                            //Debug.WriteLine("Player Tracking ID: "+playerBodyID);

                            nextTrainRockIdx++;
                            nextRockTimer.Reset();

                            //TODO: StartRock Feedback Animation                                    
                        }
                        //End Rock
                        else if (nextRockOnTrainRoute == endRockOnRoute)
                        {
                            //END ROCK REACHED VERIFIED
                            //Play "Count down to 3" video
                            playgroundMedia.Source = new Uri(FileHelper.GameplayCountdownVideoPath());

                            if (!endRockHoldTimer.IsTickHandlerSubed)
                            {
                                SetEndRockHoldTimerTickEventHandler(body, nextRockTimer, trainingRockTimerTickEventHandler, isTrainingTargetReached);

                                endRockHoldTimer.IsTickHandlerSubed = true;
                            }

                            if (!endRockHoldTimer.IsEnabled)
                            {
                                endRockHoldTimer.Reset();
                                playgroundMedia.Play();
                                endRockHoldTimer.Start();
                            }
                        }
                        //Inter Rocks
                        else
                        {
                            //TODO: Interrock reached behaviour                                  
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
            };//trainingRockTimerTickHandler = async (_sender, _e) =>

            nextRockTimer.Tick += trainingRockTimerTickEventHandler;
        }

        private void SetEndRockHoldTimerTickEventHandler(Body body, RockTimerHelper prevRockTimer, EventHandler prevRockTimerTickHandler, Func<RockOnRouteViewModel, bool> isEndRockReached)
        {
            EventHandler endRockHoldTimerTickEventHandler = null;
            endRockHoldTimerTickEventHandler = async (_holdSender, _holdE) =>
            {
                if (isEndRockReached(endRockOnRoute))
                {
                    endRockHoldTimer.RockTimerCountIncr(); 
                }

                if (endRockHoldTimer.IsTimerGoalReached())
                {
                    //END ROCK 3-second HOLD VERIFIED
                    endRockHoldTimer.Stop();

                    prevRockTimer.Tick -= prevRockTimerTickHandler;
                    endRockHoldTimer.Tick -= endRockHoldTimerTickEventHandler;

                    await OnGameplayFinishAsync();

                    //TODO: animation Feedback for that rock
                }

                if (endRockHoldTimer.IsLagThresholdExceeded())
                {
                    playgroundMedia.Stop();
                    endRockHoldTimer.Reset();
                    endRockHoldTimer.Tick -= endRockHoldTimerTickEventHandler;
                }
            };

            endRockHoldTimer.Tick += endRockHoldTimerTickEventHandler;
        }

        //CONTINUE HERE
        private bool IsBoulderTargetReached(RockOnRouteViewModel rockOnRouteVM, Body body,
            IEnumerable<Joint> LHandJoints, IEnumerable<Joint> RHandJoints)
        {
            bool reached;

            IEnumerable<Joint> fourLimbJoints =
                        body.Joints.Where(x => KinectExtensions.LimbJoints.Contains(x.Value.JointType)).Select(y => y.Value);

            switch (rockOnRouteVM.BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    //TODO: confirm condition during UAT
                    //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel);
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel);
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    break;
                case RockOnBoulderStatus.End:
                    //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, x.MyRockViewModel) && body.TrackingId == playerBodyID;
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    break;
            }

            return reached;
        }

        private void BoulderGameplay(Body body, IEnumerable<Joint> LHandJoints,
           IEnumerable<Joint> RHandJoints)
        {
            Func<RockOnRouteViewModel, bool> isBoulderTargetReached = (rockOnRouteVM) =>
            {
                return IsBoulderTargetReached(rockOnRouteVM, body, LHandJoints, RHandJoints);
            };

            if (!gameStarted) //Progress: Game not yet started, waiting player to reach Starting Point
            {
                if (isBoulderTargetReached(startRockOnRoute))
                {
                    //DO SOMETHING WHEN RELEVANT JOINT(S) TOUCHES STARTING POINT

                    playgroundWindow.LoopMedia = false;
                    playgroundMedia.Stop();

                    RockTimerHelper startRockTimer = startRockOnRoute.MyRockTimerHelper;
                    EventHandler startRockTimerTickHandler = null;
                    startRockTimerTickHandler = async (_sender, _e) =>
                    {
                        if (isBoulderTargetReached(startRockOnRoute))
                        {
                            startRockTimer.RockTimerCountIncr();

                            if (startRockTimer.IsTimerGoalReached())
                            {
                                //START ROCK REACHED VERIFIED
                                playerBodyID = body.TrackingId;
                                //Debug.WriteLine("Player Tracking ID: " + playerBodyID);

                                startRockTimer.Stop();
                                startRockTimer.Tick -= startRockTimerTickHandler;
                                await OnGameplayStartAsync();

                                //TODO: StartRock Feedback Animation                                
                            }
                        }

                        if (startRockTimer.IsLagThresholdExceeded())
                        {
                            startRockTimer.Reset();
                        }
                    };


                    if (!startRockTimer.IsEnabled)
                    {
                        startRockTimer.Tick += startRockTimerTickHandler;
                        startRockTimer.Reset();
                        startRockTimer.Start();
                    }
                }
            }
            else //gameStarted = true
            {
                //CHECK GAME OVER AT ALL TIMES AFTER GAME STARTED              
                CheckGameOverWithTimer(body);

                //Progress: Game Started, waiting player to reach End Point
                //CHECK END ROCK REACHED ALL THE TIME
                if (isBoulderTargetReached(endRockOnRoute))
                {
                    RockTimerHelper endRockTimer = endRockOnRoute.MyRockTimerHelper;

                    if (!endRockTimer.IsTickHandlerSubed)
                    {
                        EventHandler endRockTimerTickEventHandler = null;
                        endRockTimerTickEventHandler = (_sender, _e) =>
                        {
                            if (isBoulderTargetReached(endRockOnRoute))
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
                                        SetEndRockHoldTimerTickEventHandler(body, endRockTimer, endRockTimerTickEventHandler, isBoulderTargetReached);
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

                        endRockTimer.Tick += endRockTimerTickEventHandler;
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

                    if (isBoulderTargetReached(rockOnRoute))
                    {
                        if (!anyRockTimer.IsTickHandlerSubed)
                        {
                            EventHandler anyRocTimerTickEventHandler = null;
                            anyRocTimerTickEventHandler = (_sender, _e) =>
                            {
                                if (isBoulderTargetReached(rockOnRoute))
                                {
                                    anyRockTimer.RockTimerCountIncr();

                                    if (anyRockTimer.IsTimerGoalReached())
                                    {
                                        anyRockTimer.Stop();
                                        //TODO: animation Feedback for that rock
                                    }

                                    if (anyRockTimer.IsLagThresholdExceeded())
                                    {
                                        anyRockTimer.Reset();
                                    }
                                }
                            };

                            anyRockTimer.Tick += anyRocTimerTickEventHandler;
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


        #region determine overlap between Rock & Joint / gameover

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

        private bool IsBodyGameOver(Body body, float wallThr = 2f, float floorThr = 1f)//Default thresholds are in Meters
        {
            
            Joint bodyCenterJoint = body.Joints[JointType.SpineMid];
            Vector3 bodyCenterVec = new Vector3(bodyCenterJoint.Position.X, bodyCenterJoint.Position.Y, bodyCenterJoint.Position.Z);
            float distanceFromWall = Math.Abs(Plane.DotCoordinate(wallPlane, bodyCenterVec));

            //Console.WriteLine("DistanceFromWall = " + distanceFromWall);

            //Joint headJoint = body.Joints[JointType.Head];
            //Vector3 headVec = new Vector3(headJoint.Position.X, headJoint.Position.Y, headJoint.Position.Z);
            //double distanceFromFloor = Plane.DotCoordinate(floorPlane, headVec);

            Joint LFootJoint = body.Joints[JointType.FootLeft];
            Joint RFootJoint = body.Joints[JointType.FootRight];
            Vector3 LFootVec = new Vector3(LFootJoint.Position.X, LFootJoint.Position.Y, LFootJoint.Position.Z);
            Vector3 RFootVec = new Vector3(RFootJoint.Position.X, RFootJoint.Position.Y, RFootJoint.Position.Z);

            float distanceFromFloor = Math.Min(Math.Abs(Plane.DotCoordinate(wallPlane, LFootVec)), Math.Abs(Plane.DotCoordinate(wallPlane, RFootVec)));

            return (distanceFromWall > wallThr);
            //return (distanceFromWall > wallThr || distanceFromFloor < floorThr );
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
                //TODO: delete db video entry when failed to save video file
            }

            // !!! Important !!! refresh data grid to see the route video item newly inserted
            //DgridRouteVideos.Items.Refresh();
            //CollectionViewSource.GetDefaultView(DgridRouteVideos.ItemsSource).Refresh();
            viewModel.LoadRouteVideoData();

            //navHead.IsRecordDemoVideo setter already checks current state before changing value
            // this would change this.isRecordingDemo as well
            // as isRecordingDemo is bound to navHead.IsRecordDemoVideo
            // via event handler OnNavHeadIsRecordDemoChanged
            navHead.IsRecordDemoVideo = false;                
            
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
