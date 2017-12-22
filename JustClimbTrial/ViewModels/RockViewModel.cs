using JustClimbTrial.DataAccess;
using JustClimbTrial.Extensions;
using JustClimbTrial.Kinect;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.ViewModels
{
    public class RockViewModel
    {
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


        // derived quantities
        // normalised
        private double smallX
        {
            get
            {
                return BPoint.X - MyRock.Width.GetValueOrDefault(0) * 0.5;
            }
        }

        private double largeX
        {
            get
            {
                return BPoint.X + MyRock.Width.GetValueOrDefault(0) * 0.5;
            }
        }

        private double smallY
        {
            get
            {
                return BPoint.Y - MyRock.Height.GetValueOrDefault(0) * 0.5;
            }
        }

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
            bPoint = canvas.GetActualPoint(bCanvasPoint);
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

            CameraSpacePoint csp = new CameraSpacePoint();
            csp.X = (float)aRock.CoorX.GetValueOrDefault(0);
            csp.Y = (float)aRock.CoorY.GetValueOrDefault(0);
            csp.Z = (float)aRock.CoorZ.GetValueOrDefault(0);

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
            BCanvas.Children.Remove(BoulderShape);
            BoulderShape = null;
        }

        private void SetBoulderTopLeftPositionOnCanvas()
        {
            double normedLeft = bPoint.X - MyRock.Width.GetValueOrDefault(0) * 0.5;
            double normedTop = bPoint.Y - MyRock.Height.GetValueOrDefault(0) * 0.5;

            Canvas.SetLeft(BoulderShape, BCanvas.GetActualLengthWrtWidth(normedLeft));
            Canvas.SetTop(BoulderShape, BCanvas.GetActualLengthWrtHeight(normedTop));
        }

        #endregion


        #region draw helpers

        public TextBlock DrawSequenceRockOnCanvas(int seqNo)
        {
            // https://www.codeproject.com/Questions/629557/write-text-onto-canvas-wpf
            TextBlock textBlock = new TextBlock();
            textBlock.Text = seqNo.ToString();
            textBlock.Foreground = Brushes.Blue;
            textBlock.FontSize = 36;
            textBlock.FontWeight = FontWeight.FromOpenTypeWeight(10);
            Canvas.SetLeft(textBlock, bCanvasPoint.X);
            Canvas.SetTop(textBlock, bCanvasPoint.Y);
            //textBlock.RenderTransform = new RotateTransform(90, 0, 0); // this line can rotate it but not in the axis i want
            textBlock.Margin = new Thickness(0, 0, 0, 0);
            BCanvas.Children.Add(textBlock);
            return textBlock;
        }

        public Shape ChangeRockShapeToStart()
        {

            BoulderShape = GetNewStartRockEllipse();
            //DrawEllipseOnCanvas(startRockCircle, bCanvasPoint);
            return BoulderShape;

            Ellipse startRockCircle = GetNewStartRockEllipse();
            BCanvas.DrawShape(startRockCircle, bCanvasPoint);
            BoulderShape = startRockCircle;
            return startRockCircle;

        }

        public Shape ChangeRockShapeToIntermediate()
        {           

            BoulderShape = GetNewIntermediateRockEllipse();
            //DrawEllipseOnCanvas(intermediateRockCircle, bCanvasPoint);
            return BoulderShape;
        }

        public Shape ChangeRockShapeToEnd()
        {
            BoulderShape = GetNewEndRockEllipse();
            //DrawEllipseOnCanvas(endRockCircle, bCanvasPoint);
            return BoulderShape;

            Ellipse intermediateRockCircle = GetNewIntermediateRockEllipse();
            BCanvas.DrawShape(intermediateRockCircle, bCanvasPoint);
            BoulderShape = intermediateRockCircle;
            return intermediateRockCircle;
        }

        public Shape ChangeRockShapeToEnd()
        {           
            Ellipse endRockCircle = GetNewEndRockEllipse();
            BCanvas.DrawShape(endRockCircle, bCanvasPoint);
            BoulderShape = endRockCircle;
            return endRockCircle;

        }

        #endregion


        #region ellipses

        private Ellipse GetNewRockOnWallEllipse()
        {
            Ellipse boulderEllipse = new Ellipse
            {
                Width = BCanvas.GetActualLengthWrtWidth(MyRock.Width.GetValueOrDefault(0)),
                Height = BCanvas.GetActualLengthWrtHeight(MyRock.Height.GetValueOrDefault(0)),
                Fill = null,
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.DarkRed)
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
                StrokeThickness = 5,
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
                StrokeThickness = 5,
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
                StrokeThickness = 5,
                Stroke = Brushes.Red
            };

            return boulderEllipse;
        }        

        #endregion
    }
}
