using JustClimbTrial.DataAccess;
using JustClimbTrial.Extensions;
using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JustClimbTrial.ViewModels
{
    public class RockViewModel
    {
        private const double DefaultBoulderShapeStrokeThickness = 5;


        #region properties

        // CoorX, CoorY, CoorZ are exact copies from CameraSpacePoint
        // Width and Height are normalised with respect to canvas [0, 1]
        public Rock MyRock { get; set; }

        //2D positions (x/y), normalized [0,1]
        private readonly Point bPoint;
        public Point BPoint
        {
            get
            {
                return bPoint;
            }            
        }

        //scaled point wrt canvas dimensions (x/y)
        private readonly Point bCanvasPoint;
        public Point BCanvasPoint
        {
            get
            {
                return BCanvas.GetActualPoint(BPoint);
            }            
        }
        
        public Shape BoulderShape { get; set; }

        public Canvas BCanvas { get; set; }

        private TextBlock TrainingRockSeqNoText;

        public Image BoulderImage;
        public ImageSequenceHelper BoulderButtonSequence;


        // derived quantities
        // normalised
        //Left Edge
        private double smallX
        {
            get
            {
                return BPoint.X - MyRock.Width.GetValueOrDefault(0) * 0.5;
            }
        }
        //Right Edge
        private double largeX
        {
            get
            {
                return BPoint.X + MyRock.Width.GetValueOrDefault(0) * 0.5;
            }
        }
        //Top Edge
        private double smallY
        {
            get
            {
                return BPoint.Y - MyRock.Height.GetValueOrDefault(0) * 0.5;
            }
        }
        //Bottom Edge
        private double largeY
        {
            get
            {
                return BPoint.Y + MyRock.Height.GetValueOrDefault(0) * 0.5;
            }
        }

        #endregion
        

        #region Constructors

        // used only within methods in this class
        // all arguments are parameters w.r.t. canvas, used of checking overlap
        private RockViewModel(Point pointOnCanvas, Size sizeOnCanvas, Canvas canvas)
        {
            MyRock = new Rock
            {
                Width = canvas.GetNormalisedLengthWrtWidth(sizeOnCanvas.Width),
                Height = canvas.GetNormalisedLengthWrtHeight(sizeOnCanvas.Height)
            };

            bCanvasPoint = pointOnCanvas;
            bPoint = canvas.GetNormalisedPoint(bCanvasPoint);
            BCanvas = canvas;
        }

        public RockViewModel(float xP, float yP, float zP,
            double widthOnCanvas, double heightOnCanvas, Canvas canvas, CoordinateMapper coorMap) :
            this(new CameraSpacePoint { X = xP, Y = yP, Z = zP },
                new Size(widthOnCanvas, heightOnCanvas), canvas, coorMap)
        { }

        public RockViewModel(DepthSpacePoint depPoint, ushort dep,
            double widthOnCanvas, double heightOnCanvas, Canvas canvas, CoordinateMapper coorMap) :
            this(coorMap.MapDepthPointToCameraSpace(depPoint, dep), new Size(widthOnCanvas, heightOnCanvas), canvas, coorMap)
        { }

        public RockViewModel(CameraSpacePoint camPoint, Size sizeOnCanvas,
            Canvas canvas, CoordinateMapper coorMap) :
            this(new Rock
            {
                CoorX = camPoint.X,
                CoorY = camPoint.Y,
                CoorZ = camPoint.Z,
                Width = canvas.GetNormalisedLengthWrtWidth(sizeOnCanvas.Width),
                Height = canvas.GetNormalisedLengthWrtHeight(sizeOnCanvas.Height)
            }, canvas, coorMap)
        { }        

        // coorMap = null case for use when loading rocks from database
        public RockViewModel(Rock aRock, Canvas canvas, CoordinateMapper coorMap = null)
        {
            MyRock = aRock;
            BCanvas = canvas;            
            BoulderShape = GetNewRockOnWallEllipse();
            
            // TODO: performance issue!!!
            //CreateRockImageSequence();

            CameraSpacePoint csp = aRock.GetCameraSpacePoint();

            if (coorMap != null)
            {
                bCanvasPoint = coorMap.MapCameraSpacePointToPointOnCanvas(csp, canvas, SpaceMode.Color);
                bPoint = canvas.GetNormalisedPoint(bCanvasPoint);
            }
        }

        #endregion


        public void ChangeBWidth(double widthOnCanvas)
        {
            MyRock.Width = BCanvas.GetNormalisedLengthWrtWidth(widthOnCanvas);            
            RedrawBoulder();            
        }

        public void ChangeBHeight(double heightOnCanvas)
        {
            MyRock.Height = BCanvas.GetNormalisedLengthWrtHeight(heightOnCanvas);            
            RedrawBoulder();            
        }
        
        // we assume the boulder is a rectangle to determine its area
        public bool IsCoincideWithCanvasPoint(Point canvasPoint)
        {
            Point normedCanvasPoint = BCanvas.GetNormalisedPoint(canvasPoint);            
            return (this.smallX < normedCanvasPoint.X && normedCanvasPoint.X < this.largeX)
                && (this.smallY < normedCanvasPoint.Y && normedCanvasPoint.Y < this.largeY);
        }

        // we assume the boulder is a rectangle
        public bool IsOverlapWithAnotherBoulder(RockViewModel anotherBoulder)
        {
            bool isWidthCoincide = !((anotherBoulder.largeX < this.smallX) || (this.largeX < anotherBoulder.smallX));
            bool isHeightCoincide = !((anotherBoulder.largeY < this.smallY) || (this.largeY < anotherBoulder.smallY));
            return isWidthCoincide && isHeightCoincide;
        }

        // we assume the boulder is a rectangle
        public bool IsOverlapWithAnotherBoulder(Point pointOnCanvas, Size sizeOnCanvas)
        {
            return IsOverlapWithAnotherBoulder(new RockViewModel(pointOnCanvas, sizeOnCanvas, BCanvas));                          
        }
        

        #region draw helpers

        public void DrawBoulder()
        {
            SetBoulderTopLeftPositionOnCanvas();
            BCanvas.AddChild(BoulderShape);      
        }

        // TODO: need to change name as the function just changes width & height
        private void RedrawBoulder()
        {
            BoulderShape.Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0));
            BoulderShape.Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0));
            SetBoulderTopLeftPositionOnCanvas();
        }

        public void UndrawBoulder()
        {
            BCanvas.RemoveChild(BoulderShape);
            BoulderShape = null;
        }

        private void SetBoulderTopLeftPositionOnCanvas()
        {
            double normedLeft = bPoint.X - MyRock.Width.GetValueOrDefault(0) * 0.5;
            double normedTop = bPoint.Y - MyRock.Height.GetValueOrDefault(0) * 0.5;

            BCanvas.SetLeftAndTop(BoulderShape, BCanvas.GetActualLengthWrtWidth(normedLeft), 
                BCanvas.GetActualLengthWrtHeight(normedTop));
        }

        public TextBlock DrawSequenceRockOnCanvas(int seqNo, bool mirrorTxt = false)
        {
            // https://www.codeproject.com/Questions/629557/write-text-onto-canvas-wpf
            TrainingRockSeqNoText = new TextBlock()
            {
                Text = seqNo.ToString(),
                Foreground = Brushes.Blue,
                FontSize = 28,
                FontWeight = FontWeight.FromOpenTypeWeight(10),
                Margin = new Thickness(0, 0, 0, 0)
            };
            if (mirrorTxt)
            {
                TrainingRockSeqNoText.RenderTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
            }
            BCanvas.SetLeftAndTop(TrainingRockSeqNoText, bCanvasPoint);            
            //TrainingRockSeqNoText.RenderTransform = new RotateTransform(90, 0, 0); // this line can rotate it but not in the axis i want
            BCanvas.AddChild(TrainingRockSeqNoText);
            return TrainingRockSeqNoText;
        }

        public void UndrawSequenceRockOnCanvas()
        {
            if (TrainingRockSeqNoText != null)
            {
                BCanvas.RemoveChild(TrainingRockSeqNoText);
                TrainingRockSeqNoText = null;
            }
        }

        #endregion


        #region rock shapes        

        public Shape ChangeRockShapeToStart()
        {
            return ChangeRockShape(GetNewStartRockEllipse);      
        }

        public Shape ChangeRockShapeToIntermediate()
        {                  
            return ChangeRockShape(GetNewIntermediateRockEllipse);
        }

        public Shape ChangeRockShapeToEnd()
        {
            return ChangeRockShape(GetNewEndRockEllipse);
        }
        
        public Shape ChangeRockShapeToDefault()
        {
            return ChangeRockShape(GetNewRockOnWallEllipse);
        }

        private Shape ChangeRockShape(Func<Shape> shapeFactory)
        {
            UndrawBoulder();
            BoulderShape = shapeFactory();
            DrawBoulder();           
            return BoulderShape;
        }
        
        #endregion


        #region ellipses

        // default for any rocks on wall
        private Ellipse GetNewRockOnWallEllipse()
        {
            Ellipse boulderEllipse = new Ellipse
            {
                Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)),
                Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)),
                Fill = Brushes.Transparent,
                StrokeThickness = DefaultBoulderShapeStrokeThickness,
                Stroke = Brushes.DarkRed
            };

            return boulderEllipse;
        }

        private Ellipse GetNewStartRockEllipse()
        {
            Ellipse boulderEllipse = new Ellipse
            {
                Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)),
                Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)),
                Fill = Brushes.Transparent,
                StrokeThickness = DefaultBoulderShapeStrokeThickness,
                Stroke = Brushes.Green
            };

            return boulderEllipse;
        }

        private Ellipse GetNewIntermediateRockEllipse()
        {
            Ellipse boulderEllipse = new Ellipse
            {
                Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)),
                Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)),
                Fill = Brushes.Transparent,
                StrokeThickness = DefaultBoulderShapeStrokeThickness,
                Stroke = Brushes.Yellow
            };

            return boulderEllipse;
        }

        private Ellipse GetNewEndRockEllipse()
        {
            Ellipse boulderEllipse = new Ellipse
            {
                Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)),
                Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)),
                Fill = Brushes.Transparent,
                StrokeThickness = DefaultBoulderShapeStrokeThickness,
                Stroke = Brushes.Red
            };

            return boulderEllipse;
        }

        #endregion

        #region Recolor Shape Stroke
        public void RecolorRockShape(Brush color)
        {
            BoulderShape.Stroke = color;
        }
        #endregion

        #region image sequence helpers

        public void CreateRockImageSequence()
        {
            double meanLength = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)) * 0.5 + BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)) * 0.5;

            BoulderImage = new Image
            {
                //png image dimension: 500 x 500
                //centre circle size: 300x300
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(FileHelper.BoulderButtonNormalImgSequenceDirectory(), "1_00007.png"))),
                Width = 500 / 300 * meanLength,
                Height = 500 / 300 * meanLength,
                Stretch = Stretch.Fill
            };
        }

        public void SetRockImage()
        {
            BCanvas.SetLeftAndTop(BoulderImage, bCanvasPoint.X - BoulderImage.Width * 0.5, bCanvasPoint.Y - BoulderImage.Height * 0.5);
            BCanvas.AddChild(BoulderImage);

            BoulderButtonSequence = new ImageSequenceHelper(BoulderImage, true);
            BoulderButtonSequence.Load();
        }

        public void CreateBoulderImageSequence()
        {
            double meanLength = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)) * 0.5 + BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)) * 0.5;

            BoulderImage = new Image
            {
                //png image dimension: 500 x 500
                //centre circle size: 300x300
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(FileHelper.BoulderButtonNormalImgSequenceDirectory(), "1_00007.png"))),
                Width = 500 / 300 * meanLength,
                Height = 500 / 300 * meanLength,
                Stretch = Stretch.Fill
            };

            BCanvas.SetLeftAndTop(BoulderImage, bCanvasPoint.X - BoulderImage.Width * 0.5, bCanvasPoint.Y - BoulderImage.Height * 0.5);
            BCanvas.AddChild(BoulderImage);

            BoulderButtonSequence = new ImageSequenceHelper(BoulderImage, true);
        }

        #endregion
    }
}
