﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class TrainingRouteDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.TR;

        public static IEnumerable<TrainingRoute> TrainingRoutes
        {
            get
            {
                return database.TrainingRoutes;
            }
        }

        public static IEnumerable<TrainingRoute> ValidTrainingRoutes
        {
            get
            {
                return TrainingRoutes.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static IEnumerable<TrainingRoute> TrainingRoutesByWall(string wallId)
        {
            return TrainingRoutes.Where(x => x.Wall == wallId);
        }

        public static IEnumerable<TrainingRoute> ValidTrainingRoutesByWall(string wallId)
        {
            return ValidTrainingRoutes.Where(x => x.Wall == wallId);
        }

        public static TrainingRoute TrainingRouteById(string routeId)
        {
            return TrainingRoutes.Where(x => x.RouteID == routeId).Single();
        }

        public static string Insert(TrainingRoute proposedRoute, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedRoute.IsDeleted = false;
            proposedRoute.CreateDT = createDT;

            //Tuple<string, string> routeIdAndNo = KeyGenerator.GenerateNewKeyAndNo(myEntityType, createDT);
            //proposedRoute.RouteID = routeIdAndNo.Item1;
            //proposedRoute.RouteNo = routeIdAndNo.Item2;

            // routeNo set in the view, stored in the passed-in proposedRoute
            proposedRoute.RouteID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.TrainingRoutes.InsertOnSubmit(proposedRoute);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedRoute.RouteID;
        }

        public static void SetIsDeletedToTrue(string routeId, bool isSubmitChanges = true)
        {
            TrainingRoute routeToDelete = TrainingRouteById(routeId);
            routeToDelete.IsDeleted = true;
            routeToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        public static string TrainingRouteNoById(string routeId)
        {
            return TrainingRouteById(routeId).RouteNo;
        }  

        public static int LargestTrainingRouteNoByWall(string wallId)
        {
            IEnumerable<TrainingRoute> trainingRoutesOfTheWall = TrainingRoutes.Where(x => x.Wall == wallId);
            if (trainingRoutesOfTheWall.Any())
            {
                return trainingRoutesOfTheWall.Select(x => int.Parse(x.RouteNo)).Max();
            }
            else
            {
                return 0;
            }
        }
    }
}
