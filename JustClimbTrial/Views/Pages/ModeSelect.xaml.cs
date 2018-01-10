using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for ModeSelect.xaml
    /// </summary>
    public partial class ModeSelect : Page
    {
        private readonly bool debug = AppGlobal.DEBUG;

        private MainWindow parentMainWindow;

        public ModeSelect()
        {
            InitializeComponent();
        }

        private void BtnBoulder_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Boulder);
        }

        private void BtnTraining_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Training);
        }

        private void GoToRoutesPage(ClimbMode climbMode)
        {
            Routes routesPage = new Routes(climbMode);
            NavigationService.Navigate(routesPage);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            parentMainWindow = this.Parent as MainWindow;
            if (debug)
            {
                parentMainWindow.SubscribeColorImgSrcToPlaygrd(); 
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            parentMainWindow.UnsubColorImgSrcToPlaygrd();
        }
    }
}
