using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
using JustClimbTrial.Enums;
using JustClimbTrial.Extensions;
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


        #region circles

        private static Ellipse GetNewRockOnWallCircle(double radius)
        {
            SolidColorBrush mySolidColorBrush = new SolidColorBrush();
            mySolidColorBrush.Color = Color.FromArgb(123, 255, 255, 0);
            return new Ellipse
            {
                Fill = mySolidColorBrush,
                StrokeThickness = 2,
                Stroke = Brushes.Black,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        private static Ellipse GetNewStartRockCircle(double radius)
        {
            return new Ellipse
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 4,
                Stroke = Brushes.Green,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        private static Ellipse GetNewIntermediateRockCircle(double radius)
        {
            return new Ellipse
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 4,
                Stroke = Brushes.Yellow,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        private static Ellipse GetNewEndRockCircle(double radius)
        {
            return new Ellipse
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 4,
                Stroke = Brushes.Red,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        private static Ellipse GetNewSelectedRockIndicatorCircle()
        {
            double radius = 2;

            return new Ellipse
            {
                Fill = Brushes.Red,
                StrokeThickness = 0,
                Stroke = Brushes.Red,
                Width = radius * 2,
                Height = radius * 2
            };
        }

        #endregion


        #region draw helpers

        private Shape DrawBoulderRockOnCanvas(RockOnRouteViewModel rockOnBoulderRoute)
        {
            Shape shapeToReturn;
            switch (rockOnBoulderRoute.BoulderStatus)
            {
                case RockOnBoulderStatus.Start:
                    shapeToReturn = DrawStartRockOnCanvas(rockOnBoulderRoute.MyRockViewModel.MyRock);
                    break;
                case RockOnBoulderStatus.End:
                    shapeToReturn = DrawEndRockOnCanvas(rockOnBoulderRoute.MyRockViewModel.MyRock);
                    break;
                case RockOnBoulderStatus.Int:
                default:
                    shapeToReturn = DrawIntermediateRockOnCanvas(rockOnBoulderRoute.MyRockViewModel.MyRock);
                    break;
            }
            return shapeToReturn;
        }

        private Shape DrawStartRockOnCanvas(Rock rock)
        {
            // TODO: change draw ellipse logic
            double radius = Math.Max(rock.Width.GetValueOrDefault(0), rock.Height.GetValueOrDefault(0));
            Ellipse startRockCircle = GetNewStartRockCircle(radius);
            DrawCircleOnCanvas(startRockCircle, rock.CoorX.GetValueOrDefault(0), rock.CoorY.GetValueOrDefault(0));
            return startRockCircle;
        }

        private Shape DrawIntermediateRockOnCanvas(Rock rock)
        {
            // TODO: change draw ellipse logic
            double radius = Math.Max(rock.Width.GetValueOrDefault(0), rock.Height.GetValueOrDefault(0));
            Ellipse intermediateRockCircle = GetNewIntermediateRockCircle(radius);
            DrawCircleOnCanvas(intermediateRockCircle, rock.CoorX.GetValueOrDefault(0), rock.CoorY.GetValueOrDefault(0));
            return intermediateRockCircle;
        }

        private Shape DrawEndRockOnCanvas(Rock rock)
        {
            // TODO: change draw ellipse logic
            double radius = Math.Max(rock.Width.GetValueOrDefault(0), rock.Height.GetValueOrDefault(0));
            Ellipse endRockCircle = GetNewEndRockCircle(radius);
            DrawCircleOnCanvas(endRockCircle, rock.CoorX.GetValueOrDefault(0), rock.CoorY.GetValueOrDefault(0));
            return endRockCircle;
        }

        private void DrawCircleOnCanvas(Ellipse circle, Point position)
        {
            DrawCircleOnCanvas(circle, position.X, position.Y);
        }

        private void DrawCircleOnCanvas(Ellipse circle, double x, double y)
        {
            double radius = circle.SemiMajorAxis();

            Canvas.SetLeft(circle, x - radius);
            Canvas.SetTop(circle, y - radius);

            canvas.Children.Add(circle);
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
