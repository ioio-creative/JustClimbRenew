using JustClimbTrial.DataAccess;
using Microsoft.Kinect;
using System.Windows;

namespace JustClimbTrial.Extensions
{
    public static class RockExtension
    {
        public static Point GetPoint(this Rock rock)
        {
            return new Point(rock.CoorX.GetValueOrDefault(0), rock.CoorY.GetValueOrDefault(0));
        }

        public static CameraSpacePoint GetCameraSpacePoint(this Rock rock)
        {
            CameraSpacePoint csp = new CameraSpacePoint();
            csp.X = (float)rock.CoorX.GetValueOrDefault(0);
            csp.Y = (float)rock.CoorY.GetValueOrDefault(0);
            csp.Z = (float)rock.CoorZ.GetValueOrDefault(0);            
            return csp;
        }        
    }
}
