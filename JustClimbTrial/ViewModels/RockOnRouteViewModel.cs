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

        public ImageSequenceHelper RockAnimationSequence;
        
        #endregion

        public RockTimerHelper InitializeRockTimerHelper(int goal, int lag, int msInterval = 100)
        {
            MyRockTimerHelper = new RockTimerHelper(goal, lag, msInterval);
            return MyRockTimerHelper;
        }


        #region set training rock

        public void SetRockTrainingSeqAndDraw(int seqNo, bool mirrorSeqNo = false)
        {
            TrainingSeq = seqNo;
            MyRockViewModel.DrawSequenceRockOnCanvas(seqNo, mirrorSeqNo);
        }

        public void UndrawRockTrainingSeq()
        {
            MyRockViewModel.UndrawSequenceRockOnCanvas();
        }

        #endregion


        #region set boulder rock

        public void SetRockStatusAndDrawShape(RockOnBoulderStatus status)
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
            if (TrainingSeq == 1)
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

        public void SetRockImageWrtStatus()
        {
            //TODO: initialize boulder img sequences according to boulder status

            //MyRockViewModel.CreateRockImageSequence();
            switch (BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    MyRockViewModel.SetRockImage();
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    MyRockViewModel.SetRockImage();
                    break;
                case RockOnBoulderStatus.End:
                    MyRockViewModel.SetRockImage();
                    break;
            }
        }

        public void SetRockImageWrtTrainSeq(int maxSeqNo)
        {
            RockAnimationSequence = new ImageSequenceHelper(MyRockViewModel.SetRockImage(), true);

            if (TrainingSeq == 1)
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
