using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using JustClimbTrial.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for Routes.xaml
    /// </summary>
    public partial class Routes : Page
    {
        private RoutesViewModel viewModel;
        private ClimbMode climbMode;                    

        public Routes() : this(ClimbMode.Boulder) { }

        public Routes(ClimbMode aClimbMode)
        {           
            InitializeComponent();            

            climbMode = aClimbMode;

            // pass cvsBoulderRoutes to the view model
            viewModel = DataContext as RoutesViewModel;
            if (viewModel != null)
            {
                CollectionViewSource cvsRoutes = gridContainer.Resources["cvsRoutes"] as CollectionViewSource;
                viewModel.SetCvsRoutes(cvsRoutes);
                viewModel.SetAgeGroupListFirstItem(new AgeGroup
                {
                    AgeGroupID = "",
                    AgeDesc = ""                    
                });
                viewModel.SetDifficultyListFirstItem(new RouteDifficulty
                {
                    RouteDifficultyID = "",
                    DifficultyDesc = ""
                });
                viewModel.SetClimbMode(aClimbMode);
            }

            // pass this Page to the top row user control so it can use this Page's NavigationService
            navHead.ParentPage = this;

            // set titles
            string titleFormat = "Just Climb - {0} Routes";
            string headerRowTitleFormat = "{0} - Route Select";
            switch (climbMode)
            {
                case ClimbMode.Training:
                    Title = string.Format(titleFormat, "Training");                    
                    navHead.HeaderRowTitle = string.Format(headerRowTitleFormat, "Training");
                    break;
                case ClimbMode.Boulder:
                default:
                    Title = string.Format(titleFormat, "Boulder");                    
                    navHead.HeaderRowTitle = string.Format(headerRowTitleFormat, "Boulder");
                    break;
            }
            WindowTitle = Title;
        }


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadData();
        }

        private void btnGameStart_Click(object sender, RoutedEventArgs e)
        {
            RouteViewModel route = dgridRoutes.SelectedItem as RouteViewModel;
            if (route == null)
            {                
                UiHelper.NotifyUser("Please select a route.");
            }
            else
            {
                GameStart gameStartPage = new GameStart(route.RouteID, climbMode);
                NavigationService.Navigate(gameStartPage);
            }
        }

        #endregion        
    }
}
