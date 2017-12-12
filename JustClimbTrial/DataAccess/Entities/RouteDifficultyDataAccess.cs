using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class RouteDifficultyDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.RD;

        public static IEnumerable<RouteDifficulty> RouteDifficulties
        {
            get
            {
                return database.RouteDifficulties;
            }
        }

        public static IEnumerable<RouteDifficulty> ValidRouteDifficulties
        {
            get
            {
                return RouteDifficulties.Where(x => x.IsValid.GetValueOrDefault(true));
            }
        }

        public static RouteDifficulty RouteDifficultyById(string difficultyId)
        {
            return RouteDifficulties.Where(x => x.RouteDifficultyID == difficultyId).Single();
        }

        public static string Insert(RouteDifficulty proposedDifficulty, bool isSubmitChanges = true)
        {
            proposedDifficulty.IsValid = true;

            proposedDifficulty.RouteDifficultyID = KeyGenerator.GenerateNewKey(myEntityType, DateTime.Now);
            
            database.RouteDifficulties.InsertOnSubmit(proposedDifficulty);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedDifficulty.RouteDifficultyID;
        }

        public static void SetIsValidToFalse(string difficultyId, bool isSubmitChanges = true)
        {
            RouteDifficulty difficultyToDelete = RouteDifficultyById(difficultyId);
            difficultyToDelete.IsValid = false;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }
    }
}
