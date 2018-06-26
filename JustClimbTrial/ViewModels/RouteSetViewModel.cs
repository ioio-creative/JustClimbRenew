﻿using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using JustClimbTrial.Mvvm.Infrastructure;
using System.Collections.ObjectModel;
using System.Linq;

namespace JustClimbTrial.ViewModels
{
    public class RouteSetViewModel : ViewModelBase
    {
        #region private members

        private ClimbMode _climbMode;

        public void SetClimbMode(ClimbMode aClimbMode)
        {
            _climbMode = aClimbMode;
        }

        #endregion


        #region members for databinding

        public ObservableCollection<AgeGroup> AgeGroups
        {
            get { return GetValue(() => AgeGroups); }
            set { SetValue(() => AgeGroups, value); }
        }

        public ObservableCollection<RouteDifficulty> RouteDifficulties
        {
            get { return GetValue(() => RouteDifficulties); }
            set { SetValue(() => RouteDifficulties, value); }
        }

        #endregion


        public void LoadData()
        {
            AgeGroups = new ObservableCollection<AgeGroup>(AgeGroupDataAccess.ValidAgeGroups);
            RouteDifficulties = new ObservableCollection<RouteDifficulty>(RouteDifficultyDataAccess.ValidRouteDifficulties);
        }
    }
}
