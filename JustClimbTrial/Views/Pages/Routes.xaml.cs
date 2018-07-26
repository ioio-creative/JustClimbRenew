using JustClimbTrial.DataAccess;
using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using JustClimbTrial.Helpers;
using JustClimbTrial.ViewModels;
using JustClimbTrial.Views.UserControls;
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
        private readonly bool debug = AppGlobal.DEBUG;

        private MainWindow mainWindowClient;

        private RoutesViewModel viewModel;
        private ClimbMode climbMode;


        #region constructors

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
            
            WindowTitle = Title;
        }

        #endregion


        #region initialization

        private void InitializeNavHead()
        {
            HeaderRowNavigation navHead = master.NavHead;

            // pass this Page to the top row user control so it can use this Page's NavigationService
            navHead.ParentPage = this;

            string headerRowTitleFormat = "{0} - Route Select";
            navHead.HeaderRowTitle = string.Format(headerRowTitleFormat,
                ClimbModeGlobals.StringDict[climbMode]);
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeNavHead();

            mainWindowClient = this.Parent as MainWindow;
            mainWindowClient.CheckAndLoadAndPlayScrnSvr();

            if (debug)
            {
                mainWindowClient.SubscribeColorImgSrcToPlaygrd(); 
            }
            viewModel.LoadData();

            // set titles
            string titleFormat = "Just Climb - {0} Routes";
            Title = string.Format(titleFormat, 
                ClimbModeGlobals.StringDict[climbMode]);           
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindowClient.UnsubColorImgSrcToPlaygrd();
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
