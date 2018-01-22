using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JustClimbTrial.Helpers
{
    public static class PlaneHelper
    {

        //PlaneHelper(Vector3 p1, Vector3 p2, Vector3 p3)
        //{
        //    //Create 2 vectors by subtracting p3 from p1 and p2
        //    Vector3 v1 = p1 - p3;
        //    Vector3 v2 = p2 - p3;

        //    //Create cross product from the 2 vectors
        //    normVec = Vector3.Cross(v1, v2);

        //    //find d in the equation aX + bY + cZ = d
        //    d = Vector3.Dot(normVec, p3);
        //}

        public static double DistanceFromPoint(Plane plane, Vector3 v)
        {
            return (Vector3.Dot(plane.Normal, v) + plane.D) / plane.Normal.Length();
        }
      
    }
}
