using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class WallAndRocksDataAccess : DataAccessBase
    {
        public static string InsertWallAndRocks(Wall aWall, ICollection<Rock> someRocks, bool isSubmitChanges = true)
        {
            string newWallKey = WallDataAccess.Insert(aWall, false);

            if (someRocks.Any())
            {
                foreach (Rock rock in someRocks)
                {
                    rock.Wall = newWallKey;
                }

                RockDataAccess.InsertAll(someRocks);
            }

            // submit changes altogether
            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return newWallKey;
        }
    }
}
