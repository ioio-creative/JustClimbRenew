using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using JustClimbTrial.Mvvm.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace JustClimbTrial.ViewModels
{
    public class GameStartViewModel : ViewModelBase
    {
        #region private members

        private const string MonthToStringFormat = "d2";
        private const string DayToStringFormat = "d2";
        private const string HourToStringFormat = "d2";
       
        private string _routeId;
        private ClimbMode _climbMode;
        private CollectionViewSource _cvsVideosFromView;

        private string _yearListFirstItem;
        private string _monthListFirstItem;
        private string _dayListFirstItem;
        private FilterHourViewModel _hourListFirstItem;

        private GeneralFilter _yearFilter;
        private GeneralFilter _monthFilter;
        private GeneralFilter _dayFilter;
        private GeneralFilter _hourFilter;
        
        public void SetRouteId(string anId)
        {
            _routeId = anId;
        }

        public void SetClimbMode(ClimbMode aClimbMode)
        {
            _climbMode = aClimbMode;
        }

        public void SetCvsVideos(CollectionViewSource cvsFromView)
        {
            _cvsVideosFromView = cvsFromView;
            _yearFilter = new GeneralFilter(cvsFromView, FilterByYear);
            _monthFilter = new GeneralFilter(cvsFromView, FilterByMonth);
            _dayFilter = new GeneralFilter(cvsFromView, FilterByDay);
            _hourFilter = new GeneralFilter(cvsFromView, FilterByHour);
        }

        public void SetYearListFirstItem(string str)
        {
            _yearListFirstItem = str;
        }

        public void SetMonthListFirstItem(string str)
        {
            _monthListFirstItem = str;
        }

        public void SetDayListFirstItem(string str)
        {
            _dayListFirstItem = str;
        }

        public void SetHourListFirstItem(FilterHourViewModel hourModel)
        {
            _hourListFirstItem = hourModel;
        }

        #endregion


        #region constructor

        public GameStartViewModel()
        {
            InitializeCommands();            
        }

        #endregion


        #region filter properties

        public string SelectedYear
        {
            get { return GetValue(() => SelectedYear); }
            set
            {
                SetValue(() => SelectedYear, value);

                if (string.IsNullOrEmpty(value))
                {
                    _yearFilter.RemoveFilter();
                }
                else
                {
                    _yearFilter.AddFilter();
                }
            }
        }

        public string SelectedMonth
        {
            get { return GetValue(() => SelectedMonth); }
            set
            {
                SetValue(() => SelectedMonth, value);

                if (string.IsNullOrEmpty(value))
                {
                    _monthFilter.RemoveFilter();
                }
                else
                {
                    _monthFilter.AddFilter();
                }
            }
        }

        public string SelectedDay
        {
            get { return GetValue(() => SelectedDay); }
            set
            {
                SetValue(() => SelectedDay, value);

                if (string.IsNullOrEmpty(value))
                {
                    _dayFilter.RemoveFilter();
                }
                else
                {
                    _dayFilter.AddFilter();
                }
            }
        }

        public FilterHourViewModel SelectedHour
        {
            get { return GetValue(() => SelectedHour); }
            set
            {
                SetValue(() => SelectedHour, value);

                if (value == null || string.IsNullOrEmpty(value.HourString))
                {
                    _hourFilter.RemoveFilter();
                }
                else
                {
                    _hourFilter.AddFilter();
                }
            }
        }

        #endregion


        #region collections generated or from database

        public ObservableCollection<string> Years
        {
            get { return GetValue(() => Years); }
            set { SetValue(() => Years, value); }
        }

        public ObservableCollection<string> Months
        {
            get { return GetValue(() => Months); }
            set { SetValue(() => Months, value); }
        }

        public ObservableCollection<string> Days
        {
            get { return GetValue(() => Days); }
            set { SetValue(() => Days, value); }
        }

        public ObservableCollection<FilterHourViewModel> Hours
        {
            get { return GetValue(() => Hours); }
            set { SetValue(() => Hours, value); }
        }

        public ObservableCollection<RouteVideoViewModel> RouteVideoViewModels
        {
            get { return GetValue(() => RouteVideoViewModels); }
            set { SetValue(() => RouteVideoViewModels, value); }
        }

        // can be called by the view during Page.OnLoad
        public void LoadData()
        {
            List<string> years = DateTimeHelper.GetYearsForComboBox().ToList();
            List<string> months = DateTimeHelper.GetMonthsForComboBox(MonthToStringFormat).ToList();
            List<string> days = DateTimeHelper.GetDaysForComboBox(DayToStringFormat).ToList();
            List<FilterHourViewModel> hours = DateTimeHelper.GetHoursForComboBox(HourToStringFormat).ToList();

            // join valid video and (no specify valid) route
            IEnumerable<RouteVideoViewModel> videoViewModels;
            switch (_climbMode)
            {
                case ClimbMode.Training:
                    videoViewModels =
                        from video in TrainingRouteVideoDataAccess.ValidTrainingRouteVideosByRouteId(_routeId)
                        join route in TrainingRouteDataAccess.TrainingRoutes on video.Route equals route.RouteID
                        select new RouteVideoViewModel
                        {
                            VideoID = video.VideoID,
                            VideoNo = video.VideoNo,
                            RouteID = video.Route,
                            RouteNo = route.RouteNo,
                            IsDemo = video.IsDemo.GetValueOrDefault(false),
                            CreateDT = video.CreateDT.GetValueOrDefault(),
                            CreateDTString = FormatVideoCreateDTStringForDisplay(
                                video.CreateDT.GetValueOrDefault())
                        };
                    break;
                case ClimbMode.Boulder:
                default:
                    videoViewModels =
                        from video in BoulderRouteVideoDataAccess.ValidBoulderRouteVideosByRouteId(_routeId)
                        join route in BoulderRouteDataAccess.BoulderRoutes on video.Route equals route.RouteID
                        select new RouteVideoViewModel
                        {
                            VideoID = video.VideoID,
                            VideoNo = video.VideoNo,
                            RouteID = video.Route,
                            RouteNo = route.RouteNo,
                            IsDemo = video.IsDemo.GetValueOrDefault(false),
                            CreateDT = video.CreateDT.GetValueOrDefault(),
                            CreateDTString = FormatVideoCreateDTStringForDisplay(
                                video.CreateDT.GetValueOrDefault())
                        };
                    break;
            }

            // add null item at the front of the combo boxes
            if (_yearListFirstItem != null)
            {
                years.Insert(0, _yearListFirstItem);
            }

            if (_monthListFirstItem != null)
            {
                months.Insert(0, _monthListFirstItem);
            }

            if (_dayListFirstItem != null)
            {
                days.Insert(0, _dayListFirstItem);
            }

            if (_hourListFirstItem != null)
            {
                hours.Insert(0, _hourListFirstItem);
            }

            Years = new ObservableCollection<string>(years);
            Months = new ObservableCollection<string>(months);
            Days = new ObservableCollection<string>(days);
            Hours = new ObservableCollection<FilterHourViewModel>(hours);
            RouteVideoViewModels =
                new ObservableCollection<RouteVideoViewModel>(videoViewModels);
        }

        private string FormatVideoCreateDTStringForDisplay(DateTime createDT)
        {
            return string.Format("{0:yyyyMMdd_HHmm}", createDT);
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
                    _yearFilter, _monthFilter, _dayFilter, _hourFilter
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

        private void FilterByYear(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteVideoViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_yearListFirstItem != null && SelectedYear == _yearListFirstItem)
                e.Accepted = true;
            else if (SelectedYear.CompareTo(src.CreateDT.Year.ToString()) != 0)
                e.Accepted = false;
        }

        private void FilterByMonth(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteVideoViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_monthListFirstItem != null && SelectedMonth == _monthListFirstItem)
                e.Accepted = true;
            else if (SelectedMonth.CompareTo(src.CreateDT.Month.ToString(MonthToStringFormat)) != 0)
                e.Accepted = false;
        }

        private void FilterByDay(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteVideoViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_dayListFirstItem != null && SelectedDay == _dayListFirstItem)
                e.Accepted = true;
            else if (SelectedDay.CompareTo(src.CreateDT.Day.ToString(DayToStringFormat)) != 0)
                e.Accepted = false;
        }

        private void FilterByHour(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as RouteVideoViewModel;
            if (src == null)
                e.Accepted = false;
            else if (_hourListFirstItem != null && SelectedHour.Hour == _hourListFirstItem.Hour)
                e.Accepted = true;
            else if (SelectedHour.Hour.CompareTo(src.CreateDT.Hour) != 0)
                e.Accepted = false;
        }

        #endregion
    }
}
