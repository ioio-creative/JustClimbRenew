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
using JustClimbTrial.Views.UserControls;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
        private const string HeaderRowTitleFormat = "{0} Route {1}";

        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }
        private readonly bool drawSkeleton = true;

        // https://highfieldtales.wordpress.com/2013/07/27/how-to-prevent-the-navigation-off-a-page-in-wpf/
        private NavigationService navSvc;
        private HeaderRowNavigation navHead;

        private const float DefaultDistanceThreshold = 0.1f;

        private Tuple<string, string> videoIdAndNo;
        private GameStartViewModel viewModel;

        private RocksOnRouteViewModel rocksOnRouteVM;

        // Training mode use only
        private IEnumerator<RockOnRouteViewModel> nextRockOnTrainRoute;
        private Func<RockOnRouteViewModel, bool> isTrainingTargetReachedFunc;

        // Boulder mode use only
        private IEnumerable<RockOnRouteViewModel> interRocksOnBoulderRoute;
        private Func<RockOnRouteViewModel, bool> isBoulderTargetReachedFunc;
        //private IEnumerable<RockTimerHelper> interRockOnBoulderRouteTimers;


        private ulong playerBodyID;
        private Body playerBody;

        #region Manager vars

        private MainWindow mainWindowClient;
        private KinectManager kinectManagerClient;

        #endregion


        private IList<IEnumerable<Shape>> skeletonBodies = new List<IEnumerable<Shape>>();
        private VideoHelper gameplayVideoRecClient;

        private bool isRecordingDemo = false;

        private const string VideoHelperEngagedErrMsg =
            "Recording cannot be started as previous video saving processes are not finished.";


        #region ISavingVideo properties

        private string routeId;
        public string RouteId { get { return routeId; } }

        private ClimbMode climbMode;
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

        //TODO: combine hold and endrock timer to avoid confusion
        //private RockTimerHelper endRockHoldTimer = new RockTimerHelper(goal: 24, lag: 6);
        private const int EndRockHoldTimerGoal = 22; //unit = 10 millisecs
        private const int EndRockHoldTimerLag = 8;
        private bool isEndCountDownVideoPlaying = false;

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
            viewModel = this.DataContext as GameStartViewModel;
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

        private void InitializeNavHead()
        {
            navHead = master.NavHead;

            // pass this Page to the top row user control so it can use this Page's NavigationService
            navHead.ParentPage = this;

            string routeNo;
            switch (climbMode)
            {
                case ClimbMode.Training:
                    routeNo = TrainingRouteDataAccess.TrainingRouteNoById(routeId);
                    break;
                case ClimbMode.Boulder:
                default:
                    routeNo = BoulderRouteDataAccess.BoulderRouteNoById(routeId);
                    break;
            }
            navHead.HeaderRowTitle =
                string.Format(HeaderRowTitleFormat, ClimbModeGlobals.StringDict[climbMode],
                    routeNo);
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
            if (gameplayVideoRecClient.IsRecording)
            {
                MessageBoxResult mbr =
                        MessageBox.Show("Restart current play? The video in-recording will be deleted.", "Stop Recording and Restart?",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning);
                switch (mbr)
                {
                    case MessageBoxResult.Yes:
                        //We don't save the video when manually retart the game
                        await ForgoVideoRecordingAndClearBufferAsync();
                        ResetGameStart();
                        break;
                    case MessageBoxResult.No:
                    default:
                        break;
                }
            }
            else
            {
                ResetGameStart();
            }
        }

        

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeNavHead();

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

                navHead.PropertyChanged += HandleNavHeadIsRecordDemoChanged;

                //methods to access rocks on route from database
                rocksOnRouteVM = RocksOnRouteViewModel.CreateFromDatabase(climbMode,
                    routeId, mainWindowClient.GetPlaygroundCanvas(), kinectManagerClient.ManagerCoorMapper);

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
            mainWindowClient.RemovePlaygrounMediaEndedEventHandler(HandlePlaygroundVideoEndedAsync);
            mainWindowClient.ClearPlaygroundCanvas();
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
            if (debug || drawSkeleton)
            {
                foreach (Shape skeletonShape in skeletonBodies.SelectMany(shapes => shapes))
                {
                    mainWindowClient.GetPlaygroundCanvas().RemoveChild(skeletonShape);
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
                        if (debug || drawSkeleton)
                        {
                            IEnumerable<Shape> skeletonShapes = mainWindowClient.DrawSkeletonInPlaygroundCanvas(body, kinectManagerClient.ManagerCoorMapper, SpaceMode.Color);
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
                                playerBody = body;
                                GameplayMainSwitch(playerBody);
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
            mainWindowClient.RemovePlaygrounMediaEndedEventHandler(HandlePlaygroundVideoEndedAsync);
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

            //Play "Start" video
            mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameplayStartVideoPath());

            gameStarted = true;
            Console.WriteLine("Game Started !");
        }

        private void OnGameplayFinish()
        {
            gameStarted = false;
            isEndCountDownVideoPlaying = false;
            Debug.WriteLine("Finished!");
            //Play "Finish" video
            mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameplayFinishVideoPath());
            mainWindowClient.AddPlaygrounMediaEndedEventHandler(HandlePlaygroundVideoEndedAsync);
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
            Debug.WriteLine("Over!");
            mainWindowClient.ClearPlaygroundCanvas();

            //Play "GameOver" Video
            mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameOverVideoPath());
            mainWindowClient.AddPlaygrounMediaEndedEventHandler(HandlePlaygroundVideoEndedAsync);
        }

        private void ResetGameStart()
        {
            //TODO: Check Video Recorder after reset (multiple export after reset)
            //rocktimers tick sub and unsub
            mainWindowClient.ClearPlaygroundCanvas();

            if (debug)
            {
                rocksOnRouteVM.DrawAllRocksOnRouteInGame();
                if (climbMode == ClimbMode.Training)
                {
                    rocksOnRouteVM.DrawTrainingPathInGame();
                }
            }
            else
            {
                rocksOnRouteVM.SetRocksImgSequences();
                rocksOnRouteVM.PlayAllRocksOnRouteImgSequencesInGame();
            }
            
            switch (climbMode)
            {
                case ClimbMode.Training:
                    #region Training Mode Setup
                    nextRockOnTrainRoute = rocksOnRouteVM.GetEnumerator();
                    nextRockOnTrainRoute.Reset();
                    nextRockOnTrainRoute.MoveNext();

                    #endregion
                    break;

                case ClimbMode.Boulder:
                default:
                    #region Boulder Mode Setup
                    interRocksOnBoulderRoute = rocksOnRouteVM.InterRocks;

                    //interRockOnBoulderRouteTimers = allRocksOnBoulderRoute.Select( x => { return x.MyRockTimerHelper; });
                    //int interRocksCnt = interRocksOnBoulderRoute.Count();

                    #endregion
                    break;
            }//close switch (climbMode)    
            
            mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameplayReadyVideoPath(), true);
        }

        #endregion


        #region game play logic

        private void GameplayMainSwitch(Body body)
        {
            if (gameStarted)
            {
                //CHECK GAME OVER AT ALL TIMES AFTER GAME STARTED              
                CheckGameOverWithTimer(playerBody);
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

        /* training */

        private bool IsTrainingTargetReached(RockOnRouteViewModel rockOnRouteVM, Body body,
            IEnumerable<Joint> LHandJoints, IEnumerable<Joint> RHandJoints)
        {
            bool reached = false;

            //Both hands need to be on starting rock to start training mode
            if (rockOnRouteVM == rocksOnRouteVM.StartRock)
            {
                reached = IsJointGroupOnRock(LHandJoints.Union(RHandJoints), rockOnRouteVM.MyRockViewModel);
                //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel);
                if (reached)
                {
                    Debug.Write(body.Joints[JointType.HandTipLeft].Position.X);
                    Debug.Write(body.Joints[JointType.HandTipLeft].Position.Y);
                    Debug.WriteLine(body.Joints[JointType.HandTipLeft].Position.Z);
                }
            }
            //Both hands need to be on final rock to end game
            else if (rockOnRouteVM == rocksOnRouteVM.EndRock)
            {
                reached = IsJointGroupOnRock(LHandJoints.Union(RHandJoints), rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
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

        private void SetNextTrainingRockTimerTickEventHandler(ulong bodyID, RockOnRouteViewModel currentRockOnRouteVM)
        {
            EventHandler trainingRockTimerTickEventHandler = null;
            trainingRockTimerTickEventHandler = async (_sender, _e) =>
            {
                RockTimerHelper nextRockTimer = currentRockOnRouteVM.MyRockTimerHelper;            

                if (isTrainingTargetReachedFunc(currentRockOnRouteVM))
                {
                    nextRockTimer.RockTimerCountIncr();

                    if (nextRockTimer.IsTimerGoalReached())
                    {
                        nextRockTimer.Reset();
                        nextRockTimer.RemoveTickEventHandler(trainingRockTimerTickEventHandler);

                        //Starting Rock
                        if (currentRockOnRouteVM == rocksOnRouteVM.StartRock)
                        {
                            playerBodyID = bodyID;
                            Debug.WriteLine("Player Tracking ID: " + playerBodyID);

                            Debug.WriteLine("before OnGameplayStartAsync");

                            await OnGameplayStartAsync();


                            //START ROCK REACHED VERIFIED
                            if (!debug)
                            {
                                rocksOnRouteVM.StartRock.SetAndPlayFeedbackImgSeq();
                            }

                            //we move next first because of subsequent animation effect
                            nextRockOnTrainRoute.MoveNext();

                            //TODO: StartRock Feedback Animation
                            if (debug)
                            {
                                DebugRecolorRockVM(rocksOnRouteVM.StartRock);
                            }
                            else
                            {
                                //we trigger animation on the NEXT rock after start rock
                                nextRockOnTrainRoute.Current.SetAndPlayActivePopAndShineImgSeq();
                            }
                            
                        }
                        //End Rock
                        else if (currentRockOnRouteVM == rocksOnRouteVM.EndRock)
                        {
                            if (!debug)
                            {
                                rocksOnRouteVM.EndRock.SetAndPlayFeedbackShineLoopImgSeq();
                            }

                            OnGameplayFinish();
                            
                            //END ROCK REACHED VERIFIED

                            //TODO: EndRock Feedback Animation
                            if (debug)
                            {
                                DebugRecolorRockVM(rocksOnRouteVM.EndRock, Brushes.White);
                            }
                        }
                        //Inter Rocks
                        else
                        {
                            //TODO: Interrock reached behaviour                                  
                            //INTER ROCK REACHED VERIFIED
                            

                            if (debug)
                            {
                                DebugRecolorRockVM(currentRockOnRouteVM);
                            }
                            else
                            {
                                nextRockOnTrainRoute.Current.SetAndPlayFeedbackImgSeq();
                            }


                            //We call movenext after everything has been done to current RockVM
                            nextRockOnTrainRoute.MoveNext();
                            if (!debug)
                            {
                                nextRockOnTrainRoute.Current.SetAndPlayActivePopAndShineImgSeq();
                            }
                        }
                        
                    }
                }
                else
                {
                    Debug.WriteLine("RockTimerLostSampling!!");
                }

                if (nextRockTimer.IsLagThresholdExceeded())
                {
                    nextRockTimer.Reset();
                    nextRockTimer.RemoveTickEventHandler(trainingRockTimerTickEventHandler);

                    if (isEndCountDownVideoPlaying)
                    {
                        mainWindowClient.StopPlaygroundMedia();
                        isEndCountDownVideoPlaying = false;
                    }

                    if (debug && currentRockOnRouteVM == rocksOnRouteVM.EndRock)
                    {
                        DebugRecolorRockVM(rocksOnRouteVM.EndRock, Brushes.Red);
                    }
                }
            };//trainingRockTimerTickHandler = async (_sender, _e) =>

            currentRockOnRouteVM.MyRockTimerHelper.AddTickEventHandler(trainingRockTimerTickEventHandler);            
        }

        private void TrainingGameplay(Body body, IEnumerable<Joint> LHandJoints,
            IEnumerable<Joint> RHandJoints)
        {            
            RockOnRouteViewModel nextRockOnRouteVM = nextRockOnTrainRoute.Current;
            if (nextRockOnRouteVM != null)
            {
                RockTimerHelper nextRockTimer = nextRockOnRouteVM.MyRockTimerHelper;
                isTrainingTargetReachedFunc = (_rockOnRouteVM) =>
                {
                    return IsTrainingTargetReached(_rockOnRouteVM, body, LHandJoints, RHandJoints);
                };

                if (isTrainingTargetReachedFunc(nextRockOnRouteVM))
                {
                    //This Block only happens for End Rock
                    if (nextRockOnRouteVM == rocksOnRouteVM.EndRock)
                    {
                        if (gameStarted && !isEndCountDownVideoPlaying)
                        {
                            //Play "Count down to 3" video
                            mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameplayCountdownVideoPath());

                            nextRockTimer = nextRockOnRouteVM.InitializeRockTimerHelper(EndRockHoldTimerGoal, EndRockHoldTimerLag);
                            isEndCountDownVideoPlaying = true;
                        }

                        // no need to re-subscribe tick handler 
                        // after the game is finished or over
                        // (gameStarted will be set to false)
                        if (!nextRockTimer.IsTickHandlerSubed && gameStarted)
                        {
                            SetNextTrainingRockTimerTickEventHandler(body.TrackingId, nextRockOnRouteVM);
                        }

                        if (debug)
                        {
                            DebugRecolorRockVM(rocksOnRouteVM.EndRock);
                        }
                    }
                    else //non-End rocks
                    {
                        if (!nextRockTimer.IsTickHandlerSubed)
                        {
                            SetNextTrainingRockTimerTickEventHandler(body.TrackingId, nextRockOnRouteVM);
                        } 
                    }

                    if (!nextRockTimer.IsEnabled)
                    {
                        nextRockTimer.Reset();
                        nextRockTimer.Start();
                    }
                }
            }

        }

        /* end of training */


        /* boulder */

        private void SetStartBoulderRockTimerTickEventHandler(ulong bodyID)
        {
            RockTimerHelper startRockTimer = rocksOnRouteVM.StartRock.MyRockTimerHelper;

            EventHandler startRockTimerTickEventHandler = null;
            startRockTimerTickEventHandler = async (_sender, _e) =>
            {
                if (isBoulderTargetReachedFunc(rocksOnRouteVM.StartRock))
                {
                    startRockTimer.RockTimerCountIncr();

                    if (startRockTimer.IsTimerGoalReached())
                    {
                        //START ROCK REACHED VERIFIED
                        playerBodyID = bodyID;
                        //Debug.WriteLine("Player Tracking ID: " + playerBodyID);

                        //TODO: StartRock Feedback Animation
                        if (debug)
                        {
                            DebugRecolorRockVM(rocksOnRouteVM.StartRock);
                        }
                        else
                        {
                            rocksOnRouteVM.StartRock.SetAndPlayFeedbackImgSeq();
                        }

                        startRockTimer.Reset();
                        startRockTimer.RemoveTickEventHandler(startRockTimerTickEventHandler);
                        await OnGameplayStartAsync();

                        foreach (RockOnRouteViewModel interRock in interRocksOnBoulderRoute)
                        {
                            interRock.SetAndPlayActivePopAndShineImgSeq();
                        }
                    }
                }

                if (startRockTimer.IsLagThresholdExceeded())
                {
                    startRockTimer.Reset();
                }
            };//startRockTimerTickEventHandler = async (_sender, _e) =>

            startRockTimer.AddTickEventHandler(startRockTimerTickEventHandler);
        }

        private void SetEndBoulderRockTimerTickEventHandler()
        {
            RockTimerHelper endRockTimer = rocksOnRouteVM.EndRock.MyRockTimerHelper;

            EventHandler endRockTimerTickEventHandler = null;
            endRockTimerTickEventHandler = (_sender, _e) =>
            {
                if (isBoulderTargetReachedFunc(rocksOnRouteVM.EndRock))
                {
                    endRockTimer.RockTimerCountIncr();

                    if (endRockTimer.IsTimerGoalReached())
                    {
                        //TODO: EndRock Feedback Animation
                        if (debug)
                        {
                            DebugRecolorRockVM(rocksOnRouteVM.EndRock, Brushes.White);
                        }
                        else
                        {
                            rocksOnRouteVM.EndRock.SetAndPlayFeedbackShineLoopImgSeq();
                        }

                        OnGameplayFinish();
                        
                        //END ROCK REACHED VERIFIED
                        endRockTimer.Reset();
                        endRockTimer.RemoveTickEventHandler(endRockTimerTickEventHandler);

                        //DO SOMETHING WHEN ANY BOTH HANDS REACHED END ROCK
                        //Play "Count down to 3" video
                       
                    }
                }

                if (endRockTimer.IsLagThresholdExceeded())
                {
                    endRockTimer.Reset();
                    endRockTimer.RemoveTickEventHandler(endRockTimerTickEventHandler);

                    if (isEndCountDownVideoPlaying)
                    {
                        mainWindowClient.StopPlaygroundMedia();
                        isEndCountDownVideoPlaying = false;
                    }

                    if (debug)
                    {
                        DebugRecolorRockVM(rocksOnRouteVM.EndRock, Brushes.Red);
                    }
                }
            };//endRockTimerTickEventHandler = (_sender, _e) =>

            endRockTimer.AddTickEventHandler(endRockTimerTickEventHandler);
        }

        private void SetInterBoulderRockTimerTickEventHandler(RockOnRouteViewModel rockOnRoute)
        {
            RockTimerHelper interRockTimer = rockOnRoute.MyRockTimerHelper;

            EventHandler interRockTimerTickEventHandler = null;
            interRockTimerTickEventHandler = (_sender, _e) =>
            {
                if (isBoulderTargetReachedFunc(rockOnRoute))
                {
                    interRockTimer.RockTimerCountIncr();

                    if (interRockTimer.IsTimerGoalReached())
                    {
                        interRockTimer.Reset();

                        interRockTimer.RemoveTickEventHandler(interRockTimerTickEventHandler);
                        //TODO: animation Feedback for that rock
                        if (debug)
                        {
                            DebugRecolorRockVM(rockOnRoute);
                        }
                        else
                        {
                            rockOnRoute.SetAndPlayFeedbackImgSeq();
                        }
                    }

                    if (interRockTimer.IsLagThresholdExceeded())
                    {
                        interRockTimer.Reset();
                    }
                }
            };//interRockTimerTickEventHandler = (_sender, _e) =>

            interRockTimer.AddTickEventHandler(interRockTimerTickEventHandler);
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
                    //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel);
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel);
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    break;
                case RockOnBoulderStatus.End:
                    //reached = AreBothJointGroupsOnRock(LHandJoints, RHandJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    reached = IsJointGroupOnRock(fourLimbJoints, rockOnRouteVM.MyRockViewModel) && body.TrackingId == playerBodyID;
                    break;
            }

            return reached;
        }
     
        private void BoulderGameplay(Body body, IEnumerable<Joint> LHandJoints,
           IEnumerable<Joint> RHandJoints)
        {
            isBoulderTargetReachedFunc = (rockOnRouteVM) =>
            {
                return IsBoulderTargetReached(rockOnRouteVM, body, LHandJoints, RHandJoints);
            };

            if (!gameStarted) //Progress: Game not yet started, waiting player to reach Starting Point
            {
                if (isBoulderTargetReachedFunc(rocksOnRouteVM.StartRock))
                {
                    //DO SOMETHING WHEN RELEVANT JOINT(S) TOUCHES STARTING POINT
                    RockTimerHelper startRockTimer = rocksOnRouteVM.StartRock.MyRockTimerHelper;

                    if (!startRockTimer.IsTickHandlerSubed)
                    {
                        SetStartBoulderRockTimerTickEventHandler(body.TrackingId);
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
                //Progress: Game Started, waiting player to reach End Point
                //CHECK END ROCK REACHED ALL THE TIME
                if (isBoulderTargetReachedFunc(rocksOnRouteVM.EndRock))
                {
                    RockTimerHelper endRockTimer = rocksOnRouteVM.EndRock.MyRockTimerHelper;
                    if (!isEndCountDownVideoPlaying)
                    {
                        //Play "Count down to 3" video
                        mainWindowClient.ChangeSrcAndPlayInPlaygroundMedia(FileHelper.GameplayCountdownVideoPath());

                        endRockTimer = rocksOnRouteVM.EndRock.InitializeRockTimerHelper(EndRockHoldTimerGoal, EndRockHoldTimerLag);
                        isEndCountDownVideoPlaying = true;
                    }

                    if (!endRockTimer.IsTickHandlerSubed && gameStarted)
                    {
                        SetEndBoulderRockTimerTickEventHandler();
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
                        if (isBoulderTargetReachedFunc(rockOnRoute))
                        {
                            RockTimerHelper interRockTimer = rockOnRoute.MyRockTimerHelper;

                            if (!interRockTimer.IsTickHandlerSubed)
                            {
                                SetInterBoulderRockTimerTickEventHandler(rockOnRoute);
                            }
                            if (!interRockTimer.IsEnabled)
                            {
                                interRockTimer.Reset();
                                interRockTimer.Start();
                            }
                        }     
                    } //CLOSE foreach (RockOnRouteViewModel rockOnRoute in interRocksOnBoulderRoute) 
                }
            }
        }
        
        // end of boulder

        //private void SetEndRockHoldTimerTickEventHandler(Body body, RockTimerHelper endRockTimer, EventHandler endRockTimerTickHandler, Func<RockOnRouteViewModel, bool> isEndRockReached)
        //{
        //    EventHandler endRockHoldTimerTickEventHandler = null;
        //    endRockHoldTimerTickEventHandler = (_holdSender, _holdE) =>
        //    {
        //        if (isEndRockReached(rocksOnRouteVM.EndRock))
        //        {
        //            endRockHoldTimer.RockTimerCountIncr();
        //        }

        //        if (endRockHoldTimer.IsTimerGoalReached())
        //        {
        //            //TODO: Countdown finishes eventhough joint left target
        //            //END ROCK 3-second HOLD VERIFIED
        //            endRockHoldTimer.Reset();

        //            OnGameplayFinish();

        //            endRockTimer.RemoveTickEventHandler(endRockTimerTickHandler);
        //            endRockHoldTimer.RemoveTickEventHandler(endRockHoldTimerTickEventHandler);                    
                  

        //            //TODO: animation Feedback for that rock
        //        }

        //        if (endRockHoldTimer.IsLagThresholdExceeded())
        //        {
        //            playgroundMedia.Stop();
        //            endRockHoldTimer.Reset();
        //            endRockHoldTimer.RemoveTickEventHandler(endRockHoldTimerTickEventHandler);                    
        //        }
        //    };

        //    endRockHoldTimer.AddTickEventHandler(endRockHoldTimerTickEventHandler);
        //}

        #endregion


        #region determine overlap between Rock & Joint / gameover

        private static bool IsJointOnRock(Joint joint, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            float distance = KinectExtensions.GetCameraSpacePointDistance(joint.Position, rockVM.MyRock.GetCameraSpacePoint());

            //CameraSpacePoint startCamSp = startRockOnBoulderRoute.MyRockViewModel.MyRock.GetCameraSpacePoint();
            //Debug.WriteLine($"{{ {startCamSp.X},{startCamSp.Y},{startCamSp.Z} }}");

            return (distance < threshold);
        }

        private static bool IsJointGroupOnRock(IEnumerable<Joint> joints, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
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

        private static bool AreBothJointGroupsOnRock(IEnumerable<Joint> groupA, IEnumerable<Joint> groupB, RockViewModel rockVM, float threshold = DefaultDistanceThreshold)
        {
            return (IsJointGroupOnRock(groupA, rockVM, threshold) && IsJointGroupOnRock(groupB, rockVM, threshold));
        }

        private bool IsBodyGameOver(Body body, float wallThr = 1f, float floorThr = 1f)//Default thresholds are in Meters
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

        private async Task ForgoVideoRecordingAndClearBufferAsync()
        {
            await gameplayVideoRecClient.StopRecordingIfIsRecordingAndClearBufferWithoutExportVideoAsync();
            UiHelper.NotifyUser("Video Recording Stopped and Cleared.");

            //navHead.IsRecordDemoVideo setter already checks current state before changing value
            // this would change this.isRecordingDemo as well
            // as isRecordingDemo is bound to navHead.IsRecordDemoVideo
            // via event handler OnNavHeadIsRecordDemoChanged
            navHead.IsRecordDemoVideo = false;
        }

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

            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog();
            VideoPlayback videoPlaybackPage =
                new VideoPlayback(videoFilePath, mainWindowClient);

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
