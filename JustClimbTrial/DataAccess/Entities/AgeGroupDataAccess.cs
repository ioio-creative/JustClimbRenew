using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class AgeGroupDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.AG;

        public static IEnumerable<AgeGroup> AgeGroups
        {
            get
            {
                return database.AgeGroups;
            }
        }

        public static IEnumerable<AgeGroup> ValidAgeGroups
        {
            get
            {
                return AgeGroups.Where(x => x.IsValid.GetValueOrDefault(true));
            }
        }

        public static AgeGroup AgeGroupById(string ageGroupId)
        {
            return AgeGroups.Where(x => x.AgeGroupID == ageGroupId).Single();
        }

        public static string Insert(AgeGroup proposedAgeGroup, bool isSubmitChanges = true)
        {
            proposedAgeGroup.IsValid = true;

            proposedAgeGroup.AgeGroupID = KeyGenerator.GenerateNewKey(myEntityType, DateTime.Now);
            
            database.AgeGroups.InsertOnSubmit(proposedAgeGroup);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedAgeGroup.AgeGroupID;
        }

        public static void SetIsValidToFalse(string ageGroupId, bool isSubmitChanges = true)
        {
            AgeGroup ageGroupToDelete = AgeGroupById(ageGroupId);
            ageGroupToDelete.IsValid = false;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }
    }
}
