using JustClimbTrial.Enums;
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
        public ModeSelect()
        {
            InitializeComponent();
        }

        private void btnBoulder_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Boulder);
        }

        private void btnTraining_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Training);
        }

        private void GoToRoutesPage(ClimbMode climbMode)
        {
            Routes routesPage = new Routes(climbMode);
            NavigationService.Navigate(routesPage);
        }
    }
}
