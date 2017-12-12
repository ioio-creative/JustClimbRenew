using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class WallDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.WA;

        public static IEnumerable<Wall> Walls
        {
            get
            {
                return database.Walls;
            }
        }

        public static IEnumerable<Wall> ValidWalls
        {
            get
            {
                return Walls.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static Wall WallById(string wallId)
        {
            return Walls.Where(x => x.WallID == wallId).Single();
        }

        public static string Insert(Wall proposedWall, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedWall.IsDeleted = false;
            proposedWall.CreateDT = createDT;

            //Tuple<string, string> wallIdAndNo = KeyGenerator.GenerateNewKeyAndNo(myEntityType, createDT);
            //proposedWall.WallID = wallIdAndNo.Item1;
            //proposedWall.WallNo = wallIdAndNo.Item2;

            proposedWall.WallID = KeyGenerator.GenerateNewKey(myEntityType, createDT);

            database.Walls.InsertOnSubmit(proposedWall);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedWall.WallID;
        }

        public static void SetIsDeletedToTrue(string wallId, bool isSubmitChanges = true)
        {
            Wall wallToDelete = WallById(wallId);
            wallToDelete.IsDeleted = true;
            wallToDelete.DeleteDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        public static Wall NewestWall
        {
            get
            {
                return Walls.OrderBy(x => x.CreateDT).LastOrDefault();
            }
        }

        public static Wall NewestValidWall
        {
            get
            {
                return ValidWalls.OrderBy(x => x.CreateDT).LastOrDefault();
            }
        }

        public static int LargestWallNo
        {
            get
            {
                return Walls.Select(x => Convert.ToInt32(x.WallNo)).Max();
            }
        }

        public static int LargestValidWallNo
        {
            get
            {
                return ValidWalls.Select(x => Convert.ToInt32(x.WallNo)).Max();
            }
        }
    }
}
