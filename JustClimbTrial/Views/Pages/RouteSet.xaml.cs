using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.UserControls;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for RouteSet.xaml
    /// </summary>
    public partial class RouteSet : Page
    {
        #region resource keys

        private const string TrainingRockStatusTemplateResourceKey = "trainingRockStatusTemplate";
        private const string BoulderRockStatusTemplateResourceKey = "boulderRockStatusTemplate";

        private const string BtnRecordDemoTemplateResourceKey = "btnRecordDemoTemplate";
        private const string BtnDemoDoneTemplateResourceKey = "btnDemoDoneTemplate";

        #endregion        


        #region private members

        private int newRouteNo;
        private ClimbMode routeSetClimbMode;
        private RouteSetViewModel routeSetViewModel;
        private RocksOnWallViewModel rocksOnWallViewModel;
        private RocksOnRouteViewModel rocksOnRouteViewModel;

        // declare Kinect object and frame reader
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader mulSourceReader;

        #endregion


        public RouteSet() : this(ClimbMode.Boulder) { }

        public RouteSet(ClimbMode aClimbMode)
        {
            routeSetClimbMode = aClimbMode;

            InitializeComponent();

            routeSetViewModel = DataContext as RouteSetViewModel;
            if (routeSetViewModel != null)
            {
                routeSetViewModel.SetClimbMode(aClimbMode);
            }

            // set titles
            string titleFormat = "Just Climb - {0} Route Set";
            string headerRowTitleFormat = "Route Set - {0} {1}";
            string rockStatusTemplateResourceKey;            

            switch (aClimbMode)
            {
                case ClimbMode.Training:
                    newRouteNo = TrainingRouteDataAccess.LargestTrainingRouteNo + 1;
                    Title = string.Format(titleFormat, "Training");
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Training", newRouteNo);
                    rockStatusTemplateResourceKey = TrainingRockStatusTemplateResourceKey;
                    break;
                case ClimbMode.Boulder:
                default:
                    newRouteNo = BoulderRouteDataAccess.LargestBoulderRouteNo + 1;
                    Title = string.Format(titleFormat, "Boulder");
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Boulder", newRouteNo);                            
                    rockStatusTemplateResourceKey = BoulderRockStatusTemplateResourceKey;
                    break;                                    
            }

            WindowTitle = Title;            
            SetTemplateOfControlFromResource(ctrlBtnDemo, BtnRecordDemoTemplateResourceKey);
            SetTemplateOfControlFromResource(ctrlRockStatus, rockStatusTemplateResourceKey);            

            navHead.ParentPage = this;                              
        }

        // !!! Important !!!
        // don't call this method in page's constructor
        // call it in the page_load event
        // https://stackoverflow.com/questions/21482291/access-textbox-from-controltemplate-in-usercontrol-resources
        private void SetUpBtnCommandsInRockStatusUserControls()
        {
            switch (routeSetClimbMode)
            {
                case ClimbMode.Training:
                    break;
                case ClimbMode.Boulder:
                default:
                    BoulderRockStatus ucboulderRockStatus = GetBoulderRockStatusUserControl();
                    ucboulderRockStatus.btnStartStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToStart, CanSetSelectedBoulderRockToStart);
                    ucboulderRockStatus.btnEndStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToEnd, CanSetSelectedBoulderRockToEnd);
                    ucboulderRockStatus.btnIntermediateStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToIntermediate, CanSetSelectedBoulderRockToIntermediate);
                    ucboulderRockStatus.btnNoneStatus.Command =
                        new RelayCommand(RemoveSelectedBoulderRockFromRoute, CanRemoveSelectedBoulderRockFromRoute);
                    break;
            }
        }


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            routeSetViewModel.LoadData();

            rocksOnWallViewModel = new RocksOnWallViewModel(canvasWall, kinectSensor.CoordinateMapper);
            bool isAnyRocksOnWall = rocksOnWallViewModel.
                LoadAndDrawRocksOnWall(AppGlobal.WallID);

            rocksOnRouteViewModel = new RocksOnRouteViewModel(canvasWall);

            SetUpBtnCommandsInRockStatusUserControls();

            if (!isAnyRocksOnWall)
            {
                UiHelper.NotifyUser("No rocks registered with the wall!");
            }            
        }

        private void canvasWall_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(sender as Canvas);
            RockViewModel nearestRockOnWall = rocksOnWallViewModel.GetRockInListByCanvasPoint(mousePoint);
            if (nearestRockOnWall != null)
            {
                rocksOnRouteViewModel.SelectedRockOnRoute = 
                    rocksOnRouteViewModel.FindRockOnRouteViewModel(nearestRockOnWall);
                bool isRockAlreadyOnTheRoute = !rocksOnRouteViewModel.IsSelectedRockOnRouteNull();

                if (routeSetClimbMode == ClimbMode.Training)
                {

                }
                else if (routeSetClimbMode == ClimbMode.Boulder)
                {
                    if (!isRockAlreadyOnTheRoute)  // new rock on route
                    {
                        rocksOnRouteViewModel.SelectedRockOnRoute = new RockOnRouteViewModel
                        {
                            MyRockViewModel = nearestRockOnWall
                        };

                        rocksOnRouteViewModel.AddSelectedRockToRoute();                            
                        
                        SetSelectedBoulderRockToIntermediate();
                    }
                }                
            }
        }

        private void btnRecordDemo_Click(object sender, RoutedEventArgs e)
        {
            SetTemplateOfControlFromResource(ctrlBtnDemo, BtnDemoDoneTemplateResourceKey);
        }

        private void btnDemoDone_Click(object sender, RoutedEventArgs e)
        {
            SetTemplateOfControlFromResource(ctrlBtnDemo, BtnRecordDemoTemplateResourceKey);
            
            if (routeSetClimbMode == ClimbMode.Boulder)
            {
                if (rocksOnRouteViewModel.AnyRocksInRoute())
                {
                    BoulderRoute newBoulderRoute = CreateBoulderRouteFromUi();
                    rocksOnRouteViewModel.SaveRocksOnBoulderRoute(newBoulderRoute);
                }
            }
        }

        #endregion


        #region command methods for BoulderRockStatus UserControl

        private bool CanSetSelectedBoulderRockToStart(object parameter = null)
        {            
            return !rocksOnRouteViewModel.IsSelectedRockOnRouteNull();
        }

        private bool CanSetSelectedBoulderRockToIntermediate(object parameter = null)
        {            
            return !rocksOnRouteViewModel.IsSelectedRockOnRouteNull();
        }

        private bool CanSetSelectedBoulderRockToEnd(object parameter = null)
        {
            return !rocksOnRouteViewModel.IsSelectedRockOnRouteNull();
        }

        private bool CanRemoveSelectedBoulderRockFromRoute(object parameter = null)
        {            
            return !rocksOnRouteViewModel.IsSelectedRockOnRouteNull() &&
                rocksOnRouteViewModel.IsRockOnTheRoute(rocksOnRouteViewModel.SelectedRockOnRoute.MyRockViewModel);
        }

        private void SetSelectedBoulderRockToStart(object parameter = null)
        {
            SetSelectedBoulderRockStatus(RockOnBoulderStatus.Start);
        }

        private void SetSelectedBoulderRockToIntermediate(object parameter = null)
        {
            SetSelectedBoulderRockStatus(RockOnBoulderStatus.Int);
        }

        private void SetSelectedBoulderRockToEnd(object parameter = null)
        {
            SetSelectedBoulderRockStatus(RockOnBoulderStatus.End);
        }

        private void SetSelectedBoulderRockStatus(RockOnBoulderStatus status)
        {
            rocksOnRouteViewModel.SetSelectedBoulderRockStatus(status);
        }

        private void RemoveSelectedBoulderRockFromRoute(object parameter = null)
        {
            rocksOnRouteViewModel.RemoveSelectedRockFromRoute();
        }

        #endregion


        #region control template helpers

        private void SetTemplateOfControlFromResource(Control ctrl, string resourceKey)
        {
            ctrl.Template = GetControlTemplateFromResource(resourceKey);
        }

        private ControlTemplate GetControlTemplateFromResource(string resourceKey)
        {
            return Resources[resourceKey] as ControlTemplate;
        }

        // https://stackoverflow.com/questions/8126700/how-do-i-access-an-element-of-a-control-template-from-within-code-behind
        private BoulderRockStatus GetBoulderRockStatusUserControl()
        {
            ControlTemplate template = ctrlRockStatus.Template;            
            return template.FindName("ucBoulderRockStatus", ctrlRockStatus) as BoulderRockStatus;
        }

        private TrainingRockStatus GetTrainingRockStatusUserControl()
        {
            ControlTemplate template = ctrlRockStatus.Template;
            return template.FindName("ucTrainingRockStatus", ctrlRockStatus) as TrainingRockStatus;
        }

        #endregion                


        #region retrieve data from UI helpers

        private class RouteFromUiModel
        {
            public string AgeGroup { get; set; }
            public string Difficulty { get; set; }
            public string RouteNo { get; set; }
            public string Wall { get; set; }
        }

        private RouteFromUiModel CreateRouteFromUiModel()
        {
            return new RouteFromUiModel
            {
                AgeGroup = (ddlAge.SelectedItem as AgeGroup).AgeGroupID,
                Difficulty = (ddlDifficulty.SelectedItem as RouteDifficulty).RouteDifficultyID,
                RouteNo = newRouteNo.ToString(),
                Wall = AppGlobal.WallID
            };
        }

        private BoulderRoute CreateBoulderRouteFromUi()
        {
            RouteFromUiModel routeModel = CreateRouteFromUiModel();
            return new BoulderRoute
            {
                AgeGroup = routeModel.AgeGroup,
                Difficulty = routeModel.Difficulty,
                RouteNo = routeModel.RouteNo,
                Wall = routeModel.Wall
            };
        }

        private TrainingRoute CreateTrainingRouteFromUi()
        {
            RouteFromUiModel routeModel = CreateRouteFromUiModel();
            return new TrainingRoute
            {
                AgeGroup = routeModel.AgeGroup,
                Difficulty = routeModel.Difficulty,
                RouteNo = routeModel.RouteNo,
                Wall = routeModel.Wall
            };
        }

        #endregion
    }
}
