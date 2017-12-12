using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class RockDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.RO;
        
        public static IEnumerable<Rock> Rocks
        {
            get
            {
                return database.Rocks;
            }
        }

        public static IEnumerable<Rock> ValidRocks
        {
            get
            {
                return Rocks.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static IEnumerable<Rock> RocksOnWall(string wallId)
        {
            return Rocks.Where(x => x.Wall == wallId);
        }

        public static IEnumerable<Rock> ValidRocksOnWall(string wallId)
        {
            return ValidRocks.Where(x => x.Wall == wallId);
        }

        public static Rock RockById(string rockId)
        {
            return Rocks.Where(x => x.RockID == rockId).Single();
        }

        public static string Insert(Rock proposedRock, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedRock.IsDeleted = false;
            proposedRock.CreateDT = createDT;

            proposedRock.RockID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.Rocks.InsertOnSubmit(proposedRock);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedRock.RockID;
        }

        // !!! Important !!!
        // If using IEnumerable as argument, 
        // it can accept WhereSelectListIterator, which seems to be used for lazy evaluation and it seems weird
        // Moreover, it seems when WhereSelectListIterator is passed in,
        // the properties of the Rock objects in the Iterator cannot be modified
        public static void InsertAll(ICollection<Rock> someRocks, string wallId = null, bool isSubmitChanges = true)
        {
            if (someRocks.Any())
            {
                foreach (Rock rock in someRocks)
                {
                    DateTime createDT = DateTime.Now;
                    rock.IsDeleted = false;
                    rock.CreateDT = createDT;

                    rock.RockID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

                    if (!string.IsNullOrEmpty(wallId))
                    {
                        rock.Wall = wallId;
                    }
                }

                database.Rocks.InsertAllOnSubmit(someRocks.ToList());

                if (isSubmitChanges)
                {
                    database.SubmitChanges();
                }
            }
        }

        public static void SetIsDeletedToTrue(string rockId, bool isSubmitChanges = true)
        {
            Rock rockToDelete = RockById(rockId);
            rockToDelete.IsDeleted = true;
            rockToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }
    }
}
