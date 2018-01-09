using JustClimbTrial.Enums;
using JustClimbTrial.Helpers;
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
                       MyRockViewModel = new RockViewModel(rock, canvas, coorMapper),
                       MyRockTimerHelper = new RockTimerHelper()
                   };
        }

        public static string InsertRouteAndRocksOnRoute(
            BoulderRoute aRoute,
            ICollection<RockOnBoulderRoute> someRocksOnBoulderRoute,
            bool isSubmitChanges = true)
        {
            string newRouteKey = null;

            if (someRocksOnBoulderRoute.Any())
            {
                newRouteKey = BoulderRouteDataAccess.Insert(aRoute, false);

                RockOnBoulderRouteDataAccess.InsertAll(someRocksOnBoulderRoute,
                    newRouteKey, false);

                // submit changes altogether
                if (isSubmitChanges)
                {
                    database.SubmitChanges();
                }  
            }

            return newRouteKey;
        }

        public static string InsertRouteAndRocksOnRoute(
            BoulderRoute aRoute, 
            ICollection<RockOnRouteViewModel> someRocksOnRoute, 
            bool isSubmitChanges = true)
        {
            string newRouteKey = null;

            if (someRocksOnRoute.Any())
            {
                RockOnBoulderRoute[] rocksOnBoulderRoute =
                    someRocksOnRoute.Select(x => new RockOnBoulderRoute
                    {
                        BoulderRockRole = x.BoulderStatus.ToString(),
                        Rock = x.MyRockViewModel.MyRock.RockID
                    }).ToArray();

                newRouteKey = InsertRouteAndRocksOnRoute(aRoute, 
                    rocksOnBoulderRoute, isSubmitChanges);
            }
            
            return newRouteKey;
        }        
    }
}
