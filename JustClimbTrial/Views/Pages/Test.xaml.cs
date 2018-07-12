using JustClimbTrial.Views.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class Test : Page
    {
        #region constructors

        public Test()
        {
            InitializeComponent();
        }

        #endregion


        #region initialization

        private void InitializeNavHead()
        {
            HeaderRowNavigation navHead = master.NavHead;
            navHead.HeaderRowTitle = "Header Row Title";
            navHead.ParentPage = this;
        }

        #endregion


        #region event handlers

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeNavHead();
        }

        #endregion
    }
}
