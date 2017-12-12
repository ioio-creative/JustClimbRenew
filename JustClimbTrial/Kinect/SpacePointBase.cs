using Microsoft.Kinect;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JustClimbTrial.Kinect
{
    public class SpacePointBase
    {
        private SpaceMode _pointType;

        public float X { get; set; }
        public float Y { get; set; }
        public bool IsValid = true;

        public SpacePointBase(float x, float y, SpaceMode mode)
        {
            X = x;
            Y = y;
            _pointType = mode;
            if (x == float.NegativeInfinity && y == float.NegativeInfinity)
            {
                IsValid = false;
            }
            else IsValid = true;
        }

        public SpacePointBase(ColorSpacePoint pt) : this(pt.X, pt.Y, SpaceMode.Color) { }

        public SpacePointBase(DepthSpacePoint pt) : this(pt.X, pt.Y, SpaceMode.Depth) { }


        private SpacePointBase ScaleTo(double width, double height, float mapMaxX, float mapMaxY)
        {
            float scaledX = X * (float)width / mapMaxX;
            if (scaledX < 0) scaledX = 0;
            else if (scaledX > width) scaledX = (float)width;

            float scaledY = Y * (float)height / mapMaxY;
            if (scaledY < 0) scaledY = 0;
            else if (scaledY > height) scaledY = (float)height;


            SpacePointBase scaledPt = new SpacePointBase(scaledX, scaledY, _pointType);            

            return scaledPt;
        }

        public SpacePointBase ScaleTo(double width, double height, Tuple<float, float> dimensions)
        {    
            return ScaleTo(width, height, dimensions.Item1, dimensions.Item2);
        }

        public SpacePointBase ScaleTo(double width, double height, SpaceMode mode)
        {
            Tuple<float, float> dimensions = KinectExtensions.FrameDimensions[mode];
            return ScaleTo(width, height, dimensions.Item1, dimensions.Item2);
        }

        public void DrawPoint(Canvas canvas)
        {
            if (this.IsValid)
            {
                // 1) Create a WPF ellipse.
                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(Colors.LightBlue)
                };

                // 2) Position the ellipse according to the joint's coordinates.
                if (X > 0 && Y > 0)
                {
                    Canvas.SetLeft(ellipse, X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, Y - ellipse.Height / 2);
                }


                // 3) Add the ellipse to the canvas.
                canvas.Children.Add(ellipse);
            }
            else return;
        }
    

        public static void DrawLine(Canvas canvas, SpacePointBase first, SpacePointBase second)
        {

            if (first.IsValid && second.IsValid)
            {
                Line line = new Line
                {
                    X1 = first.X,
                    Y1 = first.Y,
                    X2 = second.X,
                    Y2 = second.Y,
                    StrokeThickness = 8,
                    Stroke = new SolidColorBrush(Colors.LightBlue)
                };

                canvas.Children.Add(line);
            }
            else return;
        }
    }
}
