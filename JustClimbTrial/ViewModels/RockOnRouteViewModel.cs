using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;

namespace JustClimbTrial.ViewModels
{
    public class RockOnRouteViewModel
    {
        #region members

        public RockViewModel MyRockViewModel { get; set; }  // contains Shape and DataAccess.Rock
        public RockOnBoulderStatus BoulderStatus { get; set; }
        public int TrainingSeq { get; set; }
        public RockTimerHelper MyRockTimerHelper { get; set; }
        #endregion


        #region set training rock

        public void SetRockTrainingSeqAndDraw(int seqNo)
        {
            TrainingSeq = seqNo;
            MyRockViewModel.DrawSequenceRockOnCanvas(seqNo);
        }

        public void UndrawRockTrainingSeq()
        {
            MyRockViewModel.UndrawSequenceRockOnCanvas();
        }

        #endregion


        #region set boulder rock

        public void SetRockStatusAndDraw(RockOnBoulderStatus status)
        {
            BoulderStatus = status;
            DrawRockShapeWrtStatus();
        }

        public void DrawRockShapeWrtStatus()
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

        public void DrawRockShapeWrtTrainSeq(int maxSeqNo)
        {
            if(TrainingSeq == 1)
            {
                MyRockViewModel.ChangeRockShapeToStart();
            }
            else if (TrainingSeq == maxSeqNo)
            {
                MyRockViewModel.ChangeRockShapeToEnd();
            }
            else
            {
                MyRockViewModel.ChangeRockShapeToIntermediate();
            }

            
        }

        #endregion
    }
}
