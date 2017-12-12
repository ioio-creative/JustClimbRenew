using JustClimbTrial.Mvvm.Infrastructure;

namespace JustClimbTrial.ViewModels
{
    public class RouteViewModel : ViewModelBase
    {
        public string RouteID { get; set; }
        public string RouteNo { get; set; }
        public string Difficulty { get; set; }
        public string DifficultyDesc { get; set; }
        public string AgeGroup { get; set; }
        public string AgeDesc { get; set; }
    }
}
