using JustClimbTrial.Enums;
using JustClimbTrial.ViewModels;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace JustClimbTrial.DataAccess.Entities
{
    public class TrainingRouteAndRocksDataAccess : DataAccessBase
    {
        public static IEnumerable<RockOnTrainingRoute> RocksOnTrainingRouteByRouteId(string routeId)
        {
            return database.RockOnTrainingRoutes.Where(x => x.TrainingRoute == routeId);
        }

        public static IEnumerable<RockOnRouteViewModel> RocksByRouteId(string routeId, Canvas canvas, CoordinateMapper coorMapper)
        {
            return from rockOnTrainingRoute in RocksOnTrainingRouteByRouteId(routeId)
                   join rock in RockDataAccess.Rocks
                   on rockOnTrainingRoute.Rock equals rock.RockID
                   select new RockOnRouteViewModel
                   {
                       // TODO: can I use -1 as default value?
                       TrainingSeq = rockOnTrainingRoute.TrainingRouteSeq.GetValueOrDefault(-1),
                       MyRockViewModel = new RockViewModel(rock, canvas, coorMapper)
                   };
        }

        public static IOrderedEnumerable<RockOnRouteViewModel> OrderedRocksByRouteId(string routeId, Canvas canvas, CoordinateMapper coorMapper)
        {
            return RocksByRouteId(routeId, canvas, coorMapper).OrderBy(x => x.TrainingSeq);
        }

        public static string InsertRouteAndRocksOnRoute(
            TrainingRoute aRoute,
            ICollection<RockOnTrainingRoute> someRocksOnTrainingRoute,
            bool isSubmitChanges = true)
        {
            string newRouteKey = null;

            if (someRocksOnTrainingRoute.Any())
            {
                newRouteKey = TrainingRouteDataAccess.Insert(aRoute, false);

                RockOnTrainingRouteDataAccess.InsertAll(someRocksOnTrainingRoute,
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
            TrainingRoute aRoute,
            ICollection<RockOnRouteViewModel> someRocksOnRoute,
            bool isSubmitChanges = true)
        {
            string newRouteKey = null;

            if (someRocksOnRoute.Any())
            {
                RockOnTrainingRoute[] rocksOnTrainingRoute =
                    someRocksOnRoute.Select(x => new RockOnTrainingRoute
                    {
                        TrainingRouteSeq = x.TrainingSeq,
                        Rock = x.MyRockViewModel.MyRock.RockID                     
                    }).ToArray();

                newRouteKey = InsertRouteAndRocksOnRoute(aRoute,
                    rocksOnTrainingRoute, isSubmitChanges);
            }

            return newRouteKey;
        }
    }
}
