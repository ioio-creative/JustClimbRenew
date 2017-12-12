using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class BoulderRouteDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.BR;

        public static IEnumerable<BoulderRoute> BoulderRoutes
        {
            get
            {
                return database.BoulderRoutes;
            }
        }

        public static IEnumerable<BoulderRoute> ValidBoulderRoutes
        {
            get
            {
                return BoulderRoutes.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static IEnumerable<BoulderRoute> BoulderRoutesByWall(string wallId)
        {
            return BoulderRoutes.Where(x => x.Wall == wallId);
        }

        public static IEnumerable<BoulderRoute> ValidBoulderRoutesByWall(string wallId)
        {
            return ValidBoulderRoutes.Where(x => x.Wall == wallId);
        }

        public static BoulderRoute BoulderRouteById(string routeId)
        {
            return BoulderRoutes.Where(x => x.RouteID == routeId).Single();
        }

        public static string Insert(BoulderRoute proposedRoute, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedRoute.IsDeleted = false;
            proposedRoute.CreateDT = createDT;

            //Tuple<string, string> routeIdAndNo = KeyGenerator.GenerateNewKeyAndNo(myEntityType, createDT);
            //proposedRoute.RouteID = routeIdAndNo.Item1;
            //proposedRoute.RouteNo = routeIdAndNo.Item2;

            // routeNo set in the view, stored in the passed-in proposedRoute
            proposedRoute.RouteID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.BoulderRoutes.InsertOnSubmit(proposedRoute);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedRoute.RouteID;                            
        }

        public static void SetIsDeletedToTrue(string routeId, bool isSubmitChanges = true)
        {
            BoulderRoute routeToDelete = BoulderRouteById(routeId);
            routeToDelete.IsDeleted = true;
            routeToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        public static string BoulderRouteNoById(string routeId)
        {
            return BoulderRouteById(routeId).RouteNo;
        }

        public static int LargestBoulderRouteNo
        {
            get
            {
                return BoulderRoutes.Select(x => Convert.ToInt32(x.RouteNo)).Max();
            }
        }

        public static int LargestValidBoulderRouteNo
        {
            get
            {
                return ValidBoulderRoutes.Select(x => Convert.ToInt32(x.RouteNo)).Max();
            }
        }
    }
}
