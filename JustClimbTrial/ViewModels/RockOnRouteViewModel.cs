using JustClimbTrial.Enums;

namespace JustClimbTrial.ViewModels
{
    public class RockOnRouteViewModel
    {
        public RockViewModel MyRockViewModel { get; set; }  // contains Shape and DataAccess.Rock
        public RockOnBoulderStatus BoulderStatus { get; set; }
        public int TrainingSeq { get; set; }        
    }
}
