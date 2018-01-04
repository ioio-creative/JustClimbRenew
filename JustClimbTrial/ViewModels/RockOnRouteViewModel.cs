using JustClimbTrial.Enums;

namespace JustClimbTrial.ViewModels
{
    public class RockOnRouteViewModel
    {
        #region members

        public RockViewModel MyRockViewModel { get; set; }  // contains Shape and DataAccess.Rock
        public RockOnBoulderStatus BoulderStatus { get; set; }
        public int TrainingSeq { get; set; }

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
                    MyRockViewModel.CreateRockImage();
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    MyRockViewModel.ChangeRockShapeToIntermediate();
                    MyRockViewModel.CreateRockImage();
                    break;
                case RockOnBoulderStatus.End:
                    MyRockViewModel.ChangeRockShapeToEnd();
                    MyRockViewModel.CreateRockImage();
                    break;
            }
        }

        #endregion
    }
}
