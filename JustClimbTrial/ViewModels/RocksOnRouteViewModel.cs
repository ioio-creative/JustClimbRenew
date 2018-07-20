using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using Microsoft.Kinect;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.ViewModels
{
    public class RocksOnRouteViewModel : IEnumerable<RockOnRouteViewModel>
    {
        /* used by GameStart */

        public ClimbMode RouteClimbMode { get; set; }

        public RockOnRouteViewModel StartRock
        {
            get
            {
                RockOnRouteViewModel result;
                switch (RouteClimbMode)
                {
                    case ClimbMode.Boulder:
                    default:
                        result =  rocksOnRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.Start);
                        break;
                    case ClimbMode.Training:
                        result = rocksOnRoute.FirstOrDefault();
                        break;
                }
                return result;
            }
        }
        public RockOnRouteViewModel EndRock
        {
            get
            {
                RockOnRouteViewModel result;
                switch (RouteClimbMode)
                {
                    case ClimbMode.Boulder:
                    default:
                        result = rocksOnRoute.Single(x => x.BoulderStatus == RockOnBoulderStatus.End);
                        break;
                    case ClimbMode.Training:
                        result = rocksOnRoute.LastOrDefault();
                        break;
                }
                return result;
            }
        }
        public IEnumerable<RockOnRouteViewModel> InterRocks
        {
            get
            {
                return rocksOnRoute.Except(new RockOnRouteViewModel[]
                {
                    StartRock,
                    EndRock
                });
            }
        }

        public int RouteLength
        {
            get
            {
                return rocksOnRoute.Count();
            }
        }

        /* end of used by GameStart */

        private IList<RockOnRouteViewModel> rocksOnRoute = new List<RockOnRouteViewModel>();
        private RockOnRouteViewModel selectedRockOnRoute;
        private Shape selectedRockIndicator;  // used in RouteSet
        private IList<Line> linesLinkingTrainingRocks = new List<Line>();
        private Canvas canvas;  

        public RockOnRouteViewModel SelectedRockOnRoute
        {
            get { return selectedRockOnRoute; }
            set
            {
                if (selectedRockOnRoute != value)
                {
                    selectedRockOnRoute = value;

                    // remove old selected rock indicator               
                    if (selectedRockIndicator != null)
                    {
                        canvas.RemoveChild(selectedRockIndicator);
                    }

                    if (selectedRockOnRoute != null)
                    {
                        // draw selected rock indicator
                        selectedRockIndicator = GetNewSelectedRockIndicatorCircle(
                            selectedRockOnRoute.MyRockViewModel.RockShape);
                        canvas.DrawShape(selectedRockIndicator,
                            selectedRockOnRoute.MyRockViewModel.BCanvasPoint);
                    }
                }
            }
        }


        #region constructors

        // used by RouteSet
        public RocksOnRouteViewModel(Canvas aCanvas)
        {
            canvas = aCanvas;
        }

        // used by GameStart
        private RocksOnRouteViewModel(Canvas aCanvas,
            ClimbMode aClimbMode,
            IList<RockOnRouteViewModel> rocksOnRouteVM)
        {
            canvas = aCanvas;
            RouteClimbMode = aClimbMode;
            rocksOnRoute = rocksOnRouteVM;
        }

        #endregion


        #region check if rock is on the route

        public bool AnyRocksInRoute()
        {
            return rocksOnRoute.Any();
        }

        // by rock id
        public bool IsRockOnTheRoute(RockViewModel selectedRock)
        {
            return FindRockOnRouteViewModel(selectedRock) != null;
        }

        // by rock id
        public RockOnRouteViewModel FindRockOnRouteViewModel(RockViewModel selectedRock)
        {
            if (!rocksOnRoute.Any())
            {
                return null;
            }

            IEnumerable<RockOnRouteViewModel> selectedRockOnRouteViewModels =
                   rocksOnRoute.Where(x => x.MyRockViewModel.MyRock.RockID == selectedRock.MyRock.RockID);

            if (selectedRockOnRouteViewModels.Any())
            {
                return selectedRockOnRouteViewModels.Single();
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region manipulate rock on route list
        
        private void AddRockToRoute(RockOnRouteViewModel rockOnRouteVM)
        {
            if (rockOnRouteVM != null && !rocksOnRoute.Contains(rockOnRouteVM))
            {
                rocksOnRoute.Add(rockOnRouteVM);
            }
        }

        public void AddSelectedRockToRoute()
        {
            AddRockToRoute(SelectedRockOnRoute);
        }

        private void RemoveRockFromRoute(RockOnRouteViewModel rockOnRouteVM)
        {
            if (rockOnRouteVM != null)
            {
                rocksOnRoute.Remove(rockOnRouteVM);
                rockOnRouteVM.MyRockViewModel.ChangeRockShapeToDefault();
            }
        }

        public void RemoveSelectedRockFromRoute()
        {
            RemoveRockFromRoute(SelectedRockOnRoute);
            SelectedRockOnRoute = null;
        }

        public void UndoLastTrainingRock()
        {
            if (rocksOnRoute.Any())
            {
                RockOnRouteViewModel lastRockOnRoute = rocksOnRoute[rocksOnRoute.Count - 1];

                lastRockOnRoute.UndrawRockTrainingSeq();
                RemoveLineAttachedToLastTrainingRock();

                RemoveRockFromRoute(lastRockOnRoute);

                // if the last rock is the selected rock
                if (SelectedRockOnRoute.Equals(lastRockOnRoute))
                {
                    // the result may be null
                    SelectedRockOnRoute = rocksOnRoute.LastOrDefault();
                }
            }
        }

        #endregion


        #region manipulate selected rock (route set)

        public bool IsSelectedRockOnRouteNull()
        {
            return SelectedRockOnRoute == null;
        }

        public void SetSelectedTrainingRockSeqNo(bool mirrorSeqNo = false)
        {
            int seqNo = rocksOnRoute.Count;

            if (!IsSelectedRockOnRouteNull())
            {
                // if SelectedRockOnRoute not already in rocksOnRoute,
                // add it into the rocksOnRoute list
                AddRockToRoute(SelectedRockOnRoute);

                if (SelectedRockOnRoute.MyRockViewModel.RockShape == null ||
                    SelectedRockOnRoute.TrainingSeq != seqNo)
                {
                    SelectedRockOnRoute.SetRockTrainingSeqAndDraw(seqNo, mirrorSeqNo);                    
                }

                AttachLineToLastTrainingRockOnCanvas();
            }
        }

        public void SetSelectedBoulderRockStatus(RockOnBoulderStatus status)
        {
            if (!IsSelectedRockOnRouteNull())
            {
                // if SelectedRockOnRoute not already in rocksOnRoute,
                // add it into the rocksOnRoute list
                AddRockToRoute(SelectedRockOnRoute);

                if (SelectedRockOnRoute.MyRockViewModel.RockShape == null ||
                    SelectedRockOnRoute.BoulderStatus != status)
                {
                    SelectedRockOnRoute.SetRockStatusAndDrawShape(status);             
                }
            }            
        }

        #endregion


        #region draw helpers

        /* used by RouteSet */

        private void RemoveLineAttachedToLastTrainingRock()
        {
            if (linesLinkingTrainingRocks.Any())
            {
                Line lineAttachedToLastTrainingRock = linesLinkingTrainingRocks[linesLinkingTrainingRocks.Count - 1];
                linesLinkingTrainingRocks.Remove(lineAttachedToLastTrainingRock);
                canvas.RemoveChild(lineAttachedToLastTrainingRock);
            }
        }

        private void AttachLineToLastTrainingRockOnCanvas()
        {
            if (rocksOnRoute.Count > 1)
            {
                DrawLineBetweenRockViewModels(rocksOnRoute[rocksOnRoute.Count - 2], rocksOnRoute[rocksOnRoute.Count - 1]);
            }
        }

        private static Ellipse GetNewSelectedRockIndicatorCircle(Shape selectedRock)
        {
            SolidColorBrush indicatorFill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
            return new Ellipse
            {
                Fill = indicatorFill,
                StrokeThickness = 0,
                Stroke = Brushes.Red,
                Width = selectedRock.Width,
                Height = selectedRock.Height
            };
        }

        /* end of used by RouteSet */

        /* used by GameStart */
        public void DrawAllRocksOnRouteInGame()
        {
            switch (RouteClimbMode)
            {
                case ClimbMode.Boulder:
                default:
                    foreach (RockOnRouteViewModel rockOnTrainingRoute in rocksOnRoute)
                    {
                        rockOnTrainingRoute.DrawRockShapeWrtBoulderStatus();
                    }
                    break;
                case ClimbMode.Training:
                    foreach (RockOnRouteViewModel rockOnTrainingRoute in rocksOnRoute)
                    {
                        rockOnTrainingRoute.DrawRockShapeWrtTrainSeq(RouteLength);
                    }
                    break;
            }
        }

        public void DrawTrainingPathInGame()
        {
            if (RouteClimbMode == ClimbMode.Training)
            {
                for (int i = 0; i < RouteLength - 1; i++)
                {
                    DrawLineBetweenRockViewModels(rocksOnRoute[i], rocksOnRoute[i + 1]);
                }
            }
        }

        private void DrawLineBetweenRockViewModels(RockOnRouteViewModel rockOnRouteVM_A, RockOnRouteViewModel rockOnRouteVM_B)
        {
            Line lineAttachedToLastTrainingRock = canvas.DrawLine(
                rockOnRouteVM_A.MyRockViewModel.BCanvasPoint,
                rockOnRouteVM_B.MyRockViewModel.BCanvasPoint,
                8,
                new SolidColorBrush(Colors.LightBlue));
            linesLinkingTrainingRocks.Add(lineAttachedToLastTrainingRock);
        }

        /* end of used by GameStart */

        #endregion

        #region ImgSeq helpers (game start)

        public void PlayAllRocksOnRouteImgSequencesInGame()
        {
            foreach (RockOnRouteViewModel rockOnRoute in this)
            {
                rockOnRoute.PlayRockImgSequence();
            }
        }


        public void SetRocksImgSequences()
        {
            switch (RouteClimbMode)
            {
                case ClimbMode.Boulder:
                default:
                    foreach (RockOnRouteViewModel rockOnRoute in this)
                    {
                        rockOnRoute.SetRockImageWrtBoulderStatus();
                    }
                    break;
                case ClimbMode.Training:
                    foreach (RockOnRouteViewModel rockOnRoute in this)
                    {
                        rockOnRoute.SetRockImageSeqWrtTrainSeq(RouteLength);
                    }
                    break;
            }
        }

        #endregion

        #region database

        public string SaveRocksOnTrainingRoute(TrainingRoute trainingRoute)
        {
            return TrainingRouteAndRocksDataAccess.InsertRouteAndRocksOnRoute(
                trainingRoute, rocksOnRoute, true);
        }

        public string SaveRocksOnBoulderRoute(BoulderRoute boulderRoute)
        {
            return BoulderRouteAndRocksDataAccess.InsertRouteAndRocksOnRoute(
                boulderRoute, rocksOnRoute, true);
        }

        #endregion


        #region validations before saving into db

        public string ValidateRocksOnTrainingRoute()
        {
            string errMsg = null;

            if (!IsRocksInTrainingRouteInSequence())
            {
                errMsg = "Rocks in training route not in sequence!";
                return errMsg;
            }

            if (IsTooFewRocksOnRoute())
            {
                errMsg = "Too few rocks in the route!";
                return errMsg;
            }

            return errMsg;
        }

        public string ValidateRocksOnBoulderRoute()
        {
            string errMsg = null;

            if (!IsBoulderRouteContainsSingleStartRock())
            {
                errMsg = "Please select one single start rock!";
                return errMsg;
            }

            if (!IsBoulderRouteContainsSingelEndRock())
            {
                errMsg = "Please select one single end rock!";
                return errMsg;
            }

            if (IsTooFewRocksOnRoute())
            {
                errMsg = "Too few rocks in the route!";
                return errMsg;
            }

            return errMsg;
        }

        private bool IsBoulderRouteContainsSingleStartRock()
        {
            return rocksOnRoute.Where(x => x.BoulderStatus == RockOnBoulderStatus.Start).Count() == 1;
        }

        private bool IsBoulderRouteContainsSingelEndRock()
        {
            return rocksOnRoute.Where(x => x.BoulderStatus == RockOnBoulderStatus.End).Count() == 1;
        }

        private bool IsRocksInTrainingRouteInSequence()
        {
            bool isInSeq = true;

            int i = 1;
            foreach (RockOnRouteViewModel rockOnRoute in rocksOnRoute)
            {
                if (rockOnRoute.TrainingSeq != i)
                {
                    isInSeq = false;
                    break;
                }
                i++;
            }

            return isInSeq;
        }

        private bool IsTooFewRocksOnRoute()
        {
            return rocksOnRoute.Count < 3;
        }

        #endregion


        #region factory

        public static RocksOnRouteViewModel CreateFromDatabase(ClimbMode aClimbMode,
            string routeId, Canvas aCanvas, CoordinateMapper coordinateMapper)
        {
            IList<RockOnRouteViewModel> rocksOnRouteVM;

            switch (aClimbMode)
            {
                case ClimbMode.Boulder:
                default:
                    rocksOnRouteVM = BoulderRouteAndRocksDataAccess.RocksByRouteId(routeId, aCanvas, coordinateMapper).ToList();                   
                    break;
                case ClimbMode.Training:
                    rocksOnRouteVM = TrainingRouteAndRocksDataAccess.OrderedRocksByRouteId(routeId, aCanvas, coordinateMapper).ToList();
                    break;
            }

            return new RocksOnRouteViewModel(aCanvas, aClimbMode, rocksOnRouteVM);
        }

        #endregion


        #region IEnumerable interface

        public IEnumerator<RockOnRouteViewModel> GetEnumerator()
        {
            return rocksOnRoute.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rocksOnRoute.GetEnumerator();
        }

        #endregion
    }
}
