using JustClimbTrial.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustClimbTrial.DataAccess.Entities
{
    public class BoulderRouteAndRocksDataAccess : DataAccessBase
    {
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
