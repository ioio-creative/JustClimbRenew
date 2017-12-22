using JustClimbTrial.Enums;

namespace JustClimbTrial.ViewModels
{
    public class RockOnRouteViewModel
    {
        public RockViewModel MyRockViewModel { get; set; }  // contains Shape and DataAccess.Rock
        public RockOnBoulderStatus BoulderStatus { get; set; }
        public int TrainingSeq { get; set; }
        

        public void SetRockTrainingSeqAndDraw(int seqNo)
        {
            TrainingSeq = seqNo;
            MyRockViewModel.DrawSequenceRockOnCanvas(seqNo);
        }

        public void SetRockStatusAndDraw(RockOnBoulderStatus status)
        {
            BoulderStatus = status;
            SetRockShapeWrtStatus();
        }

        private void SetRockShapeWrtStatus()
        {
            switch (BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    MyRockViewModel.ChangeRockShapeToStart();
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    MyRockViewModel.ChangeRockShapeToIntermediate();
                    break;
                case RockOnBoulderStatus.End:
                    MyRockViewModel.ChangeRockShapeToEnd();
                    break;
            }
        }
    }
}
