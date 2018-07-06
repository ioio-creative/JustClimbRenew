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
using System.Windows.Media;
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
        private const string HeaderRowTitleFormat = "{0} Route {1} - Video Playback";

        private readonly bool debug = AppGlobal.DEBUG;

        // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/
        private NavigationService navSvc;

        private const float DefaultDistanceThreshold = 0.1f;

        private string routeId;
        private ClimbMode climbMode;
        private Tuple<string, string> videoIdAndNo;
        private GameStartViewModel viewModel;

        private RocksOnRouteViewModel rocksOnRouteVM;

        // Training mode use only
        private IEnumerator<RockOnRouteViewModel> nextRockOnTrainRoute;

        // Boulder mode use only
        private IEnumerable<RockOnRouteViewModel> interRocksOnBoulderRoute;
        //private IEnumerable<RockTimerHelper> interRockOnBoulderRouteTimers;
        //private CameraSpacePoint[] interRocksOnRouteCamSP;


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

        //private int nextTrainRockIdx = 0;

        private bool gameStarted = false;

        private RockTimerHelper endRockHoldTimer = new RockTimerHelper(goal: 24, lag: 6);

        private DispatcherTimer gameOverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        private Plane wallPlane;
        private Plane floorPlane;

        private bool interRocksVisualFeedBack = true;

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
            BtnRestartGame.Command = new RelayCommand(RestartCommand, CanRestartGame);
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

        private async void RestartCommand(object parameter = null)
        {
            //TODO: review restart logic
            if (gameplayVideoRecClient.IsRecording)
            {
                await SaveVideoRecordedInDbAndLocallyAsync();
            }

            ResetGameStart();
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
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
                    mainWindowClient.SubscribeColorImgSrcToPlaygrd();
                }

                playgroundWindow = mainWindowClient.PlaygroundWindow;
                playgroundMedia = playgroundWindow.PlaygroundMedia;
                playgroundCanvas = playgroundWindow.PlaygroundCanvas;

                navHead.PropertyChanged += HandleNavHeadIsRecordDemoChanged;

                //methods to access rocks on route from database
                rocksOnRouteVM = RocksOnRouteViewModel.CreateFromDatabase(climbMode,
                    routeId, playgroundCanvas, kinectManagerClient.ManagerCoorMapper);

                ResetGameStart();                

                kinectManagerClient.BodyFrameArrived += HandleBodyListArrived;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/ 
            navSvc.Navigating -= NavigationService_Navigating;
            navSvc = null;

            gameplayVideoRecClient.StopRecordingIfIsRecording();            

            kinectManagerClient.BodyFrameArrived -= HandleBodyListArrived;
            navHead.PropertyChanged -= HandleNavHeadIsRecordDemoChanged;
            playgroundMedia.MediaEnded -= HandlePlaygroundVideoEndedAsync;
            playgroundCanvas.Children.Clear();
            if (debug)
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd(); 
            }
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

        private void HandleNavHeadIsRecordDemoChanged(object sender, PropertyChangedEventArgs e)
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
       
        private async void HandlePlaygroundVideoEndedAsync(object sender, RoutedEventArgs e)
        {
            //This Function should only be subscribed when Gameover or Finish videos are loaded to playground

            // stop video recording
            if (gameplayVideoRecClient.IsRecording)
            {
                await SaveVideoRecordedInDbAndLocallyAsync();
            }

            ResetGameStart();
            playgroundMedia.MediaEnded -= HandlePlaygroundVideoEndedAsync;
        }

        #endregion

        
        #region State Change Tasks

        private async Task OnGameplayStartAsync()
        {
            switch (climbMode)
            {
                case ClimbMode.Boulder:
                default:
                    videoIdAndNo = BoulderRouteVideoDataAccess.GenerateIdAndNo();
                    break;
                case ClimbMode.Training:
                    videoIdAndNo = TrainingRouteVideoDataAccess.GenerateIdAndNo();
                    break;                
            }

            // start video recording
            await gameplayVideoRecClient.StartRecordingAsync(videoIdAndNo.Item2);

            playgroundMedia.Stop();
            //Play "Start" video
            playgroundMedia.Source = new Uri(FileHelper.GameplayStartVideoPath());
            playgroundWindow.LoopMedia = false;
            playgroundMedia.Play();

            gameStarted = true;
        }

        private void OnGameplayFinish()
        {
            gameStarted = false;
            //Play "Finish" video
            playgroundMedia.Stop();
            playgroundMedia.Source = new Uri(FileHelper.GameplayFinishVideoPath());
            playgroundWindow.LoopMedia = false;
            playgroundMedia.MediaEnded += HandlePlaygroundVideoEndedAsync;
            playgroundMedia.Play();
        }

        private void CheckGameOverWithTimer(Body body)
        {
            if (IsBodyGameOver(body))
            {
                //We have to declare null to EventHandler before we can unsubcribe itself inside lambda expression
                ////https://stackoverflow.com/questions/3082143/can-an-anonymous-delegate-unsubscribe-itself-from-an-event-once-it-has-been-fire

                EventHandler _tickHandler  = null;
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

            playgroundMedia.Stop();
            //Play "GameOver" Video
            playgroundMedia.Source = new Uri(FileHelper.GameOverVideoPath());
            playgroundWindow.LoopMedia = false;
            playgroundMedia.MediaEnded += HandlePlaygroundVideoEndedAsync;
            playgroundMedia.Play();
        }

        private void ResetGameStart()
        {
            //TODO: Check Video Recorder after reset (multiple export after reset)
            //rocktimers tick sub and unsub
            Console.WriteLine("Before Reset: " + playgroundCanvas.Children.Count);
            playgroundCanvas.Children.Clear();
            Console.WriteLine("After Reset: " + playgroundCanvas.Children.Count);

            if (debug)
            {
                rocksOnRouteVM.DrawAllRocksOnRouteInGame();
            }

            switch (climbMode)
            {
                case ClimbMode.Training:
                    #region Training Mode Setup
                    navHead.HeaderRowTitle =
                        string.Format(HeaderRowTitleFormat, "Training", TrainingRouteDataAccess.TrainingRouteNoById(routeId));

                    nextRockOnTrainRoute = rocksOnRouteVM.RockOnRouteEnumerator;
                    nextRockOnTrainRoute.Reset();
                    nextRockOnTrainRoute.MoveNext();
                    //Console.WriteLine("rock: " + nextRockOnTrainRoute.Current.TrainingSeq);

                    if (debug)
                    {
                        rocksOnRouteVM.DrawTrainingPathInGame();
                    }

                    #endregion
                    break;

                case ClimbMode.Boulder:
                default:
                    #region Boulder Mode Setup
                    navHead.HeaderRowTitle =
                        string.Format(HeaderRowTitleFormat, "Bouldering", BoulderRouteDataAccess.BoulderRouteNoById(routeId));

                    interRocksOnBoulderRoute = rocksOnRouteVM.InterRocks;

                    //interRockOnBoulderRouteTimers = allRocksOnBoulderRoute.Select( x => { return x.MyRockTimerHelper; });
                    //int interRocksCnt = interRocksOnBoulderRoute.Count();
                    //interRocksOnRouteCamSP = new CameraSpacePoint[interRocksOnBoulderRoute.Count()];

                    int i = 0;
                    if (debug)
                    {
                        //foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                        //{
                        //    interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                        //    i++;
                        //}
                    }
                    else
                    {
                        rocksOnRouteVM.StartRock.MyRockViewModel.CreateBoulderImageSequence();
                        rocksOnRouteVM.StartRock.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                            new BitmapSource[][]
                            {
                                ImageSequenceHelper.ShowSequence,  // 1
                                ImageSequenceHelper.ShinePopSequence,  // 3
                                //ImageSequenceHelper.ShineLoopSequence  // 4
                                ImageSequenceHelper.ShineFeedbackLoopSequence
                            }
                        );

                        rocksOnRouteVM.EndRock.MyRockViewModel.CreateBoulderImageSequence();
                        rocksOnRouteVM.EndRock.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                            new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1

                        foreach (var rockOnBoulderRoute in interRocksOnBoulderRoute)
                        {
                            //rockOnBoulderRoute.DrawRockImageWrtStatus();
                            //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.Play();

                            //rockOnBoulderRoute.MyRockViewModel.CreateBoulderImageSequence();
                            //rockOnBoulderRoute.MyRockViewModel.BoulderButtonSequence.SetAndPlaySequences(true,
                            //    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1

                            //interRocksOnRouteCamSP[i] = rockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
                            i++;
                        }
                    }
                    #endregion
                    break;
            }//close switch (climbMode)              

            playgroundMedia.Stop();
            playgroundMedia.Source = new Uri(FileHelper.GameplayReadyVideoPath());
            playgroundWindow.LoopMedia = true;
            playgroundMedia.Play();
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

        // training

        private bool IsTrainingTargetReached(RockOnRouteViewModel rockOnRouteVM, Body body,
            IEnumerable<Joint> LHandJoints, IEnumerable<Joint> RHandJoints)
        {
            bool reached = false;

            //Both hands need to be on starting rock to start training mode
            if (rockOnRouteVM == rocksOnRouteVM.StartRock)
            {
                reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel);
            }
            //Both hands need to be on final rock to end game
            else if (rockOnRouteVM == rocksOnRouteVM.EndRock)
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

        private void SetNextTrainingRockTimerTickEventHandler(Body body, RockTimerHelper nextRockTimer, Func<RockOnRouteViewModel, bool> isTrainingTargetReached)
        {
            EventHandler trainingRockTimerTickEventHandler = null;
            trainingRockTimerTickEventHandler = async (_sender, _e) =>
            {
                if (isTrainingTargetReached(nextRockOnTrainRoute.Current))
                {
                    nextRockTimer.RockTimerCountIncr();

                    if (nextRockTimer.IsTimerGoalReached())
                    {
                        //Starting Rock
                        if (nextRockOnTrainRoute.Current == rocksOnRouteVM.StartRock)
                        {
                            await OnGameplayStartAsync();


                            //START ROCK REACHED VERIFIED
                            playerBodyID = body.TrackingId;
                            //Debug.WriteLine("Player Tracking ID: "+playerBodyID);

                            nextRockOnTrainRoute.MoveNext();
                            //nextTrainRockIdx++;
                            nextRockTimer.Reset();

                            //TODO: StartRock Feedback Animation
                            if (debug)
                            {
                                DebugRecolorRockVM(rocksOnRouteVM.StartRock);
                            }
                        }
                        //End Rock
                        else if (nextRockOnTrainRoute.Current == rocksOnRouteVM.EndRock)
                        {
                            //END ROCK REACHED VERIFIED
                            nextRockTimer.Reset();
                            //Play "Count down to 3" video
                            playgroundMedia.Source = new Uri(FileHelper.GameplayCountdownVideoPath());
                            playgroundWindow.LoopMedia = false;

                            if (!endRockHoldTimer.IsTickHandlerSubed)
                            {
                                SetEndRockHoldTimerTickEventHandler(body, nextRockTimer, trainingRockTimerTickEventHandler, isTrainingTargetReached);
                            }

                            if (!endRockHoldTimer.IsEnabled)
                            {
                                endRockHoldTimer.Reset();
                                playgroundMedia.Play();
                                endRockHoldTimer.Start();
                            }

                            //TODO: EndRock Feedback Animation
                            if (debug)
                            {
                                DebugRecolorRockVM(rocksOnRouteVM.EndRock);
                            }
                        }
                        //Inter Rocks
                        else
                        {
                            //TODO: Interrock reached behaviour                                  
                            //INTER ROCK REACHED VERIFIED
                            nextRockTimer.Reset();

                            if (debug)
                            {
                                DebugRecolorRockVM(nextRockOnTrainRoute.Current);
                            }


                            //We call movenext after everything has been done to current RockVM
                            nextRockOnTrainRoute.MoveNext();
                            //nextTrainRockIdx++;

                            
                        }

                    }
                }

                if (nextRockTimer.IsLagThresholdExceeded())
                {
                    nextRockTimer.Reset();
                }
            };//trainingRockTimerTickHandler = async (_sender, _e) =>

            nextRockTimer.Tick += trainingRockTimerTickEventHandler;
            nextRockTimer.IsTickHandlerSubed = true;
        }

        private void TrainingGameplay(Body body, IEnumerable<Joint> LHandJoints,
            IEnumerable<Joint> RHandJoints)
        {           
            //nextRockOnTrainRoute = rocksOnRouteVM.RocksOnRoute.ElementAt(nextTrainRockIdx);
            RockTimerHelper nextRockTimer = nextRockOnTrainRoute.Current.MyRockTimerHelper;

            if (gameStarted)
            {
                //CHECK GAME OVER AT ALL TIMES AFTER GAME STARTED              
                CheckGameOverWithTimer(body);
            }

            Func<RockOnRouteViewModel, bool> isTrainingTargetReached = (rockOnRouteVM) =>
            {
                return IsTrainingTargetReached(nextRockOnTrainRoute.Current, body, LHandJoints, RHandJoints);
            };

            if (isTrainingTargetReached(nextRockOnTrainRoute.Current))
            {
                //DO SOMETHING WHEN ANY RELEVANT JOINT TOUCHES STARTING POINT

                if (!nextRockTimer.IsTickHandlerSubed)
                {
                    SetNextTrainingRockTimerTickEventHandler(body, nextRockTimer, isTrainingTargetReached);
                }

                if (!nextRockTimer.IsEnabled)
                {
                    nextRockTimer.Reset();
                    nextRockTimer.Start();
                }
            }
        }

        // end of training

        // boulder

        private void SetStartBoulderRockTimerTickEventHandler(Body body, RockTimerHelper startRockTimer, Func<RockOnRouteViewModel, bool> isBoulderTargetReached)
        {
            EventHandler startRockTimerTickEventHandler = null;
            startRockTimerTickEventHandler = async (_sender, _e) =>
            {
                if (isBoulderTargetReached(rocksOnRouteVM.StartRock))
                {
                    startRockTimer.RockTimerCountIncr();

                    if (startRockTimer.IsTimerGoalReached())
                    {
                        //START ROCK REACHED VERIFIED
                        playerBodyID = body.TrackingId;
                        //Debug.WriteLine("Player Tracking ID: " + playerBodyID);

                        startRockTimer.Reset();
                        startRockTimer.Tick -= startRockTimerTickEventHandler;
                        startRockTimer.IsTickHandlerSubed = false;
                        await OnGameplayStartAsync();

                        //TODO: StartRock Feedback Animation
                        if (debug)
                        {
                            DebugRecolorRockVM(rocksOnRouteVM.StartRock);
                        }
                    }
                }

                if (startRockTimer.IsLagThresholdExceeded())
                {
                    startRockTimer.Reset();
                }
            };//startRockTimerTickEventHandler = async (_sender, _e) =>

            startRockTimer.Tick += startRockTimerTickEventHandler;
            startRockTimer.IsTickHandlerSubed = true;
        }

        private void SetEndBoulderRockTimerTickEventHandler(Body body, RockTimerHelper endRockTimer, Func<RockOnRouteViewModel, bool> isBoulderTargetReached)
        {
            EventHandler endRockTimerTickEventHandler = null;
            endRockTimerTickEventHandler = (_sender, _e) =>
            {
                if (isBoulderTargetReached(rocksOnRouteVM.EndRock))
                {
                    endRockTimer.RockTimerCountIncr();

                    if (endRockTimer.IsTimerGoalReached())
                    {
                        //END ROCK REACHED VERIFIED
                        endRockTimer.Reset();
                        endRockTimer.Tick -= endRockTimerTickEventHandler;
                        endRockTimer.IsTickHandlerSubed = false;

                        //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                        //Play "Count down to 3" video
                        playgroundMedia.Source = new Uri(FileHelper.GameplayCountdownVideoPath());
                        playgroundWindow.LoopMedia = false;

                        if (!endRockHoldTimer.IsTickHandlerSubed)
                        {
                            SetEndRockHoldTimerTickEventHandler(body, endRockTimer, endRockTimerTickEventHandler, isBoulderTargetReached);
                        }

                        if (!endRockHoldTimer.IsEnabled)
                        {
                            endRockHoldTimer.Reset();
                            playgroundMedia.Play();
                            endRockHoldTimer.Start();
                        }

                        //TODO: EndRock Feedback Animation
                        if (debug)
                        {
                            DebugRecolorRockVM(rocksOnRouteVM.EndRock);
                        }
                    }
                }

                if (endRockTimer.IsLagThresholdExceeded())
                {
                    endRockTimer.Reset();
                }
            };//endRockTimerTickEventHandler = (_sender, _e) =>

            endRockTimer.Tick += endRockTimerTickEventHandler;
            endRockTimer.IsTickHandlerSubed = true;
        }

        private void SetInterBoulderRockTimerTickEventHandler(Body body, RockOnRouteViewModel rockOnRoute, RockTimerHelper interRockTimer, Func<RockOnRouteViewModel, bool> isBoulderTargetReached)
        {
            EventHandler interRockTimerTickEventHandler = null;
            interRockTimerTickEventHandler = (_sender, _e) =>
            {
                if (isBoulderTargetReached(rockOnRoute))
                {
                    interRockTimer.RockTimerCountIncr();

                    if (interRockTimer.IsTimerGoalReached())
                    {
                        interRockTimer.Reset();

                        interRockTimer.Tick -= interRockTimerTickEventHandler;
                        interRockTimer.IsTickHandlerSubed = false;
                        //TODO: animation Feedback for that rock
                        if (debug)
                        {
                            DebugRecolorRockVM(rockOnRoute);
                        }
                    }

                    if (interRockTimer.IsLagThresholdExceeded())
                    {
                        interRockTimer.Reset();
                    }
                }
            };//interRockTimerTickEventHandler = (_sender, _e) =>

            interRockTimer.Tick += interRockTimerTickEventHandler;
            interRockTimer.IsTickHandlerSubed = true;
        }

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
                    reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel);
                    //reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel);
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    break;
                case RockOnBoulderStatus.End:
                    reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    //reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
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
                if (isBoulderTargetReached(rocksOnRouteVM.StartRock))
                {
                    //DO SOMETHING WHEN RELEVANT JOINT(S) TOUCHES STARTING POINT

                    //
                    playgroundWindow.LoopMedia = false;
                    playgroundMedia.Stop();

                    RockTimerHelper startRockTimer = rocksOnRouteVM.StartRock.MyRockTimerHelper;

                    if (!startRockTimer.IsTickHandlerSubed)
                    {
                        SetStartBoulderRockTimerTickEventHandler(body, startRockTimer, isBoulderTargetReached);
                    }

                    if (!startRockTimer.IsEnabled)
                    {                 
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
                if (isBoulderTargetReached(rocksOnRouteVM.EndRock))
                {
                    RockTimerHelper endRockTimer = rocksOnRouteVM.EndRock.MyRockTimerHelper;

                    if (!endRockTimer.IsTickHandlerSubed)
                    {
                        SetEndBoulderRockTimerTickEventHandler(body, endRockTimer, isBoulderTargetReached);
                    }

                    if (!endRockTimer.IsEnabled)
                    {
                        endRockTimer.Reset();
                        endRockTimer.Start();
                    }
                }

                if (interRocksVisualFeedBack)
                {
                    foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute)
                    {
                        RockTimerHelper interRockTimer = rockOnRoute.MyRockTimerHelper;

                        if (isBoulderTargetReached(rockOnRoute))
                        {
                            if (!interRockTimer.IsTickHandlerSubed)
                            {
                                SetInterBoulderRockTimerTickEventHandler(body, rockOnRoute, interRockTimer, isBoulderTargetReached);
                            }
                        }

                        if (!interRockTimer.IsEnabled)
                        {
                            interRockTimer.Reset();
                            interRockTimer.Start();
                        }
                    } //CLOSE foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute) 
                }
            }
        }
        
        // end of boulder

        private void SetEndRockHoldTimerTickEventHandler(Body body, RockTimerHelper prevRockTimer, EventHandler prevRockTimerTickHandler, Func<RockOnRouteViewModel, bool> isEndRockReached)
        {
            EventHandler endRockHoldTimerTickEventHandler = null;
            endRockHoldTimerTickEventHandler = (_holdSender, _holdE) =>
            {
                if (isEndRockReached(rocksOnRouteVM.EndRock))
                {
                    endRockHoldTimer.RockTimerCountIncr();
                }

                if (endRockHoldTimer.IsTimerGoalReached())
                {
                    //TODO: Countdown finishes eventhough joint left target
                    //END ROCK 3-second HOLD VERIFIED
                    endRockHoldTimer.Reset();

                    prevRockTimer.Tick -= prevRockTimerTickHandler;
                    prevRockTimer.IsTickHandlerSubed = false;
                    endRockHoldTimer.Tick -= endRockHoldTimerTickEventHandler;
                    endRockHoldTimer.IsTickHandlerSubed = false;

                    OnGameplayFinish();

                    //TODO: animation Feedback for that rock
                }

                if (endRockHoldTimer.IsLagThresholdExceeded())
                {
                    playgroundMedia.Stop();
                    endRockHoldTimer.Reset();
                    endRockHoldTimer.Tick -= endRockHoldTimerTickEventHandler;
                    endRockHoldTimer.IsTickHandlerSubed = false;
                }
            };

            endRockHoldTimer.Tick += endRockHoldTimerTickEventHandler;
            endRockHoldTimer.IsTickHandlerSubed = true;
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
            // TODO: show loading message

            int exportVideoErrorCode = 0;
            string routeVideoNo = videoIdAndNo.Item2;

            switch (climbMode)
            {
                case ClimbMode.Training:
                    // save local video file first
                    string trainingRouteVideoLocalPath = FileHelper.TrainingRouteVideoRecordedFullPath(
                        TrainingRouteDataAccess.TrainingRouteNoById(routeId),
                        routeVideoNo);
                    
                    exportVideoErrorCode = 
                        await gameplayVideoRecClient.StopRecordingIfIsRecordingAndExportVideoAndClearBufferAsync(trainingRouteVideoLocalPath);                    

                    if (exportVideoErrorCode == 0)
                    {
                        // save to db last                        
                        if (isRecordingDemo)
                        {
                            TrainingRouteVideoDataAccess.InsertToReplacePreviousDemo(videoIdAndNo, routeId, true);
                        }
                        else
                        {                            
                            TrainingRouteVideoDataAccess.Insert(videoIdAndNo, routeId, isRecordingDemo, true);
                        }
                    }
                    
                    break;
                case ClimbMode.Boulder:
                default:
                    // save local video file first
                    string boulderRouteVideoLocalPath = FileHelper.BoulderRouteVideoRecordedFullPath(
                        BoulderRouteDataAccess.BoulderRouteNoById(routeId),
                        routeVideoNo);

                    exportVideoErrorCode = 
                        await gameplayVideoRecClient.StopRecordingIfIsRecordingAndExportVideoAndClearBufferAsync(boulderRouteVideoLocalPath);

                    if (exportVideoErrorCode == 0)
                    {
                        // save to db last                    
                        if (isRecordingDemo)
                        {
                            BoulderRouteVideoDataAccess.InsertToReplacePreviousDemo(videoIdAndNo, routeId, true);
                        }
                        else
                        {
                            BoulderRouteVideoDataAccess.Insert(videoIdAndNo, routeId, isRecordingDemo, true);
                        } 
                    }
                    break;
            }

            // TODO: hide loading message

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

        #region debug drawing functions

        private void DebugRecolorRockVM(RockOnRouteViewModel rockVM)
        {
            DebugRecolorRockVM(rockVM, Brushes.Blue);
        }

        private void DebugRecolorRockVM(RockOnRouteViewModel rockVM, Brush color)
        {
            rockVM.MyRockViewModel.RecolorRockShape(color);
        }

        #endregion
    }
}
