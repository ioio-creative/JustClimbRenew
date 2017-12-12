using JustClimbTrial.DataAccess;
using JustClimbTrial.DataAccess.Entities;
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
    public class RocksOnWallViewModel
    {
        #region private members

        private IList<RockViewModel> rocksOnWall = new List<RockViewModel>();       
        private Canvas canvas;
        private CoordinateMapper coorMap;
        private RockViewModel selectedRock;        
        private Shape selectedRockIndicator;

        #endregion


        #region public members

        public RockViewModel SelectedRock
        {
            get { return selectedRock; }
            set
            {
                if (selectedRock != value)
                {
                    selectedRock = value;

                    // remove old selected rock indicator
                    if (selectedRockIndicator != null)
                    {
                        canvas.RemoveChild(selectedRockIndicator);
                    }

                    if (selectedRock != null)
                    {
                        // draw selected rock indicator
                        selectedRockIndicator = GetNewSelectedRockIndicator(selectedRock.BoulderShape);
                        canvas.DrawShape(selectedRockIndicator, selectedRock.BCanvasPoint);
                    }
                }
            }
        }

        #endregion


        #region constructors

        public RocksOnWallViewModel(Canvas aCanvas, CoordinateMapper aCoorMap)
        {
            canvas = aCanvas;
            coorMap = aCoorMap;
        }

        #endregion


        #region add / remove rock

        public RockViewModel AddRock(CameraSpacePoint camSpacePt, 
            Size rockSizeOnCanvas)
        {
            SelectedRock = new RockViewModel(camSpacePt, rockSizeOnCanvas,
                canvas, coorMap);
            rocksOnWall.Add(SelectedRock);
            SelectedRock.DrawBoulder();
            return SelectedRock;
        }

        public void RemoveRock(Point canvasPt)
        {
            RockViewModel rockToBeRemoved = GetRockInListByCanvasPoint(canvasPt);
            RemoveRock(rockToBeRemoved);
        }

        public void RemoveRock(RockViewModel rock)
        {
            rocksOnWall.Remove(rock);
            rock.UndrawBoulder();
        }

        public void RemoveAllRocks()
        {
            DeselectRock();

            if (AnyRocksInList())
            {
                foreach (RockViewModel rock in rocksOnWall)
                {
                    rock.UndrawBoulder();
                }

                rocksOnWall.Clear();
            }
        }

        #endregion


        #region check if rock is in list

        public bool AnyRocksInList()
        {
            return rocksOnWall.Any();
        }

        public bool IsRockInListByCanvasPoint(Point canvasPt)
        {
            return GetRockInListByCanvasPoint(canvasPt) != null;
        }

        public RockViewModel GetRockInListByCanvasPoint(Point canvasPt)
        {
            if (!rocksOnWall.Any())
            {
                return null;
            }
            
            foreach (RockViewModel rockOnWall in rocksOnWall)
            {
                if (rockOnWall.IsCoincideWithCanvasPoint(canvasPt))                
                {
                    return rockOnWall;
                }
            }

            return null;
        }

        public bool IsOverlapWithRocksOnWall(
            Point ptOnCanvas, Size sizeOnCanvas)
        {
            return IsOverlapWithRocksOnWallOtherThanSomePredicate(
                ptOnCanvas, sizeOnCanvas,
                (rock) => false);  // false means skipping no rocks in list during checking for overlap
        }

        public bool IsOverlapWithRocksOnWallOtherThanSelectedRock(
            Point ptOnCanvas, Size sizeOnCanvas)            
        {
            return IsOverlapWithRocksOnWallOtherThanSomePredicate(
                ptOnCanvas, sizeOnCanvas,
                (rock) => rock.Equals(SelectedRock));
        }

        public bool IsOverlapWithRocksOnWallOtherThanSomePredicate(
            Point ptOnCanvas, Size sizeOnCanvas,
            Predicate<RockViewModel> rockToSkipCheckingPredicate)
        {
            if (!AnyRocksInList())
            {
                return false;
            }

            bool doOverlapsExist = false;
            foreach (RockViewModel rockOnWall in rocksOnWall)
            {
                if (rockToSkipCheckingPredicate(rockOnWall))
                {
                    continue;
                }

                if (rockOnWall.IsOverlapWithAnotherBoulder(
                    ptOnCanvas, sizeOnCanvas))
                {
                    doOverlapsExist = true;
                    break;
                }
            }
            return doOverlapsExist;
        }        

        #endregion


        #region manipulate selected rock

        public void DeselectRock()
        {
            if (SelectedRock != null)
            {
                SelectedRock = null;  // this causes selectedRockIndicator to be removed from the canvas in the setter 
            }
        }

        public void RemoveSelectedRock()
        {
            if (SelectedRock != null)
            {
                RemoveRock(SelectedRock);
                DeselectRock();
            }
        }

        public void ChangeWidthOfSelectedRock(double newWidth)
        {
            if (SelectedRock != null)
            {
                SelectedRock.ChangeBWidth(newWidth);
            }
        }

        public void ChangeHeightOfSelectedRock(double newHeight)
        {
            if (SelectedRock != null)
            {
                SelectedRock.ChangeBHeight(newHeight);
            }
        }

        #endregion


        #region draw helpers

        private static Shape GetNewSelectedRockIndicator(Shape selectedRock)
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

        public void SaveRocksOnWall(string newWallNo)
        {
            if (rocksOnWall.Any())
            {
                // convert IList<ViewModels.RockViewModel> to ICollection<DataAccess.Rock>
                ICollection<Rock> rocksToSave = rocksOnWall.Select(
                    rockModel => rockModel.MyRock).ToArray();
                
                Wall newWall = new Wall
                {
                    WallNo = newWallNo,
                    WallDesc = ""                 
                };

                WallAndRocksDataAccess.InsertWallAndRocks(newWall, rocksToSave, true);
            }
        }

        private bool LoadRocksOnWall(string wallId)
        {
            rocksOnWall = RockDataAccess.ValidRocksOnWall(wallId).Select(rock =>
                new RockViewModel(rock, canvas)).ToList();
            return rocksOnWall.Any();
        }

        public bool LoadAndDrawRocksOnWall(string wallId)
        {
            bool isAnyRocks = LoadRocksOnWall(wallId);

            if (isAnyRocks)
            {
                foreach (RockViewModel rockVM in rocksOnWall)
                {
                    rockVM.DrawBoulder();
                }
            }

            return isAnyRocks;
        }

        #endregion
    }
}
