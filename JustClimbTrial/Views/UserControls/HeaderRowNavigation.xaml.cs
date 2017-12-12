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
        private Page _parentPage;      
        public Page ParentPage
        {
            private get { return _parentPage; }
            set { _parentPage = value; }
        }

        private Visibility _staffOptionsVisibility;
        public Visibility StaffOptionsVisibility
        {
            get
            {
                return _staffOptionsVisibility;
            }

            set
            {
                _staffOptionsVisibility = value;
                OnPropertyChanged(nameof(StaffOptionsVisibility));
            }
        }

        public static readonly DependencyProperty HeaderRowTitleProperty =
            DependencyProperty.Register("HeaderRowTitle", typeof(string),
                typeof(HeaderRowNavigation));        
        
        public string HeaderRowTitle
        {
            get
            {
                return (string)GetValue(HeaderRowTitleProperty);
            }

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
        }

        #endregion


        #region event handlers

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
        
        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            if (ParentPage != null)
            {
                JustClimbHome justClimbHomePage = new JustClimbHome();
                ParentPage.NavigationService.Navigate(justClimbHomePage);
            }
        }

        private void btnRescanWall_Click(object sender, RoutedEventArgs e)
        {
            if (ParentPage != null)
            {
                NewWall newWallPage = new NewWall();
                ParentPage.NavigationService.Navigate(newWallPage);
            }
        }

        private void btnRouteSet_Click(object sender, RoutedEventArgs e)
        {
            RouteSetModeSelectDialog routeSetModeSelect = new RouteSetModeSelectDialog();
            bool dialogResult = routeSetModeSelect.ShowDialog().GetValueOrDefault(false);

            if (dialogResult)
            {
                RouteSet routeSetPage = new RouteSet(routeSetModeSelect.ClimbModeSelected);
                ParentPage.NavigationService.Navigate(routeSetPage);
            }
        }

        #endregion        
    }
}
