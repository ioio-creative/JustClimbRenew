using JustClimbTrial.Enums;
using JustClimbTrial.ViewModels;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace JustClimbTrial.DataAccess.Entities
{
    public class BoulderRouteAndRocksDataAccess : DataAccessBase
    {
        public static IEnumerable<RockOnBoulderRoute> RocksOnBoulderRouteByRouteId(string routeId)
        {
            return database.RockOnBoulderRoutes.Where(x => x.BoulderRoute == routeId);
        }

        public static IEnumerable<RockOnRouteViewModel> RocksByRouteId(string routeId, Canvas canvas, CoordinateMapper coorMapper)
        {
            return from rockOnBoulderRoute in RocksOnBoulderRouteByRouteId(routeId)
                   join rock in RockDataAccess.Rocks
                   on rockOnBoulderRoute.Rock equals rock.RockID
                   select new RockOnRouteViewModel
                   {
                       BoulderStatus = (RockOnBoulderStatus)Enum.Parse(typeof(RockOnBoulderStatus), rockOnBoulderRoute.BoulderRockRole),
                       MyRockViewModel = new RockViewModel(rock, canvas, coorMapper)
                   };
        }

        public static string InsertRouteAndRocksOnRoute(
            BoulderRoute aRoute,
            ICollection<RockOnBoulderRoute> someRocksonBoulderRoute,
            bool isSubmitChanges = true)
        {
            string newRouteKey = BoulderRouteDataAccess.Insert(aRoute, false);

            if (someRocksonBoulderRoute.Any())
            {
                RockOnBoulderRouteDataAccess.InsertAll(someRocksonBoulderRoute,
                    newRouteKey, false);
            }

            // submit changes altogether
            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return newRouteKey;
        }

        public static string InsertRouteAndRocksOnRoute(
            BoulderRoute aRoute, 
            ICollection<RockOnRouteViewModel> someRocksonRoute, 
            bool isSubmitChanges = true)
        {
            string newRouteKey = BoulderRouteDataAccess.Insert(aRoute, false);

            if (someRocksonRoute.Any())
            {
                RockOnBoulderRouteDataAccess.InsertAll(someRocksonRoute,
                    newRouteKey, false);
            }

            // submit changes altogether
            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return newRouteKey;
        }        
    }
}
