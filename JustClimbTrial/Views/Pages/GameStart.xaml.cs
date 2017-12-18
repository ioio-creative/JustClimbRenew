using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;
using JustClimbTrial.Kinect;
using static JustClimbTrial.Kinect.KinectManager;
using Microsoft.Kinect;
using JustClimbTrial.Views.Windows;
using System.Windows.Media;
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

        private KinectManager kinectManagerClient;
        private Playground playgroundWindow;
        private Canvas playgroundCanvas;

        private IEnumerable<Shape> skeletonShapes;


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
            playgroundCanvas = playgroundWindow.playgroundCanvas;
            //kinectManagerClient.ColorImageSourceArrived -= (this.Parent as MainWindow).HandleColorImageSourceArrived;
            //playgroundCanvas.Background = Brushes.Black;
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
                    startRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);
                    endRockOnBoulderRoute = rocksOnBoulderRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);

                    foreach (RockOnRouteViewModel rockOnRoute in rocksOnBoulderRoute)
                    {
                        rockOnRoute.MyRockViewModel.DrawBoulder();
                    }
                    break;

            }
        }

        private void btnDemo_Click(object sender, RoutedEventArgs e)
        {
            //RouteVideoViewModel model = dgridRouteVideos.SelectedItem as RouteVideoViewModel;
            //string abx = FileHelper.VideoFullPath(model);
        }

        private void btnPlaySelectedVideo_Click(object sender, RoutedEventArgs e)
        {
            MediaElement playgroundMonitor = (this.Parent as MainWindow).PlaygroundWindow.playgroundPlayback;
            VideoPlaybackDialog videoPlaybackDialog = new VideoPlaybackDialog(playgroundMonitor);
            videoPlaybackDialog.ShowDialog();
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

        #endregion        
    }
}
