using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.Interfaces;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.Dialogs;
using JustClimbTrial.Views.UserControls;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for RouteSet.xaml
    /// </summary>
    public partial class RouteSet : Page, ISavingVideo
    {
        private readonly bool debug = AppGlobal.DEBUG;
        
        #region resource keys

        private const string TrainingRockStatusTemplateResourceKey = "trainingRockStatusTemplate";
        private const string BoulderRockStatusTemplateResourceKey = "boulderRockStatusTemplate";

        private const string BtnRecordDemoTemplateResourceKey = "btnRecordDemoTemplate";
        private const string BtnDemoDoneTemplateResourceKey = "btnDemoDoneTemplate";

        #endregion        


        #region private members

        private MainWindow parentMainWindow;
        private int newRouteNo;
        private ClimbMode routeSetClimbMode;
        private RouteSetViewModel routeSetViewModel;
        private RocksOnWallViewModel rocksOnWallViewModel;
        private RocksOnRouteViewModel rocksOnRouteViewModel;

        #endregion


        #region public members

        public string TmpVideoFilePath { get; set; }
        public bool IsConfirmSaveVideo { get; set; }
        
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
            string headerRowTitleFormat = "Route Set - {0} {1}";
            string rockStatusTemplateResourceKey;            

            switch (aClimbMode)
            {
                case ClimbMode.Training:
                    newRouteNo = TrainingRouteDataAccess.LargestTrainingRouteNoByWall(AppGlobal.WallID) + 1;
                    Title = string.Format(titleFormat, "Training");
                    navHead.HeaderRowTitle =
                        string.Format(headerRowTitleFormat, "Training", newRouteNo);
                    rockStatusTemplateResourceKey = TrainingRockStatusTemplateResourceKey;
                    break;
                case ClimbMode.Boulder:
                default:
                    newRouteNo = BoulderRouteDataAccess.LargestBoulderRouteNoByWall(AppGlobal.WallID) + 1;
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

            // !!! Important !!!
            // somehow if the following is called in Page_Loaded event handler,
            // the drawn rocks will be "covered" by the wall log image
            // hence, it is called here
            RouteSetImg.SetSourceByPath(FileHelper.WallLogImagePath(AppGlobal.WallID));
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


        #region ISavingVideo interfaces

        public void DeleteTmpVideoFileSafe()
        {
            FileHelperDLL.FileHelper.DeleteFileSafe(TmpVideoFilePath);
        }

        public void ResetSavingVideoProperties()
        {
            TmpVideoFilePath = null;
            IsConfirmSaveVideo = false;
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            parentMainWindow = this.Parent as MainWindow;

            routeSetViewModel.LoadData();

            rocksOnWallViewModel = new RocksOnWallViewModel(canvasWall, parentMainWindow.KinectManagerClient.ManagerCoorMapper);
            bool isAnyRocksOnWall = rocksOnWallViewModel.
                LoadAndDrawRocksOnWall(AppGlobal.WallID);
            rocksOnRouteViewModel = new RocksOnRouteViewModel(canvasWall);

            SetUpBtnCommandsInRockStatusUserControls();            
            
            if (!isAnyRocksOnWall)
            {
                UiHelper.NotifyUser("No rocks registered with the wall!");
            }        
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DeleteTmpVideoFileSafe();
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
                            SetSelectedTrainingRockSeqNo();
                            break;
                        case ClimbMode.Boulder:
                        default:
                            SetSelectedBoulderRockToIntermediate();
                            break;
                    }
                } 
            }
        }

        private void btnRecordDemo_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = ValidateRouteParams();

            if (!string.IsNullOrEmpty(errMsg))
            {
                UiHelper.NotifyUser(errMsg);
            }
            else
            {
                ResetSavingVideoProperties();

                SetTemplateOfControlFromResource(ctrlBtnDemo, BtnDemoDoneTemplateResourceKey);

                // show video record dialog
                VideoPlaybackDialog videoRecordDialog = new VideoPlaybackDialog(parentMainWindow.PlaygroundMedia);
                VideoRecord videoRecordPage = new VideoRecord(routeSetClimbMode,
                    VideoRecordType.IsDemo, parentMainWindow.KinectManagerClient,
                    parentMainWindow.PlaygroundMedia);

                videoRecordDialog.Navigate(videoRecordPage);
                videoRecordDialog.ShowDialog();

                if (videoRecordDialog.IsConfirmSaveVideo)
                {
                    IsConfirmSaveVideo = true;
                    TmpVideoFilePath = videoRecordDialog.TmpVideoFilePath;
                }
            }            
        }

        private void btnDemoDone_Click(object sender, RoutedEventArgs e)
        {
            string errMsg = ValidateRouteParams();
            string routeVideoRecordedFilePath = null;
            
            if (!string.IsNullOrEmpty(errMsg))
            {
                UiHelper.NotifyUser(errMsg);
            }
            else
            {
                switch (routeSetClimbMode)
                {
                    case ClimbMode.Training:
                        errMsg = rocksOnRouteViewModel.ValidateRocksOnTrainingRoute();
                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            UiHelper.NotifyUser(errMsg);
                        }
                        else
                        {
                            TrainingRoute newTrainingRoute = CreateTrainingRouteFromUi();
                            rocksOnRouteViewModel.SaveRocksOnTrainingRoute(newTrainingRoute);

                            if (IsConfirmSaveVideo)
                            {
                                TrainingRouteVideo newTrainingRouteVideo = new TrainingRouteVideo()
                                {
                                    Route = newTrainingRoute.RouteID,
                                    IsDemo = true
                                };
                                TrainingRouteVideoDataAccess.Insert(newTrainingRouteVideo, true);
                                routeVideoRecordedFilePath =
                                    FileHelper.TrainingRouteVideoRecordedFullPath(newTrainingRoute,
                                        newTrainingRouteVideo);
                                FileHelperDLL.FileHelper.MoveAndRenameFile(TmpVideoFilePath, routeVideoRecordedFilePath);
                            }
                        }                            
                        break;
                    case ClimbMode.Boulder:
                    default:
                        errMsg = rocksOnRouteViewModel.ValidateRocksOnBoulderRoute();
                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            UiHelper.NotifyUser(errMsg);
                        }
                        else
                        {
                            BoulderRoute newBoulderRoute = CreateBoulderRouteFromUi();
                            rocksOnRouteViewModel.SaveRocksOnBoulderRoute(newBoulderRoute);

                            if (IsConfirmSaveVideo)
                            {
                                BoulderRouteVideo newBoulderRouteVideo = new BoulderRouteVideo()
                                {
                                    Route = newBoulderRoute.RouteID,
                                    IsDemo = true
                                };
                                BoulderRouteVideoDataAccess.Insert(newBoulderRouteVideo, true);
                                routeVideoRecordedFilePath =
                                    FileHelper.BoulderRouteVideoRecordedFullPath(newBoulderRoute,
                                        newBoulderRouteVideo);
                                FileHelperDLL.FileHelper.MoveAndRenameFile(TmpVideoFilePath, routeVideoRecordedFilePath);
                            }
                        }
                        break;
                }                
            }

            SetTemplateOfControlFromResource(ctrlBtnDemo, BtnRecordDemoTemplateResourceKey);
        }

        #endregion


        #region command methods for TrainingRockStatus UserControl

        private void SetSelectedTrainingRockSeqNo()
        {
            rocksOnRouteViewModel.SetSelectedTrainingRockSeqNo();
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
