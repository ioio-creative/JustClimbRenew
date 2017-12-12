using JustClimbTrial.Mvvm.Infrastructure;
using System;

namespace JustClimbTrial.ViewModels
{
    public class RouteVideoViewModel : ViewModelBase
    {
        public string VideoID { get; set; }
        public string VideoNo { get; set; }
        public string RouteID { get; set; }
        public string RouteNo { get; set; }
        public bool IsDemo { get; set; }
        public DateTime CreateDT { get; set; }
        public string CreateDTString { get; set; }
    }
}
