using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
using System;
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


        #region route set drawing func

        public void SetRockTrainingSeqAndDraw(int seqNo, bool mirrorSeqNo = false)
        {
            TrainingSeq = seqNo;
            MyRockViewModel.DrawSequenceRockOnCanvas(seqNo, mirrorSeqNo);
        }

        public void UndrawRockTrainingSeq()
        {
            MyRockViewModel.UndrawSequenceRockOnCanvas();
        }

        public void SetRockStatusAndDrawShape(RockOnBoulderStatus status)
        {
            BoulderStatus = status;
            DrawRockShapeWrtBoulderStatus();
        }

        #endregion


        #region gamestart drawing func (debug)

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

        #endregion

        #region gamestart img seq func (release)
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
                        ImageSequenceHelper.ShinePopSequence,  // 4
                        ImageSequenceHelper.ShineLoopSequence  // 7c
                        //ImageSequenceHelper.ShineFeedbackLoopSequence //b
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

        public void PlayRockImgSequence()
        {
            RockAnimationSequence.Play();
        }

        public void SetRockImageSeqWrtTrainSeq(int maxSeqNo)
        {
            RockAnimationSequence = new ImageSequenceHelper(MyRockViewModel.SetRockImage(), false);
            if (TrainingSeq == 1)
            {
                RockAnimationSequence.SetSequences(true,
                    new BitmapSource[][]
                    {
                        ImageSequenceHelper.ShowSequence,  // 1
                        ImageSequenceHelper.ShinePopSequence,  // 4
                        ImageSequenceHelper.ShineLoopSequence  // 7c
                        //ImageSequenceHelper.ShineFeedbackLoopSequence //b
                    });
            }
            else if (TrainingSeq == maxSeqNo)
            {
                RockAnimationSequence.SetSequences(false,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });  // 1
            }
            else
            {
                RockAnimationSequence.SetSequences(false,
                    new BitmapSource[][] { ImageSequenceHelper.ShowSequence });
            }
        }

        public void SetAndPlayActivePopAndShineImgSeq()
        {
            RockAnimationSequence.Stop();
            RockAnimationSequence.SetSequences(true,
                new BitmapSource[][]
                {
                    ImageSequenceHelper.ShinePopSequence,  // 7b
                    ImageSequenceHelper.ShineLoopSequence  // 7c
                });
            PlayRockImgSequence();
        }

        public void SetAndPlayFeedbackImgSeq()
        {
            RockAnimationSequence.Stop();
            RockAnimationSequence.SetSequences(false,
                new BitmapSource[][]{ ImageSequenceHelper.FeedbackSequence }); // 4
            PlayRockImgSequence();
        }

        public void SetAndPlayFeedbackShineLoopImgSeq()
        {
            RockAnimationSequence.Stop();
            RockAnimationSequence.SetSequences(true,
                new BitmapSource[][]
                {
                    ImageSequenceHelper.ShineFeedbackPopSequence,  // a
                    ImageSequenceHelper.ShineFeedbackLoopSequence  // b
                });
            PlayRockImgSequence();
        }
        #endregion
    }
}
