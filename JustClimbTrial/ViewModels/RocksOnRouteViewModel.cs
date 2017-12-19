using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
                        selectedRockIndicator = GetNewSelectedRockIndicatorCircle();
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
        
        public void AddRockToRoute(RockOnRouteViewModel rockOnRouteVM)
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

        public void RemoveRockFromRoute(RockOnRouteViewModel rockOnRouteVM)
        {
            if (rockOnRouteVM != null)
            {
                rocksOnRoute.Remove(rockOnRouteVM);
                canvas.RemoveChild(rockOnRouteVM.MyRockViewModel.BoulderShape);
            }
        }

        public void RemoveSelectedRockFromRoute()
        {
            RemoveRockFromRoute(SelectedRockOnRoute);
            SelectedRockOnRoute = null;
        }

        #endregion


        #region manipulate selected rock

        public bool IsSelectedRockOnRouteNull()
        {
            return SelectedRockOnRoute == null;
        }

        public void SetSelectedTrainingRockSeqNo()
        {
            int seqNo = rocksOnRoute.Count + 1;

            if (!IsSelectedRockOnRouteNull())
            {
                // if SelectedRockOnRoute not already in rocksOnRoute,
                // add it into the rocksOnRoute list
                AddRockToRoute(SelectedRockOnRoute);

                if (SelectedRockOnRoute.MyRockViewModel.BoulderShape == null ||
                    SelectedRockOnRoute.TrainingSeq != seqNo)
                {
                    canvas.RemoveChild(SelectedRockOnRoute.MyRockViewModel.BoulderShape);
                    SelectedRockOnRoute.TrainingSeq = seqNo;
                    SelectedRockOnRoute.MyRockViewModel.DrawSequenceRockOnCanvas(seqNo);
                }
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
                    canvas.RemoveChild(SelectedRockOnRoute.MyRockViewModel.BoulderShape);
                    SelectedRockOnRoute.BoulderStatus = status;
                    SelectedRockOnRoute.MyRockViewModel.BoulderShape = DrawBoulderRockOnCanvas(SelectedRockOnRoute);                    
                }
            }            
        }

        #endregion


        #region draw helpers

        private TextBlock DrawTrainingRockOnCanvas(RockOnRouteViewModel rockTrainingRoute)
        {
            TextBlock textBlockToReturn = null;


            return textBlockToReturn;
        }

        private Shape DrawBoulderRockOnCanvas(RockOnRouteViewModel rockOnBoulderRoute)
        {
            Shape shapeToReturn;
            switch (rockOnBoulderRoute.BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    shapeToReturn = rockOnBoulderRoute.MyRockViewModel.DrawStartRockOnCanvas();
                    break;
                case RockOnBoulderStatus.End:
                    shapeToReturn = rockOnBoulderRoute.MyRockViewModel.DrawEndRockOnCanvas();
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    shapeToReturn = rockOnBoulderRoute.MyRockViewModel.DrawIntermediateRockOnCanvas();
                    break;
            }
            return shapeToReturn;
        }

        private static Ellipse GetNewSelectedRockIndicatorCircle()
        {
            double radius = 2;

            return new Ellipse
            {
                Fill = Brushes.Red,
                StrokeThickness = 4,
                Stroke = Brushes.Red,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        #endregion

        #region database

        public void SaveRocksOnBoulderRoute(BoulderRoute boulderRoute)
        {
            BoulderRouteAndRocksDataAccess.InsertRouteAndRocksOnRoute(
                boulderRoute, rocksOnRoute, true);
        }

        #endregion
    }
}
