using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.ViewModels
{
    public class RocksOnRouteViewModel
    {
        private IList<RockOnRouteViewModel> rocksOnRoute = new List<RockOnRouteViewModel>();
        private RockOnRouteViewModel selectedRockOnRoute;
        private Shape selectedRockIndicator;
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
                            selectedRockOnRoute.MyRockViewModel.BoulderShape);
                        canvas.DrawShape(selectedRockIndicator,
                            selectedRockOnRoute.MyRockViewModel.BCanvasPoint);
                    }
                }
            }
        }


        #region constructors

        public RocksOnRouteViewModel(Canvas aCanvas)
        {
            canvas = aCanvas;
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


        #region manipulate selected rock

        public bool IsSelectedRockOnRouteNull()
        {
            return SelectedRockOnRoute == null;
        }

        public void SetSelectedTrainingRockSeqNo()
        {
            int seqNo = rocksOnRoute.Count;

            if (!IsSelectedRockOnRouteNull())
            {
                // if SelectedRockOnRoute not already in rocksOnRoute,
                // add it into the rocksOnRoute list
                AddRockToRoute(SelectedRockOnRoute);

                if (SelectedRockOnRoute.MyRockViewModel.BoulderShape == null ||
                    SelectedRockOnRoute.TrainingSeq != seqNo)
                {
                    SelectedRockOnRoute.SetRockTrainingSeqAndDraw(seqNo);                    
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

                if (SelectedRockOnRoute.MyRockViewModel.BoulderShape == null ||
                    SelectedRockOnRoute.BoulderStatus != status)
                {
                    SelectedRockOnRoute.SetRockStatusAndDraw(status);             
                }
            }            
        }

        #endregion


        #region draw helpers

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
                RockOnRouteViewModel lastRockOnRouteVM = rocksOnRoute[rocksOnRoute.Count - 1];
                RockOnRouteViewModel secLastRockOnRouteVM = rocksOnRoute[rocksOnRoute.Count - 2];
                Line lineAttachedToLastTrainingRock = canvas.DrawLine(
                    secLastRockOnRouteVM.MyRockViewModel.BCanvasPoint, 
                    lastRockOnRouteVM.MyRockViewModel.BCanvasPoint,
                    8,
                    new SolidColorBrush(Colors.LightBlue));
                linesLinkingTrainingRocks.Add(lineAttachedToLastTrainingRock);
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
    }
}
