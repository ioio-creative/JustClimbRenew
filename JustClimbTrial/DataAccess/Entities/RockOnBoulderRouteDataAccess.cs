using JustClimbTrial.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class RockOnBoulderRouteDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.RB;

        public static IEnumerable<RockOnBoulderRoute> RockOnBoulderRoutes
        {
            get
            {
                return database.RockOnBoulderRoutes;
            }
        }

        public static IEnumerable<RockOnBoulderRoute> ValidRockOnBoulderRoutes
        {
            get
            {
                return RockOnBoulderRoutes.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static RockOnBoulderRoute RockOnBoulderRouteById(string anId)
        {
            return RockOnBoulderRoutes.Where(x => x.RockOnBoulderID == anId).Single();
        }

        public static string Insert(RockOnBoulderRoute proposedRockOnBoulder, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedRockOnBoulder.IsDeleted = false;
            proposedRockOnBoulder.CreateDT = createDT;

            proposedRockOnBoulder.RockOnBoulderID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.RockOnBoulderRoutes.InsertOnSubmit(proposedRockOnBoulder);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedRockOnBoulder.RockOnBoulderID;
        }

        public static void InsertAll(ICollection<RockOnBoulderRoute> rocksOnBoulderRoute, 
            string boulderRouteId = null, bool isSubmitChanges = true)
        {
            if (rocksOnBoulderRoute.Any())
            {
                foreach (RockOnBoulderRoute rockOnBoulderRoute in rocksOnBoulderRoute)
                {
                    DateTime createDT = DateTime.Now;
                    rockOnBoulderRoute.IsDeleted = false;
                    rockOnBoulderRoute.CreateDT = createDT;

                    rockOnBoulderRoute.RockOnBoulderID =
                        KeyGenerator.GenerateNewKey(EntityType.RB, createDT);

                    if (!string.IsNullOrEmpty(boulderRouteId))
                    {
                        rockOnBoulderRoute.BoulderRoute = boulderRouteId;
                    }
                }

                database.RockOnBoulderRoutes.InsertAllOnSubmit(rocksOnBoulderRoute);

                if (isSubmitChanges)
                {
                    database.SubmitChanges();
                }
            }
        }

        public static void InsertAll(IEnumerable<RockOnRouteViewModel> rockOnRouteViewModels,
            string boulderRouteId, bool isSubmitChanges = true)
        {
            if (rockOnRouteViewModels.Any())
            {
                RockOnBoulderRoute[] rocksOnBoulderRoute =
                    rockOnRouteViewModels.Select(x => new RockOnBoulderRoute
                    {
                        BoulderRockRole = x.BoulderStatus.ToString(),
                        BoulderRoute = boulderRouteId,
                        Rock = x.MyRockViewModel.MyRock.RockID
                    }).ToArray();

                InsertAll(rocksOnBoulderRoute: rocksOnBoulderRoute, isSubmitChanges: isSubmitChanges);
            }            
        }        

        public static void SetIsDeletedToTrue(string anId, bool isSubmitChanges = true)
        {
            RockOnBoulderRoute rockOnBoulderToDelete = RockOnBoulderRouteById(anId);
            rockOnBoulderToDelete.IsDeleted = true;
            rockOnBoulderToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        public static void InsertAll(ICollection<RockOnRouteViewModel> rockOnRouteViewModels)
        {

        }
    }
}
