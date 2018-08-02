using JustClimbTrial.Enums;
using JustClimbTrial.Globals;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustClimbTrial.Views.Pages
{
    /// <summary>
    /// Interaction logic for ModeSelect.xaml
    /// </summary>
    public partial class ModeSelect : Page, INotifyPropertyChanged
    {
        private bool debug
        {
            get
            {
                return AppGlobal.DEBUG;
            }
        }
        private MainWindow mainWindowClient;


        #region dependency properties

        private string _btnBoulderText;
        public string BtnBoulderText
        {
            get { return _btnBoulderText; }
            private set
            {
                if (_btnBoulderText != value)
                {
                    _btnBoulderText = value;
                    OnPropertyChanged(nameof(BtnBoulderText));
                }
            }
        }

        private string _btnTrainingText;
        public string BtnTrainingText
        {
            get { return _btnTrainingText; }
            private set
            {
                if (_btnTrainingText != value)
                {
                    _btnTrainingText = value;
                    OnPropertyChanged(nameof(BtnTrainingText));
                }
            }
        }

        #endregion


        #region constructors

        public ModeSelect()
        {
            InitializeComponent();
            BtnBoulderText = ClimbModeGlobals.StringDict[ClimbMode.Boulder];
            BtnTrainingText = ClimbModeGlobals.StringDict[ClimbMode.Training];
        }

        #endregion


        #region event handlers

        private void BtnBoulder_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Boulder);
        }

        private void BtnTraining_Click(object sender, RoutedEventArgs e)
        {
            GoToRoutesPage(ClimbMode.Training);
        }        

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowClient = this.Parent as MainWindow;
            if (debug)
            {
                mainWindowClient.SubscribeColorImgSrcToPlaygrd(); 
            }
            AppGlobal.DebugModeChanged += HandleDebugModeChanged;
            mainWindowClient.CheckAndLoadAndPlayScrnSvr();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (debug)
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd(); 
            }
            AppGlobal.DebugModeChanged -= HandleDebugModeChanged;
        }

        private void HandleDebugModeChanged(bool _debug)
        {
            if (_debug)
            {
                mainWindowClient.SubscribeColorImgSrcToPlaygrd();
            }
            else
            {
                mainWindowClient.UnsubColorImgSrcToPlaygrd();
            }
        }

        #endregion


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion


        private void GoToRoutesPage(ClimbMode climbMode)
        {
            Routes routesPage = new Routes(climbMode);
            NavigationService.Navigate(routesPage);
        }
    }
}
