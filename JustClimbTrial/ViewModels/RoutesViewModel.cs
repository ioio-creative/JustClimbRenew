using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using JustClimbTrial.Mvvm.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace JustClimbTrial.ViewModels
{
    // Multi-filtered WPF DataGrid with MVVM
    // https://www.codeproject.com/Articles/442498/Multi-filtered-WPF-DataGrid-with-MVVM
    public class RoutesViewModel : ViewModelBase
    {
        #region private members        

        /// <summary>
        /// Gets or sets the CollectionViewSource which is the proxy for the 
        /// collection of RouteViewModels and the datagrid in which each RouteViewModel is displayed.
        /// </summary>
        private CollectionViewSource _cvsRoutesFromView;

        private AgeGroup _ageGroupListFirstItem;
        private RouteDifficulty _difficultyListFirstItem;

        private GeneralFilter _ageGroupFilter;
        private GeneralFilter _difficultyFilter;

        private ClimbMode _climbMode;

        /// <summary>
        /// Allows setting of _cvsRoutesFromView from the View's constructor in (.xaml.cs)
        /// </summary>
        /// <param name="cvsFromView"></param>
        public void SetCvsRoutes(CollectionViewSource cvsFromView)
        {
            _cvsRoutesFromView = cvsFromView;
            _ageGroupFilter = new GeneralFilter(cvsFromView, FilterByAgeGroup);
            _difficultyFilter = new GeneralFilter(cvsFromView, FilterByDifficulty);
        }
        
        public void SetAgeGroupListFirstItem(AgeGroup ageModel)
        {
            _ageGroupListFirstItem = ageModel;
        }

        public void SetDifficultyListFirstItem(RouteDifficulty difficultyModel)
        {
            _difficultyListFirstItem = difficultyModel;
        }

        public void SetClimbMode(ClimbMode climbMode)
        {
            _climbMode = climbMode;
        }

        #endregion


        #region constructor

        public RoutesViewModel()
        {            
            InitializeCommands();
        }

        #endregion


        #region filter properties

        /// <summary>
        /// Gets or sets the selected AgeGroup in the list AgeGroups to filter the collection
        /// </summary>
        public AgeGroup SelectedAgeGroup
        {
            get { return GetValue(() => SelectedAgeGroup); }
            set
            {
                SetValue(() => SelectedAgeGroup, value);

                if (value == null || String.IsNullOrEmpty(value.AgeGroupID))
                {
                    _ageGroupFilter.RemoveFilter();
                }
                else
                {
                    _ageGroupFilter.AddFilter();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected RouteDifficulty in the list RouteDifficulties to filter the collection
        /// </summary>
        public RouteDifficulty SelectedDifficulty
        {
            get { return GetValue(() => SelectedDifficulty); }
            set
            {
                SetValue(() => SelectedDifficulty, value);

                if (value == null || String.IsNullOrEmpty(value.RouteDifficultyID))
                {
                    _difficultyFilter.RemoveFilter();
                }
                else
                {
                    _difficultyFilter.AddFilter();
                }
            }
        }

        #endregion


        #region collections from database

        /// <summary>
        /// Gets or sets a list of AgeGroup which is used to populate the AgeGroup filter
        /// drop down list.
        /// </summary>
        public ObservableCollection<AgeGroup> AgeGroups
        {
            get { return GetValue(() => AgeGroups); }
            set { SetValue(() => AgeGroups, value); }            
        }

        /// <summary>
        /// Gets or sets a list of RouteDifficulty which is used to populate the RouteDifficulty filter
        /// drop down list.
        /// </summary>
        public ObservableCollection<RouteDifficulty> RouteDifficulties
        {
            get { return GetValue(() => RouteDifficulties); }
            set { SetValue(() => RouteDifficulties, value);  }
        }

        /// <summary>
        /// Gets or sets the primary collection of RouteViewModel objects to be displayed       
        /// </summary>
        public ObservableCollection<RouteViewModel> RouteViewModels
        {
            get { return GetValue(() => RouteViewModels); }
            set { SetValue(() => RouteViewModels, value); }            
        }

        // can be called by the view during Page.OnLoad
        public void LoadData()
        {
            string wallId = AppGlobal.WallID;            

            /* use
             * routeDA.ValidBoulderRoutes,  <-- only this uses "valid"
             * difficultyDA.RouteDifficulties
             * ageGroupDA.AgeGroups             
             */
            IEnumerable<RouteViewModel> routeViewModels;
            switch (_climbMode)
            {
                case ClimbMode.Training:
                    routeViewModels = from route in TrainingRouteDataAccess.ValidTrainingRoutesByWall(wallId)
                                      join difficulty in RouteDifficultyDataAccess.RouteDifficulties on route.Difficulty equals difficulty.RouteDifficultyID
                                      join ageGroup in AgeGroupDataAccess.AgeGroups on route.AgeGroup equals ageGroup.AgeGroupID
                                      select new RouteViewModel
                                      {
                                          RouteID = route.RouteID,
                                          RouteNo = route.RouteNo,
                                          Difficulty = route.Difficulty,
                                          DifficultyDesc = difficulty.DifficultyDesc,
                                          AgeGroup = route.AgeGroup,
                                          AgeDesc = ageGroup.AgeDesc
                                      };
                    break;
                case ClimbMode.Boulder:
                default:                    
                    routeViewModels =
                        from route in BoulderRouteDataAccess.ValidBoulderRoutesByWall(wallId)
                        join difficulty in RouteDifficultyDataAccess.RouteDifficulties on route.Difficulty equals difficulty.RouteDifficultyID
                        join ageGroup in AgeGroupDataAccess.AgeGroups on route.AgeGroup equals ageGroup.AgeGroupID
                        select new RouteViewModel
                        {
                            RouteID = route.RouteID,
                            RouteNo = route.RouteNo,
                            Difficulty = route.Difficulty,
                            DifficultyDesc = difficulty.DifficultyDesc,
                            AgeGroup = route.AgeGroup,
                            AgeDesc = ageGroup.AgeDesc
                        };
                    break;                
            }            
            
            // add null item at the front of the combo boxes
            /* use "valid"            
             * difficultyDA.ValidRouteDifficulties
             * ageGroupDA.ValidAgeGroups             
             */
            List<AgeGroup> ageGroupList = AgeGroupDataAccess.ValidAgeGroups.ToList();
            List<RouteDifficulty> difficultyList = RouteDifficultyDataAccess.ValidRouteDifficulties.ToList();

            if (_ageGroupListFirstItem != null)
            {
                ageGroupList.Insert(0, _ageGroupListFirstItem);
            }

            if (_difficultyListFirstItem != null)
            {
                difficultyList.Insert(0, _difficultyListFirstItem);
            }

            AgeGroups = new ObservableCollection<AgeGroup>(ageGroupList);
            RouteDifficulties = new ObservableCollection<RouteDifficulty>(difficultyList);
            RouteViewModels = new ObservableCollection<RouteViewModel>(routeViewModels);
        }

        #endregion


        #region Commands

        public ICommand ResetFiltersCommand { get; private set; }        

        private void InitializeCommands()
        {
            ResetFiltersCommand = new RelayCommand(ResetFilters, x => CanResetFilters);         
        }

        #endregion


        #region CanExecute of commands
        
        public bool CanResetFilters
        {
            get { return true; }
        }

        #endregion


        #region Command methods (called by the commands)

        // the not used parameter is kept here to satisfy 
        // the interface of RelayCommand
        public void ResetFilters(object parameter = null)
        {
            // clear filters
            if (CanResetFilters)
            {
                GeneralFilter[] filters = new GeneralFilter[] 
                {
                    _ageGroupFilter, _difficultyFilter
                };

                foreach (GeneralFilter filter in filters)
                {
                    filter.RemoveFilter();
                }
            }
        }       

        #endregion


        #region filter logic implementations

        /* Notes on Filter Methods:
         * When using multiple filters, do not explicitly set anything to true.  Rather,
         * only hide things which do not match the filter criteria
         * by setting e.Accepted = false.  If you set e.Accept = true, if effectively
         * clears out any previous filters applied to it.  
         */

        private void FilterByAgeGroup(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_ageGroupListFirstItem != null && SelectedAgeGroup.AgeGroupID == _ageGroupListFirstItem.AgeGroupID)
                e.Accepted = true;
            else if (SelectedAgeGroup.AgeGroupID.CompareTo(src.AgeGroup) != 0)
                e.Accepted = false;
        }

        private void FilterByDifficulty(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_difficultyListFirstItem != null && SelectedDifficulty.RouteDifficultyID == _difficultyListFirstItem.RouteDifficultyID)
                e.Accepted = true;
            else if (SelectedDifficulty.RouteDifficultyID.CompareTo(src.Difficulty) != 0)
                e.Accepted = false;
        }

        #endregion       
    }
}
