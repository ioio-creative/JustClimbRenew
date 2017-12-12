using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace JustClimbTrial.Extensions
{
    public static class CanvasExtension
    {
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

        public static Point GetActualPoint(this Canvas canvas, Point NormPt)
        {
            double actualX = GetActualLengthWrtWidth(canvas, NormPt.X);
            double actualY = GetActualLengthWrtHeight(canvas, NormPt.Y);
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

        public static void AddChild(this Canvas canvas, UIElement uiElement)
        {
            canvas.Children.Add(uiElement);
        }

        public static void RemoveChild(this Canvas canvas, UIElement uiElement)
        {
            canvas.Children.Remove(uiElement);
        }

        public static void DrawShape(this Canvas canvas, Shape shape, Point position)
        {
            DrawShape(canvas, shape, position.X, position.Y);
        }

        public static void DrawShape(this Canvas canvas, Shape shape, double x, double y)
        {
            Canvas.SetLeft(shape, x - shape.Width * 0.5);
            Canvas.SetTop(shape, y - shape.Height * 0.5);

            canvas.Children.Add(shape);
        }
    }
}
