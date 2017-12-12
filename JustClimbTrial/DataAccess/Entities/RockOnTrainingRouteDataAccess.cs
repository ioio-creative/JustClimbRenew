using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class RockOnTrainingRouteDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.RT;

        public static IEnumerable<RockOnTrainingRoute> RockOnTrainingRoutes
        {
            get
            {
                return database.RockOnTrainingRoutes;
            }
        }

        public static IEnumerable<RockOnTrainingRoute> ValidRockOnTrainingRoutes
        {
            get
            {
                return RockOnTrainingRoutes.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static RockOnTrainingRoute RockOnTrainingRouteById(string anId)
        {
            return RockOnTrainingRoutes.Where(x => x.RockOnTrainingID == anId).Single();
        }

        public static string Insert(RockOnTrainingRoute proposedRockOnTraining, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedRockOnTraining.IsDeleted = false;
            proposedRockOnTraining.CreateDT = createDT;

            proposedRockOnTraining.RockOnTrainingID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.RockOnTrainingRoutes.InsertOnSubmit(proposedRockOnTraining);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedRockOnTraining.RockOnTrainingID;
        }

        public static void SetIsDeletedToTrue(string anId, bool isSubmitChanges = true)
        {
            RockOnTrainingRoute rockOnTrainingToDelete = RockOnTrainingRouteById(anId);
            rockOnTrainingToDelete.IsDeleted = true;
            rockOnTrainingToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }
    }
}
