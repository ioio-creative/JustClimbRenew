using JustClimbTrial.Enums;
using System.Windows;

namespace JustClimbTrial.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for RouteSetModelSelectDialog.xaml
    /// </summary>
    public partial class RouteSetModeSelectDialog : Window
    {
        public ClimbMode ClimbModeSelected { get; set; }


        public RouteSetModeSelectDialog()
        {
            InitializeComponent();
        }


        #region event handlers

        private void btnBoulder_Click(object sender, RoutedEventArgs e)
        {
            ClimbModeSelected = ClimbMode.Boulder;
            DialogResult = true;
        }

        private void btnTraining_Click(object sender, RoutedEventArgs e)
        {
            ClimbModeSelected = ClimbMode.Training;
            DialogResult = true;
        }

        #endregion
    }
}