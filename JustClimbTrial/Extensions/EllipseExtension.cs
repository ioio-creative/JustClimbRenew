using System;
using System.Windows.Shapes;

namespace JustClimbTrial.Extensions
{
    public static class EllipseExtension
    {
        public static double SemiMajorAxis(this Ellipse ellipse)
        {
            return Math.Max(ellipse.Height, ellipse.Width) * 0.5;
        }

        public static double SemiMinorAxis(this Ellipse ellipse)
        {
            return Math.Min(ellipse.Height, ellipse.Width) * 0.5;
        }
    }
}
