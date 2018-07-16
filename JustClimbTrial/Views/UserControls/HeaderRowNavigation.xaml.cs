using JustClimbTrial.Extensions;
using JustClimbTrial.Interfaces;
using JustClimbTrial.Mvvm.Infrastructure;
using JustClimbTrial.Views.Dialogs;
using JustClimbTrial.Views.Pages;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JustClimbTrial.Views.UserControls
{
    /// <summary>
    /// Interaction logic for HeaderRowNavigation.xaml
    /// </summary>
    public partial class HeaderRowNavigation : UserControl, INotifyPropertyChanged
    {
        private bool _isRecordDemoVideo;
        public bool IsRecordDemoVideo
        {
            get { return _isRecordDemoVideo; }
            set
            {
                if (_isRecordDemoVideo != value)
                {
                    _isRecordDemoVideo = value;
                    OnPropertyChanged(nameof(IsRecordDemoVideo));
                }
            }
        }

        private Page _parentPage;      
        public Page ParentPage
        {
            private get { return _parentPage; }
            set
            {
                if (_parentPage != value)
                {
                    _parentPage = value;
                    OnPropertyChanged(nameof(ParentPage));
                }
            }
        }

        private Visibility _staffOptionsVisibility;
        public Visibility StaffOptionsVisibility
        {
            get { return _staffOptionsVisibility; }
            set
            {
                if (_staffOptionsVisibility != value)
                {
                    _staffOptionsVisibility = value;
                    OnPropertyChanged(nameof(StaffOptionsVisibility));
                }
            }
        }

        private Visibility _btnRecordDemoVideoVisibility;
        public Visibility BtnRecordDemoVideoVisibility
        {
            get { return _btnRecordDemoVideoVisibility; }
            set
            {
                if (_btnRecordDemoVideoVisibility != value)
                {
                    _btnRecordDemoVideoVisibility = value;
                    OnPropertyChanged(nameof(BtnRecordDemoVideoVisibility));
                }
            }
        }

        public static readonly DependencyProperty HeaderRowTitleProperty =
            DependencyProperty.Register("HeaderRowTitle", typeof(string),
                typeof(HeaderRowNavigation));        
        
        public string HeaderRowTitle
        {
            get { return (string)GetValue(HeaderRowTitleProperty); }            

            set
            {
                SetValue(HeaderRowTitleProperty, value);
                OnPropertyChanged(nameof(HeaderRowTitle));
            }            
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion


        #region constructor

        public HeaderRowNavigation()
        {
            InitializeComponent();
            StaffOptionsVisibility = Visibility.Collapsed;
            BtnRecordDemoVideoVisibility = Visibility.Collapsed;
        }

        #endregion


        #region initialization

        private void InitializeCommands()
        {
            btnHome.Command =
                new RelayCommand(SwitchToHomePage, CanSwitchToHomePage);
            btnRescanWall.Command =
                new RelayCommand(SwitchToNewWallPage, CanSwitchToNewWallPage);
            btnRouteSet.Command =
                new RelayCommand(SwitchToRouteSetPage, CanSwitchToRouteSetPage);
            btnRecordDemoVideo.Command =
                new RelayCommand(SwitchOnRecordDemoVideoMode, CanSwitchOnRecordDemoVideoMode);
            btnCancelRecordDemoVideo.Command =
                new RelayCommand(SwitchOffRecordDemoVideoMode, CanSwitchOffRecordDemoVideoMode);
        }

        #endregion


        #region CanExecute command methods

        private bool CanSwitchToHomePage(object parameter = null)
        {
            return true;
        }

        private bool CanSwitchToNewWallPage(object parameter = null)
        {
            return true;
        }

        private bool CanSwitchToRouteSetPage(object parameter = null)
        {
            return true;
        }

        private bool CanSwitchOnRecordDemoVideoMode(object parameter = null)
        {
            return !IsRecordDemoVideo;
        }

        private bool CanSwitchOffRecordDemoVideoMode(object parameter = null)
        {
            return IsRecordDemoVideo;
        }

        #endregion


        #region command methods

        private void SwitchToHomePage(object parameter = null)
        {
            if (ParentPage != null)
            {
                JustClimbHome justClimbHomePage = new JustClimbHome();
                ParentPage.NavigationService.Navigate(justClimbHomePage);
            }
        }

        private void SwitchToNewWallPage(object parameter = null)
        {
            if (ParentPage != null)
            {
                NewWall newWallPage = new NewWall();
                ParentPage.NavigationService.Navigate(newWallPage);
            }
        }

        private void SwitchToRouteSetPage(object parameter = null)
        {
            RouteSetModeSelectDialog routeSetModeSelect = new RouteSetModeSelectDialog();
            bool dialogResult = routeSetModeSelect.ShowDialog().GetValueOrDefault(false);

            if (dialogResult)
            {
                routeSetModeSelect.Close();
                if (ParentPage != null)
                {
                    RouteSet routeSetPage = new RouteSet(routeSetModeSelect.ClimbModeSelected);
                    ParentPage.NavigationService.Navigate(routeSetPage); 
                }
            }
        }

        private void SwitchOnRecordDemoVideoMode(object parameter = null)
        {
            // if an ancestor IS ISavingVideo
            DependencyObject savingVideoObject = this.GetAncestorRecursive<ISavingVideo>();
            if (savingVideoObject != null)
            {
                ISavingVideo savingVideoPage = savingVideoObject as ISavingVideo;
                
                if (savingVideoPage.IsRouteContainDemoVideo)
                {
                    MessageBoxResult mbr =
                        MessageBox.Show("The route already contains a demo. Do you want to record a new one?", "Record new demo?",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    switch (mbr)
                    {
                        case MessageBoxResult.Yes:
                            IsRecordDemoVideo = true;
                            break;
                        case MessageBoxResult.No:
                        default:
                            IsRecordDemoVideo = false;
                            break;
                    }
                }
                else
                {
                    IsRecordDemoVideo = true;
                }
            }
            else
            {
                IsRecordDemoVideo = true;
            }            
        }

        private void SwitchOffRecordDemoVideoMode(object parameter = null)
        {
            IsRecordDemoVideo = false;
        }

        #endregion


        #region event handlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // if an ancestor IS ISavingVideo
            if (this.GetAncestorRecursive<ISavingVideo>() != null)
            {
                BtnRecordDemoVideoVisibility
                    = Visibility.Visible;
            }

            InitializeCommands();
        }

        // MouseDown event not working WPF
        // https://social.msdn.microsoft.com/Forums/en-US/61807025-d4c4-41e0-b648-b11183065009/mousedown-event-not-working-wpf?forum=wpf
        private void btnStaffOptions_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 3)
            {
                switch (StaffOptionsVisibility)
                {
                    case Visibility.Hidden:
                    case Visibility.Collapsed:
                        StaffOptionsVisibility = Visibility.Visible;
                        break;
                    case Visibility.Visible:
                        StaffOptionsVisibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        #endregion        
    }
}
