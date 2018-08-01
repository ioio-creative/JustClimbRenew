using JustClimbTrial.Globals;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.Extensions
{
    public static class CanvasExtension
    {
        public static bool debug = AppGlobal.DEBUG;

        public static Point GetNormalisedPoint(this Canvas canvas, Point pt)
        {
            double normedX = GetNormalisedLengthWrtWidth(canvas, pt.X);
            double normedY = GetNormalisedLengthWrtHeight(canvas, pt.Y);
            return new Point(normedX, normedY);
        }

        public static double GetNormalisedLengthWrtWidth(this Canvas canvas, double length)
        {
            return length / canvas.ActualWidth;
        }

        public static double GetNormalisedLengthWrtHeight(this Canvas canvas, double length)
        {
            return length / canvas.ActualHeight;
        }

        public static Size GetNormalisedSize(this Canvas canvas, Size size)
        {
            double normedWidth = GetNormalisedLengthWrtWidth(canvas, size.Width);
            double normedHeight = GetNormalisedLengthWrtHeight(canvas, size.Height);
            return new Size(normedWidth, normedHeight);
        }

        public static Point GetActualPoint(this Canvas canvas, Point normPt)
        {            
            double actualX = GetActualLengthWrtWidth(canvas, normPt.X);
            double actualY = GetActualLengthWrtHeight(canvas, normPt.Y);
            return new Point(actualX, actualY);
        }

        public static double GetActualLengthWrtWidth(this Canvas canvas, double normLength)
        {
            return normLength * canvas.ActualWidth;
        }

        public static double GetActualLengthWrtHeight(this Canvas canvas, double normLength)
        {
            return normLength * canvas.ActualHeight;
        }

        public static Size GetActualSize(this Canvas canvas, Size normSize)
        {
            double actualWidth = GetActualLengthWrtWidth(canvas, normSize.Width);
            double actualHeight = GetActualLengthWrtHeight(canvas, normSize.Height);
            return new Size(actualWidth, actualHeight);
        }

        public static void AddChild(this Canvas canvas, UIElement uiElement)
        {
            canvas.Children.Add(uiElement);
        }

        public static void RemoveChild(this Canvas canvas, UIElement uiElement)
        {
            canvas.Children.Remove(uiElement);
        }

        public static void SetLeftAndTop(this Canvas canvas, UIElement uiElement, Point position)
        {
            canvas.SetLeftAndTop(uiElement, position.X, position.Y);
        }

        public static void SetLeftAndTop(this Canvas canvas, UIElement uiElement, double x, double y)
        {
            Canvas.SetLeft(uiElement, x);
            Canvas.SetTop(uiElement, y);
        }

        public static void SetLeftAndTopForShapeWrtCentre(this Canvas canvas, Shape shape, Point position)
        {
            canvas.SetLeftAndTopForShapeWrtCentre(shape, position.X, position.Y);
        }

        public static void SetLeftAndTopForShapeWrtCentre(this Canvas canvas, Shape shape, double x, double y)
        {
            canvas.SetLeftAndTop(shape, x - shape.Width * 0.5, y - shape.Height * 0.5);
        }

        public static void DrawShape(this Canvas canvas, Shape shape, Point position)
        {
            DrawShape(canvas, shape, position.X, position.Y);
        }

        public static void DrawShape(this Canvas canvas, Shape shape, double x, double y)
        {
            canvas.SetLeftAndTopForShapeWrtCentre(shape, x, y);
            canvas.AddChild(shape);
        }

        public static Line DrawLine(this Canvas canvas, Point pt1, Point pt2, double thickness, Brush aBrush)
        {
            Line line = new Line
            {
                X1 = pt1.X,
                Y1 = pt1.Y,
                X2 = pt2.X,
                Y2 = pt2.Y,
                StrokeThickness = thickness,
                Stroke = aBrush
            };

            canvas.AddChild(line);
            return line;
        }
    }
}
