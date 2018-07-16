using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.ViewModels
{
    public class RockOnRouteViewModel
    {
        #region members

        public RockViewModel MyRockViewModel { get; set; }  // contains Shape and DataAccess.Rock
        public RockOnBoulderStatus BoulderStatus { get; set; }
        public int TrainingSeq { get; set; }
        public RockTimerHelper MyRockTimerHelper { get; set; }

        private ImageSequenceHelper RockAnimationSequence;
        
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
            DrawRockShapeWrtBoulderStatus();
        }

        public void DrawRockShapeWrtBoulderStatus()
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

        public void SetRockImageWrtBoulderStatus()
        {
            //TODO: initialize boulder img sequences according to boulder status
            RockAnimationSequence = new ImageSequenceHelper(MyRockViewModel.SetRockImage(), true);
            switch (BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][]
                    {
                        ImageSequenceHelper.ShowSequence,  // 1
                        ImageSequenceHelper.ShinePopSequence,  // 3
                        //ImageSequenceHelper.ShineLoopSequence  // 4
                        ImageSequenceHelper.ShineFeedbackLoopSequence
                    });
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });
                    break;
                case RockOnBoulderStatus.End:
                    RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });
                    break;
            }
        }

        public void SetRockImageSeqWrtTrainSeq(int maxSeqNo)
        {
            RockAnimationSequence = new ImageSequenceHelper(MyRockViewModel.SetRockImage(), true);
            if (TrainingSeq == 1)
            {
                RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][]
                    {
                        ImageSequenceHelper.ShowSequence,  // 1
                        ImageSequenceHelper.ShinePopSequence,  // 3
                        //ImageSequenceHelper.ShineLoopSequence  // 4
                        ImageSequenceHelper.ShineFeedbackLoopSequence
                    }
                );
            }
            else if (TrainingSeq == maxSeqNo)
            {      
                RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1
            }
            else
            {
                RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });
            }
        }

        public void PlayRockImgSequence()
        {
            RockAnimationSequence.Play();
        }
        #endregion
    }
}
