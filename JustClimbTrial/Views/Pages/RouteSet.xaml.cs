using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.UserControls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for RouteSet.xaml
    /// </summary>
    public partial class RouteSet : Page
    {
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }

        #region resource keys

        private const string TrainingRockStatusTemplateResourceKey = "trainingRockStatusTemplate";
        private const string BoulderRockStatusTemplateResourceKey = "boulderRockStatusTemplate";

        //private const string BtnRecordDemoTemplateResourceKey = "btnRecordDemoTemplate";
        //private const string BtnDemoDoneTemplateResourceKey = "btnDemoDoneTemplate";

        #endregion        


        #region private members

        private MainWindow mainWindowClient;
        private int newRouteNo;
        private ClimbMode routeSetClimbMode;
        private RouteSetViewModel routeSetViewModel;
        private RocksOnWallViewModel rocksOnWallViewModel;
        private RocksOnRouteViewModel rocksOnRouteViewModel;

        #endregion


        public RouteSet() : this(ClimbMode.Training) { }

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
            Title = string.Format(titleFormat, 
                ClimbModeGlobals.StringDict[aClimbMode]);
            newRouteNo = 
                ClimbModeGlobals.LargestRouteNoByWallFuncDict[aClimbMode](AppGlobal.WallID) + 1;

            string rockStatusTemplateResourceKey;            
            switch (aClimbMode)
            {
                case ClimbMode.Training:                                        
                    rockStatusTemplateResourceKey = TrainingRockStatusTemplateResourceKey;
                    break;
                case ClimbMode.Boulder:
                default:                    
                    rockStatusTemplateResourceKey = BoulderRockStatusTemplateResourceKey;
                    break;                                
            }

            WindowTitle = Title;
            //ControlTemplateHelper.SetTemplateOfControlFromResource(ctrlBtnDemo, this,
                //BtnRecordDemoTemplateResourceKey);
            ControlTemplateHelper.SetTemplateOfControlFromResource(ctrlRockStatus, this,
                rockStatusTemplateResourceKey);                        

            // !!! Important !!!
            // somehow if the following is called in Page_Loaded event handler,
            // the drawn rocks will be "covered" by the wall log image
            // hence, it is called here
            RouteSetImg.SetSourceByPath(FileHelper.WallLogImagePath(AppGlobal.WallID));
        }


        #region initialization

        private void InitializeNavHead()
        {
            HeaderRowNavigation navHead = master.NavHead;
            string headerRowTitleFormat = "Route Set - {0} {1}";
            navHead.HeaderRowTitle = string.Format(headerRowTitleFormat,
                ClimbModeGlobals.StringDict[routeSetClimbMode], newRouteNo);
            navHead.ParentPage = this;
        }

        // InitializeSaveRouteCommands() has to be called after initializing rocksOnWallViewModel
        private void InitializeSaveRouteCommands()
        {
            Func<bool> validateRoute;

            switch (routeSetClimbMode)
            {
                case ClimbMode.Training:
                    validateRoute = () => 
                        string.IsNullOrEmpty(rocksOnRouteViewModel.ValidateRocksOnTrainingRoute());
                    break;
                case ClimbMode.Boulder:
                default:
                    validateRoute = () =>
                        string.IsNullOrEmpty(rocksOnRouteViewModel.ValidateRocksOnBoulderRoute());
                    break;
            }

            Predicate<object> CanSaveRoute = x => 
                string.IsNullOrEmpty(ValidateRouteParams()) && validateRoute();

            btnConfirmRouteSet.Command = new RelayCommand(SaveRoute, CanSaveRoute);
            btnCancelRouteSet.Command = new RelayCommand(CancelSaveRoute, CanCancelSaveRoute);
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
                    TrainingRockStatus ucTrainingRockStatus = GetTrainingRockStatusUserControl();
                    ucTrainingRockStatus.btnTrainingSeqGoBack.Command = 
                        new RelayCommand(UndoLastTrainingRock, CanUndoLastSelectedTrainingRock);
                    break;
                case ClimbMode.Boulder:
                default:
                    BoulderRockStatus ucBoulderRockStatus = GetBoulderRockStatusUserControl();
                    ucBoulderRockStatus.btnStartStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToStart, CanSetSelectedBoulderRockToStart);
                    ucBoulderRockStatus.btnEndStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToEnd, CanSetSelectedBoulderRockToEnd);
                    ucBoulderRockStatus.btnIntermediateStatus.Command =
                        new RelayCommand(SetSelectedBoulderRockToIntermediate, CanSetSelectedBoulderRockToIntermediate);
                    ucBoulderRockStatus.btnNoneStatus.Command =
                        new RelayCommand(RemoveSelectedBoulderRockFromRoute, CanRemoveSelectedBoulderRockFromRoute);
                    break;
            }
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeNavHead();

            mainWindowClient = this.Parent as MainWindow;

            if (mainWindowClient.KinectManagerClient.OpenKinect())
            {
                routeSetViewModel.LoadData();

                rocksOnWallViewModel = new RocksOnWallViewModel(canvasWall, mainWindowClient.KinectManagerClient.ManagerCoorMapper);
                bool isAnyRocksOnWall = rocksOnWallViewModel.
                    LoadAndDrawRocksOnWall(AppGlobal.WallID);
                rocksOnRouteViewModel = new RocksOnRouteViewModel(canvasWall);

                // InitializeSaveRouteCommands() has to be called after initializing rocksOnWallViewModel
                InitializeSaveRouteCommands();
                SetUpBtnCommandsInRockStatusUserControls();

                if (!isAnyRocksOnWall)
                {
                    UiHelper.NotifyUser("No rocks registered with the wall!");
                }

                if (debug)
                {
                    mainWindowClient.SubscribeColorImgSrcToPlaygrd();
                }
                mainWindowClient.DebugModeChanged += HandleDebugModeChanged;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (debug)
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd();
            }
            mainWindowClient.DebugModeChanged -= HandleDebugModeChanged;
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

                if (!isRockAlreadyOnTheRoute)  // new rock on route
                {
                    rocksOnRouteViewModel.SelectedRockOnRoute = new RockOnRouteViewModel
                    {
                        MyRockViewModel = nearestRockOnWall
                    };

                    rocksOnRouteViewModel.AddSelectedRockToRoute();

                    switch (routeSetClimbMode)
                    {
                        case ClimbMode.Training:
                            SetSelectedTrainingRockSeqNo(true);
                            break;
                        case ClimbMode.Boulder:
                        default:
                            SetSelectedBoulderRockToIntermediate();
                            break;
                    }
                } 
            }
        }

        private void HandleDebugModeChanged(bool _debug)
        {
            if (_debug)
            {
                mainWindowClient.SubscribeColorImgSrcToPlaygrd();
            }
            else
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd();
            }
        }

        #endregion


        #region command methods

        private bool CanCancelSaveRoute(object parameter = null)
        {
            return true;
        }

        private void SaveRoute(object parameter = null)
        {
            string errMsg = ValidateRouteParams();

            try
            {
                if (!string.IsNullOrEmpty(errMsg))
                {
                    throw new Exception(errMsg);
                }

                switch (routeSetClimbMode)
                {
                    case ClimbMode.Training:
                        errMsg = rocksOnRouteViewModel.ValidateRocksOnTrainingRoute();
                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            throw new Exception(errMsg);
                        }
                        
                        TrainingRoute newTrainingRoute = CreateTrainingRouteFromUi();
                        rocksOnRouteViewModel.SaveRocksOnTrainingRoute(newTrainingRoute);                        
                        break;
                    case ClimbMode.Boulder:
                    default:
                        errMsg = rocksOnRouteViewModel.ValidateRocksOnBoulderRoute();
                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            throw new Exception(errMsg);
                        }
                        
                        BoulderRoute newBoulderRoute = CreateBoulderRouteFromUi();
                        rocksOnRouteViewModel.SaveRocksOnBoulderRoute(newBoulderRoute);                        
                        break;
                }

                // Switch to Routes page
                Routes routesPage = new Routes(routeSetClimbMode);
                this.NavigationService.Navigate(routesPage);
            }
            catch (Exception ex)
            {
                UiHelper.NotifyUser("Error when saving the route:" + Environment.NewLine +
                    ex.ToString());
            }
        }

        private void CancelSaveRoute(object parameter = null)
        {
            // Switch to ModeSelect page
            ModeSelect modeSelectPage = new ModeSelect();
            this.NavigationService.Navigate(modeSelectPage);
        }

        #endregion


        #region command methods for TrainingRockStatus UserControl

        private void SetSelectedTrainingRockSeqNo(bool mirrorSeqNo = false)
        {
            rocksOnRouteViewModel.SetSelectedTrainingRockSeqNo(mirrorSeqNo);
        }

        private bool CanUndoLastSelectedTrainingRock(object parameter = null)
        {
            return !rocksOnRouteViewModel.IsSelectedRockOnRouteNull() &&
                rocksOnRouteViewModel.IsRockOnTheRoute(rocksOnRouteViewModel.SelectedRockOnRoute.MyRockViewModel);
        }

        private void UndoLastTrainingRock(object parameter = null)
        {
            rocksOnRouteViewModel.UndoLastTrainingRock();
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


        #region validations

        private string ValidateRouteParams()
        {
            string errMsg = null;

            string selectedAgeGroupId = (ddlAge.SelectedItem as AgeGroup).AgeGroupID;
            if (!routeSetViewModel.AgeGroups.Where(x => x.AgeGroupID == selectedAgeGroupId).Any())
            {
                errMsg = "Age group selected is not valid!";
                return errMsg;
            }

            string selectedDifficultyId = (ddlDifficulty.SelectedItem as RouteDifficulty).RouteDifficultyID;
            if (!routeSetViewModel.RouteDifficulties.Where(x => x.RouteDifficultyID == selectedDifficultyId).Any())
            {
                errMsg = "Difficulty selected is not valid!";
                return errMsg;
            }

            if (!rocksOnRouteViewModel.AnyRocksInRoute())
            {
                errMsg = "Please add some rocks to the route!";
                return errMsg;
            }

            return errMsg;
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
